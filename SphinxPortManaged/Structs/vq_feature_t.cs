using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public struct vq_feature_t
    {
        public int score; /* score or distance */
        public int codeword; /* codeword (vector index) */
    }
}
