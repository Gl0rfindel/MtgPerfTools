using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace MtgInstrumenter
{
    internal class AssemblyInstrumenter
    {
        private readonly List<Regex> _includeTypes, _excludeTypes;

        public AssemblyInstrumenter(ToolsAssemblyContext toolsContext)
            : this(toolsContext, new InstrumenterOptions())
        {
        }

        public AssemblyInstrumenter(ToolsAssemblyContext toolsContext, InstrumenterOptions options)
        {
            ToolsContext = toolsContext ?? throw new ArgumentNullException(nameof(toolsContext));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _excludeTypes = new List<Regex>();
            foreach (var ex in options.TypeExcludes)
            {
                _excludeTypes.Add(new Regex(ex));
            }

            _includeTypes = new List<Regex>();
            foreach (var inc in options.TypeIncludes)
            {
                _includeTypes.Add(new Regex(inc));
            }
        }

        public ToolsAssemblyContext ToolsContext { get; }

        public AssemblyDefinition InstrumentAssembly(string inputDll, ReaderParameters readerParams)
        {
            var asmDefinition = AssemblyDefinition.ReadAssembly(inputDll, readerParams);
            foreach (var module in asmDefinition.Modules)
            {
                ProcessModule(module);
            }

            return asmDefinition;
        }

        private void ProcessModule(ModuleDefinition moduleDefinition)
        {
            var enterRef = moduleDefinition.ImportReference(ToolsContext.EnterMethodDefinition);
            var exitRef = moduleDefinition.ImportReference(ToolsContext.ExitMethodDefinition);

            foreach (var typeDefinition in moduleDefinition.Types)
            {
                if (_excludeTypes.Count > 0)
                {
                    bool toExclude = false;
                    foreach (var ex in _excludeTypes)
                    {
                        if (ex.IsMatch(typeDefinition.FullName))
                        {
                            toExclude = true;
                            break;
                        }
                    }

                    if (toExclude)
                    {
                        continue;
                    }
                }

                if (_includeTypes.Count > 0)
                {
                    bool toInclude = false;
                    foreach (var inc in _includeTypes)
                    {
                        if (inc.IsMatch(typeDefinition.FullName))
                        {
                            toInclude = true;
                            break;
                        }
                    }

                    if (!toInclude)
                    {
                        continue;
                    }
                }

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
