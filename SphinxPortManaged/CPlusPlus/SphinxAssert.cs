using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public static class SphinxAssert
    {
        public static void assert(bool condition)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed");
            }
        }
    }
}
