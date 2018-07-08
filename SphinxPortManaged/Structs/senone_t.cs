using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class senone_t
    {
        public Pointer<Pointer<Pointer<byte>>> pdf;       /**< gaussian density mixture weights, organized two possible
                                   ways depending on n_gauden:
                                   if (n_gauden > 1): pdf[sen][feat][codeword].  Not an
                                   efficient representation--memory access-wise--but
                                   evaluating the many codebooks will be more costly.
                                   if (n_gauden == 1): pdf[feat][codeword][sen].  Optimized
                                   for the shared-distribution semi-continuous case. */
        public Pointer<logmath_t> lmath;           /**< log math computation */
        public uint n_sen;       /**< Number senones in this set */
        public uint n_feat;      /**< Number feature streams */
        public uint n_cw;        /**< Number codewords per codebook,stream */
        public uint n_gauden;        /**< Number gaussian density codebooks referred to by senones */
        public float mixwfloor;      /**< floor applied to each PDF entry */
        public Pointer<uint> mgau;       /**< senone-id -> mgau-id mapping for senones in this set */
        public Pointer<int> featscr;              /**< The feature score for every senone, will be initialized inside senone_eval_all */
        public int aw;			/**< Inverse acoustic weight */
    }
}
