using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiffLib
{
    public class DiffRecord
    {
        public string Asm1 { get; set; }
        public string Asm1File { get; set; }
        public string Asm2 { get; set; }
        public string Asm2File { get; set; }
        public string MethodName { get; set; }
        public string FirstBytes { get; set; }
        public string SecondBytes { get; set; }
    }
}
