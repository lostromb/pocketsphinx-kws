using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fixpoint
    {
        public const int DEFAULT_RADIX = 12;

        public static int FLOAT2FIX_ANY(double x, int radix)
        {
			return (((x) < 0.0) ?
				((int)((x) * (double)(1 << (radix)) - 0.5))
				: ((int)((x) * (double)(1 << (radix)) + 0.5)));
        }

        public static int FLOAT2FIX(double x)
        {
            return FLOAT2FIX_ANY(x, DEFAULT_RADIX);
        }

        public static double FIX2FLOAT_ANY(int x, int radix)
        {
            return ((double)(x) / (1 << (radix)));
        }

        public static double FIX2FLOAT(int x)
        {
            return FIX2FLOAT_ANY(x, DEFAULT_RADIX);
        }
    }
}
