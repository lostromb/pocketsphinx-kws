using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_seg_t
    {
        public ps_segfuncs_t vt;     /**< V-table of seg methods */
        public ps_search_t search;   /**< Search object from whence this came */
        public Pointer<byte>word;      /**< Word string (pointer into dictionary hash) */
        public int sf;        /**< Start frame. */
        public int ef;        /**< End frame. */
        public int ascr;            /**< Acoustic score. */
        public int lscr;            /**< Language model score. */
        public int prob;            /**< Log posterior probability. */
        /* This doesn't need to be 32 bits, so once the scores above are
         * reduced to 16 bits (or less!), this will be too. */
        public int lback;           /**< Language model backoff. */
        /* Not sure if this should be here at all. */
        public float lwf;           /**< Language weight factor (for second-pass searches) */
    }
}
