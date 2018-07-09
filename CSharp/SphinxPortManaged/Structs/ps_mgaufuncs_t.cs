using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ps_mgaufuncs_t
    {
        public Pointer<byte> name;
        public frame_eval_func frame_eval;
        public transform_func transform;
        public free_func free;

        public delegate int frame_eval_func(
            ps_mgau_t mgau,
            Pointer<short> senscr,
            Pointer<byte> senone_active,
            int n_senone_active,
            Pointer<Pointer<float>> feats,
            int frame,
            int compallsen);

        public delegate int transform_func(ps_mgau_t mgau, Pointer<ps_mllr_t> mllr);

        public delegate void free_func(ps_mgau_t mgau);
    }
}
