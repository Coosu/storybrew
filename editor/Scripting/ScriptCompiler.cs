using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;

namespace StorybrewEditor.Scripting
{
    public class ScriptCompiler : MarshalByRefObject
    {
        private static readonly string[] environmentDirectories =
        {
            Path.GetDirectoryName(typeof(object).Assembly.Location),
            Environment.CurrentDirectory,
        };
        private static int nextId;

        public static void Compile(string[] sourcePaths, string outputPath, IEnumerable<string> referencedAssemblies)
        {
            var setup = new AppDomainSetup()
            {
                ApplicationName = $"ScriptCompiler {nextId++}",
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
            };

            Debug.Print($"{nameof(Scripting)}: Compiling {string.Join(", ", sourcePaths)}");
            var compilerDomain = AppDomain.CreateDomain(setup.ApplicationName, null, setup);
            try
            {
                var compiler = (ScriptCompiler)compilerDomain.CreateInstanceFromAndUnwrap(
                    typeof(ScriptCompiler).Assembly.ManifestModule.FullyQualifiedName,
                    typeof(ScriptCompiler).FullName);

                compiler.compile(sourcePaths, outputPath, Program.Settings.UseRoslyn, referencedAssemblies);
            }
            finally
            {
                AppDomain.Unload(compilerDomain);
            }
        }

        private void compile(string[] sourcePaths, string outputPath, bool useRoslyn, IEnumerable<string> referencedAssemblies)
        {
            var trees = new Dictionary<SyntaxTree, string>();
            foreach (var sourcePath in sourcePaths)
            {
                var sourceCode = File.ReadAllText(sourcePath);

                var sourceText = SourceText.From(sourceCode);
                var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

                var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, parseOptions);

                trees.Add(syntaxTree, sourcePath);
            }

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            foreach (var referencedAssembly in referencedAssemblies)
            {
                string asmPath = referencedAssembly;
                try
                {
                    if (Path.IsPathRooted(asmPath))
                    {
                        references.Add(MetadataReference.CreateFromFile(asmPath));
                    }
                    else
                    {
                        var isExist = false;
                        foreach (var environmentDir in environmentDirectories)
                        {
                            var actualAsmPath = Path.Combine(environmentDir, referencedAssembly);
                            if (!File.Exists(actualAsmPath)) continue;
                            isExist = true;
                            asmPath = actualAsmPath;
                            break;
                        }

                        if (isExist)
                        {
                            references.Add(MetadataReference.CreateFromFile(asmPath));
                        }
                        else
                        {
                            var paths = string.Join(";", environmentDirectories.Select(k => $"\"{k}\""));
                            throw new Exception($"Could not resolve dependency: \"{referencedAssembly}\". " +
                                                $"Searched directories: {paths}");
                        }
                    }
                }
                catch (Exception e)
                {
                    var message = new StringBuilder("Compilation error\n\n");
                    message.AppendLine(e.ToString());
                    throw new ScriptCompilationException(message.ToString());
                }
            }

            var compilation = CSharpCompilation.Create(Path.GetFileName(outputPath),
                trees.Keys,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            using (var fs = File.Create(outputPath))
            {
                var result = compilation.Emit(fs);
                if (result.Success)
                {
                    return;
                }

                var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    .ToList();
                var failureGroup = failures.GroupBy(k =>
                    {
                        if (k.Location.SourceTree == null) return "";
                        if (trees.TryGetValue(k.Location.SourceTree, out var path)) return path;
                        return "";
                    })
                    .ToDictionary(k => k.Key, k => k.ToList());
                failures.Reverse();
                var message = new StringBuilder("Compilation error\n\n");
                foreach (var kvp in failureGroup)
                {
                    var file = kvp.Key;
                    var diagnostics = kvp.Value;
                    message.AppendLine($"{file}:");
                    foreach (var diagnostic in diagnostics)
                    {
                        message.AppendLine($"  {diagnostic}");
                    }
                }

                throw new ScriptCompilationException(message.ToString());
            }
        }
    }
}
