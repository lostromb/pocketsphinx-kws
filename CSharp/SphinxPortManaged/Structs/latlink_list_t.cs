using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class latlink_list_t
    {
        public Pointer<ps_latlink_t> link;
        public Pointer<latlink_list_t> next;
    }
}
