using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class kws_keyphrase_t
    {
        public Pointer<byte> word;
        public int threshold;
        public Pointer<hmm_t> hmms;
        public int n_hmms;
    }
}
