using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace MtgInstrumenter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Give a list of directories or dlls to modify");
            }

            // TODO: Read zips.
            // TODO: Generate mods txt

            var sourceAssemblies = new List<string>();
            var directories = new HashSet<string>();
            foreach (var arg in args)
            {
                if (arg.EndsWith(".dll") && File.Exists(arg))
                {
                    sourceAssemblies.Add(arg);
                    string dir = Path.GetDirectoryName(Path.GetFullPath(arg));
                    directories.Add(dir);
                }
                else if (Directory.Exists(arg))
                {
                    var files = Directory.GetFiles(arg, "*.dll");
                    sourceAssemblies.AddRange(files);
                    directories.Add(arg);
                }
            }

            if (sourceAssemblies.Count == 0)
            {
                Console.WriteLine("No assemblies found");
                return;
            }

            string toolsAsmPath = LocateToolsAssembly();
            var asmResolver = new DefaultAssemblyResolver();
            foreach (var dir in directories)
            {
                asmResolver.AddSearchDirectory(dir);
            }

            asmResolver.AddSearchDirectory(@"D:\Steam\steamapps\common\Enter the Gungeon\EtG_Data\Managed");

            var readerParams = new ReaderParameters()
            {
                AssemblyResolver = asmResolver
            };

            var toolsAsm = AssemblyDefinition.ReadAssembly(toolsAsmPath, readerParams);
            string outputDirectory = AppContext.BaseDirectory;
            var toolsContext = new ToolsAssemblyContext(toolsAsm);

            foreach (var sourceAssemblyFile in sourceAssemblies)
            {
                Console.WriteLine($"Processing {sourceAssemblyFile}");
                ProcessFile(sourceAssemblyFile, outputDirectory, readerParams, toolsContext);
                Console.WriteLine($"Done with {sourceAssemblyFile}");
            }
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

        private static void ProcessFile(string inputDll, string outputDirectory, ReaderParameters readerParams, ToolsAssemblyContext toolsContext)
        {
            var asmDefinition = AssemblyDefinition.ReadAssembly(inputDll, readerParams);
            foreach (var module in asmDefinition.Modules)
            {
                ProcessModule(module, toolsContext);
            }

            string outputDllName = Path.Combine(outputDirectory, Path.GetFileName(inputDll));
            asmDefinition.Write(outputDllName);
        }

        private static void ProcessModule(ModuleDefinition moduleDefinition, ToolsAssemblyContext toolsContext)
        {
            var enterRef = moduleDefinition.ImportReference(toolsContext.EnterMethodDefinition);
            var exitRef = moduleDefinition.ImportReference(toolsContext.ExitMethodDefinition);

            foreach (var typeDefinition in moduleDefinition.Types)
            {
                foreach (var method in typeDefinition.Methods)
                {
                    if (!method.HasBody)
                        continue;

                    ProcessMethod(method, enterRef, exitRef);
                }
            }
        }

        private static void ProcessMethod(MethodDefinition method, MethodReference enterRef, MethodReference exitRef)
        {
            method.Body.SimplifyMacros();
            var il = method.Body.GetILProcessor();

            var firstInstruction = method.Body.Instructions.First();
            var loadstr = il.Create(OpCodes.Ldstr, method.FullName);
            il.InsertBefore(firstInstruction, loadstr);
            il.InsertAfter(loadstr, il.Create(OpCodes.Call, enterRef));

            var returnInstruction = FixReturns(method);

            var beforeReturn = Instruction.Create(OpCodes.Nop);
            il.InsertBefore(returnInstruction, beforeReturn);

            il.InsertBefore(returnInstruction, il.Create(OpCodes.Call, exitRef));

            il.InsertBefore(returnInstruction, Instruction.Create(OpCodes.Endfinally));

            var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = firstInstruction,
                TryEnd = beforeReturn,
                HandlerStart = beforeReturn,
                HandlerEnd = returnInstruction,
            };

            method.Body.ExceptionHandlers.Add(handler);
            method.Body.InitLocals = true;
            method.Body.OptimizeMacros();
        }

        // see code in https://github.com/Fody/MethodTimer/blob/master/MethodTimer.Fody/MethodProcessor.cs
        // and https://stackoverflow.com/questions/12769699/mono-cecil-injecting-try-finally
        private static Instruction FixReturns(MethodDefinition method)
        {
            var body = method.Body;
            var instructions = method.Body.Instructions;
            if (method.ReturnType == method.Module.TypeSystem.Void)
            {
                var lastRet = Instruction.Create(OpCodes.Ret);

                foreach (var instruction in instructions)
                {
                    if (instruction.OpCode == OpCodes.Ret)
                    {
                        instruction.OpCode = OpCodes.Leave;
                        instruction.Operand = lastRet;
                    }
                }
                instructions.Add(lastRet);
                return lastRet;
            }
            else
            {
                var returnVariable = new VariableDefinition(method.ReturnType);
                body.Variables.Add(returnVariable);
                var lastLd = Instruction.Create(OpCodes.Ldloc, returnVariable);
                for (var index = 0; index < instructions.Count; index++)
                {
                    var instruction = instructions[index];
                    if (instruction.OpCode == OpCodes.Ret)
                    {
                        instruction.OpCode = OpCodes.Stloc;
                        instruction.Operand = returnVariable;
                        index++;
                        instructions.Insert(index, Instruction.Create(OpCodes.Leave, lastLd));
                    }
                }
                instructions.Add(lastLd);
                instructions.Add(Instruction.Create(OpCodes.Ret));
                return lastLd;
            }
        }
    }
}
