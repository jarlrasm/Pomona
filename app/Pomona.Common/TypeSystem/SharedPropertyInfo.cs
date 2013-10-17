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
using System.Linq.Expressions;
using System.Reflection;

namespace Pomona.Common.TypeSystem
{
    public class SharedPropertyInfo : IPropertyInfo
    {
        private readonly PropertyInfo propertyInfo;

        public PropertyInfo PropertyInfo
        {
            get { return propertyInfo; }
        }

        private readonly ITypeMapper typeMapper;


        internal SharedPropertyInfo(PropertyInfo propertyInfo, ITypeMapper typeMapper)
        {
            if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");
            if (typeMapper == null) throw new ArgumentNullException("typeMapper");
            this.propertyInfo = propertyInfo;
            this.typeMapper = typeMapper;
        }

        #region Implementation of IPropertyInfo

        public bool AlwaysExpand
        {
            get { throw new NotImplementedException(); }
        }

        public PropertyCreateMode CreateMode
        {
            get { throw new NotImplementedException(); }
        }

        public IMappedType DeclaringType
        {
            get { return typeMapper.GetClassMapping(propertyInfo.DeclaringType); }
        }

        public Func<object, object> Getter
        {
            get { return x => propertyInfo.GetValue(x, null); }
        }

        public bool IsEtagProperty
        {
            get { return false; }
        }

        public Expression CreateGetterExpression(Expression instance)
        {
            throw new NotImplementedException();
        }


        public bool IsWriteable
        {
            get { return propertyInfo.GetSetMethod() != null; }
        }


        public bool IsReadable
        {
            get { return propertyInfo.GetGetMethod() != null; }
        }

        public bool IsSerialized
        {
            get { return true; }
        }

        public string JsonName
        {
            get { return Name.LowercaseFirstLetter(); }
        }

        public string LowerCaseName
        {
            get { return Name.ToLower(); }
        }

        public string Name
        {
            get { return propertyInfo.Name; }
        }

        public IMappedType PropertyType
        {
            get { return typeMapper.GetClassMapping(propertyInfo.PropertyType); }
        }

        public Action<object, object> Setter
        {
            get { return (o, v) => propertyInfo.SetValue(o, v, null); }
        }

        public bool IsPrimaryKey
        {
            get { return false; }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}::{2}", PropertyType, DeclaringType, Name);
        }

        #endregion
    }
}