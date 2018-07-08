using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_latnode_t
    {
        public int id;           /**< Unique id for this node */
        public int wid;          /**< Dictionary word id */
        public int basewid;      /**< Dictionary base word id */
        /* FIXME: These are (ab)used to store backpointer indices, therefore they MUST be 32 bits. */
        public int fef;          /**< First end frame */
        public int lef;          /**< Last end frame */
        public int sf;         /**< Start frame */
        public int reachable;        /**< From \verbatim </s> \endverbatim or \verbatim <s> \endverbatim */
        public int node_id;      /**< Node from fsg model, used to map lattice back to model */
        //union {
        //        Pointer<gnode_t> velist;         /**< List of history entries with different lmstate (tst only) */
        //        int fanin;        /**< Number nodes with links to this node */
        //        int rem_score;    /**< Estimated best score from node.sf to end */
        //        int best_exit;    /**< Best exit score (used for final nodes only) */
        //    }
        //    info;
        public Pointer<latlink_list_t> exits;      /**< Links out of this node */
        public Pointer<latlink_list_t> entries;    /**< Links into this node */

        public Pointer<ps_latnode_t> alt;   /**< Node with alternate pronunciation for this word */
        public Pointer<ps_latnode_t> next; /**< Next node in DAG (no ordering implied) */
    }
}
