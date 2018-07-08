using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class hmm_t
    {
        public Pointer<hmm_context_t> ctx;            /**< Shared context data for this HMM. */
        public Pointer<int> score = PointerHelpers.Malloc<int>(hmm.HMM_MAX_NSTATE);   /**< State scores for emitting states. */
        public Pointer<int> history = PointerHelpers.Malloc<int>(hmm.HMM_MAX_NSTATE); /**< History indices for emitting states. */
        public int out_score;               /**< Score for non-emitting exit state. */
        public int out_history;             /**< History index for non-emitting exit state. */
        public ushort ssid;                   /**< Senone sequence ID (for non-MPX) */
        public Pointer<ushort> senid = PointerHelpers.Malloc<ushort>(hmm.HMM_MAX_NSTATE);  /**< Senone IDs (non-MPX) or sequence IDs (MPX) */
        public int bestscore;    /**< Best [emitting] state score in current frame (for pruning). */
        public short tmatid;       /**< Transition matrix ID (see hmm_context_t). */
        public int frame;  /**< Frame in which this HMM was last active; <0 if inactive */
        public byte mpx;          /**< Is this HMM multiplex? (hoisted for speed) */
        public byte n_emit_state; /**< Number of emitting states (hoisted for speed) */
    }
}
