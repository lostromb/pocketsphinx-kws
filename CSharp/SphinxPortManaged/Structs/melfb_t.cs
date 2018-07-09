using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class melfb_t
    {
        public float sampling_rate;
        public int num_cepstra;
        public int num_filters;
        public int fft_size;
        public float lower_filt_freq;
        public float upper_filt_freq;
        /* DCT coefficients. */
        public Pointer<Pointer<float>> mel_cosine;
        /* Filter coefficients. */
        public Pointer<float> filt_coeffs;
        public Pointer<short> spec_start;
        public Pointer<short> filt_start;
        public Pointer<short> filt_width;
        /* Luxury mobile home. */
        public int doublewide;
        public Pointer<byte> warp_type;
        public Pointer<byte> warp_params;
        public uint warp_id;
        /* Precomputed normalization constants for unitary DCT-II/DCT-III */
        public float sqrt_inv_n;
        public float sqrt_inv_2n;
        /* Value and coefficients for HTK-style liftering */
        public int lifter_val;
        public Pointer<float> lifter;
        /* Normalize filters to unit area */
        public int unit_area;
        /* Round filter frequencies to DFT points (hurts accuracy, but is
           useful for legacy purposes) */
        public int round_filters;
    }
}
