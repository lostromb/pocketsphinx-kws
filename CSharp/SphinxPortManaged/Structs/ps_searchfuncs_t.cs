using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_searchfuncs_t
    {
        public start_func start;
        public step_func step;
        public finish_func finish;
        public reinit_func reinit;
        public free_func free;
        public lattice_func lattice;
        public hyp_func hyp;
        public prob_func prob;
        public seg_iter_func seg_iter;
        
        public delegate int start_func(ps_search_t search);
        public delegate int step_func(ps_search_t search, int frame_idx);
        public delegate int finish_func(ps_search_t search);
        public delegate int reinit_func(ps_search_t search, Pointer<dict_t> dict, Pointer<dict2pid_t> d2p);
        public delegate void free_func(ps_search_t search);

        public delegate Pointer<ps_lattice_t> lattice_func(ps_search_t search);
        public delegate Pointer<byte> hyp_func(ps_search_t search, BoxedValueInt out_score);
        public delegate int prob_func(ps_search_t search);
        public delegate ps_seg_t seg_iter_func(ps_search_t search);
    }
}
