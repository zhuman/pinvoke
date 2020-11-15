﻿// Copyright © .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Win32MetaGeneration
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Text;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                e.Cancel = true;
            };

            Console.WriteLine("Generating code... (press Ctrl+C to cancel)");

            try
            {
                string outputDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "output");
                Directory.CreateDirectory(outputDirectory);

                var sw = Stopwatch.StartNew();

                var generator = new Generator()
                {
                    WideCharOnly = true,
                };
                if (args.Length > 0)
                {
                    foreach (string name in args)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        if (name.EndsWith(".*"))
                        {
                            generator.GenerateAllExternMethods(name.Substring(0, name.Length - 2), cts.Token);
                        }
                        else
                        {
                            generator.GenerateExternMethod(name);
                        }
                    }
                }
                else
                {
                    generator.GenerateAll(cts.Token);
                }

                using var generatedSourceStream = new FileStream(Path.Combine(outputDirectory, "NativeMethods.cs"), FileMode.Create, FileAccess.Write, FileShare.Read);
                using var generatedSourceWriter = new StreamWriter(generatedSourceStream, Encoding.UTF8);
                generator.CompilationUnit.WriteTo(generatedSourceWriter);

                Console.WriteLine("Generation time: {0}", sw.Elapsed);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token)
            {
                Console.Error.WriteLine("Canceled.");
            }
        }
    }
}
