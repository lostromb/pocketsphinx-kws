using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class agc
    {
        public static Pointer<Pointer<byte>> agc_type_str = new Pointer<Pointer<byte>>(
            new Pointer<byte>[]
            {
                cstring.ToCString("none"),
                cstring.ToCString("max"),
                cstring.ToCString("emax"),
                cstring.ToCString("noise")
            });

        public const int n_agc_type_str = 4;

        // fixme use agc_type_e enum here
        public static int agc_type_from_str(Pointer<byte> str)
        {
            int i;

            for (i = 0; i<n_agc_type_str; ++i) {
                if (0 == cstring.strcmp(str, agc_type_str[i]))
                    return i;
            }
            err.E_FATAL(string.Format("Unknown AGC type '{0}'\n", cstring.FromCString(str)));
            return agc_type_e.AGC_NONE;
        }

        public static Pointer<agc_t> agc_init()
        {
            Pointer<agc_t> agc;
            agc = ckd_alloc.ckd_calloc_struct<agc_t>(1);
            agc.Deref.noise_thresh = (2.0f);

            return agc;
        }

        public static void agc_free(Pointer<agc_t> agc)
        {
            ckd_alloc.ckd_free(agc);
        }

        /**
         * Normalize c0 for all frames such that max(c0) = 0.
         */
        public static void agc_max(Pointer<agc_t> agc, Pointer<Pointer<float>> mfc, int n_frame)
        {
            int i;

            if (n_frame <= 0)
                return;
            agc.Deref.obs_max = mfc[0][0];
            for (i = 1; i < n_frame; i++)
            {
                if (mfc[i][0] > agc.Deref.obs_max)
                {
                    agc.Deref.obs_max = mfc[i][0];
                    agc.Deref.obs_frame = 1;
                }
            }

            err.E_INFO(string.Format("AGCMax: obs=max= {0}\n", agc.Deref.obs_max));
            for (i = 0; i < n_frame; i++)
            {
                mfc[i].Set(0, mfc[i][0] - agc.Deref.obs_max);
            }
        }

        public static void agc_emax_set(Pointer<agc_t> agc, float m)
        {
            agc.Deref.max = (m);
            err.E_INFO(string.Format("AGCEMax: max= {0}\n", m));
        }

        public static float agc_emax_get(Pointer<agc_t> agc)
        {
            return (agc.Deref.max);
        }

        public static void agc_emax(Pointer<agc_t> agc, Pointer<Pointer<float>> mfc, int n_frame)
        {
            int i;

            if (n_frame <= 0)
                return;
            for (i = 0; i < n_frame; ++i)
            {
                if (mfc[i][0] > agc.Deref.obs_max)
                {
                    agc.Deref.obs_max = mfc[i][0];
                    agc.Deref.obs_frame = 1;
                }

                mfc[i].Set(0, mfc[i][0] - agc.Deref.max);
            }
        }

        /* Update estimated max for next utterance */
        public static void agc_emax_update(Pointer<agc_t> agc)
        {
            if (agc.Deref.obs_frame != 0)
            {            /* Update only if some data observed */
                agc.Deref.obs_max_sum += agc.Deref.obs_max;
                agc.Deref.obs_utt++;

                /* Re-estimate max over past history; decay the history */
                agc.Deref.max = agc.Deref.obs_max_sum / agc.Deref.obs_utt;
                if (agc.Deref.obs_utt == 16)
                {
                    agc.Deref.obs_max_sum /= 2;
                    agc.Deref.obs_utt = 8;
                }
            }
            err.E_INFO(string.Format("AGCEMax: obs= {0}, new= {1}\n", agc.Deref.obs_max, agc.Deref.max));

            /* Reset the accumulators for the next utterance. */
            agc.Deref.obs_frame = 0;
            agc.Deref.obs_max = -1000.0f; /* Less than any real C0 value (hopefully!!) */
        }

        public static void agc_noise(Pointer<agc_t> agc,
                  Pointer<Pointer<float>> cep,
                  int nfr)
        {
            float min_energy; /* Minimum log-energy */
            float noise_level;        /* Average noise_level */
            int i;           /* frame index */
            int noise_frames;        /* Number of noise frames */

            /* Determine minimum log-energy in utterance */
            min_energy = cep[0][0];
            for (i = 0; i < nfr; ++i)
            {
                if (cep[i][0] < min_energy)
                    min_energy = cep[i][0];
            }

            /* Average all frames between min_energy and min_energy + agc.Deref.noise_thresh */
            noise_frames = 0;
            noise_level = 0;
            min_energy += agc.Deref.noise_thresh;
            for (i = 0; i < nfr; ++i)
            {
                if (cep[i][0] < min_energy)
                {
                    noise_level += cep[i][0];
                    noise_frames++;
                }
            }

            if (noise_frames > 0)
            {
                noise_level /= noise_frames;
                err.E_INFO(string.Format("AGC NOISE: max= {0}\n", noise_level));
                /* Subtract noise_level from all log_energy values */
                for (i = 0; i < nfr; i++)
                {
                    cep[i].Set(0, cep[i][0] - noise_level);
                }
            }
        }

        public static void agc_set_threshold(Pointer<agc_t> agc, float threshold)
        {
            agc.Deref.noise_thresh = threshold;
        }

        public static float agc_get_threshold(Pointer<agc_t> agc)
        {
            return agc.Deref.noise_thresh;
        }
    }
}
