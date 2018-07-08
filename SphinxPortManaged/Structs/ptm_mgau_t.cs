using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ptm_mgau_t : ps_mgau_t
    {
        public Pointer<cmd_ln_t> config;   /**< Configuration parameters */
        public Pointer<gauden_t> g;        /**< Set of Gaussians. */
        public int n_sen;       /**< Number of senones. */
        public Pointer<byte> sen2cb;     /**< Senone to codebook mapping. */
        public Pointer<Pointer<Pointer<byte>>> mixw;     /**< Mixture weight distributions by feature, codeword, senone */

        public Pointer<byte> mixw_cb;    /* Mixture weight codebook, if any (assume it contains 16 values) */
        public short max_topn;
        public short ds_ratio;

        public Pointer<ptm_fast_eval_t> hist;   /**< Fast evaluation info for past frames. */
        public Pointer<ptm_fast_eval_t> f;      /**< Fast eval info for current frame. */
        public int n_fast_hist;         /**< Number of past frames tracked. */

        /* Log-add table for compressed values. */
        public Pointer<logmath_t> lmath_8b;
        /* Log-add object for reloading means/variances. */
        public Pointer<logmath_t> lmath;
    }
}
