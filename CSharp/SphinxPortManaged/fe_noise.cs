using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe_noise
    {
        public const int SMOOTH_WINDOW = 4;
        public const double LAMBDA_POWER = 0.7;
        public const double LAMBDA_A = 0.995;
        public const double LAMBDA_B = 0.5;
        public const double LAMBDA_T = 0.85;
        public const double MU_T = 0.2;
        public const double MAX_GAIN = 20;
        public const double SLOW_PEAK_FORGET_FACTOR = 0.9995;
        public const double SLOW_PEAK_LEARN_FACTOR = 0.9;
        public const double SPEECH_VOLUME_RANGE = 8.0;

        public static void fe_lower_envelope(Pointer<noise_stats_t> noise_stats, Pointer<double> buf, Pointer<double> floor_buf, int num_filt)
        {
            int i;

            for (i = 0; i < num_filt; i++)
            {
                if (buf[i] >= floor_buf[i])
                {
                    floor_buf[i] =
                        noise_stats.Deref.lambda_a * floor_buf[i] + noise_stats.Deref.comp_lambda_a * buf[i];
                }
                else
                {
                    floor_buf[i] =
                        noise_stats.Deref.lambda_b * floor_buf[i] + noise_stats.Deref.comp_lambda_b * buf[i];
                }
            }
        }

        /* update slow peaks, check if max signal level big enough compared to peak */
        public static short fe_is_frame_quiet(Pointer<noise_stats_t> noise_stats, Pointer<double> buf, int num_filt)
        {
            int i;
            short is_quiet;
            double sum;
            double smooth_factor;

            sum = 0.0;
            for (i = 0; i < num_filt; i++)
            {
                sum += buf[i];
            }
            sum = Math.Log(sum);
            smooth_factor = (sum > noise_stats.Deref.slow_peak_sum) ? SLOW_PEAK_LEARN_FACTOR : SLOW_PEAK_FORGET_FACTOR;
            noise_stats.Deref.slow_peak_sum = noise_stats.Deref.slow_peak_sum * smooth_factor +
                                         sum * (1 - smooth_factor);

            is_quiet = noise_stats.Deref.slow_peak_sum - SPEECH_VOLUME_RANGE > sum ? (short)1 : (short)0;
            return is_quiet;
        }

        /* temporal masking */
        public static void fe_temp_masking(Pointer<noise_stats_t> noise_stats, Pointer<double> buf, Pointer<double> peak, int num_filt)
        {
            double cur_in;
            int i;

            for (i = 0; i < num_filt; i++)
            {
                cur_in = buf[i];

                peak[i] *= noise_stats.Deref.lambda_t;
                if (buf[i] < noise_stats.Deref.lambda_t * peak[i])
                    buf[i] = peak[i] * noise_stats.Deref.mu_t;

                if (cur_in > peak[i])
                    peak[i] = cur_in;
            }
        }

        /* spectral weight smoothing */
        public static void fe_weight_smooth(Pointer<noise_stats_t> noise_stats, Pointer<double> buf, Pointer<double> coefs, int num_filt)
        {
            int i, j;
            int l1, l2;
            double coef;

            for (i = 0; i < num_filt; i++)
            {
                l1 = ((i - SMOOTH_WINDOW) > 0) ? (i - SMOOTH_WINDOW) : 0;
                l2 = ((i + SMOOTH_WINDOW) <
                      (num_filt - 1)) ? (i + SMOOTH_WINDOW) : (num_filt - 1);

                coef = 0;
                for (j = l1; j <= l2; j++)
                {
                    coef += coefs[j];
                }
                buf[i] = buf[i] * (coef / (l2 - l1 + 1));

            }
        }

        public static Pointer<noise_stats_t> fe_init_noisestats(int num_filters)
        {
            int i;
            Pointer<noise_stats_t> noise_stats;

            noise_stats = ckd_alloc.ckd_calloc_struct<noise_stats_t>(1);

            noise_stats.Deref.power =
                (Pointer<double>)ckd_alloc.ckd_calloc<double>(num_filters);
            noise_stats.Deref.noise =
                (Pointer<double>)ckd_alloc.ckd_calloc<double>(num_filters);
            noise_stats.Deref.floor =
                (Pointer<double>)ckd_alloc.ckd_calloc<double>(num_filters);
            noise_stats.Deref.peak =
                (Pointer<double>)ckd_alloc.ckd_calloc<double>(num_filters);

            noise_stats.Deref.undefined = 1;
            noise_stats.Deref.num_filters = (uint)num_filters;

            noise_stats.Deref.lambda_power = LAMBDA_POWER;
            noise_stats.Deref.comp_lambda_power = 1 - LAMBDA_POWER;
            noise_stats.Deref.lambda_a = LAMBDA_A;
            noise_stats.Deref.comp_lambda_a = 1 - LAMBDA_A;
            noise_stats.Deref.lambda_b = LAMBDA_B;
            noise_stats.Deref.comp_lambda_b = 1 - LAMBDA_B;
            noise_stats.Deref.lambda_t = LAMBDA_T;
            noise_stats.Deref.mu_t = MU_T;
            noise_stats.Deref.max_gain = MAX_GAIN;
            noise_stats.Deref.inv_max_gain = 1.0 / MAX_GAIN;

            for (i = 1; i < 2 * SMOOTH_WINDOW + 1; i++)
            {
                noise_stats.Deref.smooth_scaling[i] = 1.0 / i;
            }

            return noise_stats;
        }

        public static void fe_reset_noisestats(Pointer<noise_stats_t> noise_stats)
        {
            if (noise_stats.IsNonNull)
                noise_stats.Deref.undefined = 1;
        }

        public static void fe_free_noisestats(Pointer<noise_stats_t> noise_stats)
        {
            ckd_alloc.ckd_free(noise_stats.Deref.power);
            ckd_alloc.ckd_free(noise_stats.Deref.noise);
            ckd_alloc.ckd_free(noise_stats.Deref.floor);
            ckd_alloc.ckd_free(noise_stats.Deref.peak);
            ckd_alloc.ckd_free(noise_stats);
        }

        /**
         * For fixed point we are doing the computation in a fixlog domain,
         * so we have to add many processing cases.
         */
        public static void fe_track_snr(Pointer<fe_t> fe, BoxedValueInt in_speech)
        {
            Pointer<double> signal;
            Pointer<double> gain;
            Pointer<noise_stats_t> noise_stats;
            Pointer<double> mfspec;
            int i, num_filts;
            short is_quiet;
            double lrt, snr;

            if (!(fe.Deref.remove_noise != 0 || fe.Deref.remove_silence != 0))
            {
                in_speech.Val = 1;
                return;
            }

            noise_stats = fe.Deref.noise_stats;
            mfspec = fe.Deref.mfspec;
            num_filts = (int)noise_stats.Deref.num_filters;

            signal = (Pointer<double>)ckd_alloc.ckd_calloc<double>(num_filts);

            if (noise_stats.Deref.undefined != 0)
            {
                noise_stats.Deref.slow_peak_sum = fixpoint.FIX2FLOAT(0);
                for (i = 0; i < num_filts; i++)
                {
                    noise_stats.Deref.power[i] = mfspec[i];
                    noise_stats.Deref.noise[i] = mfspec[i] / noise_stats.Deref.max_gain;
                    noise_stats.Deref.floor[i] = mfspec[i] / noise_stats.Deref.max_gain;
                    noise_stats.Deref.peak[i] = 0.0;
                }
                noise_stats.Deref.undefined = 0;
            }

            /* Calculate smoothed power */
            for (i = 0; i < num_filts; i++)
            {
                noise_stats.Deref.power[i] =
                    noise_stats.Deref.lambda_power * noise_stats.Deref.power[i] + noise_stats.Deref.comp_lambda_power * mfspec[i];
            }

            /* Noise estimation and vad decision */
            fe_lower_envelope(noise_stats, noise_stats.Deref.power, noise_stats.Deref.noise, num_filts);

            lrt = fixpoint.FLOAT2FIX(0.0);
            for (i = 0; i < num_filts; i++)
            {
                signal[i] = noise_stats.Deref.power[i] - noise_stats.Deref.noise[i];
                if (signal[i] < 1.0)
                    signal[i] = 1.0;
                snr = Math.Log(noise_stats.Deref.power[i] / noise_stats.Deref.noise[i]);
                if (snr > lrt)
                    lrt = snr;
            }
            is_quiet = fe_is_frame_quiet(noise_stats, signal, num_filts);

            if (fe.Deref.remove_silence != 0 && (lrt < fe.Deref.vad_threshold || is_quiet != 0))
            {
                in_speech.Val = 0;
            }
            else
            {
                in_speech.Val = 1;
            }

            fe_lower_envelope(noise_stats, signal, noise_stats.Deref.floor, num_filts);

            fe_temp_masking(noise_stats, signal, noise_stats.Deref.peak, num_filts);

            if (fe.Deref.remove_noise == 0)
            {
                /* no need for further calculations if noise cancellation disabled */
                ckd_alloc.ckd_free(signal);
                return;
            }

            for (i = 0; i < num_filts; i++)
            {
                if (signal[i] < noise_stats.Deref.floor[i])
                    signal[i] = noise_stats.Deref.floor[i];
            }

            gain = (Pointer<double>)ckd_alloc.ckd_calloc<double>(num_filts);
            for (i = 0; i < num_filts; i++)
            {
                if (signal[i] < noise_stats.Deref.max_gain * noise_stats.Deref.power[i])
                    gain[i] = signal[i] / noise_stats.Deref.power[i];
                else
                    gain[i] = noise_stats.Deref.max_gain;
                if (gain[i] < noise_stats.Deref.inv_max_gain)
                    gain[i] = noise_stats.Deref.inv_max_gain;
            }

            /* Weight smoothing and time frequency normalization */
            fe_weight_smooth(noise_stats, mfspec, gain, num_filts);

            ckd_alloc.ckd_free(gain);
            ckd_alloc.ckd_free(signal);
        }

        public static void fe_vad_hangover(Pointer<fe_t> fe, Pointer<float> feat, int is_speech, int store_pcm)
        {
            if (fe.Deref.vad_data.Deref.in_speech == 0)
            {
                fe_prespch_buf.fe_prespch_write_cep(fe.Deref.vad_data.Deref.prespch_buf, feat);
                if (store_pcm != 0)
                    fe_prespch_buf.fe_prespch_write_pcm(fe.Deref.vad_data.Deref.prespch_buf, fe.Deref.spch);
            }

            /* track vad state and deal with cepstrum prespeech buffer */
            if (is_speech != 0)
            {
                fe.Deref.vad_data.Deref.post_speech_frames = 0;
                if (fe.Deref.vad_data.Deref.in_speech == 0)
                {
                    fe.Deref.vad_data.Deref.pre_speech_frames++;
                    /* check for transition sil.Deref.speech */
                    if (fe.Deref.vad_data.Deref.pre_speech_frames >= fe.Deref.start_speech)
                    {
                        fe.Deref.vad_data.Deref.pre_speech_frames = 0;
                        fe.Deref.vad_data.Deref.in_speech = 1;
                    }
                }
            }
            else
            {
                fe.Deref.vad_data.Deref.pre_speech_frames = 0;
                if (fe.Deref.vad_data.Deref.in_speech != 0)
                {
                    fe.Deref.vad_data.Deref.post_speech_frames++;
                    /* check for transition speech.Deref.sil */
                    if (fe.Deref.vad_data.Deref.post_speech_frames >= fe.Deref.post_speech)
                    {
                        fe.Deref.vad_data.Deref.post_speech_frames = 0;
                        fe.Deref.vad_data.Deref.in_speech = 0;
                        fe_prespch_buf.fe_prespch_reset_cep(fe.Deref.vad_data.Deref.prespch_buf);
                        fe_prespch_buf.fe_prespch_reset_pcm(fe.Deref.vad_data.Deref.prespch_buf);
                    }
                }
            }
        }
    }
}
