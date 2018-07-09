using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class cmdln_macro
    {
        /** Default number of samples per second. */
//#define DEFAULT_SAMPLING_RATE 16000
//        /** Default number of frames per second. */
//#define DEFAULT_FRAME_RATE 100
//        /** Default spacing between frame starts (equal to
//         * DEFAULT_SAMPLING_RATE/DEFAULT_FRAME_RATE) */
//#define DEFAULT_FRAME_SHIFT 160
//        /** Default size of each frame (410 samples @ 16000Hz). */
//#define DEFAULT_WINDOW_LENGTH 0.025625 
//        /** Default number of FFT points. */
//#define DEFAULT_FFT_SIZE 512
//        /** Default number of MFCC coefficients in output. */
//#define DEFAULT_NUM_CEPSTRA 13
//        /** Default number of filter bands used to generate MFCCs. */
//#define DEFAULT_NUM_FILTERS 40

//        /** Default prespeech length */
//#define DEFAULT_PRE_SPEECH 20
//        /** Default postspeech length */
//#define DEFAULT_POST_SPEECH 50
//        /** Default postspeech length */
//#define DEFAULT_START_SPEECH 10

//        /** Default lower edge of mel filter bank. */
//#define DEFAULT_LOWER_FILT_FREQ 133.33334
//        /** Default upper edge of mel filter bank. */
//#define DEFAULT_UPPER_FILT_FREQ 6855.4976
//        /** Default pre-emphasis filter coefficient. */
//#define DEFAULT_PRE_EMPHASIS_ALPHA 0.97
//        /** Default type of frequency warping to use for VTLN. */
//#define DEFAULT_WARP_TYPE "inverse_linear"
//        /** Default random number seed to use for dithering. */
//#define SEED  -1

        public static List<arg_t> POCKETSPHINX_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.AddRange(POCKETSPHINX_DEBUG_OPTIONS());
            args.AddRange(POCKETSPHINX_BEAM_OPTIONS());
            args.AddRange(POCKETSPHINX_SEARCH_OPTIONS());
            args.AddRange(POCKETSPHINX_KWS_OPTIONS());
            args.AddRange(POCKETSPHINX_FSG_OPTIONS());
            args.AddRange(POCKETSPHINX_NGRAM_OPTIONS());
            args.AddRange(POCKETSPHINX_DICT_OPTIONS());
            args.AddRange(POCKETSPHINX_ACMOD_OPTIONS());
            args.AddRange(waveform_to_cepstral_command_line_macro());
            args.AddRange(cepstral_to_feature_command_line_macro());
            args.Add(null);
            return args;
        }

        public static List<arg_t> POCKETSPHINX_DEBUG_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-logfn"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("File to write log messages in")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-mfclogdir"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Directory to log feature files to")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-rawlogdir"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Directory to log raw audio files to")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-senlogdir"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Directory to log senone score files to")
            });
            return args;
        }

        public static List<arg_t> POCKETSPHINX_BEAM_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-beam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-48"),
                doc = cstring.ToCString("Beam width applied to every frame in Viterbi search (smaller values mean wider beam)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-wbeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("7e-29"),
                doc = cstring.ToCString("Beam width applied to word exits")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-pbeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-48"),
                doc = cstring.ToCString("Beam width applied to phone transitions")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-lpbeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-40"),
                doc = cstring.ToCString("Beam width applied to last phone in words")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-lponlybeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("7e-29"),
                doc = cstring.ToCString("Beam width applied to last phone in single-phone words")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fwdflatbeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-64"),
                doc = cstring.ToCString("Beam width applied to every frame in second-pass flat search")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fwdflatwbeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("7e-29"),
                doc = cstring.ToCString("Beam width applied to word exits in second-pass flat search")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-pl_window"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("5"),
                doc = cstring.ToCString("Phoneme lookahead window size, in frames")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-pl_beam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-10"),
                doc = cstring.ToCString("Beam width applied to phone loop search for lookahead")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-pl_pbeam"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-10"),
                doc = cstring.ToCString("Beam width applied to phone loop transitions for lookahead")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-pl_pip"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1.0"),
                doc = cstring.ToCString("Phone insertion penalty for phone loop")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-pl_weight"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("3.0"),
                doc = cstring.ToCString("Weight for phoneme lookahead penalties")
            });
            return args;
        }

        public static List<arg_t> POCKETSPHINX_SEARCH_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-compallsen"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Compute all senone scores in every frame (can be faster when there are many senones)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fwdtree"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Run forward lexicon-tree search (1st pass)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fwdflat"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Run forward flat-lexicon search over word lattice (2nd pass)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-bestpath"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Run bestpath (Dijkstra) search over word lattice (3rd pass)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-backtrace"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Print results and backtraces to log.")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-latsize"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("5000"),
                doc = cstring.ToCString("Initial backpointer table size")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-maxwpf"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("-1"),
                doc = cstring.ToCString("Maximum number of distinct word exits at each frame (or -1 for no pruning)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-maxhmmpf"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("30000"),
                doc = cstring.ToCString("Maximum number of active HMMs to maintain at each frame (or -1 for no pruning)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-min_endfr"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("0"),
                doc = cstring.ToCString("Nodes ignored in lattice construction if they persist for fewer than N frames")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fwdflatefwid"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("4"),
                doc = cstring.ToCString("Minimum number of end frames for a word to be searched in fwdflat search")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fwdflatsfwin"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("25"),
                doc = cstring.ToCString("Window of frames in lattice to search for successor words in fwdflat search")
            });
            return args;
        }

        public static List<arg_t> POCKETSPHINX_KWS_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-keyphrase"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Keyphrase to spot")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-kws"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("(MODIFIED - DO NOT USE) A file with keyphrases to spot, one per line")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-kws_plp"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-1"),
                doc = cstring.ToCString("Phone loop probability for keyphrase spotting")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-kws_delay"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("10"),
                doc = cstring.ToCString("Delay to wait for best detection score")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-kws_threshold"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1e-30"),
                doc = cstring.ToCString("(MODIFIED - DO NOT USE) Threshold for p(hyp)/p(alternatives) ratio")
            });
            return args;
        }

        public static List<arg_t> POCKETSPHINX_FSG_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            return args;
        }

        public static List<arg_t> POCKETSPHINX_NGRAM_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            return args;
        }

        public static List<arg_t> POCKETSPHINX_DICT_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-dict"),
                type = cmd_ln.REQARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Main pronunciation dictionary (lexicon) input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-fdict"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Noise word pronunciation dictionary input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-dictcase"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Dictionary is case sensitive (NOTE: case insensitivity applies to ASCII characters only)")
            });
            return args;
        }

        public static List<arg_t> POCKETSPHINX_ACMOD_OPTIONS()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-hmm"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Directory containing acoustic model files.")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-featparams"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("File containing feature extraction parameters.")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-mdef"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Model definition input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-senmgau"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Senone to codebook mapping input file (usually not needed)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-tmat"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("HMM state transition matrix input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-tmatfloor"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("0.0001"),
                doc = cstring.ToCString("HMM state transition probability floor (applied to -tmat file)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-mean"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Mixture gaussian means input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-var"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Mixture gaussian variances input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-varfloor"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("0.0001"),
                doc = cstring.ToCString("Mixture gaussian variance floor (applied to data from -var file)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-mixw"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Senone mixture weights input file (uncompressed)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-mixwfloor"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("0.0000001"),
                doc = cstring.ToCString("Senone mixture weights floor (applied to data from -mixw file)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-aw"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("1"),
                doc = cstring.ToCString("Inverse weight applied to acoustic scores.")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-sendump"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Senone dump (compressed mixture weights) input file")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-mllr"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("MLLR transformation to apply to means and variances")
            });
            //args.Add(new arg_t()
            //{
            //    name = cstring.ToCString("-mmap"),
            //    type = cmd_ln.ARG_BOOLEAN,
            //    deflt = cstring.ToCString("yes"),
            //    doc = cstring.ToCString("Use memory-mapped I/O (if possible) for model files")
            //});
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-ds"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("1"),
                doc = cstring.ToCString("Frame GMM computation downsampling ratio")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-topn"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("4"),
                doc = cstring.ToCString("Maximum number of top Gaussians to use in scoring.")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-topn_beam"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("0"),
                doc = cstring.ToCString("Beam width used to determine top-N Gaussians (or a list, per-feature)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-logbase"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("1.0001000165939331"),
                doc = cstring.ToCString("Base in which all log-likelihoods calculated")
            });
            return args;
        }

        public static List<arg_t> waveform_to_cepstral_command_line_macro()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-logspec"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Write out logspectral files instead of cepstra")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-smoothspec"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Write out cepstral-smoothed logspectral files")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-transform"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("legacy"),
                doc = cstring.ToCString("Which type of transform to use to calculate cepstra (legacy, dct, or htk)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-alpha"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("0.97"),
                doc = cstring.ToCString("Preemphasis parameter")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-samprate"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("16000"),
                doc = cstring.ToCString("Sampling rate")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-frate"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("100"),
                doc = cstring.ToCString("Frame rate")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-wlen"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("0.025625"),
                doc = cstring.ToCString("Hamming window length")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-nfft"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("512"),
                doc = cstring.ToCString("Size of FFT")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-nfilt"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("40"),
                doc = cstring.ToCString("Number of filter banks")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-lowerf"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("133.33334"),
                doc = cstring.ToCString("Lower edge of filters")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-upperf"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("6855.4976"),
                doc = cstring.ToCString("Upper edge of filters")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-unit_area"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Normalize mel filters to unit area")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-round_filters"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Round mel filter frequencies to DFT points")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-ncep"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("13"),
                doc = cstring.ToCString("Number of cep coefficients")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-doublebw"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Use double bandwidth filters (same center freq)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-lifter"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("0"),
                doc = cstring.ToCString("Length of sin-curve for liftering, or 0 for no liftering")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-vad_prespeech"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("20"),
                doc = cstring.ToCString("Num of speech frames to keep before silence to speech")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-vad_startspeech"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("10"),
                doc = cstring.ToCString("Num of speech frames to trigger vad from silence to speech")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-vad_postspeech"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("50"),
                doc = cstring.ToCString("Num of silence frames to keep after from speech to silence")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-vad_threshold"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("3.0"),
                doc = cstring.ToCString("Threshold for decision between noise and silence frames. Log-ratio between signal level and noise level.")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-input_endian"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("little"),
                doc = cstring.ToCString("Endianness of input data, big or little, ignored if NIST or MS Wav")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-warp_type"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("inverse_linear"),
                doc = cstring.ToCString("Warping function type (or shape)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-warp_params"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Parameters defining the warping function")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-dither"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Add 1/2-bit noise")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-seed"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("-1"),
                doc = cstring.ToCString("Seed for random number generator; if less than zero, pick our own")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-remove_dc"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Remove DC offset from each frame")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-remove_noise"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Remove noise with spectral subtraction in mel-energies")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-remove_silence"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("yes"),
                doc = cstring.ToCString("Enables VAD, removes silence frames from processing")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-verbose"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Show input filenames")
            });
            return args;
        }

        public static List<arg_t> cepstral_to_feature_command_line_macro()
        {
            List<arg_t> args = new List<arg_t>();
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-feat"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("1s_c_d_dd"),
                doc = cstring.ToCString("Feature stream type, depends on the acoustic model")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-ceplen"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("13"),
                doc = cstring.ToCString("Number of components in the input feature vector")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-cmn"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("live"),
                doc = cstring.ToCString("Cepstral mean normalization scheme ('live', 'batch', or 'none')")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-cmninit"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("40,3,-1"),
                doc = cstring.ToCString("Initial values (comma-separated) for cepstral mean when 'live' is used")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-TEMP"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("TEMP"),
                doc = cstring.ToCString("TEMP")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-varnorm"),
                type = cmd_ln.ARG_BOOLEAN,
                deflt = cstring.ToCString("no"),
                doc = cstring.ToCString("Variance normalize each utterance (only if CMN == current)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-agc"),
                type = cmd_ln.ARG_STRING,
                deflt = cstring.ToCString("none"),
                doc = cstring.ToCString("Automatic gain control for c0 ('max', 'emax', 'noise', or 'none')")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-agcthresh"),
                type = cmd_ln.ARG_FLOATING,
                deflt = cstring.ToCString("2.0"),
                doc = cstring.ToCString("Initial threshold for automatic gain control")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-lda"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("File containing transformation matrix to be applied to features (single-stream features only)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-ldadim"),
                type = cmd_ln.ARG_INTEGER,
                deflt = cstring.ToCString("0"),
                doc = cstring.ToCString("Dimensionality of output of feature transformation (0 to use entire matrix)")
            });
            args.Add(new arg_t()
            {
                name = cstring.ToCString("-svspec"),
                type = cmd_ln.ARG_STRING,
                deflt = PointerHelpers.NULL<byte>(),
                doc = cstring.ToCString("Subvector specification (e.g., 24,0-11/25,12-23/26-38 or 0-12/13-25/26-38)")
            });
            return args;
        }
    }
}
