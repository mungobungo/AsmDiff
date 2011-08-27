using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsmDiff.Lib
{
    public class ClassDiffRecord
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public IEnumerable<MethodDiffRecord>  ChangedMethods    { get; set; }
        
    }
}
