using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using Pomona.Common;
using Pomona.Common.Internals;

namespace Pomona.FluentMapping
{
    internal sealed class TypeMappingConfigurator<TDeclaringType>
        : TypeMappingConfigurator, ITypeMappingConfigurator<TDeclaringType>
    {
        private readonly IDictionary<string, PropertyMappingOptions> propertyOptions =
            new Dictionary<string, PropertyMappingOptions>();

        private ConstructorInfo constructor;

        private DefaultPropertyInclusionMode defaultPropertyInclusionMode =
            DefaultPropertyInclusionMode.AllPropertiesAreIncludedByDefault;

        private bool? isUriBaseType;
        private bool? isValueObject;

        private Type postResponseType;

        public override ConstructorInfo Constructor
        {
            get { return this.constructor; }
        }

        public override DefaultPropertyInclusionMode DefaultPropertyInclusionMode
        {
            get { return this.defaultPropertyInclusionMode; }
            set { this.defaultPropertyInclusionMode = value; }
        }

        public override bool? IsUriBaseType
        {
            get { return this.isUriBaseType; }
        }

        public override bool? IsValueObject
        {
            get { return this.isValueObject; }
        }

        public override Type PostResponseType
        {
            get { return this.postResponseType; }
        }

        public override IDictionary<string, PropertyMappingOptions> PropertyOptions
        {
            get { return this.propertyOptions; }
        }

        #region Implementation of ITypeMappingConfigurator<TDeclaringType>

        public ITypeMappingConfigurator<TDeclaringType> AllPropertiesAreExcludedByDefault()
        {
            this.defaultPropertyInclusionMode = DefaultPropertyInclusionMode.AllPropertiesAreExcludedByDefault;
            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> AllPropertiesAreIncludedByDefault()
        {
            this.defaultPropertyInclusionMode = DefaultPropertyInclusionMode.AllPropertiesAreIncludedByDefault;
            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> AllPropertiesRequiresExplicitMapping()
        {
            this.defaultPropertyInclusionMode = DefaultPropertyInclusionMode.AllPropertiesRequiresExplicitMapping;
            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> AsEntity()
        {
            throw new NotImplementedException();
        }


        public ITypeMappingConfigurator<TDeclaringType> AsUriBaseType()
        {
            this.isUriBaseType = true;
            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> AsValueObject()
        {
            this.isValueObject = true;
            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> ConstructedUsing(
            Expression<Func<TDeclaringType, TDeclaringType>> expr)
        {
            var expressionGotWrongStructureMessage =
                "Expression got wrong structure, expects expression which looks like this: x => new FooBar(x.Prop)" +
                " where each property maps to the argument";

            if (expr == null)
                throw new ArgumentNullException("expr");
            var constructExpr = expr.Body as NewExpression;
            if (expr == null)
                throw new ArgumentException(expressionGotWrongStructureMessage, "expr");

            var ctorArgCount = constructExpr.Arguments.Count;
            for (var ctorArgIndex = 0; ctorArgIndex < ctorArgCount; ctorArgIndex++)
            {
                try
                {
                    var ctorArg = constructExpr.Arguments[ctorArgIndex];
                    var propOptions = GetPropertyOptions(ctorArg);
                    propOptions.ConstructorArgIndex = ctorArgIndex;
                }
                catch (Exception exception)
                {
                    throw new ArgumentException(expressionGotWrongStructureMessage, "expr", exception);
                }
            }

            this.constructor = constructExpr.Constructor;

            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> Exclude(Expression<Func<TDeclaringType, object>> property)
        {
            var propOptions = GetPropertyOptions(property);
            propOptions.InclusionMode = PropertyInclusionMode.Excluded;
            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> Include<TPropertyType>(
            Expression<Func<TDeclaringType, TPropertyType>> property,
            Func
                <IPropertyOptionsBuilder<TDeclaringType, TPropertyType>,
                IPropertyOptionsBuilder<TDeclaringType, TPropertyType>> options = null)
        {
            var propOptions = GetPropertyOptions(property);

            propOptions.InclusionMode = PropertyInclusionMode.Included;

            if (options != null)
                options(new PropertyOptionsBuilder<TDeclaringType, TPropertyType>(propOptions));

            return this;
        }


        public ITypeMappingConfigurator<TDeclaringType> PostReturns<TPostResponseType>()
        {
            return PostReturns(typeof(TPostResponseType));
        }


        public ITypeMappingConfigurator<TDeclaringType> PostReturns(Type type)
        {
            this.postResponseType = type;
            return this;
        }


        internal override PropertyMappingOptions GetPropertyOptions(string name)
        {
            var propInfo = typeof(TDeclaringType).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propInfo == null)
            {
                throw new InvalidOperationException(
                    "No property with name " + name + " found on type " + typeof(TDeclaringType).FullName);
            }

            return this.propertyOptions.GetOrCreate(propInfo.Name, () => new PropertyMappingOptions(propInfo));
        }


        private Exception FluentRuleException(string message)
        {
            throw new InvalidOperationException(message);
        }


        private PropertyMappingOptions GetPropertyOptions<TPropertyType>(
            Expression<Func<TDeclaringType, TPropertyType>> property)
        {
            if (property == null)
                throw new ArgumentNullException("property");
            return GetPropertyOptions((Expression)property);
        }


        private PropertyMappingOptions GetPropertyOptions(Expression propertyExpr)
        {
            if (propertyExpr == null)
                throw new ArgumentNullException("propertyExpr");
            var propInfo = propertyExpr.ExtractPropertyInfo();
            var propOptions = this.propertyOptions.GetOrCreate(
                propInfo.Name, () => new PropertyMappingOptions(propInfo));
            return propOptions;
        }

        #endregion
    }

    internal abstract class TypeMappingConfigurator
    {
        public abstract ConstructorInfo Constructor { get; }
        public abstract DefaultPropertyInclusionMode DefaultPropertyInclusionMode { get; set; }
        public abstract bool? IsUriBaseType { get; }
        public abstract bool? IsValueObject { get; }
        public abstract Type PostResponseType { get; }
        public abstract IDictionary<string, PropertyMappingOptions> PropertyOptions { get; }
        internal abstract PropertyMappingOptions GetPropertyOptions(string name);
    }
}