using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class cmn
    {
        public static readonly Pointer<Pointer<byte>> cmn_type_str =
            new Pointer<Pointer<byte>>(new Pointer<byte>[]
            {
                cstring.ToCString("none"),
                cstring.ToCString("batch"),
                cstring.ToCString("live")
            });

        public static readonly Pointer<Pointer<byte>> cmn_alt_type_str =
            new Pointer<Pointer<byte>>(new Pointer<byte>[]
            {
                cstring.ToCString("none"),
                cstring.ToCString("current"),
                cstring.ToCString("prior")
            });

        public const int n_cmn_type_str = 3;

        public const int CMN_WIN_HWM = 800;
        public const int CMN_WIN = 500;

        public static int cmn_type_from_str(Pointer<byte> str)
        {
            int i;

            for (i = 0; i<n_cmn_type_str; ++i) {
                if (0 == cstring.strcmp(str, cmn_type_str[i]) || 0 == cstring.strcmp(str, cmn_alt_type_str[i]))
                    return i;
            }
            err.E_FATAL(string.Format("Unknown CMN type '{0}'\n", cstring.FromCString(str)));
            return cmn_type_e.CMN_NONE;
        }

        public static Pointer<cmn_t> cmn_init(int veclen)
        {
            Pointer<cmn_t> cmn;
            cmn = (Pointer<cmn_t>)ckd_alloc.ckd_calloc_struct<cmn_t>(1);
            cmn.Deref.veclen = veclen;
            cmn.Deref.cmn_mean = ckd_alloc.ckd_calloc<float>(veclen);
            cmn.Deref.cmn_var = ckd_alloc.ckd_calloc<float>(veclen);
            cmn.Deref.sum = ckd_alloc.ckd_calloc<float>(veclen);
            cmn.Deref.nframe = 0;

            return cmn;
        }
        
        public static void cmn_run(Pointer<cmn_t> cmn, Pointer<Pointer<float>> mfc, int varnorm, int n_frame)
        {
            Pointer<float> mfcp;
            float t;
            int i, f;
            int n_pos_frame;

            SphinxAssert.assert(mfc.IsNonNull);

            if (n_frame <= 0)
                return;

            /* If cmn.Deref.cmn_mean wasn't NULL, we need to zero the contents */
            cmn.Deref.cmn_mean.MemSet(0, cmn.Deref.veclen);

            /* Find mean cep vector for this utterance */
            for (f = 0, n_pos_frame = 0; f < n_frame; f++)
            {
                mfcp = mfc[f];

                /* Skip zero energy frames */
                if (mfcp[0] < 0)
                    continue;

                for (i = 0; i < cmn.Deref.veclen; i++)
                {
                    cmn.Deref.cmn_mean[i] += mfcp[i];
                }

                n_pos_frame++;
            }

            for (i = 0; i < cmn.Deref.veclen; i++)
                cmn.Deref.cmn_mean[i] /= n_pos_frame;

            err.E_INFO("CMN: ");
            for (i = 0; i < cmn.Deref.veclen; i++)
                err.E_INFOCONT(string.Format("{0} ", (cmn.Deref.cmn_mean[i])));
            err.E_INFOCONT("\n");
            if (varnorm == 0)
            {
                /* Subtract mean from each cep vector */
                for (f = 0; f < n_frame; f++)
                {
                    mfcp = mfc[f];
                    for (i = 0; i < cmn.Deref.veclen; i++)
                        mfcp[i] -= cmn.Deref.cmn_mean[i];
                }
            }
            else
            {
                /* Scale cep vectors to have unit variance along each dimension, and subtract means */
                /* If cmn.Deref.cmn_var wasn't NULL, we need to zero the contents */
                cmn.Deref.cmn_var.MemSet(0, cmn.Deref.veclen);

                for (f = 0; f < n_frame; f++)
                {
                    mfcp = mfc[f];

                    for (i = 0; i < cmn.Deref.veclen; i++)
                    {
                        t = mfcp[i] - cmn.Deref.cmn_mean[i];
                        cmn.Deref.cmn_var[i] += (t * t);
                    }
                }
                for (i = 0; i < cmn.Deref.veclen; i++)
                    /* Inverse Std. Dev, RAH added type case from sqrt */
                    cmn.Deref.cmn_var[i] = (float)(Math.Sqrt((double)n_frame / (cmn.Deref.cmn_var[i])));

                for (f = 0; f < n_frame; f++)
                {
                    mfcp = mfc[f];
                    for (i = 0; i < cmn.Deref.veclen; i++)
                        mfcp[i] = ((mfcp[i] - cmn.Deref.cmn_mean[i]) * cmn.Deref.cmn_var[i]);
                }
            }
        }

        /* 
         * RAH, free previously allocated memory
         */
        public static void cmn_free(Pointer<cmn_t> cmn)
        {
            if (cmn.IsNonNull)
            {
                if (cmn.Deref.cmn_var.IsNonNull)
                    ckd_alloc.ckd_free(cmn.Deref.cmn_var);

                if (cmn.Deref.cmn_mean.IsNonNull)
                    ckd_alloc.ckd_free(cmn.Deref.cmn_mean);

                if (cmn.Deref.sum.IsNonNull)
                    ckd_alloc.ckd_free(cmn.Deref.sum);

                ckd_alloc.ckd_free(cmn);
            }
        }
    }
}
