using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class kws_seg_t : ps_seg_t
    {
        public Pointer<gnode_t> detection;
        public int last_frame;
    }
}
