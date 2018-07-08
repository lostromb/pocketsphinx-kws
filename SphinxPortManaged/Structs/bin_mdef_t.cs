using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class bin_mdef_t
    {
        public int refcnt;
        public int n_ciphone;    /**< Number of base (CI) phones */
        public int n_phone;      /**< Number of base (CI) phones + (CD) triphones */
        public int n_emit_state; /**< Number of emitting states per phone (0 for heterogeneous) */
        public int n_ci_sen;     /**< Number of CI senones; these are the first */
        public int n_sen;        /**< Number of senones (CI+CD) */
        public int n_tmat;       /**< Number of transition matrices */
        public int n_sseq;       /**< Number of unique senone sequences */
        public int n_ctx;        /**< Number of phones of context */
        public int n_cd_tree;    /**< Number of nodes in cd_tree (below) */
        public int sil;      /**< CI phone ID for silence */
            
        public Pointer<Pointer<byte>> ciname;       /**< CI phone names */
        public Pointer<cd_tree_t> cd_tree;  /**< Tree mapping CD phones to phone IDs */
        public Pointer<mdef_entry_t> phone; /**< All phone structures */
        public Pointer<Pointer<ushort>> sseq;       /**< Unique senone sequences (2D array built at load time) */
        public Pointer<byte> sseq_len;     /**< Number of states in each sseq (NULL for homogeneous) */

        /* These two are not stored on disk, but are generated at load time. */
        public Pointer<short> cd2cisen;    /**< Parent CI-senone id for each senone */
        public Pointer<short> sen2cimap;   /**< Parent CI-phone for each senone (CI or CD) */

        /** Allocation mode for this object. */
        public int alloc_mode;
    }
}
