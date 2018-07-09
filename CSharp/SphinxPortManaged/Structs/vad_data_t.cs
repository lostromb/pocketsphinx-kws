using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class vad_data_t
    {
        public byte in_speech;
        public short pre_speech_frames;
        public short post_speech_frames;
        public Pointer<prespch_buf_t> prespch_buf;
    }
}
