using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
// using Mono.Cecil;

namespace RoslynTest
{
    public static class Program
    {
        private static readonly IReadOnlyCollection<MetadataReference> DefaultReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(string).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueTuple<>).GetTypeInfo().Assembly.Location)
        };

        private static void Main()
        {
            string code = File.ReadAllText("ExampleClass.txt");
            Console.WriteLine("Hit any key to proceed");
            Console.ReadKey(true);
            CreateAssemblyDefinition(code);
            Console.WriteLine("Done. Hit any key to exit");
            Console.ReadKey(true);
        }

        private static void CreateAssemblyDefinition(string code)
        {
            var languageService = new CSharpLanguageService();
            SyntaxTree syntaxTree = languageService.ParseText(code, SourceCodeKind.Regular);
            Compilation compilation = languageService.CreateLibraryCompilation("InMemoryAssembly", enableOptimizations: false)
                                                     .AddReferences(DefaultReferences)
                                                     .AddSyntaxTrees(syntaxTree);

            using (var stream = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(stream);
                if (emitResult.Success)
                {

                    stream.Seek(0, SeekOrigin.Begin);
                    var assembly2 = Assembly.Load(stream.ToArray());
                    Console.WriteLine("Compiled successfully to {0}", assembly2.FullName);

                    Type exampleType = assembly2.GetTypes().First();
                    Console.WriteLine("instantiating {0}", exampleType.AssemblyQualifiedName);
                    dynamic exampleClassInstance = Activator.CreateInstance(exampleType);
                    string message = exampleClassInstance.getMessage();
                    Console.WriteLine("Message from {0}: {1}", exampleClassInstance.GetType(), message);
                    // AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(stream);
                    // Console.WriteLine(assembly.FullName);
                }
                else
                {
                    Console.WriteLine("Errors:");
                    foreach (Diagnostic d in emitResult.Diagnostics)
                    {
                        Console.WriteLine(d.ToString());
                    }
                }
            }
        }
    }

    public class CSharpLanguageService
    {
        private static readonly LanguageVersion MaxLanguageVersion = Enum.GetValues(typeof(LanguageVersion))
                                                                         .Cast<LanguageVersion>()
                                                                         .Max();

        public SyntaxTree ParseText(string sourceCode, SourceCodeKind kind) => CSharpSyntaxTree.ParseText(
                 sourceCode,
                 new CSharpParseOptions(
                    kind: kind,
                    languageVersion: MaxLanguageVersion));

        public Compilation CreateLibraryCompilation(string assemblyName, bool enableOptimizations)
        {
            var options = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: enableOptimizations ? OptimizationLevel.Release : OptimizationLevel.Debug,
                allowUnsafe: true);

            return CSharpCompilation.Create(assemblyName, options: options);
        }
    }
}
