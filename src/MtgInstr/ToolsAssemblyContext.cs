using System;
using Mono.Cecil;

namespace MtgInstrumenter
{
    class ToolsAssemblyContext
    {
        public ToolsAssemblyContext(AssemblyDefinition toolsAsmDef)
        {
            if (toolsAsmDef == null)
                throw new ArgumentNullException(nameof(toolsAsmDef));

            AssemblyDefinition = toolsAsmDef;
            var type = toolsAsmDef.MainModule.GetType("MtgProfilerTools.Internal.Profiler");
            if (type == null)
            {
                throw new ArgumentException("MtgProfilerTools.Internal.Profiler was not found");
            }

            foreach (var method in type.Methods)
            {
                if (EnterMethodDefinition == null && method.Name == "Enter" && method.Parameters.Count == 1)
                {
                    EnterMethodDefinition = method;
                    continue;
                }

                if (ExitMethodDefinition == null && method.Name == "Exit" && method.Parameters.Count == 0)
                {
                    ExitMethodDefinition = method;
                }

                if (EnterMethodDefinition != null && ExitMethodDefinition != null)
                {
                    break;
                }
            }

            if (EnterMethodDefinition == null)
            {
                throw new ArgumentException("Could not find Profiler.Enter");
            }

            if (ExitMethodDefinition == null)
            {
                throw new ArgumentException("Could not find Profiler.Exit");
            }
        }

        public AssemblyDefinition AssemblyDefinition { get; }

        public AssemblyNameDefinition AssemblyName => AssemblyDefinition.Name;

        public MethodDefinition EnterMethodDefinition { get; }

        public MethodDefinition ExitMethodDefinition { get; }
    }
}
