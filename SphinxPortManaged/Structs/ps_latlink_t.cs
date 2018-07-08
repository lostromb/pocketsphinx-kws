using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_latlink_t
    {
        public Pointer<ps_latnode_t> from;	/**< From node */
        public Pointer<ps_latnode_t> to;   /**< To node */
        public Pointer<ps_latlink_t> best_prev;
        public int ascr;         /**< Score for from->wid (from->sf to this->ef) */
        public int path_scr;     /**< Best path score from root of DAG */
        public int ef;         /**< Ending frame of this word  */
        public int alpha;                /**< Forward probability of this link P(w,o_1^{ef}) */
        public int beta;                 /**< Backward probability of this link P(w|o_{ef+1}^T) */
    }
}
