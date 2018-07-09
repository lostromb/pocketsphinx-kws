using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe_sigproc
    {
        public const double LOG_FLOOR = 1e-4;

        public static float
        fe_mel(Pointer<melfb_t> mel, float x)
        {
            float warped = fe_warp.fe_warp_unwarped_to_warped(mel, x);

            return (float)(2595.0 * Math.Log10(1.0 + warped / 700.0));
        }

        public static float
        fe_melinv(Pointer<melfb_t> mel, float x)
        {
            float warped = (float)(700.0 * (Math.Pow(10.0, x / 2595.0) - 1.0));
            return fe_warp.fe_warp_warped_to_unwarped(mel, warped);
        }

        public static int
        fe_build_melfilters(Pointer<melfb_t> mel_fb)
        {
            float melmin, melmax, melbw, fftfreq;
            int n_coeffs, i, j;


            /* Filter coefficient matrix, in flattened form. */
            mel_fb.Deref.spec_start =
                ckd_alloc.ckd_calloc<short>(mel_fb.Deref.num_filters);
            mel_fb.Deref.filt_start =
                ckd_alloc.ckd_calloc<short>(mel_fb.Deref.num_filters);
            mel_fb.Deref.filt_width =
                ckd_alloc.ckd_calloc<short>(mel_fb.Deref.num_filters);

            /* First calculate the widths of each filter. */
            /* Minimum and maximum frequencies in mel scale. */
            melmin = fe_mel(mel_fb, mel_fb.Deref.lower_filt_freq);
            melmax = fe_mel(mel_fb, mel_fb.Deref.upper_filt_freq);

            /* Width of filters in mel scale */
            melbw = (melmax - melmin) / (mel_fb.Deref.num_filters + 1);
            if (mel_fb.Deref.doublewide != 0)
            {
                melmin -= melbw;
                melmax += melbw;
                if ((fe_melinv(mel_fb, melmin) < 0) ||
                    (fe_melinv(mel_fb, melmax) > mel_fb.Deref.sampling_rate / 2))
                {
                    err.E_WARN
                        (string.Format("Out of Range: low  filter edge = {0} ({1})\n",
                            fe_melinv(mel_fb, melmin), 0.0));
                    err.E_WARN
                        (string.Format("              high filter edge = {0} ({1})\n",
                            fe_melinv(mel_fb, melmax), mel_fb.Deref.sampling_rate / 2));
                    return fe.FE_INVALID_PARAM_ERROR;
                }
            }

            /* DFT point spacing */
            fftfreq = mel_fb.Deref.sampling_rate / (float)mel_fb.Deref.fft_size;

            /* Count and place filter coefficients. */
            n_coeffs = 0;
            for (i = 0; i < mel_fb.Deref.num_filters; ++i)
            {
                float[] freqs = new float[3];

                /* Left, center, right frequencies in Hertz */
                for (j = 0; j < 3; ++j)
                {
                    if (mel_fb.Deref.doublewide != 0)
                        freqs[j] = fe_melinv(mel_fb, (i + j * 2) * melbw + melmin);
                    else
                        freqs[j] = fe_melinv(mel_fb, (i + j) * melbw + melmin);
                    /* Round them to DFT points if requested */
                    if (mel_fb.Deref.round_filters != 0)
                        freqs[j] = ((int)(freqs[j] / fftfreq + 0.5)) * fftfreq;
                }

                /* spec_start is the start of this filter in the power spectrum. */
                mel_fb.Deref.spec_start[i] = -1;
                /* There must be a better way... */
                for (j = 0; j < mel_fb.Deref.fft_size / 2 + 1; ++j)
                {
                    float hz = j * fftfreq;
                    if (hz < freqs[0])
                        continue;
                    else if (hz > freqs[2] || j == mel_fb.Deref.fft_size / 2)
                    {
                        /* filt_width is the width in DFT points of this filter. */
                        mel_fb.Deref.filt_width[i] = checked((short)(j - mel_fb.Deref.spec_start[i]));
                        /* filt_start is the start of this filter in the filt_coeffs array. */
                        mel_fb.Deref.filt_start[i] = checked((short)n_coeffs);
                        n_coeffs += mel_fb.Deref.filt_width[i];
                        break;
                    }
                    if (mel_fb.Deref.spec_start[i] == -1)
                        mel_fb.Deref.spec_start[i] = checked((short)j);
                }
            }

            /* Now go back and allocate the coefficient array. */
            mel_fb.Deref.filt_coeffs =
                ckd_alloc.ckd_malloc<float>(n_coeffs);

            /* And now generate the coefficients. */
            n_coeffs = 0;
            for (i = 0; i < mel_fb.Deref.num_filters; ++i)
            {
                float[] freqs = new float[3];

                /* Left, center, right frequencies in Hertz */
                for (j = 0; j < 3; ++j)
                {
                    if (mel_fb.Deref.doublewide != 0)
                        freqs[j] = fe_melinv(mel_fb, (i + j * 2) * melbw + melmin);
                    else
                        freqs[j] = fe_melinv(mel_fb, (i + j) * melbw + melmin);
                    /* Round them to DFT points if requested */
                    if (mel_fb.Deref.round_filters != 0)
                        freqs[j] = ((int)(freqs[j] / fftfreq + 0.5)) * fftfreq;
                }

                for (j = 0; j < mel_fb.Deref.filt_width[i]; ++j)
                {
                    float hz, loslope, hislope;

                    hz = (mel_fb.Deref.spec_start[i] + j) * fftfreq;
                    if (hz < freqs[0] || hz > freqs[2])
                    {
                        err.E_FATAL
                            (string.Format("Failed to create filterbank, frequency range does not match. \n" +
                                "Sample rate {0}, FFT size {1}, lowerf {2} < freq {3} > upperf {4}.\n",
                                mel_fb.Deref.sampling_rate, mel_fb.Deref.fft_size, freqs[0], hz,
                                freqs[2]));
                    }
                    loslope = (hz - freqs[0]) / (freqs[1] - freqs[0]);
                    hislope = (freqs[2] - hz) / (freqs[2] - freqs[1]);
                    if (mel_fb.Deref.unit_area != 0)
                    {
                        loslope *= 2 / (freqs[2] - freqs[0]);
                        hislope *= 2 / (freqs[2] - freqs[0]);
                    }
                    if (loslope < hislope)
                    {
                        mel_fb.Deref.filt_coeffs[n_coeffs] = loslope;
                    }
                    else
                    {
                        mel_fb.Deref.filt_coeffs[n_coeffs] = hislope;
                    }
                    ++n_coeffs;
                }
            }

            return fe.FE_SUCCESS;
        }

        public static int fe_compute_melcosine(Pointer<melfb_t> mel_fb)
        {
            double freqstep;
            int i, j;

            mel_fb.Deref.mel_cosine =
                ckd_alloc.ckd_calloc_2d<float>((uint)mel_fb.Deref.num_cepstra,
                                            (uint)mel_fb.Deref.num_filters);

            freqstep = 3.1415926535897932385e0 / mel_fb.Deref.num_filters;
            /* NOTE: The first row vector is actually unnecessary but we leave
                * it in to avoid confusion. */
            for (i = 0; i < mel_fb.Deref.num_cepstra; i++)
            {
                for (j = 0; j < mel_fb.Deref.num_filters; j++)
                {
                    double cosine;
                    cosine = Math.Cos(freqstep * i * (j + 0.5));
                    mel_fb.Deref.mel_cosine[i].Set(j, (float)(cosine));
                }
            }

            /* Also precompute normalization constants for unitary DCT. */
            mel_fb.Deref.sqrt_inv_n = (float)(Math.Sqrt(1.0 / mel_fb.Deref.num_filters));
            mel_fb.Deref.sqrt_inv_2n = (float)(Math.Sqrt(2.0 / mel_fb.Deref.num_filters));

            /* And liftering weights */
            if (mel_fb.Deref.lifter_val != 0)
            {
                mel_fb.Deref.lifter = ckd_alloc.ckd_calloc<float>(mel_fb.Deref.num_cepstra);
                for (i = 0; i < mel_fb.Deref.num_cepstra; ++i)
                {
                    mel_fb.Deref.lifter[i] = (float)(1 + mel_fb.Deref.lifter_val / 2
                                                    * Math.Sin(i * 3.1415926535897932385e0 /
                                                            mel_fb.Deref.lifter_val));
                }
            }

            return (0);
        }

        public static void fe_pre_emphasis(Pointer<short> input, Pointer<double> output, int len,
                float factor, short prior)
        {
            int i;

            output[0] = (double)input[0] - (double)prior * factor;
            for (i = 1; i < len; i++)
                output[i] = (double)input[i] - (double)input[i - 1] * factor;
        }

        public static void
            fe_short_to_frame(Pointer<short> input, Pointer<double> output, int len)
        {
            int i;
            for (i = 0; i < len; i++)
                output[i] = (double)input[i];
        }

        public static void fe_create_hamming(Pointer<double> input, int in_len)
        {
            int i;

            /* Symmetric, so we only create the first half of it. */
            for (i = 0; i < in_len / 2; i++)
            {
                double hamm;
                hamm = (0.54 - 0.46 * Math.Cos(2 * 3.1415926535897932385e0 * i /
                                            ((double)in_len - 1.0)));
                input[i] = (hamm);
            }
        }

        public static void fe_hamming_window(Pointer<double> input, Pointer<double> window, int in_len,
                            int remove_dc)
        {
            int i;

            if (remove_dc != 0)
            {
                double mean = 0;

                for (i = 0; i < in_len; i++)
                    mean += input[i];
                mean /= in_len;
                for (i = 0; i < in_len; i++)
                    input[i] -= (double)mean;
            }

            for (i = 0; i < in_len / 2; i++)
            {
                input[i] = (input[i] * window[i]);
                input[in_len - 1 - i] = (input[in_len - 1 - i] * window[i]);
            }
        }

        public static int fe_spch_to_frame(Pointer<fe_t> fe, int len)
        {
            /* Copy to the frame buffer. */
            if (fe.Deref.pre_emphasis_alpha != 0.0)
            {
                fe_pre_emphasis(fe.Deref.spch, fe.Deref.frame, len,
                                fe.Deref.pre_emphasis_alpha, fe.Deref.pre_emphasis_prior);
                if (len >= fe.Deref.frame_shift)
                    fe.Deref.pre_emphasis_prior = fe.Deref.spch[fe.Deref.frame_shift - 1];
                else
                    fe.Deref.pre_emphasis_prior = fe.Deref.spch[len - 1];
            }
            else
                fe_short_to_frame(fe.Deref.spch, fe.Deref.frame, len);

            /* Zero pad up to FFT size. */
            fe.Deref.frame.Point(len).MemSet(0, (fe.Deref.fft_size - len));

            /* Window. */
            fe_hamming_window(fe.Deref.frame, fe.Deref.hamming_window, fe.Deref.frame_size,
                                fe.Deref.remove_dc);

            return len;
        }

        public static int fe_read_frame(Pointer<fe_t> fe, Pointer<short> input, int len)
        {
            int i;

            if (len > fe.Deref.frame_size)
                len = fe.Deref.frame_size;

            /* Read it into the raw speech buffer. */
            input.MemCopyTo(fe.Deref.spch, len);
            /* Swap and dither if necessary. */
            if (fe.Deref.swap != 0)
                for (i = 0; i < len; ++i)
                    byteorder.SWAP_INT16(fe.Deref.spch.Point(i));
            if (fe.Deref.dither != 0)
                for (i = 0; i < len; ++i)
                    fe.Deref.spch[i] += (short)(((genrand.genrand_int31() % 4) == 0) ? 1 : 0);

            return fe_spch_to_frame(fe, len);
        }

        public static int fe_shift_frame(Pointer<fe_t> fe, Pointer<short> input, int len)
        {
            int offset, i;

            if (len > fe.Deref.frame_shift)
                len = fe.Deref.frame_shift;
            offset = fe.Deref.frame_size - fe.Deref.frame_shift;

            /* Shift data into the raw speech buffer. */
            fe.Deref.spch.Point(fe.Deref.frame_shift).MemMove(0 - fe.Deref.frame_shift, offset); // TODO check the order of memmove
            //memmove(fe.Deref.spch, fe.Deref.spch + fe.Deref.frame_shift, offset * sizeof(*fe.Deref.spch));
            input.MemCopyTo(fe.Deref.spch + offset, len);
            /* Swap and dither if necessary. */
            if (fe.Deref.swap != 0)
                for (i = 0; i < len; ++i)
                    byteorder.SWAP_INT16(fe.Deref.spch.Point(offset + i));
            if (fe.Deref.dither != 0)
                for (i = 0; i < len; ++i)
                    fe.Deref.spch[offset + i]
                        += (short)(((genrand.genrand_int31() % 4) == 0) ? 1 : 0);

            return fe_spch_to_frame(fe, offset + len);
        }

        /**
            * Create arrays of twiddle factors.
            */
        public static void fe_create_twiddle(Pointer<fe_t> fe)
        {
            int i;

            for (i = 0; i < fe.Deref.fft_size / 4; ++i)
            {
                double a = 2 * 3.1415926535897932385e0 * i / fe.Deref.fft_size;
                fe.Deref.ccc[i] = Math.Cos(a);
                fe.Deref.sss[i] = Math.Sin(a);
            }
        }


        public static int fe_fft_real(Pointer<fe_t> fe)
        {
            int i, j, k, m, n;
            Pointer<double> x;
            double xt;

            x = fe.Deref.frame;
            m = fe.Deref.fft_order;
            n = fe.Deref.fft_size;

            /* Bit-reverse the input. */
            j = 0;
            for (i = 0; i < n - 1; ++i)
            {
                if (i < j)
                {
                    xt = x[j];
                    x[j] = x[i];
                    x[i] = xt;
                }
                k = n / 2;
                while (k <= j)
                {
                    j -= k;
                    k /= 2;
                }
                j += k;
            }

            /* Basic butterflies (2-point FFT, real twiddle factors):
                * x[i]   = x[i] +  1 * x[i+1]
                * x[i+1] = x[i] + -1 * x[i+1]
                */
            for (i = 0; i < n; i += 2)
            {
                xt = x[i];
                x[i] = (xt + x[i + 1]);
                x[i + 1] = (xt - x[i + 1]);
            }

            /* The rest of the butterflies, in stages from 1..m */
            for (k = 1; k < m; ++k)
            {
                int n1, n2, n4;

                n4 = k - 1;
                n2 = k;
                n1 = k + 1;
                /* Stride over each (1 << (k+1)) points */
                for (i = 0; i < n; i += (1 << n1))
                {
                    /* Basic butterfly with real twiddle factors:
                        * x[i]          = x[i] +  1 * x[i + (1<<k)]
                        * x[i + (1<<k)] = x[i] + -1 * x[i + (1<<k)]
                        */
                    xt = x[i];
                    x[i] = (xt + x[i + (1 << n2)]);
                    x[i + (1 << n2)] = (xt - x[i + (1 << n2)]);

                    /* The other ones with real twiddle factors:
                        * x[i + (1<<k) + (1<<(k-1))]
                        *   = 0 * x[i + (1<<k-1)] + -1 * x[i + (1<<k) + (1<<k-1)]
                        * x[i + (1<<(k-1))]
                        *   = 1 * x[i + (1<<k-1)] +  0 * x[i + (1<<k) + (1<<k-1)]
                        */
                    x[i + (1 << n2) + (1 << n4)] = -x[i + (1 << n2) + (1 << n4)];
                    x[i + (1 << n4)] = x[i + (1 << n4)];

                    /* Butterflies with complex twiddle factors.
                        * There are (1<<k-1) of them.
                        */
                    for (j = 1; j < (1 << n4); ++j)
                    {
                        double cc, ss, t1, t2;
                        int i1, i2, i3, i4;

                        i1 = i + j;
                        i2 = i + (1 << n2) - j;
                        i3 = i + (1 << n2) + j;
                        i4 = i + (1 << n2) + (1 << n2) - j;

                        /*
                            * cc = real(W[j * n / (1<<(k+1))])
                            * ss = imag(W[j * n / (1<<(k+1))])
                            */
                        cc = fe.Deref.ccc[j << (m - n1)];
                        ss = fe.Deref.sss[j << (m - n1)];

                        /* There are some symmetry properties which allow us
                            * to get away with only four multiplications here. */
                        t1 = (x[i3] * cc) + (x[i4] * ss);
                        t2 = (x[i3] * ss) - (x[i4] * cc);

                        x[i4] = (x[i2] - t2);
                        x[i3] = (-x[i2] - t2);
                        x[i2] = (x[i1] - t1);
                        x[i1] = (x[i1] + t1);
                    }
                }
            }

            /* This isn't used, but return it for completeness. */
            return m;
        }

        public static void fe_spec_magnitude(Pointer<fe_t> fe)
        {
            Pointer<double> fft;
            Pointer<double> spec;
            int j, scale, fftsize;

            /* Do FFT and get the scaling factor back (only actually used in
                * fixed-point).  Note the scaling factor is expressed in bits. */
            scale = fe_fft_real(fe);

            /* Convenience pointers to make things less awkward below. */
            fft = fe.Deref.frame;
            spec = fe.Deref.spec;
            fftsize = fe.Deref.fft_size;

            /* We need to scale things up the rest of the way to N. */
            scale = fe.Deref.fft_order - scale;

            /* The first point (DC coefficient) has no imaginary part */
            {
                spec[0] = fft[0] * fft[0];
            }

            for (j = 1; j <= fftsize / 2; j++)
            {
                spec[j] = fft[j] * fft[j] + fft[fftsize - j] * fft[fftsize - j];
            }
        }

        public static void fe_mel_spec(Pointer<fe_t> fe)
        {
            int whichfilt;
            Pointer<double> spec;
            Pointer<double> mfspec;

            /* Convenience poitners. */
            spec = fe.Deref.spec;
            mfspec = fe.Deref.mfspec;
            for (whichfilt = 0; whichfilt < fe.Deref.mel_fb.Deref.num_filters; whichfilt++)
            {
                int spec_start, filt_start, i;

                spec_start = fe.Deref.mel_fb.Deref.spec_start[whichfilt];
                filt_start = fe.Deref.mel_fb.Deref.filt_start[whichfilt];

                mfspec[whichfilt] = 0;
                for (i = 0; i < fe.Deref.mel_fb.Deref.filt_width[whichfilt]; i++)
                    mfspec[whichfilt] +=
                        spec[spec_start + i] * fe.Deref.mel_fb.Deref.filt_coeffs[filt_start +
                                                                        i];
            }

        }

        public static void fe_mel_cep(Pointer<fe_t> fe_, Pointer<float> mfcep)
        {
            int i;
            Pointer<double> mfspec;

            /* Convenience pointer. */
            mfspec = fe_.Deref.mfspec;

            for (i = 0; i < fe_.Deref.mel_fb.Deref.num_filters; ++i)
            {
                mfspec[i] = Math.Log(mfspec[i] + LOG_FLOOR);
            }

            /* If we are doing LOG_SPEC, then do nothing. */
            if (fe_.Deref.log_spec == fe.RAW_LOG_SPEC)
            {
                for (i = 0; i < fe_.Deref.feature_dimension; i++)
                {
                    mfcep[i] = (float)mfspec[i];
                }
            }
            /* For smoothed spectrum, do DCT-II followed by (its inverse) DCT-III */
            else if (fe_.Deref.log_spec == fe.SMOOTH_LOG_SPEC)
            {
                /* FIXME: This is probably broken for fixed-point. */
                fe_dct2(fe_, mfspec, mfcep, 0);
                fe_dct3(fe_, mfcep, mfspec);
                for (i = 0; i < fe_.Deref.feature_dimension; i++)
                {
                    mfcep[i] = (float)mfspec[i];
                }
            }
            else if (fe_.Deref.transform == fe.DCT_II)
                fe_dct2(fe_, mfspec, mfcep, 0);
            else if (fe_.Deref.transform == fe.DCT_HTK)
                fe_dct2(fe_, mfspec, mfcep, 1);
            else
                fe_spec2cep(fe_, mfspec, mfcep);

            return;
        }

        public static void fe_spec2cep(Pointer<fe_t> fe, Pointer<double> mflogspec, Pointer<float> mfcep)
        {
            int i, j, beta;

            /* Compute C0 separately (its basis vector is 1) to avoid
                * costly multiplications. */
            mfcep[0] = (float)(mflogspec[0] / 2);        /* beta = 0.5 */
            for (j = 1; j < fe.Deref.mel_fb.Deref.num_filters; j++)
                mfcep[0] += (float)mflogspec[j];       /* beta = 1.0 */
            mfcep[0] /= (float)fe.Deref.mel_fb.Deref.num_filters;

            for (i = 1; i < fe.Deref.num_cepstra; ++i)
            {
                mfcep[i] = 0;
                for (j = 0; j < fe.Deref.mel_fb.Deref.num_filters; j++)
                {
                    if (j == 0)
                        beta = 1;       /* 0.5 */
                    else
                        beta = 2;       /* 1.0 */
                    mfcep[i] += (float)(mflogspec[j] *
                                        fe.Deref.mel_fb.Deref.mel_cosine[i][j]) * beta;
                }
                /* Note that this actually normalizes by num_filters, like the
                    * original Sphinx front-end, due to the doubled 'beta' factor
                    * above.  */
                mfcep[i] /= (float)fe.Deref.mel_fb.Deref.num_filters * 2;
            }
        }

        public static void fe_dct2(Pointer<fe_t> fe, Pointer<double> mflogspec, Pointer<float> mfcep, int htk)
        {
            int i, j;

            /* Compute C0 separately (its basis vector is 1) to avoid
                * costly multiplications. */
            mfcep[0] = (float)mflogspec[0];
            for (j = 1; j < fe.Deref.mel_fb.Deref.num_filters; j++)
                mfcep[0] += (float)mflogspec[j];
            if (htk != 0)
                mfcep[0] = (mfcep[0] * fe.Deref.mel_fb.Deref.sqrt_inv_2n);
            else                        /* sqrt(1/N) = sqrt(2/N) * 1/sqrt(2) */
                mfcep[0] = (mfcep[0] * fe.Deref.mel_fb.Deref.sqrt_inv_n);

            for (i = 1; i < fe.Deref.num_cepstra; ++i)
            {
                mfcep[i] = 0;
                for (j = 0; j < fe.Deref.mel_fb.Deref.num_filters; j++)
                {
                    mfcep[i] += (float)(mflogspec[j] * fe.Deref.mel_fb.Deref.mel_cosine[i][j]);
                }
                mfcep[i] = (mfcep[i] * fe.Deref.mel_fb.Deref.sqrt_inv_2n);
            }
        }

        public static void fe_lifter(Pointer<fe_t> fe, Pointer<float> mfcep)
        {
            int i;

            if (fe.Deref.mel_fb.Deref.lifter_val == 0)
                return;

            for (i = 0; i < fe.Deref.num_cepstra; ++i)
            {
                mfcep[i] = (mfcep[i] * fe.Deref.mel_fb.Deref.lifter[i]);
            }
        }

        public static void fe_dct3(Pointer<fe_t> fe_, Pointer<float> mfcep, Pointer<double> mflogspec)
        {
            int i, j;

            for (i = 0; i < fe_.Deref.mel_fb.Deref.num_filters; ++i)
            {
                mflogspec[i] = (mfcep[0] * fe.SQRT_HALF);
                for (j = 1; j < fe_.Deref.num_cepstra; j++)
                {
                    mflogspec[i] += (mfcep[j] * fe_.Deref.mel_fb.Deref.mel_cosine[j][i]);
                }
                mflogspec[i] = (mflogspec[i] * fe_.Deref.mel_fb.Deref.sqrt_inv_2n);
            }
        }

        public static void fe_write_frame(Pointer<fe_t> fe, Pointer<float> feat, int store_pcm)
        {
            BoxedValueInt is_speech = new BoxedValueInt();
            fe_spec_magnitude(fe);
            fe_mel_spec(fe);
            fe_noise.fe_track_snr(fe, is_speech);
            fe_mel_cep(fe, feat);
            fe_lifter(fe, feat);
            fe_noise.fe_vad_hangover(fe, feat, is_speech.Val, store_pcm);
        }

        public static void fe_free_2d<T>(Pointer<Pointer<T>> arr)
        {
            ckd_alloc.ckd_free_2d<T>(arr);
        }
    }
}
