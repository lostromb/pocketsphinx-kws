using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class feat
    {
        public const int LIVEBUFBLOCKSIZE = 256;
        public const int S3_MAX_FRAMES = 15000;
        public const int FEAT_DCEP_WIN = 2;
        public static readonly Pointer<byte> FEAT_VERSION = cstring.ToCString("1.0");

        public static Pointer<byte> feat_name(Pointer<feat_t> f)
        {
            return f.Deref.name;
        }

        public static int feat_cepsize(Pointer<feat_t> f)
        {
            return f.Deref.cepsize;
        }

        public static int feat_window_size(Pointer<feat_t> f)
        {
            return f.Deref.window_size;
        }

        public static int feat_n_stream(Pointer<feat_t> f)
        {
            return f.Deref.n_stream;
        }

        public static uint feat_stream_len(Pointer<feat_t> f, int i)
        {
            return f.Deref.stream_len[i];
        }

        public static int feat_dimension1(Pointer<feat_t> f)
        {
            return ((f).Deref.n_sv != 0 ? (f).Deref.n_sv : f.Deref.n_stream);
        }

        public static uint feat_dimension2(Pointer<feat_t> f, int i)
        {
            return ((f).Deref.lda.IsNonNull ? (f).Deref.out_dim : ((f).Deref.sv_len.IsNonNull ? (f).Deref.sv_len[i] : f.Deref.stream_len[i]));
        }

        public static uint feat_dimension(Pointer<feat_t> f)
        {
            return f.Deref.out_dim;
        }

        public static Pointer<Pointer<int>> parse_subvecs(Pointer<byte> str)
        {
            Pointer<byte> strp;
            int n, n2, l;
            Pointer<gnode_t> dimlist;            /* List of dimensions in one subvector */
            Pointer<gnode_t> veclist;            /* List of dimlists (subvectors) */
            Pointer<Pointer<int>> subvec;
            Pointer<gnode_t> gn;
            Pointer<gnode_t> gn2;

            veclist = PointerHelpers.NULL<gnode_t>();

            strp = str;
            for (;;)
            {
                dimlist = PointerHelpers.NULL<gnode_t>();

                for (;;)
                {
                    if (stdio.sscanf_d_n(strp, out n, out l) != 1)
                        err.E_FATAL(string.Format("'%s': Couldn't read int @pos %d\n", str,
                                strp - str));
                    strp += l;

                    if (strp.Deref == '-')
                    {
                        strp++;

                        if (stdio.sscanf_d_n(strp, out n2, out l) != 1)
                            err.E_FATAL(string.Format("'%s': Couldn't read int @pos %d\n", str,
                                    strp - str));
                        strp += l;
                    }
                    else
                        n2 = n;

                    if ((n < 0) || (n > n2))
                        err.E_FATAL(string.Format("'%s': Bad subrange spec ending @pos %d\n", str,
                                strp - str));

                    for (; n <= n2; n++)
                    {
                        for (gn = dimlist; gn.IsNonNull; gn = glist.gnode_next(gn))
                            if (glist.gnode_int32(gn) == n)
                                break;
                        if (gn.IsNonNull)
                            err.E_FATAL(string.Format("'%s': Duplicate dimension ending @pos %d\n",
                                    str, strp - str));

                        dimlist = glist.glist_add_int(dimlist, n);
                    }

                    if ((strp.Deref == '\0') || (strp.Deref == '/'))
                        break;

                    if (strp.Deref != ',')
                        err.E_FATAL(string.Format("'%s': Bad delimiter @pos %d\n", str, strp - str));

                    strp++;
                }

                veclist = glist.glist_add_ptr(veclist, dimlist);

                if (strp.Deref == '\0')
                    break;

                SphinxAssert.assert(strp.Deref == '/');
                strp++;
            }

            /* Convert the glists to arrays; remember the glists are in reverse order of the input! */
            n = glist.glist_count(veclist);   /* #Subvectors */
            subvec = ckd_alloc.ckd_calloc<Pointer<int>>(n + 1);     /* +1 for sentinel */
            subvec[n] = PointerHelpers.NULL<int>();           /* sentinel */

            for (--n, gn = veclist; (n >= 0) && gn.IsNonNull; gn = glist.gnode_next(gn), --n)
            {
                gn2 = (Pointer<gnode_t>)glist.gnode_ptr(gn);

                n2 = glist.glist_count(gn2);  /* Length of this subvector */
                if (n2 <= 0)
                    err.E_FATAL(string.Format("'%s': 0-length subvector\n", str));

                subvec[n] = ckd_alloc.ckd_calloc<int>(n2 + 1);        /* +1 for sentinel */
                subvec[n].Set(n2, -1);     /* sentinel */

                for (--n2; (n2 >= 0) && gn2.IsNonNull; gn2 = glist.gnode_next(gn2), --n2)
                {
                    subvec[n].Set(n2, glist.gnode_int32(gn2));
                }

                SphinxAssert.assert((n2 < 0) && (gn2.IsNull));
            }

            SphinxAssert.assert((n < 0) && (gn.IsNull));

            /* Free the glists */
            for (gn = veclist; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                gn2 = (Pointer<gnode_t>)glist.gnode_ptr(gn);
                glist.glist_free(gn2);
            }
            glist.glist_free(veclist);

            return subvec;
        }

        public static void subvecs_free(Pointer<Pointer<int>> subvecs)
        {
            Pointer<Pointer<int>> sv;

            for (sv = subvecs; sv.IsNonNull && sv.Deref.IsNonNull; ++sv)
                ckd_alloc.ckd_free(sv.Deref);
            ckd_alloc.ckd_free(subvecs);
        }

        public static int feat_set_subvecs(Pointer<feat_t> fcb, Pointer<Pointer<int>> subvecs)
        {
            Pointer<Pointer<int>> sv;
            uint n_sv, n_dim, i;

            if (subvecs.IsNull)
            {
                subvecs_free(fcb.Deref.subvecs);
                ckd_alloc.ckd_free(fcb.Deref.sv_buf);
                ckd_alloc.ckd_free(fcb.Deref.sv_len);
                fcb.Deref.n_sv = 0;
                fcb.Deref.subvecs = PointerHelpers.NULL<Pointer<int>>();
                fcb.Deref.sv_len = PointerHelpers.NULL<uint>();
                fcb.Deref.sv_buf = PointerHelpers.NULL<float>();
                fcb.Deref.sv_dim = 0;
                return 0;
            }

            if (fcb.Deref.n_stream != 1)
            {
                err.E_ERROR("Subvector specifications require single-stream features!");
                return -1;
            }

            n_sv = 0;
            n_dim = 0;
            for (sv = subvecs; sv.IsNonNull && sv.Deref.IsNonNull; ++sv)
            {
                Pointer<int> d;

                for (d = sv.Deref; d.IsNonNull && d.Deref != -1; ++d)
                {
                    ++n_dim;
                }
                ++n_sv;
            }
            if (n_dim > feat_dimension(fcb))
            {
                err.E_ERROR(string.Format("Total dimensionality of subvector specification {0} > feature dimensionality {1}\n", n_dim, feat_dimension(fcb)));
                return -1;
            }

            fcb.Deref.n_sv = checked((int)n_sv);
            fcb.Deref.subvecs = subvecs;
            fcb.Deref.sv_len = ckd_alloc.ckd_calloc<uint>(n_sv);
            fcb.Deref.sv_buf = ckd_alloc.ckd_calloc<float>(n_dim);
            fcb.Deref.sv_dim = checked((int)n_dim);
            for (i = 0; i < n_sv; ++i)
            {
                Pointer<int> d;
                for (d = subvecs[i]; d.IsNonNull && d.Deref != -1; ++d)
                {
                    ++fcb.Deref.sv_len[i];
                }
            }

            return 0;
        }

        /**
         * Project feature components to subvectors (if any).
         */
        public static void feat_subvec_project(Pointer<feat_t> fcb, Pointer<Pointer<Pointer<float>>> inout_feat, uint nfr)
        {
            uint i;

            if (fcb.Deref.subvecs.IsNull)
                return;
            for (i = 0; i < nfr; ++i)
            {
                Pointer<float> output;
                int j;

                output = fcb.Deref.sv_buf;
                for (j = 0; j < fcb.Deref.n_sv; ++j)
                {
                    Pointer<int> d;
                    for (d = fcb.Deref.subvecs[j]; d.IsNonNull && d.Deref != -1; ++d)
                    {
                        output.Deref = inout_feat[i][0][d.Deref];
                        output = output.Point(1);
                    }
                }

                fcb.Deref.sv_buf.MemCopyTo(inout_feat[i][0], fcb.Deref.sv_dim);
            }
        }

        public static Pointer<Pointer<Pointer<float>>> feat_array_alloc(Pointer<feat_t> fcb, int nfr)
        {
            int i, j, k;
            Pointer<float> data;
            Pointer<float> d;
            Pointer<Pointer<Pointer<float>>> feats;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(nfr > 0);
            SphinxAssert.assert(feat_dimension(fcb) > 0);

            /* Make sure to use the dimensionality of the features *before*
               LDA and subvector projection. */
            k = 0;
            for (i = 0; i < fcb.Deref.n_stream; ++i)
            {
                k = checked((int)(k + fcb.Deref.stream_len[i]));
            }
            SphinxAssert.assert(k >= feat_dimension(fcb));
            SphinxAssert.assert(k >= fcb.Deref.sv_dim);

            feats = ckd_alloc.ckd_calloc_2d<Pointer<float>>((uint)nfr, (uint)feat_dimension1(fcb));
            data = ckd_alloc.ckd_calloc<float>(nfr * k);

            for (i = 0; i < nfr; i++)
            {
                d = data + i * k;
                for (j = 0; j < feat_dimension1(fcb); j++)
                {
                    feats[i].Set(j, d);
                    d += feat_dimension2(fcb, j);
                }
            }

            return feats;
        }

        public static Pointer<Pointer<Pointer<float>>> feat_array_realloc(Pointer<feat_t> fcb, Pointer<Pointer<Pointer<float>>> old_feat, int ofr, int nfr)
        {
            int i, k, cf;
            Pointer<Pointer<Pointer<float>>> new_feat;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(nfr > 0);
            SphinxAssert.assert(ofr > 0);
            SphinxAssert.assert(feat_dimension(fcb) > 0);

            /* Make sure to use the dimensionality of the features *before*
               LDA and subvector projection. */
            k = 0;
            for (i = 0; i < fcb.Deref.n_stream; ++i)
            {
                k = checked((int)(k + fcb.Deref.stream_len[i]));
            }
            SphinxAssert.assert(k >= feat_dimension(fcb));
            SphinxAssert.assert(k >= fcb.Deref.sv_dim);

            new_feat = feat_array_alloc(fcb, nfr);

            cf = (nfr < ofr) ? nfr : ofr;
            old_feat[0][0].MemCopyTo(new_feat[0][0], cf * k);

            feat_array_free(old_feat);

            return new_feat;
        }

        public static void feat_array_free(Pointer<Pointer<Pointer<float>>> feat)
        {
            ckd_alloc.ckd_free(feat[0][0]);
            ckd_alloc.ckd_free_2d(feat);
        }

        public static void feat_s2_4x_cep2feat(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            Pointer<float> f;
            Pointer<float> w;
            Pointer<float> _w;
            Pointer<float> w1;
            Pointer<float> w_1;
            Pointer<float> _w1;
            Pointer<float> _w_1;
            float d1, d2;
            int i, j;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(feat_cepsize(fcb) == 13);
            SphinxAssert.assert(feat_n_stream(fcb) == 4);
            SphinxAssert.assert(feat_stream_len(fcb, 0) == 12);
            SphinxAssert.assert(feat_stream_len(fcb, 1) == 24);
            SphinxAssert.assert(feat_stream_len(fcb, 2) == 3);
            SphinxAssert.assert(feat_stream_len(fcb, 3) == 12);
            SphinxAssert.assert(feat_window_size(fcb) == 4);

            /* CEP; skip C0 */
            (mfc[0] + 1).MemCopyTo(feats[0], (feat_cepsize(fcb) - 1));

            /*
             * DCEP(SHORT): mfc[2] - mfc[-2]
             * DCEP(LONG):  mfc[4] - mfc[-4]
             */
            w = mfc[2] + 1;             /* +1 to skip C0 */
            _w = mfc[-2] + 1;

            f = feats[1];
            for (i = 0; i < feat_cepsize(fcb) - 1; i++) /* Short-term */
                f[i] = w[i] - _w[i];

            w = mfc[4] + 1;             /* +1 to skip C0 */
            _w = mfc[-4] + 1;

            for (j = 0; j < feat_cepsize(fcb) - 1; i++, j++)    /* Long-term */
                f[i] = w[j] - _w[j];

            /* D2CEP: (mfc[3] - mfc[-1]) - (mfc[1] - mfc[-3]) */
            w1 = mfc[3] + 1;            /* Final +1 to skip C0 */
            _w1 = mfc[-1] + 1;
            w_1 = mfc[1] + 1;
            _w_1 = mfc[-3] + 1;

            f = feats[3];
            for (i = 0; i < feat_cepsize(fcb) - 1; i++)
            {
                d1 = w1[i] - _w1[i];
                d2 = w_1[i] - _w_1[i];

                f[i] = d1 - d2;
            }

            /* POW: C0, DC0, D2C0; differences computed as above for rest of cep */
            f = feats[2];
            f[0] = mfc[0][0];
            f[1] = mfc[2][0] - mfc[-2][0];

            d1 = mfc[3][0] - mfc[-1][0];
            d2 = mfc[1][0] - mfc[-3][0];
            f[2] = d1 - d2;
        }


        public static void feat_s3_1x39_cep2feat(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            Pointer<float> f;
            Pointer<float> w;
            Pointer<float> _w;
            Pointer<float> w1;
            Pointer<float> w_1;
            Pointer<float> _w1;
            Pointer<float> _w_1;
            float d1, d2;
            int i;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(feat_cepsize(fcb) == 13);
            SphinxAssert.assert(feat_n_stream(fcb) == 1);
            SphinxAssert.assert(feat_stream_len(fcb, 0) == 39);
            SphinxAssert.assert(feat_window_size(fcb) == 3);

            /* CEP; skip C0 */
            (mfc[0] + 1).MemCopyTo(feats[0], (feat_cepsize(fcb) - 1));
            /*
             * DCEP: mfc[2] - mfc[-2];
             */
            f = feats[0] + feat_cepsize(fcb) - 1;
            w = mfc[2] + 1;             /* +1 to skip C0 */
            _w = mfc[-2] + 1;

            for (i = 0; i < feat_cepsize(fcb) - 1; i++)
                f[i] = w[i] - _w[i];

            /* POW: C0, DC0, D2C0 */
            f += feat_cepsize(fcb) - 1;

            f[0] = mfc[0][0];
            f[1] = mfc[2][0] - mfc[-2][0];

            d1 = mfc[3][0] - mfc[-1][0];
            d2 = mfc[1][0] - mfc[-3][0];
            f[2] = d1 - d2;

            /* D2CEP: (mfc[3] - mfc[-1]) - (mfc[1] - mfc[-3]) */
            f += 3;

            w1 = mfc[3] + 1;            /* Final +1 to skip C0 */
            _w1 = mfc[-1] + 1;
            w_1 = mfc[1] + 1;
            _w_1 = mfc[-3] + 1;

            for (i = 0; i < feat_cepsize(fcb) - 1; i++)
            {
                d1 = w1[i] - _w1[i];
                d2 = w_1[i] - _w_1[i];

                f[i] = d1 - d2;
            }
        }


        public static void feat_s3_cep(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(feat_n_stream(fcb) == 1);
            SphinxAssert.assert(feat_window_size(fcb) == 0);

            /* CEP */
            mfc[0].MemCopyTo(feats[0], feat_cepsize(fcb));
        }

        public static void feat_s3_cep_dcep(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            Pointer<float> f;
            Pointer<float> w;
            Pointer<float> _w;
            int i;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(feat_n_stream(fcb) == 1);
            SphinxAssert.assert(feat_stream_len(fcb, 0) == feat_cepsize(fcb) * 2);
            SphinxAssert.assert(feat_window_size(fcb) == 2);

            /* CEP */
            mfc[0].MemCopyTo(feats[0], feat_cepsize(fcb));

            /*
             * DCEP: mfc[2] - mfc[-2];
             */
            f = feats[0] + feat_cepsize(fcb);
            w = mfc[2];
            _w = mfc[-2];

            for (i = 0; i < feat_cepsize(fcb); i++)
                f[i] = w[i] - _w[i];
        }

        public static void feat_1s_c_d_dd_cep2feat(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            Pointer<float> f;
            Pointer<float> w;
            Pointer<float> _w;
            Pointer<float> w1;
            Pointer<float> w_1;
            Pointer<float> _w1;
            Pointer<float> _w_1;
            float d1, d2;
            int i;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(feat_n_stream(fcb) == 1);
            SphinxAssert.assert(feat_stream_len(fcb, 0) == feat_cepsize(fcb) * 3);
            SphinxAssert.assert(feat_window_size(fcb) == FEAT_DCEP_WIN + 1);

            /* CEP */
            mfc[0].MemCopyTo(feats[0], feat_cepsize(fcb));

            /*
             * DCEP: mfc[w] - mfc[-w], where w = FEAT_DCEP_WIN;
             */
            f = feats[0] + feat_cepsize(fcb);
            w = mfc[FEAT_DCEP_WIN];
            _w = mfc[-FEAT_DCEP_WIN];

            for (i = 0; i < feat_cepsize(fcb); i++)
                f[i] = w[i] - _w[i];

            /* 
             * D2CEP: (mfc[w+1] - mfc[-w+1]) - (mfc[w-1] - mfc[-w-1]), 
             * where w = FEAT_DCEP_WIN 
             */
            f += feat_cepsize(fcb);

            w1 = mfc[FEAT_DCEP_WIN + 1];
            _w1 = mfc[-FEAT_DCEP_WIN + 1];
            w_1 = mfc[FEAT_DCEP_WIN - 1];
            _w_1 = mfc[-FEAT_DCEP_WIN - 1];

            for (i = 0; i < feat_cepsize(fcb); i++)
            {
                d1 = w1[i] - _w1[i];
                d2 = w_1[i] - _w_1[i];

                f[i] = d1 - d2;
            }
        }

        public static void feat_1s_c_d_ld_dd_cep2feat(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            Pointer<float> f;
            Pointer<float> w;
            Pointer<float> _w;
            Pointer<float> w1;
            Pointer<float> w_1;
            Pointer<float> _w1;
            Pointer<float> _w_1;
            float d1, d2;
            int i;

            SphinxAssert.assert(fcb.IsNonNull);
            SphinxAssert.assert(feat_n_stream(fcb) == 1);
            SphinxAssert.assert(feat_stream_len(fcb, 0) == feat_cepsize(fcb) * 4);
            SphinxAssert.assert(feat_window_size(fcb) == FEAT_DCEP_WIN * 2);

            /* CEP */
            mfc[0].MemCopyTo(feats[0], feat_cepsize(fcb));

            /*
             * DCEP: mfc[w] - mfc[-w], where w = FEAT_DCEP_WIN;
             */
            f = feats[0] + feat_cepsize(fcb);
            w = mfc[FEAT_DCEP_WIN];
            _w = mfc[-FEAT_DCEP_WIN];

            for (i = 0; i < feat_cepsize(fcb); i++)
                f[i] = w[i] - _w[i];

            /*
             * LDCEP: mfc[w] - mfc[-w], where w = FEAT_DCEP_WIN * 2;
             */
            f += feat_cepsize(fcb);
            w = mfc[FEAT_DCEP_WIN * 2];
            _w = mfc[-FEAT_DCEP_WIN * 2];

            for (i = 0; i < feat_cepsize(fcb); i++)
                f[i] = w[i] - _w[i];

            /* 
             * D2CEP: (mfc[w+1] - mfc[-w+1]) - (mfc[w-1] - mfc[-w-1]), 
             * where w = FEAT_DCEP_WIN 
             */
            f += feat_cepsize(fcb);

            w1 = mfc[FEAT_DCEP_WIN + 1];
            _w1 = mfc[-FEAT_DCEP_WIN + 1];
            w_1 = mfc[FEAT_DCEP_WIN - 1];
            _w_1 = mfc[-FEAT_DCEP_WIN - 1];

            for (i = 0; i < feat_cepsize(fcb); i++)
            {
                d1 = w1[i] - _w1[i];
                d2 = w_1[i] - _w_1[i];

                f[i] = d1 - d2;
            }
        }

        public static void feat_copy(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, Pointer<Pointer<float>> feats)
        {
            int win, i, j;

            win = feat_window_size(fcb);

            /* Concatenate input features */
            for (i = -win; i <= win; ++i)
            {
                uint spos = 0;

                for (j = 0; j < feat_n_stream(fcb); ++j)
                {
                    uint stream_len;

                    /* Unscale the stream length by the window. */
                    stream_len = checked((uint)(feat_stream_len(fcb, j) / (2 * win + 1)));
                    (mfc[i] + spos).MemCopyTo(feats[j] + (int)((i + win) * stream_len), (int)stream_len);
                    spos += stream_len;
                }
            }
        }

        public static Pointer<feat_t> feat_init(Pointer<byte> type, int _cmn, int varnorm, int _agc, int breport, int cepsize)
        {
            Pointer<feat_t> fcb;

            if (cepsize == 0)
                cepsize = 13;
            if (breport != 0)
                err.E_INFO
                    (string.Format("Initializing feature stream to type: '{0}', ceplen={1}, CMN='{2}', VARNORM='{3}', AGC='{4}'\n",
                     cstring.FromCString(type),
                     cepsize,
                     cstring.FromCString(cmn.cmn_type_str[_cmn]),
                     varnorm != 0 ? "yes" : "no",
                     cstring.FromCString(agc.agc_type_str[_agc])));

            fcb = ckd_alloc.ckd_calloc_struct<feat_t>(1);
            fcb.Deref.refcount = 1;
            fcb.Deref.name = ckd_alloc.ckd_salloc(type);
            if (cstring.strcmp(type, cstring.ToCString("s2_4x")) == 0)
            {
                /* Sphinx-II format 4-stream feature (Hack!! hardwired constants below) */
                if (cepsize != 13)
                {
                    err.E_ERROR("s2_4x features require cepsize == 13\n");
                    ckd_alloc.ckd_free(fcb);
                    return PointerHelpers.NULL<feat_t>();
                }
                fcb.Deref.cepsize = 13;
                fcb.Deref.n_stream = 4;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(4);
                fcb.Deref.stream_len[0] = 12;
                fcb.Deref.stream_len[1] = 24;
                fcb.Deref.stream_len[2] = 3;
                fcb.Deref.stream_len[3] = 12;
                fcb.Deref.out_dim = 51;
                fcb.Deref.window_size = 4;
                fcb.Deref.compute_feat_func = feat_s2_4x_cep2feat;
            }
            else if ((cstring.strcmp(type, cstring.ToCString("s3_1x39")) == 0) || (cstring.strcmp(type, cstring.ToCString("1s_12c_12d_3p_12dd")) == 0))
            {
                /* 1-stream cep/dcep/pow/ddcep (Hack!! hardwired constants below) */
                if (cepsize != 13)
                {
                    err.E_ERROR("s2_4x features require cepsize == 13\n");
                    ckd_alloc.ckd_free(fcb);
                    return PointerHelpers.NULL<feat_t>();
                }
                fcb.Deref.cepsize = 13;
                fcb.Deref.n_stream = 1;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(1);
                fcb.Deref.stream_len[0] = 39;
                fcb.Deref.out_dim = 39;
                fcb.Deref.window_size = 3;
                fcb.Deref.compute_feat_func = feat_s3_1x39_cep2feat;
            }
            else if (cstring.strncmp(type, cstring.ToCString("1s_c_d_dd"), 9) == 0)
            {
                fcb.Deref.cepsize = cepsize;
                fcb.Deref.n_stream = 1;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(1);
                fcb.Deref.stream_len[0] = (uint)(cepsize * 3);
                fcb.Deref.out_dim = (uint)(cepsize * 3);
                fcb.Deref.window_size = FEAT_DCEP_WIN + 1; /* ddcep needs the extra 1 */
                fcb.Deref.compute_feat_func = feat_1s_c_d_dd_cep2feat;
            }
            else if (cstring.strncmp(type, cstring.ToCString("1s_c_d_ld_dd"), 12) == 0)
            {
                fcb.Deref.cepsize = cepsize;
                fcb.Deref.n_stream = 1;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(1);
                fcb.Deref.stream_len[0] = (uint)(cepsize * 4);
                fcb.Deref.out_dim = (uint)(cepsize * 4);
                fcb.Deref.window_size = FEAT_DCEP_WIN * 2;
                fcb.Deref.compute_feat_func = feat_1s_c_d_ld_dd_cep2feat;
            }
            else if (cstring.strncmp(type, cstring.ToCString("cep_dcep"), 8) == 0 || cstring.strncmp(type, cstring.ToCString("1s_c_d"), 6) == 0)
            {
                /* 1-stream cep/dcep */
                fcb.Deref.cepsize = cepsize;
                fcb.Deref.n_stream = 1;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(1);
                fcb.Deref.stream_len[0] = (uint)(feat_cepsize(fcb) * 2);
                fcb.Deref.out_dim = fcb.Deref.stream_len[0];
                fcb.Deref.window_size = 2;
                fcb.Deref.compute_feat_func = feat_s3_cep_dcep;
            }
            else if (cstring.strncmp(type, cstring.ToCString("cep"), 3) == 0 || cstring.strncmp(type, cstring.ToCString("1s_c"), 4) == 0)
            {
                /* 1-stream cep */
                fcb.Deref.cepsize = cepsize;
                fcb.Deref.n_stream = 1;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(1);
                fcb.Deref.stream_len[0] = (uint)feat_cepsize(fcb);
                fcb.Deref.out_dim = fcb.Deref.stream_len[0];
                fcb.Deref.window_size = 0;
                fcb.Deref.compute_feat_func = feat_s3_cep;
            }
            else if (cstring.strncmp(type, cstring.ToCString("1s_3c"), 5) == 0 || cstring.strncmp(type, cstring.ToCString("1s_4c"), 5) == 0)
            {
                /* 1-stream cep with frames concatenated, so called cepwin features */
                if (cstring.strncmp(type, cstring.ToCString("1s_3c"), 5) == 0)
                    fcb.Deref.window_size = 3;
                else
                    fcb.Deref.window_size = 4;

                fcb.Deref.cepsize = cepsize;
                fcb.Deref.n_stream = 1;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(1);
                fcb.Deref.stream_len[0] = (uint)(feat_cepsize(fcb) * (2 * fcb.Deref.window_size + 1));
                fcb.Deref.out_dim = fcb.Deref.stream_len[0];
                fcb.Deref.compute_feat_func = feat_copy;
            }
            else
            {
                int i, k, l;
                uint len;
                Pointer<byte> strp;
                Pointer<byte> mtype = ckd_alloc.ckd_salloc(type);
                Pointer<byte> wd = ckd_alloc.ckd_salloc(type);
                /*
                 * Generic definition: Format should be %d,%d,%d,...,%d (i.e.,
                 * comma separated list of feature stream widths; #items =
                 * #streams).  An optional window size (frames will be
                 * concatenated) is also allowed, which can be specified with
                 * a colon after the list of feature streams.
                 */
                len = cstring.strlen(mtype);
                k = 0;
                for (i = 1; i < len - 1; i++)
                {
                    if (mtype[i] == ',')
                    {
                        mtype[i] = (byte)' ';
                        k++;
                    }
                    else if (mtype[i] == ':')
                    {
                        mtype[i] = (byte)'\0';
                        fcb.Deref.window_size = cstring.atoi(mtype + i + 1);
                        break;
                    }
                }
                k++;                    /* Presumably there are (#commas+1) streams */
                fcb.Deref.n_stream = k;
                fcb.Deref.stream_len = ckd_alloc.ckd_calloc<uint>(k);

                /* Scan individual feature stream lengths */
                strp = mtype;
                i = 0;
                fcb.Deref.out_dim = 0;
                fcb.Deref.cepsize = 0;
                while (stdio.sscanf_s_n(strp, wd, out l) == 1)
                {
                    strp += l;
                    uint t1 = 0;
                    if ((i >= fcb.Deref.n_stream)
                        || (stdio.sscanf_u(wd, out t1) != 1)
                        || (t1 <= 0))
                        err.E_FATAL("Bad feature type argument\n");
                    fcb.Deref.stream_len[i] = t1;
                    /* Input size before windowing */
                    fcb.Deref.cepsize = checked((int)(fcb.Deref.cepsize + fcb.Deref.stream_len[i]));
                    if (fcb.Deref.window_size > 0)
                        fcb.Deref.stream_len[i] = checked((uint)(fcb.Deref.stream_len[i] * (fcb.Deref.window_size * 2 + 1)));
                    /* Output size after windowing */
                    fcb.Deref.out_dim += fcb.Deref.stream_len[i];
                    i++;
                }
                if (i != fcb.Deref.n_stream)
                    err.E_FATAL("Bad feature type argument\n");
                if (fcb.Deref.cepsize != cepsize)
                    err.E_FATAL("Bad feature type argument\n");

                /* Input is already the feature stream */
                fcb.Deref.compute_feat_func = feat_copy;
                ckd_alloc.ckd_free(mtype);
                ckd_alloc.ckd_free(wd);
            }

            if (_cmn != cmn_type_e.CMN_NONE)
                fcb.Deref.cmn_struct = cmn.cmn_init(feat_cepsize(fcb));
            fcb.Deref.cmn = _cmn;
            fcb.Deref.varnorm = varnorm;
            if (_agc != agc_type_e.AGC_NONE)
            {
                fcb.Deref.agc_struct = agc.agc_init();
                /*
                 * No need to check if agc is set to EMAX; agc_emax_set() changes only emax related things
                 * Moreover, if agc is not NONE and block mode is used, feat_agc() SILENTLY
                 * switches to EMAX
                 */
                /* HACK: hardwired initial estimates based on use of CMN (from Sphinx2) */
                agc.agc_emax_set(fcb.Deref.agc_struct, (_cmn != cmn_type_e.CMN_NONE) ? 5.0f : 10.0f);
            }
            fcb.Deref.agc = _agc;
            /*
             * Make sure this buffer is large enough to be used in feat_s2mfc2feat_block_utt()
             */
            fcb.Deref.cepbuf = ckd_alloc.ckd_calloc_2d<float>(
                (LIVEBUFBLOCKSIZE < feat_window_size(fcb) * 2) ? (uint)(feat_window_size(fcb) * 2) : (uint)LIVEBUFBLOCKSIZE, (uint)feat_cepsize(fcb));
            /* This one is actually just an array of pointers to "flatten out"
             * wraparounds. */
            fcb.Deref.tmpcepbuf = ckd_alloc.ckd_calloc<Pointer<float>>(2 * feat_window_size(fcb) + 1);

            return fcb;
        }

        public static void feat_cmn(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, int nfr, int beginutt, int endutt)
        {
            int cmn_type = fcb.Deref.cmn;

            if (!(beginutt != 0 && endutt != 0)
                && cmn_type != cmn_type_e.CMN_NONE) /* Only cmn_prior in block computation mode. */
                fcb.Deref.cmn = cmn_type = cmn_type_e.CMN_LIVE;

            switch (cmn_type)
            {
                case cmn_type_e.CMN_BATCH:
                    cmn.cmn_run(fcb.Deref.cmn_struct, mfc, fcb.Deref.varnorm, nfr);
                    break;
                case cmn_type_e.CMN_LIVE:
                    cmn_live.cmn_live_run(fcb.Deref.cmn_struct, mfc, fcb.Deref.varnorm, nfr);
                    if (endutt != 0)
                        cmn_live.cmn_live_update(fcb.Deref.cmn_struct);
                    break;
                default:
                    break;
            }

            // cep_dump_dbg(fcb, mfc, nfr, "After CMN");
        }

        public static void feat_agc(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, int nfr, int beginutt, int endutt)
        {
            int agc_type = fcb.Deref.agc;

            if (!(beginutt != 0 && endutt != 0)
                && agc_type != agc_type_e.AGC_NONE) /* Only agc_emax in block computation mode. */
                agc_type = agc_type_e.AGC_EMAX;

            switch (agc_type)
            {
                case agc_type_e.AGC_MAX:
                    agc.agc_max(fcb.Deref.agc_struct, mfc, nfr);
                    break;
                case agc_type_e.AGC_EMAX:
                    agc.agc_emax(fcb.Deref.agc_struct, mfc, nfr);
                    if (endutt != 0)
                        agc.agc_emax_update(fcb.Deref.agc_struct);
                    break;
                case agc_type_e.AGC_NOISE:
                    agc.agc_noise(fcb.Deref.agc_struct, mfc, nfr);
                    break;
                default:
                    break;
            }

            // cep_dump_dbg(fcb, mfc, nfr, "After AGC");
        }

        public static void feat_compute_utt(Pointer<feat_t> fcb, Pointer<Pointer<float>> mfc, int nfr, int win, Pointer<Pointer<Pointer<float>>> feats)
        {
            int i;

            // cep_dump_dbg(fcb, mfc, nfr, "Incoming features (after padding)");

            /* Create feature vectors */
            for (i = win; i < nfr - win; i++)
            {
                fcb.Deref.compute_feat_func(fcb, mfc + i, feats[i - win]);
            }

            // feat_print_dbg(fcb, feats, nfr - win * 2, "After dynamic feature computation");

            if (fcb.Deref.lda.IsNonNull)
            {
                lda.feat_lda_transform(fcb, feats, checked((uint)(nfr - win * 2)));
                // feat_print_dbg(fcb, feats, nfr - win * 2, "After LDA");
            }

            if (fcb.Deref.subvecs.IsNonNull)
            {
                feat_subvec_project(fcb, feats, checked((uint)(nfr - win * 2)));
                // feat_print_dbg(fcb, feats, nfr - win * 2, "After subvector projection");
            }
        }


        /**
         * Read Sphinx-II format mfc file (s2mfc = Sphinx-II format MFC data).
         * If out_mfc is NULL, no actual reading will be done, and the number of 
         * frames (plus padding) that would be read is returned.
         * 
         * It's important that normalization is done before padding because
         * frames outside the data we are interested in shouldn't be taken
         * into normalization stats.
         *
         * @return # frames read (plus padding) if successful, -1 if
         * error (e.g., mfc array too small).  
         */
        public static int feat_s2mfc_read_norm_pad(Pointer<feat_t> fcb, Pointer<byte> file, int win,
                             int sf, int ef,
                             BoxedValue<Pointer<Pointer<float>>> out_mfc,
                             int maxfr,
                             int cepsize)
        {
            FILE fp;
            int n_float32;
            Pointer<float> float_feat;
            stat_t statbuf;
            int i, n, byterev;
            int start_pad, end_pad;
            Pointer<Pointer<float>> mfc;

            err.E_INFO(string.Format("Reading mfc file: '{0}'[{1}..{2}]\n", cstring.FromCString(file), sf, ef));
            if (ef >= 0 && ef <= sf)
            {
                err.E_ERROR(string.Format("{0}: End frame ({1}) <= Start frame ({2})\n", cstring.FromCString(file), ef, sf));
                return -1;
            }

            /* Find filesize; HACK!! To get around intermittent NFS failures, use stat_retry */
            BoxedValue<stat_t> boxed_statbuf = new BoxedValue<stat_t>();
            if ((FILE.stat(file, boxed_statbuf) < 0)
                || ((fp = FILE.fopen(file, "rb")) == null))
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to open file '{0}' for reading", cstring.FromCString(file)));
                return -1;
            }
            statbuf = boxed_statbuf.Val;

            /* Read #floats in header */
            byte[] nfloat_buf = new byte[4];
            if (fp.fread(new Pointer<byte>(nfloat_buf), 3, 1) != 1)
            {
                err.E_ERROR(string.Format("{0}: fread(#floats) failed\n", cstring.FromCString(file)));
                fp.fclose();
                return -1;
            }

            n_float32 = BitConverter.ToInt32(nfloat_buf, 0);

            /* Check if n_float32 matches file size */
            byterev = 0;
            if ((int)(n_float32 * 4 + 4) != (int)statbuf.st_size)
            { /* RAH, typecast both sides to remove compile warning */
                n = n_float32;
                n = byteorder.SWAP_INT32(n);

                if ((int)(n * 4 + 4) != (int)(statbuf.st_size))
                {   /* RAH, typecast both sides to remove compile warning */
                    err.E_ERROR
                        (string.Format("{0}: Header size field: {1}({2}); filesize: {3}({4})\n",
                         cstring.FromCString(file), n_float32, n_float32, statbuf.st_size,
                         statbuf.st_size));
                    fp.fclose();
                    return -1;
                }

                n_float32 = n;
                byterev = 1;
            }
            if (n_float32 <= 0)
            {
                err.E_ERROR(string.Format("{0}: Header size field (#floats) = {1}\n", cstring.FromCString(file), n_float32));
                fp.fclose();
                return -1;
            }

            /* Convert n to #frames of input */
            n = n_float32 / cepsize;
            if (n * cepsize != n_float32)
            {
                err.E_ERROR(string.Format("Header size field: {0}; not multiple of {1}\n", n_float32,
                        cepsize));
                fp.fclose();
                return -1;
            }

            /* Check start and end frames */
            if (sf > 0)
            {
                if (sf >= n)
                {
                    err.E_ERROR(string.Format("{0}: Start frame ({1}) beyond file size ({2})\n", cstring.FromCString(file), sf, n));
                    fp.fclose();
                    return -1;
                }
            }
            if (ef < 0)
                ef = n - 1;
            else if (ef >= n)
            {
                err.E_WARN(string.Format("{0}: End frame ({1}) beyond file size ({2}), will truncate\n",
                       cstring.FromCString(file), ef, n));
                ef = n - 1;
            }

            /* Add window to start and end frames */
            sf -= win;
            ef += win;
            if (sf < 0)
            {
                start_pad = -sf;
                sf = 0;
            }
            else
                start_pad = 0;
            if (ef >= n)
            {
                end_pad = ef - n + 1;
                ef = n - 1;
            }
            else
                end_pad = 0;

            /* Limit n if indicated by [sf..ef] */
            if ((ef - sf + 1) < n)
                n = (ef - sf + 1);
            if (maxfr > 0 && n + start_pad + end_pad > maxfr)
            {
                err.E_ERROR(string.Format("{0}: Maximum output size({1} frames) < actual #frames({2})\n",
                        cstring.FromCString(file), maxfr, n + start_pad + end_pad));
                fp.fclose();
                return -1;
            }

            /* If no output buffer was supplied, then skip the actual data reading. */
            if (out_mfc != null)
            {
                /* Position at desired start frame and read actual MFC data */
                mfc = ckd_alloc.ckd_calloc_2d<float>((uint)(n + start_pad + end_pad), (uint)cepsize);
                if (sf > 0)
                    fp.fseek(sf * cepsize * 4, FILE.SEEK_CUR);
                n_float32 = n * cepsize;
                float_feat = mfc[start_pad];
                byte[] TEMP_FLOAT_BUF = new byte[n_float32 * 4]; // OPT: SLOWWWW
                if (fp.fread(new Pointer<byte>(TEMP_FLOAT_BUF), 4, (uint)n_float32) != n_float32)
                {
                    err.E_ERROR(string.Format("{0}: fread({1}x{2}) (MFC data) failed\n", cstring.FromCString(file), n, cepsize));
                    ckd_alloc.ckd_free_2d(mfc);
                    fp.fclose();
                    return -1;
                }
                
                for (i = 0; i < n_float32; i++)
                {
                    float_feat[i] = BitConverter.ToSingle(TEMP_FLOAT_BUF, i * 4);
                }

                if (byterev != 0)
                {
                    for (i = 0; i < n_float32; i++)
                    {
                        byteorder.SWAP_FLOAT32(float_feat.Point(i));
                    }
                }

                /* Normalize */
                feat_cmn(fcb, mfc + start_pad, n, 1, 1);
                feat_agc(fcb, mfc + start_pad, n, 1, 1);

                /* Replicate start and end frames if necessary. */
                for (i = 0; i < start_pad; ++i)
                    mfc[start_pad].MemCopyTo(mfc[i], cepsize);
                for (i = 0; i < end_pad; ++i)
                    mfc[start_pad + n - 1].MemCopyTo(mfc[start_pad + n + i], cepsize);

                out_mfc.Val = mfc;
            }

            fp.fclose();
            return n + start_pad + end_pad;
        }

        public static int feat_s2mfc2feat(Pointer<feat_t> fcb, Pointer<byte> file, Pointer<byte> dir, Pointer<byte> cepext,
                        int sf, int ef, Pointer<Pointer<Pointer<float>>> feat, int maxfr)
        {
            Pointer<byte> path;
            Pointer<byte> ps = cstring.ToCString("/");
            int win, nfr;
            uint file_length, cepext_length, path_length = 0;
            Pointer<Pointer<float>> mfc;

            if (fcb.Deref.cepsize <= 0)
            {
                err.E_ERROR(string.Format("Bad cepsize: {0}\n", fcb.Deref.cepsize));
                return -1;
            }

            if (cepext.IsNonNull)
                cepext = cstring.ToCString("");

            /*
             * Create mfc filename, combining file, dir and extension if
             * necessary
             */

            /*
             * First we decide about the path. If dir is defined, then use
             * it. Otherwise assume the filename already contains the path.
             */
            if (dir.IsNull)
            {
                dir = cstring.ToCString("");
                ps = cstring.ToCString("");
                /*
                 * This is not true but some 3rd party apps
                 * may parse the output explicitly checking for this line
                 */
                err.E_INFO("At directory . (current directory)\n");
            }
            else
            {
                err.E_INFO(string.Format("At directory {0}\n", cstring.FromCString(dir)));
                /*
                 * Do not forget the path separator!
                 */
                path_length += cstring.strlen(dir) + 1;
            }

            /*
             * Include cepext, if it's not already part of the filename.
             */
            file_length = cstring.strlen(file);
            cepext_length = cstring.strlen(cepext);
            if ((file_length > cepext_length)
                && (cstring.strcmp(file + file_length - (int)cepext_length, cepext) == 0))
            {
                cepext = cstring.ToCString("");
                cepext_length = 0;
            }

            /*
             * Do not forget the '\0'
             */
            path_length += file_length + cepext_length + 1;
            path = ckd_alloc.ckd_calloc<byte>(path_length);
            stdio.sprintf(path, string.Format("{0}{1}{2}{3}", dir, ps, file, cepext));

            win = feat_window_size(fcb);
            /* Pad maxfr with win, so we read enough raw feature data to
             * calculate the requisite number of dynamic features. */
            if (maxfr >= 0)
                maxfr += win * 2;

            if (feat.IsNonNull)
            {
                /* Read mfc file including window or padding if necessary. */
                BoxedValue<Pointer<Pointer<float>>> boxed_mfc = new BoxedValue<Pointer<Pointer<float>>>();
                nfr = feat_s2mfc_read_norm_pad(fcb, path, win, sf, ef, boxed_mfc, maxfr, fcb.Deref.cepsize);
                mfc = boxed_mfc.Val;
                ckd_alloc.ckd_free(path);
                if (nfr < 0)
                {
                    ckd_alloc.ckd_free_2d(mfc);
                    return -1;
                }

                /* Actually compute the features */
                feat_compute_utt(fcb, mfc, nfr, win, feat);

                ckd_alloc.ckd_free_2d(mfc);
            }
            else
            {
                /* Just calculate the number of frames we would need. */
                nfr = feat_s2mfc_read_norm_pad(fcb, path, win, sf, ef, null, maxfr, fcb.Deref.cepsize);
                ckd_alloc.ckd_free(path);
                if (nfr < 0)
                    return nfr;
            }


            return (nfr - win * 2);
        }

        public static int feat_s2mfc2feat_block_utt(Pointer<feat_t> fcb, Pointer<Pointer<float>> uttcep, int nfr, Pointer<Pointer<Pointer<float>>> ofeat)
        {
            Pointer<Pointer<float>> cepbuf;
            int i, win, cepsize;

            win = feat_window_size(fcb);
            cepsize = feat_cepsize(fcb);

            /* Copy and pad out the utterance (this requires that the
             * feature computation functions always access the buffer via
             * the frame pointers, which they do)  */
            cepbuf = ckd_alloc.ckd_calloc<Pointer<float>>(nfr + win * 2);
            uttcep.MemCopyTo(cepbuf + win, nfr);

            /* Do normalization before we interpolate on the boundary */
            feat_cmn(fcb, cepbuf + win, nfr, 1, 1);
            feat_agc(fcb, cepbuf + win, nfr, 1, 1);

            /* Now interpolate */
            for (i = 0; i < win; ++i)
            {
                cepbuf[i] = fcb.Deref.cepbuf[i];
                uttcep[0].MemCopyTo(cepbuf[i], cepsize);
                cepbuf[nfr + win + i] = fcb.Deref.cepbuf[win + i];
                uttcep[nfr - 1].MemCopyTo(cepbuf[nfr + win + i], cepsize);
            }

            /* Compute as usual. */
            feat_compute_utt(fcb, cepbuf, nfr + win * 2, win, ofeat);
            ckd_alloc.ckd_free(cepbuf);
            return nfr;
        }

        public static int feat_s2mfc2feat_live(Pointer<feat_t> fcb, Pointer<Pointer<float>> uttcep, BoxedValueInt inout_ncep,
                     int beginutt, int endutt, Pointer<Pointer<Pointer<float>>> ofeat)
        {
            int win, cepsize, nbufcep;
            int i, j, nfeatvec;

            /* Avoid having to check this everywhere. */
            if (inout_ncep == null) inout_ncep = new BoxedValueInt(0);

            /* Special case for entire utterances. */
            if (beginutt != 0 && endutt != 0 && inout_ncep.Val > 0)
                return feat_s2mfc2feat_block_utt(fcb, uttcep, inout_ncep.Val, ofeat);

            win = feat_window_size(fcb);
            cepsize = feat_cepsize(fcb);

            /* Empty the input buffer on start of utterance. */
            if (beginutt != 0)
                fcb.Deref.bufpos = fcb.Deref.curpos;

            /* Calculate how much data is in the buffer already. */
            nbufcep = fcb.Deref.bufpos - fcb.Deref.curpos;
            if (nbufcep < 0)
                nbufcep = fcb.Deref.bufpos + LIVEBUFBLOCKSIZE - fcb.Deref.curpos;
            /* Add any data that we have to replicate. */
            if (beginutt != 0 && inout_ncep.Val > 0)
                nbufcep += win;
            if (endutt != 0)
                nbufcep += win;

            /* Only consume as much input as will fit in the buffer. */
            if (nbufcep + inout_ncep.Val > LIVEBUFBLOCKSIZE)
            {
                /* We also can't overwrite the trailing window, hence the
                 * reason why win is subtracted here. */
                inout_ncep.Val = LIVEBUFBLOCKSIZE - nbufcep - win;
                /* Cancel end of utterance processing. */
                endutt = 0;
            }

            /* FIXME: Don't modify the input! */
            feat_cmn(fcb, uttcep, inout_ncep.Val, beginutt, endutt);
            feat_agc(fcb, uttcep, inout_ncep.Val, beginutt, endutt);

            /* Replicate first frame into the first win frames if we're at the
             * beginning of the utterance and there was some actual input to
             * deal with.  (FIXME: Not entirely sure why that condition) */
            if (beginutt != 0 && inout_ncep.Val > 0)
            {
                for (i = 0; i < win; i++)
                {
                    uttcep[0].MemCopyTo(fcb.Deref.cepbuf[fcb.Deref.bufpos++],
                           cepsize);
                    fcb.Deref.bufpos %= LIVEBUFBLOCKSIZE;
                }
                /* Move the current pointer past this data. */
                fcb.Deref.curpos = fcb.Deref.bufpos;
                nbufcep -= win;
            }

            /* Copy in frame data to the circular buffer. */
            for (i = 0; i < inout_ncep.Val; ++i)
            {
                uttcep[i].MemCopyTo(fcb.Deref.cepbuf[fcb.Deref.bufpos++],
                       cepsize);
                fcb.Deref.bufpos %= LIVEBUFBLOCKSIZE;
                ++nbufcep;
            }

            /* Replicate last frame into the last win frames if we're at the
             * end of the utterance (even if there was no input, so we can
             * flush the output). */
            if (endutt != 0)
            {
                int tpos; /* Index of last input frame. */
                if (fcb.Deref.bufpos == 0)
                    tpos = LIVEBUFBLOCKSIZE - 1;
                else
                    tpos = fcb.Deref.bufpos - 1;
                for (i = 0; i < win; ++i)
                {
                    fcb.Deref.cepbuf[tpos].MemCopyTo(fcb.Deref.cepbuf[fcb.Deref.bufpos++], cepsize);
                    fcb.Deref.bufpos %= LIVEBUFBLOCKSIZE;
                }
            }

            /* We have to leave the trailing window of frames. */
            nfeatvec = nbufcep - win;
            if (nfeatvec <= 0)
                return 0; /* Do nothing. */

            for (i = 0; i < nfeatvec; ++i)
            {
                /* Handle wraparound cases. */
                if (fcb.Deref.curpos - win < 0 || fcb.Deref.curpos + win >= LIVEBUFBLOCKSIZE)
                {
                    /* Use tmpcepbuf for this case.  Actually, we just need the pointers. */
                    for (j = -win; j <= win; ++j)
                    {
                        int tmppos =
                            (fcb.Deref.curpos + j + LIVEBUFBLOCKSIZE) % LIVEBUFBLOCKSIZE;
                        fcb.Deref.tmpcepbuf[win + j] = fcb.Deref.cepbuf[tmppos];
                    }
                    fcb.Deref.compute_feat_func(fcb, fcb.Deref.tmpcepbuf + win, ofeat[i]);
                }
                else
                {
                    fcb.Deref.compute_feat_func(fcb, fcb.Deref.cepbuf + fcb.Deref.curpos, ofeat[i]);
                }
                /* Move the read pointer forward. */
                ++fcb.Deref.curpos;
                fcb.Deref.curpos %= LIVEBUFBLOCKSIZE;
            }

            if (fcb.Deref.lda.IsNonNull)
                lda.feat_lda_transform(fcb, ofeat, (uint)nfeatvec);

            if (fcb.Deref.subvecs.IsNonNull)
                feat_subvec_project(fcb, ofeat, checked((uint)nfeatvec));

            return nfeatvec;
        }

        public static void feat_update_stats(Pointer<feat_t> fcb)
        {
            if (fcb.Deref.cmn == cmn_type_e.CMN_LIVE)
            {
                cmn_live.cmn_live_update(fcb.Deref.cmn_struct);
            }
            if (fcb.Deref.agc == agc_type_e.AGC_EMAX || fcb.Deref.agc == agc_type_e.AGC_MAX)
            {
                agc.agc_emax_update(fcb.Deref.agc_struct);
            }
        }

        public static Pointer<feat_t> feat_retain(Pointer<feat_t> f)
        {
            ++f.Deref.refcount;
            return f;
        }

        public static int feat_free(Pointer<feat_t> f)
        {
            if (f.IsNull)
                return 0;
            if (--f.Deref.refcount > 0)
                return f.Deref.refcount;

            if (f.Deref.cepbuf.IsNonNull)
                ckd_alloc.ckd_free_2d(f.Deref.cepbuf);
            ckd_alloc.ckd_free(f.Deref.tmpcepbuf);

            if (f.Deref.name.IsNonNull)
            {
                ckd_alloc.ckd_free(f.Deref.name);
            }
            if (f.Deref.lda.IsNonNull)
                ckd_alloc.ckd_free_3d(f.Deref.lda);

            ckd_alloc.ckd_free(f.Deref.stream_len);
            ckd_alloc.ckd_free(f.Deref.sv_len);
            ckd_alloc.ckd_free(f.Deref.sv_buf);
            subvecs_free(f.Deref.subvecs);

            cmn.cmn_free(f.Deref.cmn_struct);
            agc.agc_free(f.Deref.agc_struct);

            ckd_alloc.ckd_free(f);
            return 0;
        }
    }
}
