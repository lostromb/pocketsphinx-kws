using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class cmn_t
    {
        public Pointer<float> cmn_mean;
        public Pointer<float> cmn_var;
        public Pointer<float> sum;
        public int nframe;
        public int veclen;
    }
}
