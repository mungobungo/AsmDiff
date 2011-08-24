using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;
namespace AsmDiff.Lib
{
    public class MethodInfo
    {
        public string FullName { get; set; }
        public TypeDefinition Type { get; set; }
        public MethodDefinition Method { get; set; }
        public Collection<ParameterDefinition> Parameters { get; set; }
        public TypeReference ReturnType { get; set; }
        public string ByteCode { get; set; }

    }
}
