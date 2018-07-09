using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class prespch_buf_t
    {
        /* saved mfcc frames */
        public Pointer<Pointer<float>> cep_buf;
        /* saved pcm audio */
        public Pointer<short> pcm_buf;

        /* flag for pcm buffer initialization */
        public short cep_write_ptr;
        /* read pointer for cep buffer */
        public short cep_read_ptr;
        /* Count */
        public short ncep;


        /* flag for pcm buffer initialization */
        public short pcm_write_ptr;
        /* read pointer for cep buffer */
        public short pcm_read_ptr;
        /* Count */
        public short npcm;

        /* frames amount in cep buffer */
        public short num_frames_cep;
        /* frames amount in pcm buffer */
        public short num_frames_pcm;
        /* filters amount */
        public short num_cepstra;
        /* amount of fresh samples in frame */
        public short num_samples;
    }
}
