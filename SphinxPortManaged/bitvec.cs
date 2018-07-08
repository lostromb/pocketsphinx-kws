using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class bitvec
    {
        public const int BITVEC_BITS = 32;

        /// <summary>
        /// Number of uints in a bit vector
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static int bitvec_size(int bits)
        {
            return ((bits + BITVEC_BITS - 1) / BITVEC_BITS);
        }

        /// <summary>
        /// Allocate a bit vector, all bits are clear
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static Pointer<uint> bitvec_alloc(int bits)
        {
            return ckd_alloc.ckd_calloc<uint>(bitvec_size(bits));
        }

        /// <summary>
        /// Free a bit vector.
        /// </summary>
        /// <param name="v"></param>
        public static void bitvec_free(Pointer<uint> v)
        {
            ckd_alloc.ckd_free(v);
        }

        /**
         * Set the b-th bit of bit vector v
         * @param v is a vector
         * @param b is the bit which will be set
         */
        public static void bitvec_set(Pointer<uint> vec, int b)
        {
            vec[(b) / BITVEC_BITS] |= (1U << ((b) & (BITVEC_BITS - 1)));
        }

        /**
         * Set all n bits in bit vector v
         * @param v is a vector
         * @param n is the number of bits
         */
        public static void bitvec_set_all(Pointer<uint> vec, int n)
        {
            for (int c = 0; c < bitvec_size(n); c++)
            {
                vec[c] = 0xFFFFFFFFU;
            }
        }

        /**
         * Clear the b-th bit of bit vector v
         * @param v is a vector
         * @param b is the bit which will be set
         */
        public static void bitvec_clear(Pointer<uint> vec, int b)
        {
            vec[(b) / BITVEC_BITS] &= ~(1U << ((b) & (BITVEC_BITS - 1)));
        }

        /**
         * Clear all n bits in bit vector v
         * @param v is a vector
         * @param n is the number of bits
         */
        public static void bitvec_clear_all(Pointer<uint> vec, int n)
        {
            for (int c = 0; c < bitvec_size(n); c++)
            {
                vec[c] = 0;
            }
        }

        public static uint bitvec_is_set(Pointer<uint> vec, int bit)
        {
            return (vec[(bit) / BITVEC_BITS] & (1U << ((bit) & (BITVEC_BITS - 1))));
        }

        public static uint bitvec_is_clear(Pointer<uint> vec, int bit)
        {
            return bitvec_is_set(vec, bit) == 0 ? 1U : 0U;
        }
    }
}
