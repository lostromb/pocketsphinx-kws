using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class tied_mgau_common
    {
        public static readonly Pointer<byte> MGAU_MIXW_VERSION = cstring.ToCString("1.0");   /* Sphinx-3 file format version for mixw */
        public static readonly Pointer<byte> MGAU_PARAM_VERSION = cstring.ToCString("1.0");   /* Sphinx-3 file format version for mean/var */
        public const int NONE = -1;
        public const int WORST_DIST = unchecked((int)0x80000000);
        public const int MAX_NEG_MIXW = 159;
        public const int MAX_NEG_ASCR = 96;

        public static int fast_logmath_add(Pointer<logmath_t> lmath, int mlx, int mly)
        {
            logadd_t t = lmath.Deref.t;
            int d, r;

            /* d must be positive, obviously. */
            if (mlx > mly)
            {
                d = (mlx - mly);
                r = mly;
            }
            else
            {
                d = (mly - mlx);
                r = mlx;
            }

            return r - ((t.table_uint8)[d]);
        }
    }
}
