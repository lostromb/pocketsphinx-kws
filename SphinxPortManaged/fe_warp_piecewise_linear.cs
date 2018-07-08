using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe_warp_piecewise_linear
    {
        public const int N_PARAM = 2;
        public static Pointer<float> parameters = new Pointer<float>(new float[] { 1.0f, 6800.0f });
        public static Pointer<float> final_piece = PointerHelpers.Malloc<float>(2);
        public static int is_neutral = 1;
        public static readonly Pointer<byte> p_str = PointerHelpers.Malloc<byte>(256);
        public static float nyquist_frequency = 0.0f;

        public static Pointer<byte> fe_warp_piecewise_linear_doc()
        {
            return cstring.ToCString("piecewise_linear :== < w' = a * w, w < F >");
        }

        public static uint fe_warp_piecewise_linear_id()
        {
            return fe.FE_WARP_ID_PIECEWISE_LINEAR;
        }

        public static uint fe_warp_piecewise_linear_n_param()
        {
            return N_PARAM;
        }

        public static void fe_warp_piecewise_linear_set_parameters(Pointer<byte> param_str,
                                        float sampling_rate)
        {
            Pointer<byte> tok;
            Pointer<byte> seps = cstring.ToCString(" \t");
            Pointer<byte> temp_param_str = PointerHelpers.Malloc<byte>(256);
            int param_index = 0;

            nyquist_frequency = sampling_rate / 2;
            if (param_str.IsNull)
            {
                is_neutral = 1;
                return;
            }
            /* The new parameters are the same as the current ones, so do nothing. */
            if (cstring.strcmp(param_str, p_str) == 0)
            {
                return;
            }
            is_neutral = 0;
            cstring.strcpy(temp_param_str, param_str);
            parameters.MemSet(0, N_PARAM);
            final_piece.MemSet(0, 2);
            cstring.strcpy(p_str, param_str);
            /* FIXME: strtok() is not re-entrant... */
            tok = cstring.strtok(temp_param_str, seps);
            while (tok.IsNonNull)
            {
                parameters[param_index++] = (float)strfuncs.atof_c(tok);
                tok = cstring.strtok(PointerHelpers.NULL<byte>(), seps);
                if (param_index >= N_PARAM)
                {
                    break;
                }
            }
            if (tok.IsNonNull)
            {
                err.E_INFO
                    (string.Format("Piecewise linear warping takes up to two arguments, {0} ignored.\n",
                     cstring.FromCString(tok)));
            }
            if (parameters[1] < sampling_rate)
            {
                /* Precompute these. These are the coefficients of a
                 * straight line that contains the points (F, aF) and (N,
                 * N), where a = params[0], F = params[1], N = Nyquist
                 * frequency.
                 */
                if (parameters[1] == 0)
                {
                    parameters[1] = sampling_rate * 0.85f;
                }
                final_piece[0] =
                    (nyquist_frequency -
                     parameters[0] * parameters[1]) / (nyquist_frequency - parameters[1]);
                final_piece[1] =
                    nyquist_frequency * parameters[1] * (parameters[0] -
                                                 1.0f) / (nyquist_frequency -
                                                          parameters[1]);
            }
            else
            {
                final_piece.MemSet(0, 2);
            }
            if (parameters[0] == 0)
            {
                is_neutral = 1;
                err.E_INFO
                    ("Piecewise linear warping cannot have slope zero, warping not applied.\n");
            }
        }

        public static float fe_warp_piecewise_linear_warped_to_unwarped(float nonlinear)
        {
            if (is_neutral != 0)
            {
                return nonlinear;
            }
            else
            {
                /* linear = (nonlinear - b) / a */
                float temp;
                if (nonlinear < parameters[0] * parameters[1])
                {
                    temp = nonlinear / parameters[0];
                }
                else
                {
                    temp = nonlinear - final_piece[1];
                    temp /= final_piece[0];
                }
                if (temp > nyquist_frequency)
                {
                    err.E_WARN
                        (string.Format("Warp factor {0} results in frequency ({1}) higher than Nyquist ({2})\n",
                         parameters[0], temp, nyquist_frequency));
                }
                return temp;
            }
        }

        public static float fe_warp_piecewise_linear_unwarped_to_warped(float linear)
        {
            if (is_neutral != 0)
            {
                return linear;
            }
            else
            {
                float temp;
                /* nonlinear = a * linear - b */
                if (linear < parameters[1])
                {
                    temp = linear * parameters[0];
                }
                else
                {
                    temp = final_piece[0] * linear + final_piece[1];
                }
                return temp;
            }
        }

        public static void fe_warp_piecewise_linear_print(Pointer<byte> label)
        {
            uint i;

            for (i = 0; i < N_PARAM; i++)
            {
                Console.Write("{0}[{1}]: {2} ", cstring.FromCString(label), i, parameters[i]);
            }
            Console.Write("\n");
        }
    }
}
