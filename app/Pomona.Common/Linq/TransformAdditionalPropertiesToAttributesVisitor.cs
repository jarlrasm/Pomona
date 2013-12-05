﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Pomona.Common.Internals;
using Pomona.Internals;

namespace Pomona.Common.Linq
{
    public class ExpressionTypeVisitor : ExpressionVisitor
    {
        private readonly IDictionary<ParameterExpression, ParameterExpression> replacementParameters =
            new Dictionary<ParameterExpression, ParameterExpression>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var serverType = VisitType(node.Type);
            if (serverType != node.Type)
            {
                return replacementParameters.GetOrCreate(node, () => Expression.Parameter(serverType, node.Name));
            }
            return base.VisitParameter(node);
        }

        protected virtual Type VisitType(Type typeToSearch)
        {
            if (typeToSearch.IsGenericType)
            {
                var genArgs = typeToSearch.GetGenericArguments();
                var newGenArgs =
                    genArgs.Select(x => VisitType(typeToSearch)).ToArray();

                if (newGenArgs.SequenceEqual(genArgs))
                    return typeToSearch;

                return typeToSearch.GetGenericTypeDefinition().MakeGenericType(newGenArgs);
            }

            return typeToSearch;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var replacementMethod = VisitMethod(node.Method);
            if (replacementMethod != node.Method)
            {
                var visitedArguments = Visit(node.Arguments);
                if (node.Object != null)
                    return Expression.Call(node.Object, replacementMethod, visitedArguments);
                return Expression.Call(replacementMethod, visitedArguments);
            }
            return base.VisitMethodCall(node);
        }

        protected virtual MethodInfo VisitMethod(MethodInfo methodToSearch)
        {
            var newReflectedType = VisitType(methodToSearch.ReflectedType);
            if (newReflectedType != methodToSearch.ReflectedType)
            {
                methodToSearch = newReflectedType.GetMethod(methodToSearch.Name,
                    (methodToSearch.IsStatic ? BindingFlags.Static : BindingFlags.Instance)
                    | (methodToSearch.IsPublic
                        ? BindingFlags.Public
                        : BindingFlags.NonPublic),
                    null,
                    methodToSearch.GetParameters().Select(x => x.ParameterType).ToArray(),
                    null);
            }

            if (!methodToSearch.IsGenericMethod)
                return methodToSearch;

            var genArgs = methodToSearch.GetGenericArguments();
            var newGenArgs = genArgs.Select(VisitType).ToArray();
            if (genArgs.SequenceEqual(newGenArgs))
                return methodToSearch;

            return methodToSearch.GetGenericMethodDefinition().MakeGenericMethod(newGenArgs);
        }
    }
    public class TransformAdditionalPropertiesToAttributesVisitor : ExpressionTypeVisitor
    {
        private static readonly MethodInfo dictionarySafeGetMethod;
        private readonly IPomonaClient client;

        private readonly IDictionary<ParameterExpression, ParameterExpression> replacementParameters =
            new Dictionary<ParameterExpression, ParameterExpression>();

        static TransformAdditionalPropertiesToAttributesVisitor()
        {
            dictionarySafeGetMethod =
                ReflectionHelper.GetMethodDefinition<IDictionary<string, string>>(x => x.SafeGet(null));
        }


        public TransformAdditionalPropertiesToAttributesVisitor(IPomonaClient client)
        {
            this.client = client;
        }

        private bool IsUserType(Type userType)
        {
            CustomUserTypeInfo tmpvar;
            return IsUserType(userType, out tmpvar);
        }

        private bool TryReplaceWithServerType(Type userType, out Type serverType)
        {
            serverType = ReplaceInGenericArguments(userType);
            return userType != serverType;
        }

        private bool IsUserType(Type userType, out CustomUserTypeInfo userTypeInfo)
        {
            return CustomUserTypeInfo.TryGetCustomUserTypeInfo(userType, client, out userTypeInfo);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            CustomUserTypeInfo userTypeInfo;
            if (node.Type.UniqueToken() == typeof (RestQuery<>).UniqueToken() &&
                IsUserType(node.Type.GetGenericArguments()[0], out userTypeInfo))
            {
                var queryable = node.Value as IQueryable;
                if (queryable != null)
                {
                    var provider = (RestQueryProvider)queryable.Provider;
                    var restQueryOfTargetType = typeof (RestQuery<>).MakeGenericType(userTypeInfo.ServerType);
                    var modifiedSourceQueryable = Activator.CreateInstance(
                        restQueryOfTargetType,
                        new RestQueryProvider(provider.Client, userTypeInfo.ServerType, provider.Uri));
                    return Expression.Constant(modifiedSourceQueryable, restQueryOfTargetType);
                }
            }
            return base.VisitConstant(node);
        }


        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var visitedBody = Visit(node.Body);
            var visitedParams = node.Parameters.Select(Visit).Cast<ParameterExpression>();
            var visitedNode = Expression.Lambda(visitedBody, node.Name, node.TailCall, visitedParams);

            return visitedNode;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var member = node.Member;
            CustomUserTypeInfo declaringUserTypeInfo;
            var propInfo = member as PropertyInfo;
            var visitedExpression = Visit(node.Expression);

            if (IsUserType(member.DeclaringType, out declaringUserTypeInfo))
            {
                if (propInfo == null)
                    throw new InvalidOperationException(
                        "Only properties can be defined on custom user types, not methods or fields.");

                Type memberServerType;
                if (TryReplaceWithServerType(propInfo.PropertyType, out memberServerType) &&
                    propInfo.GetIndexParameters().Length == 0)
                {
                    var serverProp =
                        declaringUserTypeInfo.ServerType.GetAllInheritedPropertiesFromInterface()
                                             .FirstOrDefault(x => x.Name == propInfo.Name);
                    if (serverProp == null)
                        throw new InvalidOperationException("Unable to find underlying server side property " +
                                                            propInfo.Name);

                    //if (!serverProp.PropertyType.IsAssignableFrom(propInfo.PropertyType))
                    //    throw new InvalidOperationException("Unable to convert from type " + propInfo.PropertyType +
                    //                                        " to " + serverProp.PropertyType);

                    return Expression.Property(visitedExpression, serverProp);
                }
                else
                {
                    Type targetDictInterface;
                    var targetDictProperty = declaringUserTypeInfo.DictProperty;
                    var idictionaryMetadataToken = typeof (IDictionary<,>).UniqueToken();
                    if (targetDictProperty.PropertyType.UniqueToken() == idictionaryMetadataToken)
                        targetDictInterface = targetDictProperty.PropertyType;
                    else
                    {
                        targetDictInterface = targetDictProperty
                            .PropertyType
                            .GetInterfaces()
                            .FirstOrDefault(x => x.UniqueToken() == idictionaryMetadataToken);

                        if (targetDictInterface == null)
                        {
                            throw new InvalidOperationException(
                                "Unable to find IDictionary interface in type "
                                + targetDictProperty.PropertyType.FullName);
                        }
                    }

                    Expression attrAccessExpression =
                        Expression.Call(
                            dictionarySafeGetMethod.MakeGenericMethod(targetDictInterface.GetGenericArguments()),
                            Expression.Property(visitedExpression, targetDictProperty),
                            Expression.Constant(propInfo.Name));

                    if (attrAccessExpression.Type != propInfo.PropertyType)
                        attrAccessExpression = Expression.TypeAs(attrAccessExpression, propInfo.PropertyType);

                    return attrAccessExpression;
                }

                //return Expression.Call(Expression.Property(Visit(node.Expression), targetDictProperty), OdataFunctionMapping.DictGetMethod,
                //                       Expression.Constant(propInfo.Name));
            }


            var originalDeclaringType = node.Member.DeclaringType;
            var modifiedDeclaringType = ReplaceInGenericArguments(originalDeclaringType);
            if (modifiedDeclaringType != originalDeclaringType)
            {
                var modifiedMember =
                    modifiedDeclaringType.GetMember(node.Member.Name, node.Member.MemberType,
                                                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                                                    BindingFlags.Public)
                                         .First(x => x.UniqueToken() == node.Member.UniqueToken());
                return Expression.MakeMemberAccess(node.Expression != null ? visitedExpression : null,
                                                   modifiedMember);
            }

            return base.VisitMember(node);
        }


        public override Expression Visit(Expression node)
        {
            var visitedNode = base.Visit(node);

#if false
    // For debugging:
            if (System.Diagnostics.Debugger.IsAttached)
            {
                if (visitedNode.Type.WrapAsEnumerable()
                               .WalkTree(x => x.Count() > 0
                                                  ? x.SelectMany(y => y.IsGenericType
                                                                          ? y.GetGenericArguments()
                                                                          : new Type[] { })
                                                  : null).SelectMany(x => x).Where(x => x.IsNested).Any(IsUserType))
                {
                    base.Visit(node);
                    throw new InvalidOperationException("Is user type!");
                }

            }
#endif
            return visitedNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var modifiedMethod = ReplaceInGenericMethod(node.Method);
            var modifiedArguments = node.Arguments.Select(Visit);
            var argTypes = modifiedArguments.Select(x => x.Type).ToList();

            var blah =
                ((IEnumerable<Type>)argTypes).WalkTree(
                    x =>
                    x.Any() ? x.SelectMany(y => y.IsGenericType ? y.GetGenericArguments() : new Type[] { }) : null)
                                             .SelectMany(x => x)
                                             .Where(x => x.IsNested)
                                             .ToList();

            return Expression.Call(
                node.Object != null ? Visit(node.Object) : null,
                modifiedMethod,
                modifiedArguments);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var serverType = ReplaceInGenericArguments(node.Type);
            if (serverType != node.Type)
            {
                return replacementParameters.GetOrCreate(node,
                                                         () => Expression.Parameter(serverType, node.Name));
            }
            return base.VisitParameter(node);
        }


        private Type ReplaceInGenericArguments(Type typeToSearch)
        {
            return ReplaceInGenericArguments(typeToSearch,
                t =>
                {
                    CustomUserTypeInfo userTypeInfo;
                    if (IsUserType(t, out userTypeInfo))
                        return userTypeInfo.ServerType;

                    return t;
                });
        }

        private Type ReplaceInGenericArguments(Type typeToSearch, Func<Type, Type> typeReplacer)
        {
            typeToSearch = typeReplacer(typeToSearch);

            if (typeToSearch.IsGenericType)
            {
                var genArgs = typeToSearch.GetGenericArguments();
                var newGenArgs =
                    genArgs.Select(x => ReplaceInGenericArguments(x, typeReplacer)).ToArray();

                if (newGenArgs.SequenceEqual(genArgs))
                    return typeToSearch;

                return typeToSearch.GetGenericTypeDefinition().MakeGenericType(newGenArgs);
            }

            return typeToSearch;
        }


        private MethodInfo ReplaceInGenericMethod(MethodInfo methodToSearch)
        {
            if (!methodToSearch.IsGenericMethod)
                return methodToSearch;

            var genArgs = methodToSearch.GetGenericArguments();
            var newGenArgs = genArgs.Select(ReplaceInGenericArguments).ToArray();
            if (genArgs.SequenceEqual(newGenArgs))
                return methodToSearch;

            return methodToSearch.GetGenericMethodDefinition().MakeGenericMethod(newGenArgs);
        }
    }
}