using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Mono.Cecil;
using Cecil.Decompiler.Languages;
using System.Diagnostics;


namespace AsmDiff.Lib
{

    /// <summary>
    /// Class taken from 
    /// http://blog.lavablast.com/post/2010/05/05/Lambda-IEqualityComparer3cT3e.aspx
    /// </summary>
    /// <typeparam name="T"></typeparam>

    public class KeyEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> comparer;
        private readonly Func<T, object> keyExtractor;

        // Allows us to simply specify the key to compare with: y => y.CustomerID
        public KeyEqualityComparer(Func<T, object> keyExtractor) : this(keyExtractor, null) { }
        // Allows us to tell if two objects are equal: (x, y) => y.CustomerID == x.CustomerID
        public KeyEqualityComparer(Func<T, T, bool> comparer) : this(null, comparer) { }

        public KeyEqualityComparer(Func<T, object> keyExtractor, Func<T, T, bool> comparer)
        {
            this.keyExtractor = keyExtractor;
            this.comparer = comparer;
        }

        public bool Equals(T x, T y)
        {
            if (comparer != null)
                return comparer(x, y);
            else
            {
                var valX = keyExtractor(x);
                if (valX is IEnumerable<object>) // The special case where we pass a list of keys
                    return ((IEnumerable<object>)valX).SequenceEqual((IEnumerable<object>)keyExtractor(y));

                return valX.Equals(keyExtractor(y));
            }
        }

        public int GetHashCode(T obj)
        {
            if (keyExtractor == null)
                return obj.ToString().ToLower().GetHashCode();
            else
            {
                var val = keyExtractor(obj);
                if (val is IEnumerable<object>) // The special case where we pass a list of keys
                    return (int)((IEnumerable<object>)val).Aggregate((x, y) => x.GetHashCode() ^ y.GetHashCode());

                return val.GetHashCode();
            }
        }
    }
    public static class Extenstions
    {
        public static bool Contains<T>(this IEnumerable<T> list, T item, Func<T, object> keyExtractor)
        {
            return list.Contains(item, new KeyEqualityComparer<T>(keyExtractor));
        }
        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, object> keyExtractor)
        {
            return first.Intersect(second, new KeyEqualityComparer<T>(keyExtractor));
        }
        public static IEnumerable<T> Except<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, object> keyExtractor)
        {
            return first.Except(second, new KeyEqualityComparer<T>(keyExtractor));
        }

        /// <summary>
        /// taken from
        /// http://naveensrinivasan.com/2010/06/08/using-mono-cecil-decompiler-within-windbg-to-decompile/
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static string SourceCode(this MethodDefinition methodName)
        {

            var writer = new StringWriter();

            CSharp.GetLanguage(CSharpVersion.V3).GetWriter(new PlainTextFormatter(writer)).Write(methodName);

            return writer.ToString();

        }

    }

    public class Engine
    {


        # region Cecil Assemlby handling

        public  AssemblyDiffRecord AssemblyDiff(string firstPath, string secondPath)
        {
            var f1 = new FileInfo(firstPath);
            var f2 = new FileInfo(secondPath);
            var asm1 = AssemblyDefinition.ReadAssembly(f1.FullName);
            var asm2 = AssemblyDefinition.ReadAssembly(f2.FullName);
            return CecilAssebmlyDiff(asm1, asm2);
        }
        public  AssemblyDiffRecord CecilAssebmlyDiff(AssemblyDefinition firstAssembly, AssemblyDefinition secondAssembly)
        {
            Dictionary<string, MethodInfo> firstHash = ConvertToMethodDictionary(firstAssembly);
            Dictionary<string, MethodInfo> secondHash = ConvertToMethodDictionary(secondAssembly);

            var delta = CalculateMethodsDiff(firstHash, secondHash);


            //var changedMethods = from firstMethod in firstMethods
            //                     from secondMethod in secondMethods
            //                     where
            //                         // type names are the same
            //                     firstMethod.Type.FullName.Equals(secondMethod.Type.FullName)
            //                         // method names are the same
            //                      && firstMethod.Method.Name == secondMethod.Method.Name
            //                         // return typse are the same
            //                      && firstMethod.ReturnType.FullName == secondMethod.ReturnType.FullName
            //                         // parameters are the same
            //                      && firstMethod.Parameters.Except(secondMethod.Parameters, x => x.ParameterType.FullName).Count() == 0
            //                         //!!! dirty hack, byte arrays compared by their string representations
            //                         // bodies ARE NOT the same
            //                     && !firstMethod.ByteCode.Equals(secondMethod.ByteCode)

            //                     select new { firstMethod = firstMethod, secondMethod = secondMethod };
            // just collection results
            //var methodDiffs = new List<MethodDiffRecord>();
            //foreach (var diff in delta)
            //{
            //    var first = diff.firstMethod;
            //    //var paramNames = from parameter in first.parameters select parameter.Name + ":" + parameter.ParameterType.Name;

            //    //var methodName = first.method.ReturnType.Name + " " +
            //    //            first.type.Name + "." +
            //    //            first.method.Name + "(" + string.Join(",", paramNames) + ")";

            //    methodDiffs.Add(new MethodDiffRecord()
            //    {
            //        MethodName = diff.firstMethod.Method.FullName,
            //        FirstBytes = diff.firstMethod.ByteCode,
            //        SecondBytes = diff.secondMethod.ByteCode,

            //    });

            //}
            var res = new AssemblyDiffRecord()
            {
                Asm1 = firstAssembly.Name.FullName,
                Asm1File = firstAssembly.MainModule.Name,
                Asm1Classes = firstAssembly.Modules.Sum(x=> x.GetTypes().Count()),
                Asm1Methods = firstAssembly.Modules.Sum(x => x.GetTypes().Sum(y=> y.Methods.Count)),
                Asm2 = secondAssembly.Name.FullName,
                Asm2File = secondAssembly.MainModule.Name,
                Asm2Classes = secondAssembly.Modules.Sum(x => x.GetTypes().Count()),
                Asm2Methods = secondAssembly.Modules.Sum(x => x.GetTypes().Sum(y => y.Methods.Count)),
                MethodDiffs = delta

            };
            return res;

        }

        public  Dictionary<string, MethodInfo> ConvertToMethodDictionary(AssemblyDefinition secondAssembly)
        {
            var secondMethods = GetMethods(secondAssembly);

            Dictionary<string, MethodInfo> secondHash = new Dictionary<string, MethodInfo>();
            foreach (var method in secondMethods)
            {
                // if (!secondHash.ContainsKey(method.Method.FullName))
                //  {
                try
                {
                    secondHash.Add(method.Method.FullName, method);
                }
                catch(Exception ex) {
                    ex.ToString();
                }
                //  }
            }
            return secondHash;
        }

        private  IEnumerable<MethodDiffRecord> CalculateMethodsDiff(Dictionary<string, MethodInfo> firstHash, Dictionary<string, MethodInfo> secondHash)
        {
            var res = new List<MethodDiffRecord>();
            foreach (var key in firstHash.Keys)
            {
                try
                {

                    var secondMethod = secondHash[key];
                    var firstMethod = firstHash[key];
                    if(!firstMethod.ByteCode.Equals(secondMethod.ByteCode))
                        res.Add(new MethodDiffRecord() { MethodName = key, FirstBytes = firstMethod.ByteCode, SecondBytes = secondMethod.ByteCode });

                }
                catch { }
            }
            return res;
        }

        public  IEnumerable<MethodInfo> GetMethods(AssemblyDefinition firstAssembly)
        {
            var result = new List<MethodInfo>();
            var modules = firstAssembly.Modules;

            foreach (var module in modules)
            {

                foreach (var type in module.GetTypes())
                {

                    foreach (var method in type.Methods)
                    {
                        var parameters = method.Parameters;
                        var returnType = method.ReturnType;
                        
                        var sb = new StringBuilder();
                        if (returnType.HasGenericParameters)
                        {
                            sb.Append(returnType.FullName);
                            sb.Append(" ");
                        }
                        else
                        {
                            
                        }
                        

                        // for some system methods body can be null, so we should check it
                        if (method.HasBody)
                        {
                            var instructions = method.Body.Instructions;

                            //let code = method.SourceCode()
                            var bytes = string.Join(",", method.Body.Instructions);
                            result.Add(new MethodInfo() { Type = type, Method = method, Parameters = parameters, ReturnType = returnType, ByteCode = bytes });
                        }
                    }

                }
            }
            return result;
        }
        public  IEnumerable<AssemblyDiffRecord> CecilGetDirectoryDiff(string firstFolder, string secondFolder)
        {
            var firstAssembiles = CecilLoadAssemblies(firstFolder);
            var secondAssemblies = CecilLoadAssemblies(secondFolder);


            var res = new List<AssemblyDiffRecord>();
            foreach (var asm1 in firstAssembiles)
                foreach (var asm2 in secondAssemblies)
                    if (asm1.Name.Name == asm2.Name.Name)
                    {
                        var diff = CecilAssebmlyDiff(asm1, asm2);
                        yield return diff;
                        //res.Add(diff);
                    }
            //return res;
        }
        /// <summary>
        /// just loads assemblies from specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public  IEnumerable<AssemblyDefinition> CecilLoadAssemblies(string path)
        {
            var directory = new DirectoryInfo(path);
            var files = directory.GetFiles("*.dll");

            var res = new List<AssemblyDefinition>();
            foreach (var file in files)
            {
                AssemblyDefinition asm = null;
              
                
                try
                {
                    asm = AssemblyDefinition.ReadAssembly(file.FullName);
                    

                }
                catch 
                {

                }
                if (asm != null)
                {
                    
                    //yield return asm;
                    res.Add(asm);
                }
            }
            return res;
        }

        #endregion

    }
}
