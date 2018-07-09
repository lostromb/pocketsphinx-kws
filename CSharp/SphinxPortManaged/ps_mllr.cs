using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ps_mllr
    {
        public static Pointer<ps_mllr_t> ps_mllr_read(Pointer<byte> regmatfile)
        {
            Pointer<ps_mllr_t> mllr;
            FILE fp;
            int n, i, m, j, k;

            mllr = ckd_alloc.ckd_calloc_struct<ps_mllr_t>(1);
            mllr.Deref.refcnt = 1;

            if ((fp = FILE.fopen(regmatfile, "r")) == null)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to open MLLR file '{0}' for reading", cstring.FromCString(regmatfile)));
                goto error_out;
            }
            else
                err.E_INFO(string.Format("Reading MLLR transformation file '{0}'\n", cstring.FromCString(regmatfile)));

            if ((fp.fscanf_d(out n) != 1) || (n < 1))
            {
                err.E_ERROR("Failed to read number of MLLR classes\n");
                goto error_out;
            }

            mllr.Deref.n_class = n;

            if ((fp.fscanf_d(out n) != 1))
            {
                err.E_ERROR("Failed to read number of feature streams\n");
                goto error_out;
            }
            mllr.Deref.n_feat = n;
            mllr.Deref.veclen = ckd_alloc.ckd_calloc<int>(mllr.Deref.n_feat);

            mllr.Deref.A = ckd_alloc.ckd_calloc<Pointer<Pointer<Pointer<float>>>>(mllr.Deref.n_feat);
            mllr.Deref.b = ckd_alloc.ckd_calloc<Pointer<Pointer<float>>>(mllr.Deref.n_feat);
            mllr.Deref.h = ckd_alloc.ckd_calloc<Pointer<Pointer<float>>>(mllr.Deref.n_feat);

            for (i = 0; i < mllr.Deref.n_feat; ++i)
            {
                if (fp.fscanf_d(out n) != 1)
                {
                    err.E_ERROR(string.Format("Failed to read stream length for feature {0}\n", i));
                    goto error_out;
                }
                mllr.Deref.veclen[i] = n;
                mllr.Deref.A[i] = ckd_alloc.ckd_calloc_3d<float>((uint)mllr.Deref.n_class, (uint)mllr.Deref.veclen[i], (uint)mllr.Deref.veclen[i]);
                mllr.Deref.b[i] = ckd_alloc.ckd_calloc_2d<float>((uint)mllr.Deref.n_class, (uint)mllr.Deref.veclen[i]);
                mllr.Deref.h[i] = ckd_alloc.ckd_calloc_2d<float>((uint)mllr.Deref.n_class, (uint)mllr.Deref.veclen[i]);

                float t;
                for (m = 0; m < mllr.Deref.n_class; ++m)
                {
                    for (j = 0; j < mllr.Deref.veclen[i]; ++j)
                    {
                        for (k = 0; k < mllr.Deref.veclen[i]; ++k)
                        {
                            if (fp.fscanf_f_(out t) != 1)
                            {
                                err.E_ERROR(string.Format("Failed reading MLLR rotation ({0},{1},{2},{3})\n",
                                        i, m, j, k));
                                goto error_out;
                            }
                            mllr.Deref.A[i][m][j].Set(k, t);
                        }
                    }
                    for (j = 0; j < mllr.Deref.veclen[i]; ++j)
                    {
                        if (fp.fscanf_f_(out t) != 1)
                        {
                            err.E_ERROR(string.Format("Failed reading MLLR bias ({0},{1},{2})\n",
                                    i, m, j));
                            goto error_out;
                        }
                        mllr.Deref.b[i][m].Set(j, t);
                    }
                    for (j = 0; j < mllr.Deref.veclen[i]; ++j)
                    {
                        if (fp.fscanf_f_(out t) != 1)
                        {
                            err.E_ERROR(string.Format("Failed reading MLLR variance scale ({0},{1},{2})\n",
                                    i, m, j));
                            goto error_out;
                        }
                        mllr.Deref.h[i][m].Set(j, t);
                    }
                }
            }
            fp.fclose();
            return mllr;

            error_out:
            if (fp != null)
                fp.fclose();
            ps_mllr_free(mllr);
            return PointerHelpers.NULL<ps_mllr_t>();
        }

        public static int ps_mllr_free(Pointer<ps_mllr_t> mllr)
        {
            int i;

            if (mllr.IsNull)
                return 0;
            if (--mllr.Deref.refcnt > 0)
                return mllr.Deref.refcnt;

            for (i = 0; i < mllr.Deref.n_feat; ++i)
            {
                if (mllr.Deref.A.IsNonNull)
                    ckd_alloc.ckd_free_3d(mllr.Deref.A[i]);
                if (mllr.Deref.b.IsNonNull)
                    ckd_alloc.ckd_free_2d(mllr.Deref.b[i]);
                if (mllr.Deref.h.IsNonNull)
                    ckd_alloc.ckd_free_2d(mllr.Deref.h[i]);
            }
            ckd_alloc.ckd_free(mllr.Deref.veclen);
            ckd_alloc.ckd_free(mllr.Deref.A);
            ckd_alloc.ckd_free(mllr.Deref.b);
            ckd_alloc.ckd_free(mllr.Deref.h);
            ckd_alloc.ckd_free(mllr);

            return 0;
        }
    }
}
