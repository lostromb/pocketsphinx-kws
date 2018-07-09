using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ph_lc_t
    {
        public short lc;           /**< Specific lc for a parent <wpos,ci> */
        public Pointer<ph_rc_t> rclist;        /**< rc list for above lc instance */
        public Pointer<ph_lc_t> next;	/**< Next lc entry for same parent <wpos,ci> */
    }
}
