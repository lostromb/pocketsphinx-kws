using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_search_t
    {
        public ps_searchfuncs_t vt;  /**< V-table of search methods. */

        public Pointer<byte> type;
        public Pointer<byte> name;

        public ps_search_t pls;      /**< Phoneme loop for lookahead. */
        public Pointer<cmd_ln_t> config;      /**< Configuration. */
        public Pointer<acmod_t> acmod;        /**< Acoustic model. */
        public Pointer<dict_t> dict;        /**< Pronunciation dictionary. */
        public Pointer<dict2pid_t> d2p;       /**< Dictionary to senone mappings. */
        public Pointer<byte> hyp_str;         /**< Current hypothesis string. */
        public Pointer<ps_lattice_t> dag;     /**< Current hypothesis word graph. */
        public Pointer<ps_latlink_t> last_link; /**< Final link in best path. */
        public int post;            /**< Utterance posterior probability. */
        public int n_words;         /**< Number of words known to search (may
                              be less than in the dictionary) */

        /* Magical word IDs that must exist in the dictionary: */
        public int start_wid;       /**< Start word ID. */
        public int silence_wid;     /**< Silence word ID. */
        public int finish_wid;      /**< Finish word ID. */
    }
}
