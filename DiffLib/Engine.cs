using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Mono.Cecil;
using Cecil.Decompiler.Languages;


namespace DiffLib
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

        public static AssemblyDiffRecord CecilAssebmlyDiff(AssemblyDefinition firstAssembly, AssemblyDefinition secondAssembly)
        {
            var firstMethods = GetMethods(firstAssembly);
            Dictionary<string, MethodInfo> firstHash = new Dictionary<string, MethodInfo>();

            foreach (var method in firstMethods)
            {
                if (!firstHash.ContainsKey(method.Method.FullName))
                {
                    firstHash.Add(method.Method.FullName, method);
                }
            }
            var secondMethods = GetMethods(secondAssembly);

            Dictionary<string, MethodInfo> secondHash = new Dictionary<string, MethodInfo>();
            foreach (var method in secondMethods)
            {
                if (!secondHash.ContainsKey(method.Method.FullName))
                {
                    secondHash.Add(method.Method.FullName, method);
                }
            }
            
            var delta = from key in firstHash.Keys
                        where secondHash.ContainsKey(key)
                        let firstMethod = firstHash[key]
                        let secondMethod = secondHash[key]
                        select  new MethodDiffRecord(){MethodName = key, FirstBytes = firstMethod.ByteCode, SecondBytes = secondMethod.ByteCode};
            

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

        private static IEnumerable<MethodInfo> GetMethods(AssemblyDefinition firstAssembly)
        {
            var firstData = from module in firstAssembly.Modules
                            from type in module.GetTypes()
                            from method in type.Methods
                            let parameters = method.Parameters
                            let returnType = method.ReturnType

                            // for some system methods body can be null, so we should check it
                            where method.HasBody
                            let instructions = method.Body.Instructions

                            //let code = method.SourceCode()
                            let bytes = string.Join(";", method.Body.Instructions)
                            select new MethodInfo() { Type = type, Method = method, Parameters = parameters, ReturnType = returnType, ByteCode = bytes };
            return firstData;
        }
        public static IEnumerable<AssemblyDiffRecord> CecilGetDirectoryDiff(string firstFolder, string secondFolder)
        {
            var firstAssembiles = CecilLoadAssemblies(firstFolder);
            var secondAssemblies = CecilLoadAssemblies(secondFolder);
            
            var diffs = from asm1 in firstAssembiles
                        from asm2 in secondAssemblies
                        where asm1.Name.Name == asm2.Name.Name
                        select CecilAssebmlyDiff(asm1, asm2);
            return diffs;
        }
        /// <summary>
        /// just loads assemblies from specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static IEnumerable<AssemblyDefinition> CecilLoadAssemblies(string path)
        {
            var directory = new DirectoryInfo(path);
            var files = directory.GetFiles("*.dll");

            foreach (var file in files)
            {
                AssemblyDefinition asm = null;
                try
                {
                    asm =  AssemblyDefinition.ReadAssembly(file.FullName);
                    
                }
                catch (ArgumentException)
                {

                }
                catch (BadImageFormatException)
                {
                    
                }
                if (asm != null)
                    yield return asm;
            }
        }

        #endregion

    }
}
