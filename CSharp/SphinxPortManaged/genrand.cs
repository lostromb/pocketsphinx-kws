using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class genrand
    {
        public const int N = 624;
        public const int M = 397;
        public const uint MATRIX_A = 0x9908b0dfU;   /* constant vector a */
        public const uint UPPER_MASK = 0x80000000U; /* most significant w-r bits */
        public const uint LOWER_MASK = 0x7fffffffU; /* least significant r bits */
        
        public static void genrand_seed(uint s)
        {
            init_genrand(s);
        }


        public static Pointer<uint> mt = PointerHelpers.Malloc<uint>(N);     /* the array for the state vector  */
        public static uint mti = N + 1;         /* mti==N+1 means mt[N] is not initialized */
        public static Pointer<uint> mag01 = new Pointer<uint>(new uint[] { 0x0U, MATRIX_A });

        /* initializes mt[N] with a seed */
        public static void init_genrand(uint s)
        {
            mt[0] = s & 0xffffffffU;
            for (mti = 1; mti < N; mti++)
            {
                mt[mti] =
                    (0x6C078965U * (mt[mti - 1] ^ (mt[mti - 1] >> 30)) + mti);
                /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
                /* In the previous versions, MSBs of the seed affect   */
                /* only MSBs of the array mt[].                        */
                /* 2002/01/09 modified by Makoto Matsumoto             */
                mt[mti] &= 0xffffffffU;
                /* for >32 bit machines */
            }
        }

        /* generates a random number on [0,0xffffffff]-interval */
        public static uint genrand_int32()
        {
            uint y;
            /* mag01[x] = x * MATRIX_A  for x=0,1 */

            if (mti >= N)
            {             /* generate N words at one time */
                int kk;

                if (mti == N + 1)       /* if init_genrand() has not been called, */
                    init_genrand(5489U);       /* a default initial seed is used */

                for (kk = 0; kk < N - M; kk++)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1U];
                }
                for (; kk < N - 1; kk++)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1U];
                }
                y = (mt[N - 1] & UPPER_MASK) | (mt[0] & LOWER_MASK);
                mt[N - 1] = mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1U];

                mti = 0;
            }

            y = mt[mti++];

            /* Tempering */
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9d2c5680U;
            y ^= (y << 15) & 0xefc60000U;
            y ^= (y >> 18);

            return y;
        }

        /* generates a random number on [0,0x7fffffff]-interval */
        public static long genrand_int31()
        {
            return (long)(genrand_int32() >> 1);
        }

        /* generates a random number on [0,1]-real-interval */
        public static double genrand_real1()
        {
            return genrand_int32() * (1.0 / 4294967295.0);
            /* divided by 2^32-1 */
        }

        /* generates a random number on [0,1)-real-interval */
        public static double genrand_real2()
        {
            return genrand_int32() * (1.0 / 4294967296.0);
            /* divided by 2^32 */
        }

        /* generates a random number on (0,1)-real-interval */
        public static double genrand_real3()
        {
            return (((double)genrand_int32()) + 0.5) * (1.0 / 4294967296.0);
            /* divided by 2^32 */
        }

        /* generates a random number on [0,1) with 53-bit resolution*/
        public static double genrand_res53()
        {
            uint a = genrand_int32() >> 5, b = genrand_int32() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }
    }
}
