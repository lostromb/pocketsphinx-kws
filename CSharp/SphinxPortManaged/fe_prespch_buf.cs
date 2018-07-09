using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe_prespch_buf
    {
        public static Pointer<prespch_buf_t> fe_prespch_init(int num_frames, int num_cepstra, int num_samples)
        {
            Pointer<prespch_buf_t> prespch_buf;

            prespch_buf = ckd_alloc.ckd_calloc_struct<prespch_buf_t>(1);

            prespch_buf.Deref.num_cepstra = checked((short)num_cepstra);
            prespch_buf.Deref.num_frames_cep = checked((short)num_frames);
            prespch_buf.Deref.num_samples = checked((short)num_samples);
            prespch_buf.Deref.num_frames_pcm = 0;

            prespch_buf.Deref.cep_write_ptr = 0;
            prespch_buf.Deref.cep_read_ptr = 0;
            prespch_buf.Deref.ncep = 0;

            prespch_buf.Deref.pcm_write_ptr = 0;
            prespch_buf.Deref.pcm_read_ptr = 0;
            prespch_buf.Deref.npcm = 0;

            prespch_buf.Deref.cep_buf = ckd_alloc.ckd_calloc_2d<float>((uint)num_frames, (uint)num_cepstra);

            prespch_buf.Deref.pcm_buf = ckd_alloc.ckd_calloc<short>(prespch_buf.Deref.num_frames_pcm * prespch_buf.Deref.num_samples);

            return prespch_buf;
        }


        public static int fe_prespch_read_cep(Pointer<prespch_buf_t> prespch_buf, Pointer<float> feat)
        {
            if (prespch_buf.Deref.ncep == 0)
                return 0;
            prespch_buf.Deref.cep_buf[prespch_buf.Deref.cep_read_ptr].MemCopyTo(feat, prespch_buf.Deref.num_cepstra);
            prespch_buf.Deref.cep_read_ptr = checked((short)((prespch_buf.Deref.cep_read_ptr + 1) % prespch_buf.Deref.num_frames_cep));
            prespch_buf.Deref.ncep--;
            return 1;
        }

        public static void fe_prespch_write_cep(Pointer<prespch_buf_t> prespch_buf, Pointer<float> feat)
        {
            feat.MemCopyTo(prespch_buf.Deref.cep_buf[prespch_buf.Deref.cep_write_ptr], prespch_buf.Deref.num_cepstra);
            prespch_buf.Deref.cep_write_ptr = checked((short)((prespch_buf.Deref.cep_write_ptr + 1) % prespch_buf.Deref.num_frames_cep));
            if (prespch_buf.Deref.ncep < prespch_buf.Deref.num_frames_cep)
            {
                prespch_buf.Deref.ncep++;
            }
            else
            {
                prespch_buf.Deref.cep_read_ptr = checked((short)((prespch_buf.Deref.cep_read_ptr + 1) % prespch_buf.Deref.num_frames_cep));
            }
        }

        public static void fe_prespch_read_pcm(Pointer<prespch_buf_t> prespch_buf, Pointer<short> samples,
                            Pointer<int> samples_num)
        {
            int i;
            Pointer<short> cursample = samples;
            samples_num.Deref = prespch_buf.Deref.npcm * prespch_buf.Deref.num_samples;
            for (i = 0; i < prespch_buf.Deref.npcm; i++)
            {
                prespch_buf.Deref.pcm_buf.Point(prespch_buf.Deref.pcm_read_ptr * prespch_buf.Deref.num_samples).MemCopyTo(cursample, prespch_buf.Deref.num_samples);
                prespch_buf.Deref.pcm_read_ptr = checked((short)((prespch_buf.Deref.pcm_read_ptr + 1) % prespch_buf.Deref.num_frames_pcm));
            }

            prespch_buf.Deref.pcm_read_ptr = 0;
            prespch_buf.Deref.pcm_write_ptr = 0;
            prespch_buf.Deref.npcm = 0;
            return;
        }

        public static void fe_prespch_write_pcm(Pointer<prespch_buf_t> prespch_buf, Pointer<short> samples)
        {
            int sample_ptr;

            sample_ptr = prespch_buf.Deref.pcm_write_ptr * prespch_buf.Deref.num_samples;
            samples.MemCopyTo(prespch_buf.Deref.pcm_buf.Point(sample_ptr), prespch_buf.Deref.num_samples);

            prespch_buf.Deref.pcm_write_ptr = checked((short)((prespch_buf.Deref.pcm_write_ptr + 1) % prespch_buf.Deref.num_frames_pcm));
            if (prespch_buf.Deref.npcm < prespch_buf.Deref.num_frames_pcm)
            {
                prespch_buf.Deref.npcm++;
            }
            else
            {
                prespch_buf.Deref.pcm_read_ptr = checked((short)((prespch_buf.Deref.pcm_read_ptr + 1) % prespch_buf.Deref.num_frames_pcm));
            }
        }

        public static void fe_prespch_reset_cep(Pointer<prespch_buf_t> prespch_buf)
        {
            prespch_buf.Deref.cep_read_ptr = 0;
            prespch_buf.Deref.cep_write_ptr = 0;
            prespch_buf.Deref.ncep = 0;
        }

        public static void fe_prespch_reset_pcm(Pointer<prespch_buf_t> prespch_buf)
        {
            prespch_buf.Deref.pcm_read_ptr = 0;
            prespch_buf.Deref.pcm_write_ptr = 0;
            prespch_buf.Deref.npcm = 0;
        }

        public static void fe_prespch_free(Pointer<prespch_buf_t> prespch_buf)
        {
            if (prespch_buf.IsNull)
                return;
            if (prespch_buf.Deref.cep_buf.IsNonNull)
                ckd_alloc.ckd_free_2d(prespch_buf.Deref.cep_buf);
            if (prespch_buf.Deref.pcm_buf.IsNonNull)
                ckd_alloc.ckd_free(prespch_buf.Deref.pcm_buf);
            ckd_alloc.ckd_free(prespch_buf);
        }

        public static int fe_prespch_ncep(Pointer<prespch_buf_t> prespch_buf)
        {
            return prespch_buf.Deref.ncep;
        }
    }
}
