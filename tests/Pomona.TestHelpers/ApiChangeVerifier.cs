﻿// ----------------------------------------------------------------------------
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

using System;
using System.IO;
using Nancy.Testing;
using Pomona.Schemas;

namespace Pomona.TestHelpers
{
    public class ApiChangeVerifier
    {
        private readonly string schemaDirectory;

        public ApiChangeVerifier(string schemaDirectory)
        {
            if (schemaDirectory == null) throw new ArgumentNullException("schemaDirectory");
            this.schemaDirectory = schemaDirectory;
        }

        public void VerifyCompatibility(Schema changedSchema)
        {
            foreach (var schemaFilename in Directory.GetFiles(schemaDirectory, "*.json"))
            {
                var oldSchema = Schema.FromJson(File.ReadAllText(schemaFilename));
                var errorWriter = new StringWriter();
                if (!changedSchema.IsBackwardsCompatibleWith(oldSchema, errorWriter))
                    throw new AssertException("Schema " + changedSchema.Version + " breaks compatibility with " +
                                              schemaFilename);
            }
        }
    }
}