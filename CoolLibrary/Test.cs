using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoolLibrary
{
    public class Test
    {

        // public property
        public int PublicProp { get { return 0; } set { } }

        // private void method with none parameters
        void Sample()
        {
            return;
        }

        // private int -> int method
        int Sample(int x)
        {
            return 3 * 2;
        }

        // public int -> string method
        public int Sample(string x)
        {
            return 332323 * 2;
        }
        // protected string->string->string method
        protected string Sample(string x, string y)
        {
            return "1";
        }
        // one generic parameter, uses as usual
        int Sample<T>(T x)
        {
            return 0;
        }

        // two generic parameters, none is used
        int Sample<T, S>(int x)
        {
            return 0;
        }

        // two generic parametes only first is used
        int Sample<T, S>(T x)
        {
            return 0;
        }

        // two generic parametes, only second is used    
        int Sample<T, S>(S x)
        {
            return 0;
        }

        // two generic parameters, second is not used
        int Sample<T, S>(int x, T y)
        {
            return 0;
        }


        // two generic parameters, only first is not used
        int Sample<T, S>(int x, S y)
        {
            return 0;
        }


        // three genrics, parameter uses generic type, return type int
        int ThreeGenericsInt<T, S, Q>(Dictionary<T, Dictionary<S, Q>> dic) where T : IEnumerable<int>
        {
            return 0;
        }

        // three genrics, parameter uses generic type, return 
        Dictionary<S, Q> ThreeGenericsInt<T, S, Q>(Dictionary<S, Q> dic)
        {
            return dic;
        }

        // one generic parameter that is not used
        int Sample<T>(int x)
        {
            return 0;
        }

        // two non-generic parameters and non-generic return type
        string Sample(string x, int y)
        {
            return "4232dfsdfsdf323";

        }
        // static method
        public static int StaticMethod(int x)
        {
            return 0;
        }
        // virtual method
        public virtual int VirtualMethod(int x)
        {
            return 0;
        }

        // override should be checked too
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
