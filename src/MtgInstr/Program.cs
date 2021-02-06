using System;
using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Mono.Cecil;

namespace MtgInstrumenter
{
    class Program
    {
        static void Main(string[] args)
        {
            var cli = new CommandLineApplication();
            cli.HelpOption();
            var paths = cli.Argument("paths", "The input dlls or directories of dlls to instrument", true).IsRequired();
            var outputOption = cli.Option("-o|--output <DIR>", "The directory where output files will be written", CommandOptionType.SingleValue);
            var refPathsOption = cli.Option("-r|--reference <DIR>", "Directory to search for references", CommandOptionType.MultipleValue);
            //var exAsmOption = cli.Option("-xa|--excludeAsm <REGEX>", "", CommandOptionType.MultipleValue);
            //var incAsmOption = cli.Option("-ia|--includeAsm <REGEX>", "", CommandOptionType.MultipleValue);
            var exTypeRegexOption = cli.Option("-xt|--excludeType <REGEX>", "A regular expression used to exclude types for instrumentation.", CommandOptionType.MultipleValue);
            var incTypeRegexOption = cli.Option("-it|--includeType <REGEX>", "A regular expression used to include types for instrumentation.", CommandOptionType.MultipleValue);
            
            cli.OnExecute(() =>
            {
                // TODO: Read zips.
                // TODO: Generate mods txt
                var sourceAssemblies = new List<string>();
                var directories = new HashSet<string>();
                foreach (var path in paths.Values)
                {
                    if (File.Exists(path))
                    {
                        if (path.EndsWith(".dll"))
                        {
                            sourceAssemblies.Add(path);
                            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
                            directories.Add(dir);
                        }
                    }
                    else if (Directory.Exists(path))
                    {
                        var files = Directory.GetFiles(path, "*.dll");
                        sourceAssemblies.AddRange(files);
                        directories.Add(path);
                    }
                }

                if (sourceAssemblies.Count == 0)
                {
                    Console.WriteLine("No assemblies found");
                    return;
                }

                string toolsAsmPath = LocateToolsAssembly();
                var globalResolver = new DefaultAssemblyResolver();
                foreach (var dir in directories)
                {
                    globalResolver.AddSearchDirectory(dir);
                }

                foreach (var refPath in refPathsOption.Values)
                {
                    globalResolver.AddSearchDirectory(refPath);
                }

#if DEBUG
                string resolverPaths = Environment.GetEnvironmentVariable("MTG_INSTR_RESOLVERPATH");
                if (resolverPaths != null)
                {
                    Console.WriteLine("Adding debugging assembly resolver logic");
                    foreach (var path in resolverPaths.Split(";"))
                    {
                        Console.WriteLine($"Added resolver path: {path}");
                        globalResolver.AddSearchDirectory(path);
                    }
                }
#endif 

                var readerParams = new ReaderParameters()
                {
                    AssemblyResolver = globalResolver
                };

                var toolsAsm = AssemblyDefinition.ReadAssembly(toolsAsmPath, readerParams);
                string outputDirectory = outputOption.Value() ?? AppContext.BaseDirectory;
                var toolsContext = new ToolsAssemblyContext(toolsAsm);

                var opts = new InstrumenterOptions();
                opts.TypeExcludes.AddRange(exTypeRegexOption.Values);
                opts.TypeIncludes.AddRange(incTypeRegexOption.Values);

                var instrumenter = new AssemblyInstrumenter(toolsContext, opts);

                foreach (var sourceAssemblyFile in sourceAssemblies)
                {
                    Console.WriteLine($"Processing {sourceAssemblyFile}");

                    using (var instrumented = instrumenter.InstrumentAssembly(sourceAssemblyFile, readerParams))
                    {
                        string outputDllName = Path.Combine(outputDirectory, Path.GetFileName(sourceAssemblyFile));
                        instrumented.Write(outputDllName);
                    }

                    Console.WriteLine($"Done processing {sourceAssemblyFile}");
                }
            });

            cli.Execute(args);
        }

        private static string LocateToolsAssembly()
        {
            string profileToolsPath = Path.Combine(AppContext.BaseDirectory, "MtgProfilerTools.dll");
            if (File.Exists(profileToolsPath))
            {
                return profileToolsPath;
            }

#if DEBUG
            DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
            while ((current = current.Parent) != null)
            {
                string candidate = Path.Combine(current.FullName, "MtgProfilerTools");
                if (Directory.Exists(candidate))
                {
                    candidate = Path.Combine(candidate, "bin", "Debug", "net35", "MtgProfilerTools.dll");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
#endif
            throw new InvalidOperationException("Cannot find MtgProfilerTools.dll");
        }
    }

    internal class ProcessingOptions
    {
        public string OutputDirectory { get; set; }
    }

    abstract class InstrumentationProcessor
    {
        public abstract void Process(ProcessingOptions options);
    }

    /// <summary>
    /// Processes s single dll file
    /// </summary>
    internal class FileProcessor : InstrumentationProcessor
    {
        public FileProcessor(string file)
        {
        }

        public override void Process(ProcessingOptions options)
        {
        }
    }

    /// <summary>
    /// Processes a directory
    /// </summary>
    internal class DirectoryProcessor : InstrumentationProcessor
    {
        public DirectoryProcessor(string directory)
        {
        }

        public override void Process(ProcessingOptions options)
        {
        }
    }
}
