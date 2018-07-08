using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class tmat_t
    {
        public Pointer<Pointer<Pointer<byte>>> tp;
        public short n_tmat;
        public short n_state;
    }
}
