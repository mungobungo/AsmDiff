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
        public MethodDefinition Method { get; set; }
        public string ByteCode { get; set; }
        //public string SourceCode { get; set; }
        public override string ToString()
        {
            return FullName;
        }

    }
}
