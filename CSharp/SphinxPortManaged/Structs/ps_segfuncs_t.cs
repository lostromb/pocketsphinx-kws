using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_segfuncs_t
    {
        public seg_next_func seg_next;
        public seg_free_func seg_free;

        public delegate ps_seg_t seg_next_func(ps_seg_t seg);
        public delegate void seg_free_func(ps_seg_t seg);
    }
}
