using Mono.Cecil;

namespace MtgInstrumenter
{
    class ToolsAssemblyContext
    {
        public ToolsAssemblyContext(AssemblyDefinition toolsAsmDef)
        {
            AssemblyDefinition = toolsAsmDef;
            var type = toolsAsmDef.MainModule.GetType("Profiler");
            foreach (var method in type.Methods)
            {
                if (EnterMethodDefinition == null && method.Name == "Enter" && method.Parameters.Count == 1)
                {
                    EnterMethodDefinition = method;
                    continue;
                }

                if (ExitMethodDefinition == null && method.Name == "Enter" && method.Parameters.Count == 1)
                {
                    ExitMethodDefinition = method;
                }

                if (EnterMethodDefinition != null && ExitMethodDefinition != null)
                {
                    break;
                }
            }
        }

        public AssemblyDefinition AssemblyDefinition { get; }

        public AssemblyNameDefinition AssemblyName => AssemblyDefinition.Name;

        public MethodDefinition EnterMethodDefinition { get; }

        public MethodDefinition ExitMethodDefinition { get; }
    }
}
