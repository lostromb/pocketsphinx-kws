using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class mdef_t
    {
        public int n_ciphone;        /**< number basephones actually present */
        public int n_phone;      /**< number basephones + number triphones actually present */
        public int n_emit_state;     /**< number emitting states per phone */
        public int n_ci_sen;     /**< number CI senones; these are the first */
        public int n_sen;        /**< number senones (CI+CD) */
        public int n_tmat;       /**< number transition matrices */

        public Pointer<hash_table_t> ciphone_ht;   /**< Hash table for mapping ciphone strings to ids */
        public Pointer<ciphone_t> ciphone;     /**< CI-phone information for all ciphones */
        public Pointer<phone_t> phone;     /**< Information for all ciphones and triphones */
        public Pointer<Pointer<ushort>> sseq;      /**< Unique state (or senone) sequences in this model, shared
                                   among all phones/triphones */
        public int n_sseq;       /**< No. of unique senone sequences in this model */

        public Pointer<short> cd2cisen;        /**< Parent CI-senone id for each senone; the first
				   n_ci_sen are identity mappings; the CD-senones are
				   contiguous for each parent CI-phone */
        public Pointer<short> sen2cimap;       /**< Parent CI-phone for each senone (CI or CD) */

        public short sil;          /**< SILENCE_CIPHONE id */

        public Pointer<Pointer<Pointer<ph_lc_t>>> wpos_ci_lclist;	/**< wpos_ci_lclist[wpos][ci] = list of lc for <wpos,ci>.
                                   wpos_ci_lclist[wpos][ci][lc].rclist = list of rc for
                                   <wpos,ci,lc>.  Only entries for the known triphones
                                   are created to conserve space.
                                   (NOTE: FOR INTERNAL USE ONLY.) */
    }
}
