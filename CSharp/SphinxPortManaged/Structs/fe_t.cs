using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    /** Structure for the front-end computation. */
    public class fe_t
    {
        public Pointer<cmd_ln_t> config;
        public int refcount;

        public float sampling_rate;
        public short frame_rate;
        public short frame_shift;

        public float window_length;
        public short frame_size;
        public short fft_size;

        public byte fft_order;
        public byte feature_dimension;
        public byte num_cepstra;
        public byte remove_dc;
        public byte log_spec;
        public byte swap;
        public byte dither;
        public byte transform;
        public byte remove_noise;
        public byte remove_silence;

        public float pre_emphasis_alpha;
        public short pre_emphasis_prior;
        public int dither_seed;

        public short num_overflow_samps;
        public uint num_processed_samps;

        /* Twiddle factors for FFT. */
        public Pointer<double> ccc;
        public Pointer<double> sss;
        /* Mel filter parameters. */
        public Pointer<melfb_t> mel_fb;
        /* Half of a Hamming Window. */
        public Pointer<double> hamming_window;

        /* Noise removal  */
        public Pointer<noise_stats_t> noise_stats;

        /* VAD variables */
        public short pre_speech;
        public short post_speech;
        public short start_speech;
        public float vad_threshold;
        public Pointer<vad_data_t> vad_data;

        /* Temporary buffers for processing. */
        /* FIXME: too many of these. */
        public Pointer<short> spch;
        public Pointer<double> frame;
        public Pointer<double> spec;
        public Pointer<double> mfspec;
        public Pointer<short> overflow_samps;
    };
}
