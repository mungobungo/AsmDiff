using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiffLib
{
    public class AssemblyDiffRecord
    {

        public string Asm1 { get; set; }
        public string Asm1File { get; set; }
        public int Asm1Classes { get; set; }
        public int Asm1Methods { get; set; }
        public string Asm2 { get; set; }
        public string Asm2File { get; set; }
        public int Asm2Classes { get; set; }
        public int Asm2Methods { get; set; }
        public IEnumerable<MethodDiffRecord> MethodDiffs { get; set; }
    }
}
