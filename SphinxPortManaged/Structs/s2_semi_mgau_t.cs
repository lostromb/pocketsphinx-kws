using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class s2_semi_mgau_t : ps_mgau_t
    {
        public Pointer<cmd_ln_t> config;   /* configuration parameters */

        public Pointer<gauden_t> g;        /* Set of Gaussians (pointers below point in here and will go away soon) */

        public Pointer<Pointer<Pointer<byte>>> mixw;     /* mixture weight distributions */

        public Pointer<byte> mixw_cb;    /* mixture weight codebook, if any (assume it contains 16 values) */
        public int n_sen;    /* Number of senones */
        public Pointer<byte> topn_beam;   /* Beam for determining per-frame top-N densities */
        public short max_topn;
        public short ds_ratio;

        public Pointer<Pointer<Pointer<vq_feature_t>>> topn_hist; /**< Top-N scores and codewords for past frames. */
        public Pointer<Pointer<byte>> topn_hist_n;      /**< Variable top-N for past frames. */
        public Pointer<Pointer<vq_feature_t>> f;          /**< Topn-N for currently scoring frame. */
        public int n_topn_hist;          /**< Number of past frames tracked. */

        /* Log-add table for compressed values. */
        public Pointer<logmath_t> lmath_8b;
        /* Log-add object for reloading means/variances. */
        public Pointer<logmath_t> lmath;
    }
}
