using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class lineiter_t
    {
        public Pointer<byte> buf;
        public FILE fh;
        public int bsiz;
        public int len;
        public int clean;
        public int lineno;
    }
}
