using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ph_rc_t
    {
        public short rc;           /**< Specific rc for a parent <wpos,ci,lc> */
        public int pid;          /**< Triphone id for above rc instance */
        public Pointer<ph_rc_t> next;	/**< Next rc entry for same parent <wpos,ci,lc> */
    }
}
