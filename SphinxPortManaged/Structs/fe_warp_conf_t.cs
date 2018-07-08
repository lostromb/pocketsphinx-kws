using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    // LOGAN FIXME ever heard of interfaces?
    public class fe_warp_conf_t
    {
        public set_parameters_func set_parameters;
        public doc_func doc;
        public id_func id;
        public n_param_func n_param;
        public warped_to_unwarped_func warped_to_unwarped;
        public unwarped_to_warped_func unwarped_to_warped;
        public print_func print;

        public delegate void set_parameters_func(Pointer<byte> param_str, float sampling_rate);
        public delegate Pointer<byte> doc_func();
        public delegate uint id_func();
        public delegate uint n_param_func();
        public delegate float warped_to_unwarped_func(float nonlinear);
        public delegate float unwarped_to_warped_func(float linear);
        public delegate void print_func(Pointer<byte> label);
    }
}
