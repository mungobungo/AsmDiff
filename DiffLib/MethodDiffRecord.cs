using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiffLib
{
    public class MethodDiffRecord
    {

        public string MethodName { get; set; }
        public string FirstBytes { get; set; }
        public string SecondBytes { get; set; }
    }
}
