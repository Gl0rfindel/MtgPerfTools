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
            cli.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
            cli.HelpOption();
            var paths = cli.Argument("paths", "The input dlls or directories of dlls to instrument", true).IsRequired();
            var outputOption = cli.Option("-o|--output <DIR>", "The directory where output files will be written", CommandOptionType.SingleValue);
            var refPathsOption = cli.Option("-r|--reference <DIR>", "Directory to search for references", CommandOptionType.MultipleValue);
            var exRegexOption = cli.Option("-x|--exclude <REGEX>", "A regular expression used to exclude methods for instrumentation.", CommandOptionType.MultipleValue);
            var incRegexOption = cli.Option("-i|--include <REGEX>", "A regular expression used to include methods for instrumentation.", CommandOptionType.MultipleValue);

            cli.OnExecute(() =>
            {
                var processors = new List<InstrumentationProcessor>();
                var directories = new HashSet<string>();
                foreach (var path in paths.Values)
                {
                    if (File.Exists(path))
                    {
                        if (path.EndsWith(".dll"))
                        {
                            processors.Add(new FileProcessor(path));
                            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
                            directories.Add(dir);
                        }
                        else if (path.EndsWith(".zip"))
                        {
                            processors.Add(new ZipProcessor(path));
                        }
                    }
                    else if (Directory.Exists(path))
                    {
                        processors.Add(new DirectoryProcessor(path));
                        directories.Add(path);
                    }
                }

                if (processors.Count == 0)
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
                string resolverPaths = Environment.GetEnvironmentVariable("MTGINSTR_RESOLVERPATH");
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

                string outputDirectory = Environment.CurrentDirectory;
                if (outputOption.HasValue())
                {
                    outputDirectory = outputOption.Value();
                    Console.WriteLine($"Output directory: {outputDirectory}");
                }

                var toolsContext = new ToolsAssemblyContext(toolsAsm);

                var opts = new InstrumenterOptions();
                opts.Excludes.AddRange(exRegexOption.Values);
                opts.Includes.AddRange(incRegexOption.Values);

                var instrumenter = new AssemblyInstrumenter(toolsContext, opts);
                var processingContext = new ProcessingContext()
                {
                    OutputDirectory = outputDirectory,
                    Instrumenter = instrumenter,
                    ReaderParams = readerParams
                };

                int success = 0;
                foreach (var processor in processors)
                {
                    try
                    {
                        Console.WriteLine($"Processing {processor.DisplayName}");

                        processor.Process(processingContext);

                        Console.WriteLine($"Done processing {processor.DisplayName}");

                        success++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing {processor.DisplayName}");
                        Console.WriteLine($"Error details: {e}");
                    }
                }

                if (success > 0)
                    Console.WriteLine($"Successfully processed {success} items");
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
}
