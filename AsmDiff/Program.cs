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
            //if (args.Length != 2)
            //{
            //    Console.WriteLine("Usage : asmdiff folder1 folder2");
            //    return;
            //}

            //var folder1 = args[0];
            //var folder2 = args[1];

           // var folderDiff = Engine.GetDirectoryDiff(folder1, folder2);
            var folder1 = @"C:\Windows\Microsoft.NET\Framework\v2.0.50727";
            var folder2 = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319";
            var folderDiff = Engine.CecilGetDirectoryDiff(folder1, folder2);
            
            //var diff = Engine.GetAssemblyDiff(Assembly.GetAssembly(typeof(Test)), Assembly.GetAssembly(typeof(Engine)));
            foreach (var diff in folderDiff)
            {
                Console.WriteLine(diff.Asm1File);
                Console.WriteLine(diff.Asm1);
                Console.WriteLine("Classes: {0}, Methods : {1}", diff.Asm1Classes, diff.Asm1Methods);
                
                Console.WriteLine(diff.Asm2File);
                Console.WriteLine(diff.Asm2);
                Console.WriteLine("Classes: {0}, Methods : {1}", diff.Asm2Classes, diff.Asm2Methods);
                Console.WriteLine("Different methods : {0}", diff.MethodDiffs.Count());
                foreach (var d in diff.MethodDiffs)
                {

                    Console.WriteLine(d.MethodName);
                   // Console.WriteLine("\n\t" + d.FirstBytes);

                    
                  //  Console.WriteLine("\n\t" + d.SecondBytes);

                    Console.WriteLine("\n");

                }
            }
            if (folderDiff.Sum(x => x.MethodDiffs.Count()) == 0)
                Console.WriteLine("No diffs found");
            //Console.ReadLine();
        }
        
    }
}
