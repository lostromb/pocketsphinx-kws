using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class strfuncs
    {
        private static readonly Pointer<byte> whitespace = cstring.ToCString(" \t\n\r\f");

        public static Pointer<byte> string_trim(Pointer<byte> str, string_edge_e which)
        {
            uint len;

            len = cstring.strlen(str);
            if (which == string_edge_e.STRING_START || which == string_edge_e.STRING_BOTH)
            {
                uint sub = cstring.strspn(str, whitespace);
                if (sub > 0)
                {
                    str.Point(sub).MemMove(0 - (int)sub, (int)(len + 1 - sub));
                    len -= sub;
                }
            }

            if (which == string_edge_e.STRING_END || which == string_edge_e.STRING_BOTH)
            {
                int sub = (int)len;
                while (--sub >= 0)
                    if (cstring.strchr(whitespace, str[sub]).IsNull)
                        break;
                if (sub == -1)
                    str[0] = (byte)'\0';
                else
                    str[sub + 1] = (byte)'\0';
            }

            return str;
        }
        
        public static double atof_c(Pointer<byte> str)
        {
            // LOGAN changed: Previously this pointed to a MASSIVE function in dtoa.c that parsed the string into a double.
            // But I am going to hope that that is not necessary and using the platform double.Parse will be sufficient
            double returnVal;
            stdio.sscanf_f(str, out returnVal);
            return returnVal;
        }

        public static int isspace_c(byte ch)
        {
            if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                return 1;
            return 0;
        }

        public static int str2words(Pointer<byte> line, Pointer<Pointer<byte>> ptr, int max_ptr)
        {
            int i, n;

            n = 0;                      /* #words found so far */
            i = 0;                      /* For scanning through the input string */
            while (true)
            {
                /* Skip whitespace before next word */
                while (line[i] != 0 && isspace_c(line[i]) != 0)
                    ++i;
                if (line[i] == 0)
                    break;

                if (ptr.IsNonNull && n >= max_ptr)
                {
                    /*
                     * Pointer array size insufficient.  Restore NULL chars inserted so far
                     * to space chars.  Not a perfect restoration, but better than nothing.
                     */
                    for (; i >= 0; --i)
                        if (line[i] == '\0')
                            line[i] = (byte)' ';

                    return -1;
                }

                /* Scan to end of word */
                if (ptr.IsNonNull)
                    ptr[n] = line + i;
                ++n;
                while (line[i] != 0 && isspace_c(line[i]) == 0)
                    ++i;
                if (line[i] == 0)
                    break;
                if (ptr.IsNonNull)
                    line[i] = (byte)'\0';
                ++i;
            }

            return n;
        }
    }
}
