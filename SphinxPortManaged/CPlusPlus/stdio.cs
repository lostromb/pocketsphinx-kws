using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public static class stdio
    {
        /// <summary>
        /// Implements sscanf(line, "%s%n")
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static int sscanf_s_n(Pointer<byte> source, Pointer<byte> output, out int len)
        {
            string bigstr = cstring.FromCString(source);
            Regex matcher = new Regex("^\\s*(\\S+)");
            Match m = matcher.Match(bigstr);
            if (m.Success)
            {
                cstring.strcpy(output, cstring.ToCString(m.Groups[1].Value));
                len = m.Length;
                return 1;
            }
            else
            {
                len = 0;
                return 0;
            }
        }

        /// <summary>
        /// implements sscanf(line, "%s")
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static int sscanf_s(Pointer<byte> source, Pointer<byte> output)
        {
            int dummy;
            return sscanf_s_n(source, output, out dummy);
        }

        /// <summary>
        /// Implements sscanf(line, "%d%n")
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int sscanf_d_n(Pointer<byte> source, out int d, out int n)
        {
            string bigstr = cstring.FromCString(source);
            Regex matcher = new Regex("^\\s*([\\+\\-]?[0-9]+)");
            Match m = matcher.Match(bigstr);
            if (m.Success)
            {
                d = int.Parse(m.Groups[1].Value);
                n = m.Length;
                return 1;
            }
            else
            {
                d = 0;
                n = 0;
                return 0;
            }
        }

        public static int sscanf_f(Pointer<byte> source, out double d)
        {
            string bigstr = cstring.FromCString(source);
            Regex matcher = new Regex("^\\s*([\\+\\-]?[0-9]+(\\.[0-9]+)?([eE][\\+\\-]?[0-9]+)?)");
            Match m = matcher.Match(bigstr);
            if (m.Success)
            {
                d = double.Parse(m.Groups[1].Value);
                return 1;
            }
            else
            {
                d = 0;
                return 0;
            }
        }

        /// <summary>
        /// implements sscanf(line, "%d")
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static int sscanf_d(Pointer<byte> source, out int n)
        {
            int dummy;
            return sscanf_d_n(source, out n, out dummy);
        }

        /// <summary>
        /// Implements sscanf(line, "u")
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static int sscanf_u(Pointer<byte> source, out uint d)
        {
            string bigstr = cstring.FromCString(source);
            Regex matcher = new Regex("^\\s*([0-9]+)");
            Match m = matcher.Match(bigstr);
            if (m.Success)
            {
                d = uint.Parse(m.Groups[1].Value);
                return 1;
            }
            else
            {
                d = 0;
                return 0;
            }
        }

        /// <summary>
        /// implements sscanf "%d %s"
        /// </summary>
        /// <param name="source"></param>
        /// <param name="n"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static int sscanf_d_s(Pointer<byte> source, out int d, Pointer<byte> s)
        {
            throw new NotImplementedException();
        }
        
        public static int sprintf(Pointer<byte> target, string str)
        {
            Pointer<byte> conv = cstring.ToCString(str);
            cstring.strcpy(target, conv);
            return (int)cstring.strlen(target);
        }
    }
}
