using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoolLibrary
{
    public class Test
    {
        void Sample()
        {
            int a = 1;
            return;
        }

        int Sample(int x)
        {
            return 3*2;
        }

        int Sample(string x)
        {
            return 332323*2;
        }
        int Sample<T>(T x)
        {
            return 0;
        }

        int Sample<T, S>(int x)
        {
            return 0;
        }

        int Sample<T, S>(int x, T y)
        {
            return 0;
        }

        int Sample<T, S>(int x, S y)
        {
            return 0;
        }
        int Sample<T>(int x)
        {
            return 0;
        }
        


        string Sample(string x, int y)
        {
            return "4232dfsdfsdf323";
        }
    }
}
