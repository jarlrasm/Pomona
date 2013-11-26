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
using System.IO;
using System.Linq;

using Pomona.CodeGen;
using Pomona.Common;
using Pomona.Common.Internals;
using Pomona.Common.TypeSystem;
using Pomona.Example;
using Pomona.Example.Models;
using Pomona.FluentMapping;

namespace Pomona.UnitTests.GenerateClientDllApp
{

    internal class Program
    {
        internal class ModifiedFluentRules
        {
            public void Map(ITypeMappingConfigurator<UnpostableThingOnServer> map)
            {
                map.PostAllowed();
            }


            public void Map(ITypeMappingConfigurator<Critter> map)
            {
                map.Include(x => x.PublicAndReadOnlyThroughApi, o => o.ReadOnly());
            }
        }

        private class ModifiedCritterPomonaConfiguration : CritterPomonaConfiguration
        {
            public override IEnumerable<object> FluentRuleObjects
            {
                get { return base.FluentRuleObjects.Concat(new ModifiedFluentRules()); }
            }
        }
        private static void Main(string[] args)
        {
            var typeMapper = new TypeMapper(new ModifiedCritterPomonaConfiguration());

            // Modify property Protected of class Critter to not be protected in client dll.
            // This is to test setting a protected property will throw exception on server.
#if false

            Console.WriteLine("Test");
            var protectedPropertyOfCritter =
                typeMapper.TransformedTypes.First(x => x.Name == "Critter")
                    .Properties.OfType<PropertyMapping>()
                    .First(x => x.Name == "Protected");

            protectedPropertyOfCritter.AccessMode = HttpMethod.Get | HttpMethod.Put;

            // Modify UnpostableThingOnServer to generate form type for post.
            // This is to check that server generates correct status code.

            var unpostableThing = typeMapper.TransformedTypes.First(x => x.Name == "UnpostableThingOnServer");
            unpostableThing.AllowedMethods |= HttpMethod.Post;
#endif
            using (var file = new FileStream(@"..\..\..\..\lib\Critters.Client.dll", FileMode.OpenOrCreate))
            {
                ClientLibGenerator.WriteClientLibrary(typeMapper, file, embedPomonaClient : false);
            }

            using (
                var file = new FileStream(
                    @"..\..\..\..\lib\Critters.Client.WithEmbeddedPomonaClient.dll",
                    FileMode.OpenOrCreate))
            {
                ClientLibGenerator.WriteClientLibrary(typeMapper, file, embedPomonaClient : true);
            }

            Console.WriteLine("Wrote client dll.");
        }
    }
}