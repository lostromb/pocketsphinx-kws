using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class cmd_ln_t
    {
        public int refcount;
        public Pointer<hash_table_t> ht;
        public Pointer<Pointer<byte>> f_argv;
        public uint f_argc;
    }
}
