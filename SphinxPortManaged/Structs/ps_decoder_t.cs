using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_decoder_t
    {
        /* Model parameters and such. */
        public Pointer<cmd_ln_t> config;  /**< Configuration. */
        public int refcount;      /**< Reference count. */

        /* Basic units of computation. */
        public Pointer<acmod_t> acmod;    /**< Acoustic model. */
        public Pointer<dict_t> dict;    /**< Pronunciation dictionary. */
        public Pointer<dict2pid_t> d2p;   /**< Dictionary to senone mapping. */
        public Pointer<logmath_t> lmath;  /**< Log math computation. */

        /* Search modules. */
        public Pointer<hash_table_t> searches;        /**< Set of search modules. */
        /* TODO: Convert this to a stack of searches each with their own
         * lookahead value. */
        public ps_search_t search;     /**< Currently active search module. */
        public ps_search_t phone_loop; /**< Phone loop search for lookahead. */
        public int pl_window;           /**< Window size for phoneme lookahead. */

        /* Utterance-processing related stuff. */
        public uint uttno;       /**< Utterance counter. */
        public ptmr_t perf;        /**< Performance counter for all of decoding. */
        public uint n_frame;     /**< Total number of frames processed. */
        public Pointer<byte> mfclogdir; /**< Log directory for MFCC files. */
        public Pointer<byte> rawlogdir; /**< Log directory for audio files. */
        public Pointer<byte> senlogdir; /**< Log directory for senone score files. */

        public ps_decoder_t()
        {
            perf = new ptmr_t();
        }
    }
}
