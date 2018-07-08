using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe_interface
    {
        public static int fe_parse_general_params(Pointer<cmd_ln_t> config, Pointer<fe_t> fet)
        {
            int j, frate;

            fet.Deref.config = config;
            fet.Deref.sampling_rate = (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-samprate"));
            frate = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-frate"));
            if (frate > short.MaxValue || frate > fet.Deref.sampling_rate || frate < 1)
            {
                err.E_ERROR
                    (string.Format("Frame rate {0} can not be bigger than sample rate {1}\n",
                     frate, fet.Deref.sampling_rate));
                return -1;
            }

            fet.Deref.frame_rate = (short)frate;
            if (cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-dither")) != 0)
            {
                fet.Deref.dither = 1;
                fet.Deref.dither_seed = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-seed"));
            }
            fet.Deref.swap = cstring.strcmp(cstring.ToCString("little"), cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-input_endian"))) == 0 ? (byte)0 : (byte)1;
            fet.Deref.window_length = (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-wlen"));
            fet.Deref.pre_emphasis_alpha = (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-alpha"));

            fet.Deref.num_cepstra = (byte)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-ncep"));
            fet.Deref.fft_size = (short)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-nfft"));

            /* Check FFT size, compute FFT order (log_2(n)) */
            for (j = fet.Deref.fft_size, fet.Deref.fft_order = 0; j > 1; j >>= 1, fet.Deref.fft_order++)
            {
                if (((j % 2) != 0) || (fet.Deref.fft_size <= 0))
                {
                    err.E_ERROR(string.Format("fft: number of points must be a power of 2 (is {0})\n",
                            fet.Deref.fft_size));
                    return -1;
                }
            }
            /* Verify that FFT size is greater or equal to window length. */
            if (fet.Deref.fft_size < (int)(fet.Deref.window_length * fet.Deref.sampling_rate))
            {
                err.E_ERROR(string.Format("FFT: Number of points must be greater or equal to frame size ({0} samples)\n",
                        (int)(fet.Deref.window_length * fet.Deref.sampling_rate)));
                return -1;
            }

            fet.Deref.pre_speech = (short)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-vad_prespeech"));
            fet.Deref.post_speech = (short)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-vad_postspeech"));
            fet.Deref.start_speech = (short)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-vad_startspeech"));
            fet.Deref.vad_threshold = (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-vad_threshold"));

            fet.Deref.remove_dc = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-remove_dc"));
            fet.Deref.remove_noise = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-remove_noise"));
            fet.Deref.remove_silence = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-remove_silence"));

            if (0 == cstring.strcmp(cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-transform")), cstring.ToCString("dct")))
                fet.Deref.transform = fe.DCT_II;
            else if (0 == cstring.strcmp(cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-transform")), cstring.ToCString("legacy")))
                fet.Deref.transform = fe.LEGACY_DCT;
            else if (0 == cstring.strcmp(cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-transform")), cstring.ToCString("htk")))
                fet.Deref.transform = fe.DCT_HTK;
            else
            {
                err.E_ERROR("Invalid transform type (values are 'dct', 'legacy', 'htk')\n");
                return -1;
            }

            if (cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-logspec")) != 0)
                fet.Deref.log_spec = fe.RAW_LOG_SPEC;
            if (cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-smoothspec")) != 0)
                fet.Deref.log_spec = fe.SMOOTH_LOG_SPEC;

            return 0;
        }

        public static int fe_parse_melfb_params(Pointer<cmd_ln_t> config, Pointer<fe_t> fet, Pointer<melfb_t> mel)
        {
            mel.Deref.sampling_rate = fet.Deref.sampling_rate;
            mel.Deref.fft_size = fet.Deref.fft_size;
            mel.Deref.num_cepstra = fet.Deref.num_cepstra;
            mel.Deref.num_filters = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-nfilt"));

            if (fet.Deref.log_spec != 0)
                fet.Deref.feature_dimension = (byte)mel.Deref.num_filters;
            else
                fet.Deref.feature_dimension = fet.Deref.num_cepstra;

            mel.Deref.upper_filt_freq = (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-upperf"));
            mel.Deref.lower_filt_freq = (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-lowerf"));

            mel.Deref.doublewide = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-doublebw"));

            mel.Deref.warp_type = cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-warp_type"));
            mel.Deref.warp_params = cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-warp_params"));
            mel.Deref.lifter_val = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-lifter"));

            mel.Deref.unit_area = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-unit_area"));
            mel.Deref.round_filters = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-round_filters"));

            if (fe_warp.fe_warp_set(mel, mel.Deref.warp_type) != fe.FE_SUCCESS)
            {
                err.E_ERROR("Failed to initialize the warping function.\n");
                return -1;
            }
            fe_warp.fe_warp_set_parameters(mel, mel.Deref.warp_params, mel.Deref.sampling_rate);
            return 0;
        }

        public static Pointer<fe_t> fe_init_auto_r(Pointer<cmd_ln_t> config)
        {
            Pointer<fe_t> returnVal;
            int prespch_frame_len;

            returnVal = ckd_alloc.ckd_calloc_struct<fe_t>(1);
            returnVal.Deref.refcount = 1;

            /* transfer params to front end */
            if (fe_parse_general_params(cmd_ln.cmd_ln_retain(config), returnVal) < 0)
            {
                fe_free(returnVal);
                return PointerHelpers.NULL<fe_t>();
            }

            /* compute remaining fe parameters */
            /* We add 0.5 so approximate the float with the closest
             * integer. E.g., 2.3 is truncate to 2, whereas 3.7 becomes 4
             */
            returnVal.Deref.frame_shift = checked((short)(returnVal.Deref.sampling_rate / returnVal.Deref.frame_rate + 0.5));
            returnVal.Deref.frame_size = checked((short)(returnVal.Deref.window_length * returnVal.Deref.sampling_rate + 0.5));
            returnVal.Deref.pre_emphasis_prior = 0;

            fe_start_stream(returnVal);

            SphinxAssert.assert(returnVal.Deref.frame_shift > 1);

            if (returnVal.Deref.frame_size < returnVal.Deref.frame_shift)
            {
                err.E_ERROR
                    (string.Format("Frame size {0} (-wlen) must be greater than frame shift {1} (-frate)\n",
                     returnVal.Deref.frame_size, returnVal.Deref.frame_shift));
                fe_free(returnVal);
                return PointerHelpers.NULL<fe_t>();
            }


            if (returnVal.Deref.frame_size > (returnVal.Deref.fft_size))
            {
                err.E_ERROR
                    (string.Format("Number of FFT points has to be a power of 2 higher than {0}, it is {1}\n",
                     returnVal.Deref.frame_size, returnVal.Deref.fft_size));
                fe_free(returnVal);
                return PointerHelpers.NULL<fe_t>();
            }

            if (returnVal.Deref.dither != 0)
                fe_init_dither(returnVal.Deref.dither_seed);

            /* establish buffers for overflow samps and hamming window */
            returnVal.Deref.overflow_samps = ckd_alloc.ckd_calloc<short>(returnVal.Deref.frame_size);
            returnVal.Deref.hamming_window = ckd_alloc.ckd_calloc<double>(returnVal.Deref.frame_size / 2);

            /* create hamming window */
            fe_sigproc.fe_create_hamming(returnVal.Deref.hamming_window, returnVal.Deref.frame_size);

            /* init and fill appropriate filter structure */
            returnVal.Deref.mel_fb = ckd_alloc.ckd_calloc_struct<melfb_t>(1);

            /* transfer params to mel fb */
            fe_parse_melfb_params(config, returnVal, returnVal.Deref.mel_fb);

            if (returnVal.Deref.mel_fb.Deref.upper_filt_freq > returnVal.Deref.sampling_rate / 2 + 1.0)
            {
                err.E_ERROR(string.Format("Upper frequency {0} is higher than samprate/2 ({1})\n",
                    returnVal.Deref.mel_fb.Deref.upper_filt_freq, returnVal.Deref.sampling_rate / 2));
                fe_free(returnVal);
                return PointerHelpers.NULL<fe_t>();
            }

            fe_sigproc.fe_build_melfilters(returnVal.Deref.mel_fb);

            fe_sigproc.fe_compute_melcosine(returnVal.Deref.mel_fb);
            if (returnVal.Deref.remove_noise != 0 || returnVal.Deref.remove_silence != 0)
                returnVal.Deref.noise_stats = fe_noise.fe_init_noisestats(returnVal.Deref.mel_fb.Deref.num_filters);

            returnVal.Deref.vad_data = ckd_alloc.ckd_calloc_struct<vad_data_t>(1);
            prespch_frame_len = returnVal.Deref.log_spec != fe.RAW_LOG_SPEC ? returnVal.Deref.num_cepstra : returnVal.Deref.mel_fb.Deref.num_filters;
            returnVal.Deref.vad_data.Deref.prespch_buf = fe_prespch_buf.fe_prespch_init(returnVal.Deref.pre_speech + 1, prespch_frame_len, returnVal.Deref.frame_shift);

            /* Create temporary FFT, spectrum and mel-spectrum buffers. */
            /* FIXME: Gosh there are a lot of these. */
            returnVal.Deref.spch = ckd_alloc.ckd_calloc<short>(returnVal.Deref.frame_size);
            returnVal.Deref.frame = ckd_alloc.ckd_calloc<double>(returnVal.Deref.fft_size);
            returnVal.Deref.spec = ckd_alloc.ckd_calloc<double>(returnVal.Deref.fft_size);
            returnVal.Deref.mfspec = ckd_alloc.ckd_calloc<double>(returnVal.Deref.mel_fb.Deref.num_filters);

            /* create twiddle factors */
            returnVal.Deref.ccc = ckd_alloc.ckd_calloc<double>(returnVal.Deref.fft_size / 4);
            returnVal.Deref.sss = ckd_alloc.ckd_calloc<double>(returnVal.Deref.fft_size / 4);
            fe_sigproc.fe_create_twiddle(returnVal);

            // LOGAN removed
            //if (cmd_ln.cmd_ln_boolean_r(config, "-verbose")) {
            //    fe_print_current(fe);
            //}

            /*** Initialize the overflow buffers ***/
            fe_start_utt(returnVal);
            return returnVal;
        }

        public static void fe_init_dither(int seed)
        {
            err.E_INFO(string.Format("Using {0} as the seed.\n", seed));
            genrand.genrand_seed((uint)seed);
        }

        public static void fe_reset_vad_data(Pointer<vad_data_t> vad_data)
        {
            vad_data.Deref.in_speech = 0;
            vad_data.Deref.pre_speech_frames = 0;
            vad_data.Deref.post_speech_frames = 0;
            fe_prespch_buf.fe_prespch_reset_cep(vad_data.Deref.prespch_buf);
        }

        public static int fe_start_utt(Pointer<fe_t> fet)
        {
            fet.Deref.num_overflow_samps = 0;
            fet.Deref.overflow_samps.MemSet(0, fet.Deref.frame_size);
            fet.Deref.pre_emphasis_prior = 0;
            fe_reset_vad_data(fet.Deref.vad_data);
            return 0;
        }

        public static void fe_start_stream(Pointer<fe_t> fet)
        {
            fet.Deref.num_processed_samps = 0;
            fe_noise.fe_reset_noisestats(fet.Deref.noise_stats);
        }

        public static int fe_get_output_size(Pointer<fe_t> fet)
        {
            return (int)fet.Deref.feature_dimension;
        }

        public static byte fe_get_vad_state(Pointer<fe_t> fet)
        {
            return fet.Deref.vad_data.Deref.in_speech;
        }

        public static int fe_process_frames(Pointer<fe_t> fet,
                          BoxedValue<Pointer<short>> inout_spch,
                          BoxedValueInt inout_nsamps,
                          Pointer<Pointer<float>> buf_cep,
                          BoxedValueInt inout_nframes,
                          BoxedValueInt out_frameidx)
        {
            return fe_process_frames_ext(fet, inout_spch, inout_nsamps, buf_cep, inout_nframes, PointerHelpers.NULL<short>(), PointerHelpers.NULL<int>(), out_frameidx);
        }
   
        /**
         * Copy frames collected in prespeech buffer
         */
        public static int fe_copy_from_prespch(Pointer<fe_t> fet, BoxedValueInt inout_nframes, Pointer<Pointer<float>> buf_cep, int outidx)
        {
            while ((inout_nframes.Val) > 0 && fe_prespch_buf.fe_prespch_read_cep(fet.Deref.vad_data.Deref.prespch_buf, buf_cep[outidx]) > 0)
            {
                outidx++;
                (inout_nframes.Val)--;
            }
            return outidx;
        }

        /**
         * Update pointers after we processed a frame. A complex logic used in two places in fe_process_frames
         */
        public static int fe_check_prespeech(Pointer<fe_t> fet, BoxedValueInt inout_nframes, Pointer<Pointer<float>> buf_cep, int outidx, BoxedValueInt out_frameidx, BoxedValueInt inout_nsamps, int orig_nsamps)
        {
            if (fet.Deref.vad_data.Deref.in_speech != 0)
            {
                if (fe_prespch_buf.fe_prespch_ncep(fet.Deref.vad_data.Deref.prespch_buf) > 0)
                {
                    /* Previous frame triggered vad into speech state. Last frame is in the end of 
                       prespeech buffer, so overwrite it */
                    outidx = fe_copy_from_prespch(fet, inout_nframes, buf_cep, outidx);

                    /* Sets the start frame for the returned data so that caller can update timings */
                    if (out_frameidx != null)
                    {
                        out_frameidx.Val = checked((int)(fet.Deref.num_processed_samps + orig_nsamps - inout_nsamps.Val) / fet.Deref.frame_shift - fet.Deref.pre_speech);
                    }
                }
                else
                {
                    outidx++;
                    (inout_nframes.Val)--;
                }
            }
            /* Amount of data behind the original input which is still needed. */
            if (fet.Deref.num_overflow_samps > 0)
                fet.Deref.num_overflow_samps -= fet.Deref.frame_shift;

            return outidx;
        }

        public static int fe_process_frames_ext(Pointer<fe_t> fet,
                          BoxedValue<Pointer<short>> inout_spch,
                          BoxedValueInt inout_nsamps,
                          Pointer<Pointer<float>> buf_cep,
                          BoxedValueInt inout_nframes,
                          Pointer<short> voiced_spch,
                          Pointer<int> voiced_spch_nsamps,
                          BoxedValueInt out_frameidx)
        {
            int outidx, n_overflow, orig_n_overflow;
            Pointer<short> orig_spch;
            int orig_nsamps;

            /* The logic here is pretty complex, please be careful with modifications */

            /* FIXME: Dump PCM data if needed */

            /* In the special case where there is no output buffer, return the
             * maximum number of frames which would be generated. */
            if (buf_cep.IsNull)
            {
                if (inout_nsamps.Val + fet.Deref.num_overflow_samps < (uint)fet.Deref.frame_size)
                    inout_nframes.Val = 0;
                else
                    inout_nframes.Val = (1 + ((inout_nsamps.Val + fet.Deref.num_overflow_samps - fet.Deref.frame_size)
                           / fet.Deref.frame_shift));
                if (fet.Deref.vad_data.Deref.in_speech == 0)
                    inout_nframes.Val += fe_prespch_buf.fe_prespch_ncep(fet.Deref.vad_data.Deref.prespch_buf);
                return inout_nframes.Val;
            }

            if (out_frameidx != null)
                out_frameidx.Val = 0;

            /* Are there not enough samples to make at least 1 frame? */
            if (inout_nsamps.Val + fet.Deref.num_overflow_samps < (uint)fet.Deref.frame_size)
            {
                if (inout_nsamps.Val > 0)
                {
                    /* Append them to the overflow buffer. */
                    inout_spch.Val.MemCopyTo(fet.Deref.overflow_samps + fet.Deref.num_overflow_samps, (int)inout_nsamps.Val);
                    fet.Deref.num_overflow_samps = checked((short)(fet.Deref.num_overflow_samps + inout_nsamps.Val));
                    fet.Deref.num_processed_samps = checked((uint)(fet.Deref.num_processed_samps + inout_nsamps.Val));
                    inout_spch.Val += inout_nsamps.Val;
                    inout_nsamps.Val = 0;
                }
                /* We produced no frames of output, sorry! */
                inout_nframes.Val = 0;
                return 0;
            }

            /* Can't write a frame?  Then do nothing! */
            if (inout_nframes.Val < 1)
            {
                inout_nframes.Val = 0;
                return 0;
            }

            /* Index of output frame. */
            outidx = 0;

            /* Try to read from prespeech buffer */
            if (fet.Deref.vad_data.Deref.in_speech != 0 && fe_prespch_buf.fe_prespch_ncep(fet.Deref.vad_data.Deref.prespch_buf) > 0)
            {
                outidx = fe_copy_from_prespch(fet, inout_nframes, buf_cep, outidx);
                if ((inout_nframes.Val) < 1)
                {
                    /* mfcc buffer is filled from prespeech buffer */
                    inout_nframes.Val = outidx;
                    return 0;
                }
            }

            /* Keep track of the original start of the buffer. */
            orig_spch = inout_spch.Val;
            orig_nsamps = inout_nsamps.Val;
            orig_n_overflow = fet.Deref.num_overflow_samps;

            /* Start processing, taking care of any incoming overflow. */
            if (fet.Deref.num_overflow_samps > 0)
            {
                int offset = fet.Deref.frame_size - fet.Deref.num_overflow_samps;
                /* Append start of spch to overflow samples to make a full frame. */
                inout_spch.Val.MemCopyTo(fet.Deref.overflow_samps + fet.Deref.num_overflow_samps, offset);
                fe_sigproc.fe_read_frame(fet, fet.Deref.overflow_samps, fet.Deref.frame_size);
                /* Update input-output pointers and counters. */
                inout_spch.Val += offset;
                inout_nsamps.Val = inout_nsamps.Val - offset;
            }
            else
            {
                fe_sigproc.fe_read_frame(fet, inout_spch.Val, fet.Deref.frame_size);
                /* Update input-output pointers and counters. */
                inout_spch.Val += fet.Deref.frame_size;
                inout_nsamps.Val = inout_nsamps.Val - fet.Deref.frame_size;
            }

            fe_sigproc.fe_write_frame(fet, buf_cep[outidx], voiced_spch.IsNonNull ? 1 : 0);
            outidx = fe_check_prespeech(fet, inout_nframes, buf_cep, outidx, out_frameidx, inout_nsamps, orig_nsamps);

            /* Process all remaining frames. */
            while (inout_nframes.Val > 0 && inout_nsamps.Val >= (uint)fet.Deref.frame_shift)
            {
                fe_sigproc.fe_shift_frame(fet, inout_spch.Val, fet.Deref.frame_shift);
                fe_sigproc.fe_write_frame(fet, buf_cep[outidx], voiced_spch.IsNonNull ? 1 : 0);

                outidx = fe_check_prespeech(fet, inout_nframes, buf_cep, outidx, out_frameidx, inout_nsamps, orig_nsamps);

                /* Update input-output pointers and counters. */
                inout_spch.Val += fet.Deref.frame_shift;
                inout_nsamps.Val -= fet.Deref.frame_shift;
            }

            /* How many relevant overflow samples are there left? */
            if (fet.Deref.num_overflow_samps <= 0)
            {
                /* Maximum number of overflow samples past *inout_spch to save. */
                n_overflow = inout_nsamps.Val;
                if (n_overflow > fet.Deref.frame_shift)
                    n_overflow = fet.Deref.frame_shift;
                fet.Deref.num_overflow_samps = checked((short)(fet.Deref.frame_size - fet.Deref.frame_shift));
                /* Make sure this isn't an illegal read! */
                if (fet.Deref.num_overflow_samps > inout_spch.Val - orig_spch)
                    fet.Deref.num_overflow_samps = checked((short)(inout_spch.Val - orig_spch));
                fet.Deref.num_overflow_samps = checked((short)(fet.Deref.num_overflow_samps + n_overflow));
                if (fet.Deref.num_overflow_samps > 0)
                {
                    (inout_spch.Val - (fet.Deref.frame_size - fet.Deref.frame_shift)).MemCopyTo(fet.Deref.overflow_samps, fet.Deref.num_overflow_samps);
                    /* Update the input pointer to cover this stuff. */
                    inout_spch.Val += n_overflow;
                    inout_nsamps.Val -= n_overflow;
                }
            }
            else
            {
                /* There is still some relevant data left in the overflow buffer. */
                /* Shift existing data to the beginning. */
                (fet.Deref.overflow_samps + orig_n_overflow - fet.Deref.num_overflow_samps).MemMove(fet.Deref.num_overflow_samps - orig_n_overflow, fet.Deref.num_overflow_samps);
                // LOGAN TODO Check this memmove!
                //memmove(fet.Deref.overflow_samps, fet.Deref.overflow_samps + orig_n_overflow - fet.Deref.num_overflow_samps,  * sizeof(*fet.Deref.overflow_samps));
                /* Copy in whatever we had in the original speech buffer. */
                n_overflow = inout_spch.Val - orig_spch + inout_nsamps.Val;
                if (n_overflow > fet.Deref.frame_size - fet.Deref.num_overflow_samps)
                    n_overflow = fet.Deref.frame_size - fet.Deref.num_overflow_samps;
                orig_spch.MemCopyTo(fet.Deref.overflow_samps + fet.Deref.num_overflow_samps, n_overflow);
                fet.Deref.num_overflow_samps = checked((short)(fet.Deref.num_overflow_samps + n_overflow));
                /* Advance the input pointers. */
                if (n_overflow > inout_spch.Val - orig_spch)
                {
                    n_overflow -= (inout_spch.Val - orig_spch);
                    inout_spch.Val += n_overflow;
                    inout_nsamps.Val -= n_overflow;
                }
            }

            /* Finally update the frame counter with the number of frames
             * and global sample counter with number of samples we procesed */
            inout_nframes.Val = outidx; /* FIXME: Not sure why I wrote it this way... */
            fet.Deref.num_processed_samps = checked((uint)(fet.Deref.num_processed_samps + (orig_nsamps - inout_nsamps.Val)));

            return 0;
        }

        public static int fe_end_utt(Pointer<fe_t> fet, Pointer<float> cepvector, BoxedValueInt nframes)
        {
            /* Process any remaining data, not very accurate for the VAD */
            nframes.Val = 0;
            if (fet.Deref.num_overflow_samps > 0)
            {
                fe_sigproc.fe_read_frame(fet, fet.Deref.overflow_samps, fet.Deref.num_overflow_samps);
                fe_sigproc.fe_write_frame(fet, cepvector, 0);
                if (fet.Deref.vad_data.Deref.in_speech != 0)
                    nframes.Val = 1;
            }

            /* reset overflow buffers... */
            fet.Deref.num_overflow_samps = 0;

            return 0;
        }

        public static Pointer<fe_t> fe_retain(Pointer<fe_t> fe)
        {
            ++fe.Deref.refcount;
            return fe;
        }

        public static int fe_free(Pointer<fe_t> fet)
        {
            if (fet.IsNull)
                return 0;
            if (--fet.Deref.refcount > 0)
                return fet.Deref.refcount;

            /* kill FE instance - free everything... */
            if (fet.Deref.mel_fb.IsNonNull)
            {
                if (fet.Deref.mel_fb.Deref.mel_cosine.IsNonNull)
                {
                    fe_sigproc.fe_free_2d(fet.Deref.mel_fb.Deref.mel_cosine);
                }

                ckd_alloc.ckd_free(fet.Deref.mel_fb.Deref.lifter);
                ckd_alloc.ckd_free(fet.Deref.mel_fb.Deref.spec_start);
                ckd_alloc.ckd_free(fet.Deref.mel_fb.Deref.filt_start);
                ckd_alloc.ckd_free(fet.Deref.mel_fb.Deref.filt_width);
                ckd_alloc.ckd_free(fet.Deref.mel_fb.Deref.filt_coeffs);
                ckd_alloc.ckd_free(fet.Deref.mel_fb);
            }
            ckd_alloc.ckd_free(fet.Deref.spch);
            ckd_alloc.ckd_free(fet.Deref.frame);
            ckd_alloc.ckd_free(fet.Deref.ccc);
            ckd_alloc.ckd_free(fet.Deref.sss);
            ckd_alloc.ckd_free(fet.Deref.spec);
            ckd_alloc.ckd_free(fet.Deref.mfspec);
            ckd_alloc.ckd_free(fet.Deref.overflow_samps);
            ckd_alloc.ckd_free(fet.Deref.hamming_window);

            if (fet.Deref.noise_stats.IsNonNull)
                fe_noise.fe_free_noisestats(fet.Deref.noise_stats);

            if (fet.Deref.vad_data.IsNonNull)
            {
                fe_prespch_buf.fe_prespch_free(fet.Deref.vad_data.Deref.prespch_buf);
                ckd_alloc.ckd_free(fet.Deref.vad_data);
            }

            cmd_ln.cmd_ln_free_r(fet.Deref.config);
            ckd_alloc.ckd_free(fet);

            return 0;
        }
    }
}
