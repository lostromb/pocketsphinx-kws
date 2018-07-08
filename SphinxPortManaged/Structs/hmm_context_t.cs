using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class hmm_context_t
    {
        public int n_emit_state;     /**< Number of emitting states in this set of HMMs. */
        public Pointer<Pointer<Pointer<byte>>> tp;     /**< State transition scores tp[id][from][to] (logs3 values). */
        public Pointer<short> senscore;  /**< State emission scores senscore[senid]
                               (negated scaled logs3 values). */
        public Pointer<Pointer<ushort>> sseq;   /**< Senone sequence mapping. */
        public Pointer<int> st_sen_scr;      /**< Temporary array of senone scores (for some topologies). */
        public Pointer<listelem_alloc_t> mpx_ssid_alloc; /**< Allocator for senone sequence ID arrays. */
        public object udata;            /**< Whatever you feel like, gosh. */
    }
}
