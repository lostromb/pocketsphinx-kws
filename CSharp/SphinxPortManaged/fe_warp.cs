using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class fe_warp
    {
        public const int FE_WARP_ID_MAX = 2;

        public static Pointer<Pointer<byte>> __name2id = new Pointer<Pointer<byte>>(new Pointer<byte>[]
        {
            cstring.ToCString("inverse"),
            cstring.ToCString("linear"),
            cstring.ToCString("piecewise"),
            PointerHelpers.NULL<byte>()
        });

        public static Pointer<Pointer<byte>> name2id = new Pointer<Pointer<byte>>(new Pointer<byte>[]
        {
            cstring.ToCString("inverse_linear"),
            cstring.ToCString("affine"),
            cstring.ToCString("piecewise_linear"),
            PointerHelpers.NULL<byte>()
        });

        public static Pointer<fe_warp_conf_t> fe_warp_conf = new Pointer<fe_warp_conf_t>(new fe_warp_conf_t[]
        {
            new fe_warp_conf_t()
            {
                set_parameters = fe_warp_inverse_linear.fe_warp_inverse_linear_set_parameters,
                doc = fe_warp_inverse_linear.fe_warp_inverse_linear_doc,
                id = fe_warp_inverse_linear.fe_warp_inverse_linear_id,
                n_param = fe_warp_inverse_linear.fe_warp_inverse_linear_n_param,
                warped_to_unwarped = fe_warp_inverse_linear.fe_warp_inverse_linear_warped_to_unwarped,
                unwarped_to_warped = fe_warp_inverse_linear.fe_warp_inverse_linear_unwarped_to_warped,
                print = fe_warp_inverse_linear.fe_warp_inverse_linear_print
            },     /* Inverse linear warping */
            new fe_warp_conf_t()
            {
                set_parameters = fe_warp_affine.fe_warp_affine_set_parameters,
                doc = fe_warp_affine.fe_warp_affine_doc,
                id = fe_warp_affine.fe_warp_affine_id,
                n_param = fe_warp_affine.fe_warp_affine_n_param,
                warped_to_unwarped = fe_warp_affine.fe_warp_affine_warped_to_unwarped,
                unwarped_to_warped = fe_warp_affine.fe_warp_affine_unwarped_to_warped,
                print = fe_warp_affine.fe_warp_affine_print
            },     /* Affine warping */
            new fe_warp_conf_t()
            {
                set_parameters = fe_warp_piecewise_linear.fe_warp_piecewise_linear_set_parameters,
                doc = fe_warp_piecewise_linear.fe_warp_piecewise_linear_doc,
                id = fe_warp_piecewise_linear.fe_warp_piecewise_linear_id,
                n_param = fe_warp_piecewise_linear.fe_warp_piecewise_linear_n_param,
                warped_to_unwarped = fe_warp_piecewise_linear.fe_warp_piecewise_linear_warped_to_unwarped,
                unwarped_to_warped = fe_warp_piecewise_linear.fe_warp_piecewise_linear_unwarped_to_warped,
                print = fe_warp_piecewise_linear.fe_warp_piecewise_linear_print
            },   /* Piecewise_Linear warping */
        });

        public static int fe_warp_set(Pointer<melfb_t> mel, Pointer<byte> id_name)
        {
            uint i;

            for (i = 0; name2id[i].IsNonNull; i++)
            {
                if (cstring.strcmp(id_name, name2id[i]) == 0)
                {
                    mel.Deref.warp_id = i;
                    break;
                }
            }

            if (name2id[i].IsNull)
            {
                for (i = 0; __name2id[i].IsNonNull; i++)
                {
                    if (cstring.strcmp(id_name, __name2id[i]) == 0)
                    {
                        mel.Deref.warp_id = i;
                        break;
                    }
                }
                if (__name2id[i].IsNull)
                {
                    err.E_ERROR(string.Format("Unimplemented warping function {0}\n", cstring.FromCString(id_name)));
                    err.E_ERROR("Implemented functions are:\n");
                    for (i = 0; name2id[i].IsNonNull; i++)
                    {
                        Console.Write("\t{0}\n", cstring.FromCString(name2id[i]));
                    }
                    mel.Deref.warp_id = fe.FE_WARP_ID_NONE;

                    return fe.FE_START_ERROR;
                }
            }

            return fe.FE_SUCCESS;
        }

        public static void fe_warp_set_parameters(Pointer<melfb_t> mel, Pointer<byte> param_str, float sampling_rate)
        {
            if (mel.Deref.warp_id <= FE_WARP_ID_MAX)
            {
                fe_warp_conf[mel.Deref.warp_id].set_parameters(param_str, sampling_rate);
            }
            else if (mel.Deref.warp_id == fe.FE_WARP_ID_NONE)
            {
                err.E_FATAL("feat module must be configured w/ a valid ID\n");
            }
            else
            {
                err.E_FATAL
                    (string.Format("fe_warp module misconfigured with invalid fe_warp_id {0}\n",
                     mel.Deref.warp_id));
            }
        }

        public static float fe_warp_warped_to_unwarped(Pointer<melfb_t> mel, float nonlinear)
        {
            if (mel.Deref.warp_id <= FE_WARP_ID_MAX)
            {
                return fe_warp_conf[mel.Deref.warp_id].warped_to_unwarped(nonlinear);
            }
            else if (mel.Deref.warp_id == fe.FE_WARP_ID_NONE)
            {
                err.E_FATAL("fe_warp module must be configured w/ a valid ID\n");
            }
            else
            {
                err.E_FATAL
                    (string.Format("fe_warp module misconfigured with invalid fe_warp_id {0}\n",
                     mel.Deref.warp_id));
            }

            return 0;
        }

        public static float fe_warp_unwarped_to_warped(Pointer<melfb_t> mel, float linear)
        {
            if (mel.Deref.warp_id <= FE_WARP_ID_MAX)
            {
                return fe_warp_conf[mel.Deref.warp_id].unwarped_to_warped(linear);
            }
            else if (mel.Deref.warp_id == fe.FE_WARP_ID_NONE)
            {
                err.E_FATAL("fe_warp module must be configured w/ a valid ID\n");
            }
            else
            {
                err.E_FATAL
                    (string.Format("fe_warp module misconfigured with invalid fe_warp_id {0}\n",
                     mel.Deref.warp_id));
            }

            return 0;
        }
    }
}
