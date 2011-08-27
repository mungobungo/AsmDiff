using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Mono.Cecil;
using Cecil.Decompiler.Languages;
using System.Diagnostics;
using Mono.Collections.Generic;
using Mono.Cecil.Cil;


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

        public AssemblyDiffRecord AssemblyDiff(string firstPath, string secondPath)
        {
            var f1 = new FileInfo(firstPath);
            var f2 = new FileInfo(secondPath);
            var asm1 = AssemblyDefinition.ReadAssembly(f1.FullName);
            var asm2 = AssemblyDefinition.ReadAssembly(f2.FullName);
            return CecilAssebmlyDiff(asm1, asm2);
        }
        public AssemblyDiffRecord CecilAssebmlyDiff(AssemblyDefinition firstAssembly, AssemblyDefinition secondAssembly)
        {

            var delta = new List<MethodDiffRecord>();

            var firstTypes = new Dictionary<string, TypeDefinition>();
            var secondTypes = new Dictionary<string, TypeDefinition>();

            foreach (var module in firstAssembly.Modules)
                foreach (var type in module.Types)
                {
                    try
                    {
                        firstTypes.Add(type.FullName, type);
                    }
                    catch { }
                }

            foreach (var module in secondAssembly.Modules)
                foreach (var type in module.Types)
                {
                    try
                    {
                        secondTypes.Add(type.FullName, type);
                    }
                    catch { }
                }

            foreach (var t in firstAssembly.MainModule.Types)
            {
                try
                {
                    var second = secondTypes[t.FullName];
                    Dictionary<string, MethodInfo> firstHash = GetMethods(t);
                    Dictionary<string, MethodInfo> secondHash = GetMethods(second);
                    var diff = CalculateMethodsDiff(firstHash, secondHash);
                    delta.AddRange(diff);
                }
                catch { }
            }
         
            var res = new AssemblyDiffRecord()
            {
                Asm1 = firstAssembly.Name.FullName,
                Asm1File = firstAssembly.MainModule.Name,
                Asm1Classes = firstTypes.Count,
                Asm1Methods = firstTypes.Values.Sum(x => x.Methods.Count),
                Asm2 = secondAssembly.Name.FullName,
                Asm2File = secondAssembly.MainModule.Name,
                Asm2Classes = secondTypes.Count,
                Asm2Methods = secondTypes.Values.Sum(x => x.Methods.Count),
                MethodDiffs = delta

            };
            return res;

        }

        private IEnumerable<MethodDiffRecord> CalculateMethodsDiff(Dictionary<string, MethodInfo> firstHash, Dictionary<string, MethodInfo> secondHash)
        {
            var res = new List<MethodDiffRecord>();
            foreach (var key in firstHash.Keys)
            {
                try
                {

                    var secondMethod = secondHash[key];
                    var firstMethod = firstHash[key];

                    if (!firstMethod.ByteCode.Equals(secondMethod.ByteCode))
                    {
                       //TODO: Ass deciompilation firstMethod.Method.SourceCode();
                        res.Add(new MethodDiffRecord() { MethodName = key, FirstBytes = firstMethod.ByteCode, SecondBytes = secondMethod.ByteCode });
                    }
                    
                }
                catch { }
            }
            return res;
        }

        public Dictionary<string, MethodInfo> GetMethods(TypeDefinition type)
        {
            Dictionary<string, MethodInfo> dictionary = new Dictionary<string, MethodInfo>();
            
            foreach (var method in type.Methods)
            {
                var sb = new StringBuilder();
               
                if (method.HasGenericParameters)
                {
                    sb.Append("<");
                    sb.Append(string.Join(",", method.GenericParameters));
                    sb.Append(">");
                }

                // for some system methods body can be null, so we should check it
                if (method.HasBody)
                {
                   
                    var bytes = string.Join(",", method.Body.Instructions);

                    var name = method.FullName;
                    if (method.HasGenericParameters)
                    {
                        // adding generic parameters to string representation i.e Func() become Func<T1,T2>()
                        var index = name.IndexOf("(");
                        name = name.Insert(index, sb.ToString());
                    }

                    var minfo = new MethodInfo() { FullName = name, Method = method, ByteCode = bytes};
                    dictionary.Add(name, minfo);
                }
            }
            return dictionary;
        }
        public IEnumerable<AssemblyDiffRecord> CecilGetDirectoryDiff(string firstFolder, string secondFolder)
        {
            var firstAssembiles = CecilLoadAssemblies(firstFolder);
            var secondAssemblies = CecilLoadAssemblies(secondFolder);


            var res = new List<AssemblyDiffRecord>();
            foreach (var asm1 in firstAssembiles)
                foreach (var asm2 in secondAssemblies)
                    if (asm1.Name.Name == asm2.Name.Name)
                    {
                        var diff = CecilAssebmlyDiff(asm1, asm2);
                        if(diff.MethodDiffs.Count() > 0)
                            yield return diff;
                        //res.Add(diff);
                    }

            
        }
        /// <summary>
        /// Loads assemblies from specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<AssemblyDefinition> CecilLoadAssemblies(string path)
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
