using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ms_gauden
    {
        public static readonly Pointer<byte> GAUDEN_PARAM_VERSION = cstring.ToCString("1.0");
        public const int WORST_DIST = unchecked((int)0x80000000);

        public static Pointer<Pointer<Pointer<Pointer<float>>>> gauden_param_read(
            Pointer<byte> file_name,
            BoxedValueInt out_n_mgau,
            BoxedValueInt out_n_feat,
            BoxedValueInt out_n_density,
            BoxedValue<Pointer<int>> out_veclen)
        {
            FILE fp;
            int i, j, k, l, n, blk;
            int n_mgau;
            int n_feat;
            int n_density;
            Pointer<int> veclen;
            int byteswap, chksum_present;
            Pointer<Pointer<Pointer<Pointer<float>>>> output;
            Pointer<float> buf;
            Pointer<Pointer<byte>> argname;
            Pointer<Pointer<byte>> argval;
            BoxedValue<uint> chksum = new BoxedValue<uint>();

            err.E_INFO(string.Format("Reading mixture gaussian parameter: {0}\n", cstring.FromCString(file_name)));

            if ((fp = FILE.fopen(file_name, "rb")) == null)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to open file '{0}' for reading", cstring.FromCString(file_name)));
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }

            /* Read header, including argument-value info and 32-bit byteorder magic */
            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(fp, boxed_argname, boxed_argval, out byteswap) < 0)
            {
                err.E_ERROR(string.Format("Failed to read header from file '{0}'\n", cstring.FromCString(file_name)));
                fp.fclose();
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }
            argname = boxed_argname.Val;
            argval = boxed_argval.Val;

            /* Parse argument-value list */
            chksum_present = 0;
            for (i = 0; argname[i].IsNonNull; i++)
            {
                if (cstring.strcmp(argname[i], cstring.ToCString("version")) == 0)
                {
                    if (cstring.strcmp(argval[i], GAUDEN_PARAM_VERSION) != 0)
                        err.E_WARN(string.Format("Version mismatch({0}): {1}, expecting {2}\n",
                       cstring.FromCString(file_name), cstring.FromCString(argval[i]), cstring.FromCString(GAUDEN_PARAM_VERSION)));
                }
                else if (cstring.strcmp(argname[i], cstring.ToCString("chksum0")) == 0)
                {
                    chksum_present = 1; /* Ignore the associated value */
                }
            }
            bio.bio_hdrarg_free(argname, argval);
            argname = argval = PointerHelpers.NULL<Pointer<byte>>();

            chksum.Val = 0;
            byte[] tmp_buf = new byte[4];
            Pointer<byte> tmp = new Pointer<byte>(tmp_buf);

            /* #Codebooks */
            if (bio.bio_fread(tmp, 4, 1, fp, byteswap, chksum) != 1)
            {
                err.E_ERROR(string.Format("Failed to read number of codebooks from {0}\n", cstring.FromCString(file_name)));
                fp.fclose();
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }
            n_mgau = BitConverter.ToInt32(tmp_buf, 0);
            out_n_mgau.Val = n_mgau;

            /* #Features/codebook */
            if (bio.bio_fread(tmp, 4, 1, fp, byteswap, chksum) != 1)
            {
                err.E_ERROR(string.Format("Failed to read number of features from {0}\n", cstring.FromCString(file_name)));
                fp.fclose();
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }
            n_feat = BitConverter.ToInt32(tmp_buf, 0);
            out_n_feat.Val = n_feat;

            /* #Gaussian densities/feature in each codebook */
            if (bio.bio_fread(tmp, 4, 1, fp, byteswap, chksum) != 1)
            {
                err.E_ERROR(string.Format("fread({0}) (#density/codebook) failed\n", cstring.FromCString(file_name)));
            }
            n_density = BitConverter.ToInt32(tmp_buf, 0);
            out_n_density.Val = n_density;

            /* #Dimensions in each feature stream */
            veclen = ckd_alloc.ckd_calloc<int>(n_feat);
            out_veclen.Val = veclen;
            Pointer<byte> veclen_buf = PointerHelpers.Malloc<byte>(n_feat * 4);
            if (bio.bio_fread(veclen_buf, 4, n_feat, fp, byteswap, chksum) != n_feat)
            {
                err.E_ERROR(string.Format("fread({0}) (feature-lengths) failed\n", cstring.FromCString(file_name)));
                fp.fclose();
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }

            // LOGAN modified Convert file data from byte to int32
            Pointer<int> upcastVeclen = veclen_buf.ReinterpretCast<int>();
            upcastVeclen.MemCopyTo(veclen, n_feat);

            /* blk = total vector length of all feature streams */
            for (i = 0, blk = 0; i < n_feat; i++)
                blk += veclen[i];

            /* #Floats to follow; for the ENTIRE SET of CODEBOOKS */
            if (bio.bio_fread(tmp, 4, 1, fp, byteswap, chksum) != 1)
            {
                err.E_ERROR(string.Format("Failed to read number of parameters from {0}\n", cstring.FromCString(file_name)));
                fp.fclose();
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }
            n = BitConverter.ToInt32(tmp_buf, 0);

            if (n != n_mgau * n_density * blk)
            {
                err.E_ERROR(string.Format("Number of parameters in {0}({1}) doesn't match dimensions: {2} x {3} x {4}\n", cstring.FromCString(file_name), n, n_mgau, n_density, blk));
                fp.fclose();
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }

            /* Allocate memory for mixture gaussian densities if not already allocated */
            output = ckd_alloc.ckd_calloc_3d<Pointer<float>>((uint)n_mgau, (uint)n_feat, (uint)n_density);
            buf = ckd_alloc.ckd_calloc<float>(n);
            for (i = 0, l = 0; i < n_mgau; i++)
            {
                for (j = 0; j < n_feat; j++)
                {
                    for (k = 0; k < n_density; k++)
                    {
                        output[i][j].Set(k, buf.Point(l));
                        l += veclen[j];
                    }
                }
            }

            /* Read mixture gaussian densities data */
            Pointer<byte> buf2 = PointerHelpers.Malloc<byte>(n * 4);
            if (bio.bio_fread(buf2, 4, n, fp, byteswap, chksum) != n)
            {
                err.E_ERROR(string.Format("Failed to read density data from file '{0}'\n", cstring.FromCString(file_name)));
                fp.fclose();
                ckd_alloc.ckd_free_3d(output);
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }

            // LOGAN modified Convert file data from byte to float32
            Pointer<float> upcastBuf = buf2.ReinterpretCast<float>();
            upcastBuf.MemCopyTo(buf, n);

            if (chksum_present != 0)
                bio.bio_verify_chksum(fp, byteswap, chksum.Val);

            if (fp.fread(tmp, 1, 1) == 1)
            {
                err.E_ERROR(string.Format("More data than expected in {0}\n", cstring.FromCString(file_name)));
                fp.fclose();
                ckd_alloc.ckd_free_3d(output);
                return PointerHelpers.NULL<Pointer<Pointer<Pointer<float>>>>();
            }

            fp.fclose();

            err.E_INFO(string.Format("{0} codebook, {1} feature, size: \n", n_mgau, n_feat));
            for (i = 0; i < n_feat; i++)
            {
                err.E_INFO(string.Format(" {0}x{1}\n", n_density, veclen[i]));
            }

            return output;
        }

        public static void gauden_param_free(Pointer<Pointer<Pointer<Pointer<float>>>> p)
        {
            ckd_alloc.ckd_free(p[0][0][0]);
            ckd_alloc.ckd_free_3d(p);
        }

        /*
         * Some of the gaussian density computation can be carried out in advance:
         * 	log(determinant) calculation,
         * 	1/(2*var) in the exponent,
         * NOTE; The density computation is performed in log domain.
         */
        public static int gauden_dist_precompute(Pointer<gauden_t> g, Pointer<logmath_t> lmath, float varfloor)
        {
            int i, m, f, d, flen;
            Pointer<float> meanp;
            Pointer<float> varp;
            Pointer<float> detp;
            int floored;

            floored = 0;
            /* Allocate space for determinants */
            g.Deref.det = ckd_alloc.ckd_calloc_3d<float>((uint)g.Deref.n_mgau, (uint)g.Deref.n_feat, (uint)g.Deref.n_density);

            for (m = 0; m < g.Deref.n_mgau; m++)
            {
                for (f = 0; f < g.Deref.n_feat; f++)
                {
                    flen = g.Deref.featlen[f];

                    /* Determinants for all variance vectors in g.Deref.[m][f] */
                    for (d = 0, detp = g.Deref.det[m][f]; d < g.Deref.n_density; d++, detp++)
                    {
                        detp.Deref = 0;
                        for (i = 0, varp = g.Deref.var[m][f][d], meanp = g.Deref.mean[m][f][d];
                             i < flen; i++, varp++, meanp++)
                        {
                            Pointer<float> fvarp = varp;
                            if (fvarp.Deref < varfloor)
                            {
                                fvarp.Deref = varfloor;
                                ++floored;
                            }

                            detp.Deref = detp.Deref + (float)logmath.logmath_log(lmath,
                                                         1.0 / Math.Sqrt(fvarp.Deref * 2.0 * 3.1415926535897932385e0));
                            /* Precompute this part of the exponential */
                            varp.Deref = (float)logmath.logmath_ln_to_log(lmath,
                                                              (1.0 / (fvarp.Deref * 2.0)));
                        }
                        
                    }
                }
            }

            err.E_INFO(string.Format("{0} variance values floored\n", floored));

            return 0;
        }


        public static Pointer<gauden_t> gauden_init(Pointer<byte> meanfile, Pointer<byte> varfile, float varfloor, Pointer<logmath_t> lmath)
        {
            int i, m, f, d;
            Pointer<int> flen;
            Pointer<gauden_t> g;

            SphinxAssert.assert(meanfile.IsNonNull);
            SphinxAssert.assert(varfile.IsNonNull);
            SphinxAssert.assert(varfloor > 0.0);

            g = ckd_alloc.ckd_calloc_struct<gauden_t>(1);
            g.Deref.lmath = lmath;

            BoxedValueInt out_n_mgau = new BoxedValueInt();
            BoxedValueInt out_n_feat = new BoxedValueInt();
            BoxedValueInt out_n_density = new BoxedValueInt();
            BoxedValue<Pointer<int>> boxed_featlen = new BoxedValue<Pointer<int>>();
            g.Deref.mean = gauden_param_read(meanfile, out_n_mgau, out_n_feat, out_n_density, boxed_featlen);
            if (g.Deref.mean.IsNull)
            {
                return PointerHelpers.NULL<gauden_t>();
            }
            g.Deref.n_mgau = out_n_mgau.Val;
            g.Deref.n_feat = out_n_feat.Val;
            g.Deref.n_density = out_n_density.Val;
            g.Deref.featlen = boxed_featlen.Val;

            g.Deref.var = gauden_param_read(varfile, out_n_mgau, out_n_feat, out_n_density, boxed_featlen);
            if (g.Deref.var.IsNull)
            {
                return PointerHelpers.NULL<gauden_t>();
            }
            m = out_n_mgau.Val;
            f = out_n_feat.Val;
            d = out_n_density.Val;
            flen = boxed_featlen.Val;

            /* Verify mean and variance parameter dimensions */
            if ((m != g.Deref.n_mgau) || (f != g.Deref.n_feat) || (d != g.Deref.n_density))
            {
                err.E_ERROR
                    ("Mixture-gaussians dimensions for means and variances differ\n");
                ckd_alloc.ckd_free(flen);
                gauden_free(g);
                return PointerHelpers.NULL<gauden_t>();
            }
            for (i = 0; i < g.Deref.n_feat; i++)
            {
                if (g.Deref.featlen[i] != flen[i])
                {
                    err.E_ERROR("Feature lengths for means and variances differ\n");
                    ckd_alloc.ckd_free(flen);
                    gauden_free(g);
                    return PointerHelpers.NULL<gauden_t>();
                }
            }

            ckd_alloc.ckd_free(flen);

            gauden_dist_precompute(g, lmath, varfloor);

            return g;
        }

        public static void gauden_free(Pointer<gauden_t> g)
        {
            if (g.IsNull)
                return;
            if (g.Deref.mean.IsNonNull)
                gauden_param_free(g.Deref.mean);
            if (g.Deref.var.IsNonNull)
                gauden_param_free(g.Deref.var);
            if (g.Deref.det.IsNonNull)
                ckd_alloc.ckd_free_3d(g.Deref.det);
            if (g.Deref.featlen.IsNonNull)
                ckd_alloc.ckd_free(g.Deref.featlen);
            ckd_alloc.ckd_free(g);
        }

        /* See compute_dist below */
        public static int compute_dist_all(Pointer<gauden_dist_t> out_dist, Pointer<float> obs, int featlen,
                         Pointer<Pointer<float>> mean, Pointer<Pointer<float>> var, Pointer<float> det,
                         int n_density)
        {
            int i, d;

            for (d = 0; d < n_density; ++d)
            {
                Pointer<float> m;
                Pointer<float> v;
                float dval;

                m = mean[d];
                v = var[d];
                dval = det[d];

                for (i = 0; i < featlen; i++)
                {
                    float diff;
                    diff = obs[i] - m[i];
                    /* The compiler really likes this to be a single
                     * expression, for whatever reason. */
                    dval -= diff * diff * v[i];
                }

                out_dist[d].dist = dval;
                out_dist[d].id = d;
            }

            return 0;
        }


        /*
         * Compute the top-N closest gaussians from the chosen set (mgau,feat)
         * for the given input observation vector.
         */
        public static int
        compute_dist(Pointer<gauden_dist_t> out_dist, int n_top,
                     Pointer<float> obs, int featlen,
                     Pointer<Pointer<float>> mean, Pointer<Pointer<float>> var, Pointer<float> det,
                     int n_density)
        {
            int i, j, d;
            Pointer<gauden_dist_t> worst;

            /* Special case optimization when n_density <= n_top */
            if (n_top >= n_density)
                return (compute_dist_all
                        (out_dist, obs, featlen, mean, var, det, n_density));

            for (i = 0; i < n_top; i++)
                out_dist[i].dist = WORST_DIST;
            worst = out_dist.Point(n_top - 1);

            for (d = 0; d < n_density; d++)
            {
                Pointer<float> m;
                Pointer<float> v;
                float dval;

                m = mean[d];
                v = var[d];
                dval = det[d];

                for (i = 0; (i < featlen) && (dval >= worst.Deref.dist); i++)
                {
                    float diff;
                    diff = obs[i] - m[i];
                    /* The compiler really likes this to be a single
                     * expression, for whatever reason. */
                    dval -= diff * diff * v[i];
                }

                if ((i < featlen) || (dval < worst.Deref.dist))     /* Codeword d worse than worst */
                    continue;

                /* Codeword d at least as good as worst so far; insert in the ordered list */
                for (i = 0; (i < n_top) && (dval < out_dist[i].dist); i++) ;
                SphinxAssert.assert(i < n_top);
                for (j = n_top - 1; j > i; --j)
                    out_dist[j] = out_dist[j - 1];
                out_dist[i].dist = dval;
                out_dist[i].id = d;
            }

            return 0;
        }


        /*
         * Compute distances of the input observation from the top N codewords in the given
         * codebook (g.Deref.{mean,var}[mgau]).  The input observation, obs, includes vectors for
         * all features in the codebook.
         */
        public static int
        gauden_dist(Pointer<gauden_t> g,
                    int mgau, int n_top, Pointer<Pointer<float>> obs, Pointer<Pointer<gauden_dist_t>> out_dist)
        {
            int f;

            SphinxAssert.assert((n_top > 0) && (n_top <= g.Deref.n_density));

            for (f = 0; f < g.Deref.n_feat; f++)
            {
                compute_dist(out_dist[f], n_top,
                             obs[f], g.Deref.featlen[f],
                             g.Deref.mean[mgau][f], g.Deref.var[mgau][f], g.Deref.det[mgau][f],
                             g.Deref.n_density);
                err.E_DEBUG(string.Format("Top CW({0},{1}) = {2} {3}\n", mgau, f, out_dist[f][0].id,
                        (int)out_dist[f][0].dist >> hmm.SENSCR_SHIFT));
            }

            return 0;
        }

        public static int
        gauden_mllr_transform(Pointer<gauden_t> g, Pointer<ps_mllr_t> _mllr, Pointer<cmd_ln_t> config)
        {
            int i, m, f, d;
            Pointer<int> flen;

            /* Free data if already here */
            if (g.Deref.mean.IsNonNull)
                gauden_param_free(g.Deref.mean);
            if (g.Deref.var.IsNonNull)
                gauden_param_free(g.Deref.var);
            if (g.Deref.det.IsNonNull)
                ckd_alloc.ckd_free_3d(g.Deref.det);
            if (g.Deref.featlen.IsNonNull)
                ckd_alloc.ckd_free(g.Deref.featlen);
            g.Deref.det = PointerHelpers.NULL<Pointer<Pointer<float>>>();
            g.Deref.featlen = PointerHelpers.NULL<int>();

            /* Reload means and variances (un-precomputed). */
            BoxedValueInt out_n_mgau = new BoxedValueInt();
            BoxedValueInt out_n_feat = new BoxedValueInt();
            BoxedValueInt out_n_density = new BoxedValueInt();
            BoxedValue<Pointer<int>> out_featlen = new BoxedValue<Pointer<int>>();
            g.Deref.mean = gauden_param_read(cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_mean")), out_n_mgau, out_n_feat, out_n_density, out_featlen);
            g.Deref.n_mgau = out_n_mgau.Val;
            g.Deref.n_feat = out_n_feat.Val;
            g.Deref.n_density = out_n_density.Val;
            g.Deref.featlen = out_featlen.Val;

            g.Deref.var = gauden_param_read(cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_var")), out_n_mgau, out_n_feat, out_n_density, out_featlen);
            m = out_n_mgau.Val;
            f = out_n_feat.Val;
            d = out_n_density.Val;
            flen = out_featlen.Val;

            /* Verify mean and variance parameter dimensions */
            if ((m != g.Deref.n_mgau) || (f != g.Deref.n_feat) || (d != g.Deref.n_density))
                err.E_FATAL
                    ("Mixture-gaussians dimensions for means and variances differ\n");
            for (i = 0; i < g.Deref.n_feat; i++)
                if (g.Deref.featlen[i] != flen[i])
                    err.E_FATAL("Feature lengths for means and variances differ\n");
            ckd_alloc.ckd_free(flen);

            /* Transform codebook for each stream s */
            for (i = 0; i < g.Deref.n_mgau; ++i)
            {
                for (f = 0; f < g.Deref.n_feat; ++f)
                {
                    Pointer<double> temp;
                    temp = ckd_alloc.ckd_calloc<double>(g.Deref.featlen[f]);
                    /* Transform each density d in selected codebook */
                    for (d = 0; d < g.Deref.n_density; d++)
                    {
                        int l;
                        for (l = 0; l < g.Deref.featlen[f]; l++)
                        {
                            temp[l] = 0.0;
                            for (m = 0; m < g.Deref.featlen[f]; m++)
                            {
                                /* FIXME: For now, only one class, hence the zeros below. */
                                temp[l] += _mllr.Deref.A[f][0][l][m] * g.Deref.mean[i][f][d][m];
                            }
                            temp[l] += _mllr.Deref.b[f][0][l];
                        }
                        
                        for (l = 0; l < g.Deref.featlen[f]; l++)
                        {
                            g.Deref.mean[i][f][d].Set(l, (float)temp[l]);
                            g.Deref.var[i][f][d].Set(l, g.Deref.var[i][f][d][l] * _mllr.Deref.h[f][0][l]);
                        }
                    }
                    ckd_alloc.ckd_free(temp);
                }
            }

            /* Re-precompute (if we aren't adapting variances this isn't
             * actually necessary...) */
            gauden_dist_precompute(g, g.Deref.lmath, (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-varfloor")));
            return 0;
        }
    }
}
