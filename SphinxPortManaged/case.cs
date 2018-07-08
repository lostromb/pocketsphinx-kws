using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class @case
    {
        public static byte UPPER_CASE(byte c)
        {
            return ((((c) >= (byte)'a') && ((c) <= (byte)'z')) ? (byte)(c - 32) : c);
        }

        public static byte LOWER_CASE(byte c)
        {
            return ((((c) >= (byte)'A') && ((c) <= (byte)'Z')) ? (byte)(c + 32) : c);
        }

        /**
         * (FIXME! The implementation is incorrect!) 
         * Case insensitive string compare.  Return the usual -1, 0, +1, depending on
         * str1 <, =, > str2 (case insensitive, of course).
         * @param str1 is the first string.
         * @param str2 is the second string. 
         */
        public static int strcmp_nocase(Pointer<byte> str1, Pointer<byte> str2)
        {
            byte c1, c2;

            if (str1.Equals(str2))
                return 0;
            if (str1.IsNonNull && str2.IsNonNull)
            {
                for (;;)
                {
                    str1 = str1.Iterate(out c1);
                    c1 = UPPER_CASE(c1);
                    str2 = str2.Iterate(out c2);
                    c2 = UPPER_CASE(c2);
                    if (c1 != c2)
                        return (c1 - c2);
                    if (c1 == '\0')
                        return 0;
                }
            }
            else
                return (str1.IsNull) ? -1 : 1;

            return 0;
        }
    }
}
