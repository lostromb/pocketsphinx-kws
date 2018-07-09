using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ptm_fast_eval_t
    {
        public Pointer<Pointer<Pointer<ptm_topn_t>>> topn;     /**< Top-N for each codebook (mgau x feature x topn) */
        public Pointer<uint> mgau_active; /**< Set of active codebooks */
    }
}
