using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MtgInstrumenter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Give list of directories or dlls to modify");
            }

            // TODO: Read zips.
            // TODO: Generate mods txt

            var sourceAssemblies = new List<string>();
            foreach (var arg in args)
            {
                if (arg.EndsWith(".dll") && File.Exists(arg))
                {
                    sourceAssemblies.Add(arg);
                }
                else if (Directory.Exists(arg))
                {
                    var files = Directory.GetFiles(arg, "*.dll");
                    sourceAssemblies.AddRange(files);
                }
            }

            if (sourceAssemblies.Count == 0)
            {
                return;
            }

            var toolsAsm = AssemblyDefinition.ReadAssembly("MtgProfilerTools.dll");
            string outputDirectory = AppContext.BaseDirectory;
            var toolsContext = new ToolsAssemblyContext(toolsAsm);

            foreach (var sourceAssemblyFile in sourceAssemblies)
            {
                ProcessFile(sourceAssemblyFile, outputDirectory, toolsContext);
            }
        }

        private static void ProcessFile(string inputDll, string outputDirectory, ToolsAssemblyContext toolsContext)
        {
            var asmDefinition = AssemblyDefinition.ReadAssembly(inputDll);
            foreach (var module in asmDefinition.Modules)
            {
                ProcessModule(module, toolsContext);
            }

            string outputDllName = Path.Combine(outputDirectory, Path.GetFileName(inputDll));
            asmDefinition.Write(outputDllName);
        }

        private static void ProcessModule(ModuleDefinition moduleDefinition, ToolsAssemblyContext toolsContext)
        {
            moduleDefinition.AssemblyReferences.Add(new AssemblyNameReference(toolsContext.AssemblyName.Name, toolsContext.AssemblyName.Version));

            foreach (var typeDefinition in moduleDefinition.Types)
            {
                foreach (var method in typeDefinition.Methods)
                {
                    var instr = Instruction.Create(OpCodes.Call, toolsContext.EnterMethodDefinition);
                    method.Body.Instructions.Insert(0, instr);

                    instr = Instruction.Create(OpCodes.Ldstr, method.Name);
                    method.Body.Instructions.Insert(0, instr);
                }
            }
        }
    }
}
