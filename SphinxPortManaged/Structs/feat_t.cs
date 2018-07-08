using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class feat_t
    {
        public int refcount;       /**< Reference count. */
        public Pointer<byte> name;     /**< Printable name for this feature type */
        public int cepsize;  /**< Size of input speech vector (typically, a cepstrum vector) */
        public int n_stream; /**< Number of feature streams; e.g., 4 in Sphinx-II */
        public Pointer<uint> stream_len; /**< Vector length of each feature stream */
        public int window_size;  /**< Number of extra frames around given input frame needed to compute
                           corresponding output feature (so total = window_size*2 + 1) */
        public int n_sv;         /**< Number of subvectors */
        public Pointer<uint> sv_len;      /**< Vector length of each subvector */
        public Pointer<Pointer<int>> subvecs;    /**< Subvector specification (or NULL for none) */
        public Pointer<float> sv_buf;      /**< Temporary copy buffer for subvector projection */
        public int sv_dim;       /**< Total dimensionality of subvector (length of sv_buf) */

        public int cmn; /**< Type of CMN to be performed on each utterance */
        public int varnorm;  /**< Whether variance normalization is to be performed on each utt;
                           Irrelevant if no CMN is performed */
        public int agc; /**< Type of AGC to be performed on each utterance */
        public compute_feat compute_feat_func;
        public Pointer<cmn_t> cmn_struct;  /**< Structure that stores the temporary variables for cepstral 
                           means normalization*/
        public Pointer<agc_t> agc_struct;  /**< Structure that stores the temporary variables for acoustic
                           gain control*/

        public Pointer<Pointer<float>> cepbuf;    /**< Circular buffer of MFCC frames for live feature computation. */
        public Pointer<Pointer<float>> tmpcepbuf; /**< Array of pointers into cepbuf to handle border cases. */
        public int bufpos;     /**< Write index in cepbuf. */
        public int curpos;     /**< Read index in cepbuf. */

        public Pointer<Pointer<Pointer<float>>> lda; /**< Array of linear transformations (for LDA, MLLT, or whatever) */
        public uint n_lda;   /**< Number of linear transformations in lda. */
        public uint out_dim; /**< Output dimensionality */

        /**
         * Feature computation function. 
         * @param fcb the feat_t describing this feature type
         * @param input pointer into the input cepstra
         * @param feat a 2-d array of output features (n_stream x stream_len)
         * @return 0 if successful, -ve otherwise.
         *
         * Function for converting window of input speech vector
         * (input[-window_size..window_size]) to output feature vector
         * (feat[stream][]).  If NULL, no conversion available, the
         * speech input must be feature vector itself.
         **/
        public delegate void compute_feat(Pointer<feat_t> fcb, Pointer<Pointer<float>> input, Pointer<Pointer<float>> feats);
    }
}
