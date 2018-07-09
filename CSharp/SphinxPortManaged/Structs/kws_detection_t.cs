using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class kws_detection_t
    {
        public Pointer<byte> keyphrase;
        public int sf;
        public int ef;
        public int prob;
        public int ascr;
    }
}
