using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiffLib;
using System.Reflection;
using System.IO;

namespace AsmDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage : asmdiff folder1 folder2");
                return;
            }

            var folder1 = args[0];
            var folder2 = args[1];

           // var folderDiff = Engine.GetDirectoryDiff(folder1, folder2);
            var folderDiff = Engine.CecilGetDirectoryDiff(folder1, folder2);
            
            //var diff = Engine.GetAssemblyDiff(Assembly.GetAssembly(typeof(Test)), Assembly.GetAssembly(typeof(Engine)));
            foreach (var diff in folderDiff)
            {
                foreach (var d in diff)
                {

                    Console.WriteLine(d.Asm1File);
                    Console.WriteLine(d.Asm1 + " \n\t" + d.MethodName);
                    Console.WriteLine("\t" + d.FirstBytes);

                    Console.WriteLine(d.Asm2File);
                    Console.WriteLine(d.Asm2 + " \n\t" + d.MethodName);
                    Console.WriteLine("\t" + d.SecondBytes);

                    Console.WriteLine("\n");

                }
            }
            if (folderDiff.Sum(x => x.Count()) == 0)
                Console.WriteLine("No diffs found");
            //Console.ReadLine();
        }
        
    }
}
