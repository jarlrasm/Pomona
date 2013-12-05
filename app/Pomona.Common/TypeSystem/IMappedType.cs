#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2013 Karsten Nikolai Strand
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
using Newtonsoft.Json;

namespace Pomona.Common.TypeSystem
{
    /// <summary>
    /// This is the pomona way of representing a type.
    /// 
    /// Can't use Type directly, since the transformed types might not exist
    /// as Type in server context.
    /// </summary>
    public interface IMappedType
    {
        IMappedType BaseType { get; }
        IMappedType ElementType { get; }
        Type CustomClientLibraryType { get; }
        IMappedType DictionaryKeyType { get; }
        IMappedType DictionaryType { get; }
        IMappedType DictionaryValueType { get; }
        IList<IMappedType> GenericArguments { get; }
        bool HasUri { get; }
        bool IsAlwaysExpanded { get; }
        bool IsBasicWireType { get; }
        bool IsCollection { get; }
        bool IsNullable { get; }
        bool IsDictionary { get; }
        bool IsGenericType { get; }
        bool IsGenericTypeDefinition { get; }
        bool IsValueType { get; }
        string PluralName { get; }
        JsonConverter JsonConverter { get; }
        Type MappedType { get; }
        Type MappedTypeInstance { get; }
        string Name { get; }
        IPropertyInfo PrimaryId { get; }
        IList<IPropertyInfo> Properties { get; }
        TypeSerializationMode SerializationMode { get; }

        /// <summary>
        /// When true this type is considered a value object, which will affect serialization.
        /// It also means that it don't have an URL or identity.
        /// </summary>
        bool MappedAsValueObject { get; }

        object Create(IDictionary<IPropertyInfo, object> args);
    }
}