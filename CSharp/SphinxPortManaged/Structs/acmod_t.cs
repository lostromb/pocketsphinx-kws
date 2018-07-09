using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class acmod_t
    {
        /* Global objects, not retained. */
        public Pointer<cmd_ln_t> config;          /**< Configuration. */
        public Pointer<logmath_t> lmath;          /**< Log-math computation. */
        public Pointer<gnode_t> strings;           /**< Temporary acoustic model filenames. */

        /* Feature computation: */
        public Pointer<fe_t> fe;                  /**< Acoustic feature computation. */
        public Pointer<feat_t> fcb;               /**< Dynamic feature computation. */

        /* Model parameters: */
        public Pointer<bin_mdef_t> mdef;          /**< Model definition. */
        public Pointer<tmat_t> tmat;              /**< Transition matrices. */
        public ps_mgau_t mgau;           /**< Model parameters. */
        public Pointer<ps_mllr_t> mllr;           /**< Speaker transformation. */

        /* Senone scoring: */
        public Pointer<short> senone_scores;      /**< GMM scores for current frame. */
        public Pointer<uint> senone_active_vec; /**< Active GMMs in current frame. */
        public Pointer<byte> senone_active;      /**< Array of deltas to active GMMs. */
        public int senscr_frame;          /**< Frame index for senone_scores. */
        public int n_senone_active;       /**< Number of active GMMs. */
        public int log_zero;              /**< Zero log-probability value. */

        /* Utterance processing: */
        public Pointer<Pointer<float>> mfc_buf;   /**< Temporary buffer of acoustic features. */
        public Pointer<Pointer<Pointer<float>>> feat_buf; /**< Temporary buffer of dynamic features. */
        public FILE rawfh;        /**< File for writing raw audio data. */
        public FILE mfcfh;        /**< File for writing acoustic feature data. */
        public FILE senfh;        /**< File for writing senone score data. */
        public FILE insenfh;  /**< Input senone score file. */
        public Pointer<long> framepos;     /**< File positions of recent frames in senone file. */

        /* Rawdata collected during decoding */
        public Pointer<short> rawdata;
        public int rawdata_size;
        public int rawdata_pos;

        /* A whole bunch of flags and counters: */
        public byte state;        /**< State of utterance processing. */
        public byte compallsen;   /**< Compute all senones? */
        public byte grow_feat;    /**< Whether to grow feat_buf. */
        public byte insen_swap;   /**< Whether to swap input senone score. */

        public int utt_start_frame; /**< Index of the utterance start in the stream, all timings are relative to that. */

        public int output_frame; /**< Index of next frame of dynamic features. */
        public int n_mfc_alloc;  /**< Number of frames allocated in mfc_buf */
        public int n_mfc_frame;  /**< Number of frames active in mfc_buf */
        public int mfc_outidx;   /**< Start of active frames in mfc_buf */
        public int n_feat_alloc; /**< Number of frames allocated in feat_buf */
        public int n_feat_frame; /**< Number of frames active in feat_buf */
        public int feat_outidx;  /**< Start of active frames in feat_buf */
    }
}
