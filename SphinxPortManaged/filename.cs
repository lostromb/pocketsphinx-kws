using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class filename
    {
        public static Pointer<byte> path2basename(Pointer<byte> path)
        {
            Pointer<byte> result;
            result = cstring.strrchr(path, (byte)'\\');
            return (result.IsNull ? path : result + 1);
        }
    }
}
