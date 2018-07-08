using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class trigger_adapter
    {
        public Pointer<ps_decoder_t> ps;
        public bool utt_started;
        public bool user_is_speaking;
        public bool triggered;
        public Pointer<byte> last_hyp;
        public Pointer<kws_search_t> kwss;
    }
}
