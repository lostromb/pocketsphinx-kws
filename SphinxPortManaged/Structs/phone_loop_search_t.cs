using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class phone_loop_search_t : ps_search_t
    {
        public Pointer<hmm_t> hmms;                       /**< Basic HMM structures for CI phones. */
        public Pointer<hmm_context_t> hmmctx;             /**< HMM context structure. */
        public short frame;                       /**< Current frame being searched. */
        public short n_phones;                    /**< Size of phone array. */
        public Pointer<Pointer<int>> pen_buf;                /**< Penalty buffer */
        public short pen_buf_ptr;                 /**< Pointer for frame to fill in penalty buffer */
        public Pointer<int> penalties;                  /**< Penalties for CI phones in current frame */
        public double penalty_weight;            /**< Weighting factor for penalties */

        public int best_score;                  /**< Best Viterbi score in current frame. */
        public int beam;                        /**< HMM pruning beam width. */
        public int pbeam;                       /**< Phone exit pruning beam width. */
        public int pip;                         /**< Phone insertion penalty ("language score"). */
        public int window;                        /**< Window size for phoneme lookahead */
        public Pointer<gnode_t> renorm;                    /**< List of renormalizations. */
    }
}
