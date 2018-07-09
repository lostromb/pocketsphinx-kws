using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class arg_t
    {
        public Pointer<byte> name;
        public int type;
        public Pointer<byte> deflt;
        public Pointer<byte> doc;
    }
}
