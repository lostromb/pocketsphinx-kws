using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class agc_t
    {
        public float max;
        public float obs_max;
        public int obs_frame;
        public int obs_utt;
        public float obs_max_sum;
        public float noise_thresh;
    }
}
