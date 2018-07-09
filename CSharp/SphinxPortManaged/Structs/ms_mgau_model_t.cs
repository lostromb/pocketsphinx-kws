using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ms_mgau_model_t : ps_mgau_t
    {
        public Pointer<gauden_t> g;   /**< The codebook */
        public Pointer<senone_t> s;   /**< The senone */
        public int topn;      /**< Top-n gaussian will be computed */

        /**< Intermediate used in computation */
        public Pointer<Pointer<Pointer<gauden_dist_t>>> dist;
        public Pointer<byte> mgau_active;
        public Pointer<cmd_ln_t> config;
    }
}
