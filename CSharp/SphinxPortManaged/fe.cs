using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe
    {
        public const double SQRT_HALF = 0.707106781186548;

        // LOGAN TODO make these enums
        public const int RAW_LOG_SPEC = 1;
        public const int SMOOTH_LOG_SPEC = 2;

        public const int LEGACY_DCT = 0;
        public const int DCT_II = 1;
        public const int DCT_HTK = 2;

        public const int FE_SUCCESS = 0;
        public const int FE_OUTPUT_FILE_SUCCESS = 0;
        public const int FE_CONTROL_FILE_ERROR = -1;
        public const int FE_START_ERROR = -2;
        public const int FE_UNKNOWN_SINGLE_OR_BATCH = -3;
        public const int FE_INPUT_FILE_OPEN_ERROR = -4;
        public const int FE_INPUT_FILE_READ_ERROR = -5;
        public const int FE_MEM_ALLOC_ERROR = -6;
        public const int FE_OUTPUT_FILE_WRITE_ERROR = -7;
        public const int FE_OUTPUT_FILE_OPEN_ERROR = -8;
        public const int FE_ZERO_ENERGY_ERROR = -9;
        public const int FE_INVALID_PARAM_ERROR =  -10;
        
        public const uint FE_WARP_ID_INVERSE_LINEAR = 0;
        public const uint FE_WARP_ID_AFFINE = 1;
        public const uint FE_WARP_ID_PIECEWISE_LINEAR = 2;
        public const uint FE_WARP_ID_EIDE_GISH = 3;
        public const uint FE_WARP_ID_MAX = 2;
        public const uint FE_WARP_ID_NONE = 0xFFFFFFFFU;
    }
}
