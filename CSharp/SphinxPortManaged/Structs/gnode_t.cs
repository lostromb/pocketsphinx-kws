using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class gnode_t
    {
        public object data;     /** See prim_type.h */
        public Pointer<gnode_t> next;	/** Next node in list */
    }
}
