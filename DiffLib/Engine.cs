using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace DiffLib
{
  
    //http://blog.lavablast.com/post/2010/05/05/Lambda-IEqualityComparer3cT3e.aspx
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
    }
   
    public class Engine
    {
        /// <summary>
        /// Compares two assemblies by their method bytecodes. If there's change in method,  it well be part of result
        /// </summary>
        /// <returns>list of differences of methods</returns>
        public static  IEnumerable<DiffRecord> GetAssemblyDiff(Assembly firstAssembly, Assembly secondAssemlby)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Default | BindingFlags.DeclaredOnly | BindingFlags.CreateInstance | BindingFlags.Instance;
            var firstData = from type in firstAssembly.GetTypes()
                            from method in type.GetMethods(bindingFlags)
                            let parameters = method.GetParameters()
                            let returnType = method.ReturnType
                            let body = method.GetMethodBody()
                            // for some system methods body can be null, so we should check it
                            where body != null 
                            let bytes = BitConverter.ToString(body.GetILAsByteArray())
                            select new { type, method, parameters, returnType, bytes };

            var secondData = from type in secondAssemlby.GetTypes()
                             from method in type.GetMethods(bindingFlags)
                             let parameters = method.GetParameters()
                             let returnType = method.ReturnType
                             let body = method.GetMethodBody()
                             // for some system methods body can be null, so we should check it
                             where body != null 
                             let bytes = BitConverter.ToString(body.GetILAsByteArray())
                             select new { type, method, parameters, returnType, bytes };

            var changedMethods = from firstMethod in firstData
                                  from secondMethod in secondData
                                  where
                                     // type names are the same
                                  firstMethod.type.FullName.Equals(secondMethod.type.FullName)
                                     // method names are the same
                                   && firstMethod.method.Name == secondMethod.method.Name
                                     // return typse are the same
                                   && firstMethod.returnType == secondMethod.returnType
                                     // parameters are the same
                                   && firstMethod.parameters.Except(secondMethod.parameters, x => x.ParameterType).Count() == 0 
                                   //!!! dirty hack, byte arrays compared by their string representations
                                   // bodies ARE NOT the same
                                   && !firstMethod.bytes.Equals(secondMethod.bytes)  
                                   
                                  select new { firstMethod = firstMethod, secondMethod = secondMethod };

            // just collection results
            var res = new List<DiffRecord>();
            foreach (var diff in changedMethods)
            {
                var first = diff.firstMethod;
                var paramNames = from parameter in first.parameters select parameter.Name + ":" + parameter.ParameterType.Name;

                var methodName = first.method.ReturnType.Name + " " +
                            first.type.Name + "." +
                            first.method.Name + "(" + string.Join(",", paramNames) + ")";

                res.Add( new DiffRecord() 
                { 
                    MethodName = methodName, 
                    FirstBytes = diff.firstMethod.bytes, 
                    SecondBytes = diff.secondMethod.bytes , 
                    Asm1 = diff.firstMethod.type.AssemblyQualifiedName,
                    Asm1File = diff.firstMethod.type.Assembly.CodeBase,
                    Asm2 = diff.secondMethod.type.AssemblyQualifiedName,
                    Asm2File = diff.secondMethod.type.Assembly.CodeBase,
                });
                
            }
            return res;
        }
        /// <summary>
        /// makes assembly comparison all to all for two folders
        /// </summary>
        /// <returns>list of lists of differences</returns>
        public static IEnumerable<IEnumerable<DiffRecord>> GetDirectoryDiff(string firstFolder, string secondFolder)
        {
            var firstAssembiles = LoadAssemblies(firstFolder);
            var secondAssemblies = LoadAssemblies(secondFolder);
            var diffs = from asm1 in firstAssembiles
                        from asm2 in secondAssemblies
                        select GetAssemblyDiff(asm1, asm2);
            return diffs;
        }
        /// <summary>
        /// just loads assemblies from specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static IEnumerable<Assembly> LoadAssemblies(string path)
        {
            var directory = new DirectoryInfo(path);
            var files = directory.GetFiles("*.dll");

            foreach (var file in files)
            {
                var asm = Assembly.LoadFile(file.FullName);
                if(asm != null)
                    yield return asm;
            }
        }

    }
}
