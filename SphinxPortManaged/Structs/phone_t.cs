using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class phone_t
    {
        public int ssid;         /**< State sequence (or senone sequence) ID, considering the
				                   n_emit_state senone-ids are a unit.  The senone sequences
				                   themselves are in a separate table */
        public int tmat;         /**< Transition matrix id */
        public short ci, lc, rc;       /**< Base, left, right context ciphones */
        public int wpos;		/**< Word position */
    }
}
