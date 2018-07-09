using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class s2_semi_mgau
    {
        public static readonly ps_mgaufuncs_t s2_semi_mgau_funcs = new ps_mgaufuncs_t()
        {
            name = cstring.ToCString("s2_semi"),
            frame_eval = s2_semi_mgau_frame_eval,      /* frame_eval */
            transform = s2_semi_mgau_mllr_transform,  /* transform */
            free = s2_semi_mgau_free             /* free */
        };

        public static void
        eval_topn(s2_semi_mgau_t s, int feat, Pointer<float> z)
        {
            int i, ceplen;
            Pointer<vq_feature_t> topn;

            topn = s.f[feat];
            ceplen = s.g.Deref.featlen[feat];

            for (i = 0; i < s.max_topn; i++)
            {
                Pointer<float> mean;
                float diff;
                float sqdiff;
                float compl; /* diff, diff^2, component likelihood */
                vq_feature_t vtmp;
                Pointer<float> var;
                float d;
                Pointer<float> obs;
                int cw, j;

                cw = topn[i].codeword;
                mean = s.g.Deref.mean[0][feat][0] + cw * ceplen;
                var = s.g.Deref.var[0][feat][0] + cw * ceplen;
                d = s.g.Deref.det[0][feat][cw];
                obs = z;
                for (j = 0; j < ceplen; j++)
                {
                    diff = (obs.Deref) - (mean.Deref);
                    obs++;
                    mean++;
                    sqdiff = (diff * diff);
                    compl = (sqdiff * var.Deref);
                    d = (d - compl);
                    ++var;
                }
                topn.Set(i, new vq_feature_t()
                {
                    codeword = topn[i].codeword,
                    score = (int)d
                });
                 
                if (i == 0)
                    continue;
                vtmp = topn[i];
                for (j = i - 1; j >= 0 && (int)d > topn[j].score; j--)
                {
                    topn[j + 1] = topn[j];
                }
                topn[j + 1] = vtmp;
            }
        }

        public static void eval_cb(s2_semi_mgau_t s, int feat, Pointer<float> z)
        {
            Pointer<vq_feature_t> topn;
            Pointer<float> mean;
            Pointer<float> var, det;
            int i, ceplen;

            // LOGAN modified - these loops relied a lot on pointer arithmetic, which I resolved
            // by reducing the pointers into int _ptr values instead.
            topn = s.f[feat];
            mean = s.g.Deref.mean[0][feat][0];
            var = s.g.Deref.var[0][feat][0];
            det = s.g.Deref.det[0][feat];
            ceplen = s.g.Deref.featlen[feat];

            int cur_ptr = 0;
            int best_ptr = 0;
            int worst_ptr = (s.max_topn - 1);

            int detP_ptr = 0;
            int detE_ptr = s.g.Deref.n_density;
            for (detP_ptr = 0; detP_ptr < detE_ptr; ++detP_ptr)
            {
                float diff, sqdiff, compl; /* diff, diff^2, component likelihood */
                float d;
                Pointer<float> obs;
                int cw, j;

                d = det[detP_ptr];
                obs = z;
                cw = detP_ptr;
                for (j = 0; (j < ceplen) && (d >= topn[worst_ptr].score); ++j)
                {
                    diff = (obs.Deref) - (mean.Deref);
                    obs++;
                    mean++;
                    sqdiff = (diff * diff);
                    compl = (sqdiff * var.Deref);
                    d = (d - compl);
                    ++var;
                }
                if (j < ceplen)
                {
                    /* terminated early, so not in topn */
                    mean += (ceplen - j);
                    var += (ceplen - j);
                    continue;
                }
                if ((int)d < topn[worst_ptr].score)
                    continue;
                for (i = 0; i < s.max_topn; i++)
                {
                    /* already there, so don't need to insert */
                    if (topn[i].codeword == cw)
                        break;
                }
                if (i < s.max_topn)
                    continue;       /* already there.  Don't insert */
                                    /* remaining code inserts codeword and dist in correct spot */
                for (cur_ptr = worst_ptr - 1; cur_ptr >= best_ptr && (int)d >= topn[cur_ptr].score; --cur_ptr)
                {
                    topn.Set(cur_ptr + 1, new vq_feature_t()
                    {
                        codeword = topn[cur_ptr].codeword,
                        score = topn[cur_ptr].score
                    });
                }
                ++cur_ptr;

                topn.Set(cur_ptr, new vq_feature_t()
                {
                    codeword = cw,
                    score = (int)d
                });
            }
        }

        public static void mgau_dist(s2_semi_mgau_t s, int frame, int feat, Pointer<float> z)
        {
            eval_topn(s, feat, z);

            /* If this frame is skipped, do nothing else. */
            if (frame % s.ds_ratio != 0)
                return;

            /* Evaluate the rest of the codebook (or subset thereof). */
            eval_cb(s, feat, z);
        }

        public static int mgau_norm(s2_semi_mgau_t s, int feat)
        {
            int norm;
            int j;

            /* Compute quantized normalizing constant. */
            norm = s.f[feat][0].score >> hmm.SENSCR_SHIFT;

            /* Normalize the scores, negate them, and clamp their dynamic range. */
            for (j = 0; j < s.max_topn; ++j)
            {
                int scr = s.f[feat][j].score;
                scr = -((scr >> hmm.SENSCR_SHIFT) - norm);
                if (scr > tied_mgau_common.MAX_NEG_ASCR)
                    scr = tied_mgau_common.MAX_NEG_ASCR;

                s.f[feat].Set(j, new vq_feature_t()
                {
                    score = scr,
                    codeword = s.f[feat][j].codeword
                });

                if (s.topn_beam[feat] != 0 && s.f[feat][j].score > s.topn_beam[feat])
                    break;
            }
            return j;
        }

        public static int get_scores_8b_feat_6(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2, pid_cw3, pid_cw4, pid_cw5;

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];
            pid_cw3 = s.mixw[i][s.f[i][3].codeword];
            pid_cw4 = s.mixw[i][s.f[i][4].codeword];
            pid_cw5 = s.mixw[i][s.f[i][5].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                int tmp = pid_cw0[sen] + s.f[i][0].score;

                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw1[sen] + s.f[i][1].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw2[sen] + s.f[i][2].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw3[sen] + s.f[i][3].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw4[sen] + s.f[i][4].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw5[sen] + s.f[i][5].score);

                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat_5(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2, pid_cw3, pid_cw4;

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];
            pid_cw3 = s.mixw[i][s.f[i][3].codeword];
            pid_cw4 = s.mixw[i][s.f[i][4].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                int tmp = pid_cw0[sen] + s.f[i][0].score;

                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw1[sen] + s.f[i][1].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw2[sen] + s.f[i][2].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw3[sen] + s.f[i][3].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw4[sen] + s.f[i][4].score);

                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat_4(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2, pid_cw3;

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];
            pid_cw3 = s.mixw[i][s.f[i][3].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                int tmp = pid_cw0[sen] + s.f[i][0].score;

                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw1[sen] + s.f[i][1].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw2[sen] + s.f[i][2].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw3[sen] + s.f[i][3].score);

                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat_3(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2;

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                int tmp = pid_cw0[sen] + s.f[i][0].score;

                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw1[sen] + s.f[i][1].score);
                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw2[sen] + s.f[i][2].score);

                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat_2(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1;

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                int tmp = pid_cw0[sen] + s.f[i][0].score;

                tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                       pid_cw1[sen] + s.f[i][1].score);

                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat_1(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0;

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                int tmp = pid_cw0[sen] + s.f[i][0].score;
                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat_any(s2_semi_mgau_t s, int i, int topn,
                               Pointer<short> senone_scores, Pointer<byte> senone_active,
                               int n_senone_active)
        {
            int j, k, l;

            for (l = j = 0; j < n_senone_active; j++)
            {
                int sen = senone_active[j] + l;
                Pointer<byte> pid_cw;
                int tmp;
                pid_cw = s.mixw[i][s.f[i][0].codeword];
                tmp = pid_cw[sen] + s.f[i][0].score;
                for (k = 1; k < topn; ++k)
                {
                    pid_cw = s.mixw[i][s.f[i][k].codeword];
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                           pid_cw[sen] + s.f[i][k].score);
                }
                senone_scores[sen] = checked((short)(senone_scores[sen] + tmp));
                l = sen;
            }
            return 0;
        }

        public static int get_scores_8b_feat(s2_semi_mgau_t s, int i, int topn,
                           Pointer<short> senone_scores, Pointer<byte> senone_active, int n_senone_active)
        {
            switch (topn)
            {
                case 6:
                    return get_scores_8b_feat_6(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 5:
                    return get_scores_8b_feat_5(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 4:
                    return get_scores_8b_feat_4(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 3:
                    return get_scores_8b_feat_3(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 2:
                    return get_scores_8b_feat_2(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 1:
                    return get_scores_8b_feat_1(s, i, senone_scores,
                                                senone_active, n_senone_active);
                default:
                    return get_scores_8b_feat_any(s, i, topn, senone_scores,
                                                  senone_active, n_senone_active);
            }
        }

        public static int
        get_scores_8b_feat_all(s2_semi_mgau_t s, int i, int topn, Pointer<short> senone_scores)
        {
            int j, k;

            for (j = 0; j < s.n_sen; j++)
            {
                Pointer<byte> pid_cw;
                int tmp;
                pid_cw = s.mixw[i][s.f[i][0].codeword];
                tmp = pid_cw[j] + s.f[i][0].score;
                for (k = 1; k < topn; ++k)
                {
                    pid_cw = s.mixw[i][s.f[i][k].codeword];
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                           pid_cw[j] + s.f[i][k].score);
                }
                senone_scores[j] = checked((short)(senone_scores[j] + tmp));
            }
            return 0;
        }

        public static int get_scores_4b_feat_6(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2, pid_cw3, pid_cw4, pid_cw5;
            byte[,] w_den = new byte[6, 16];

            /* Precompute scaled densities. */
            for (j = 0; j < 16; ++j)
            {
                w_den[0, j] = checked((byte)(s.mixw_cb[j] + s.f[i][0].score));
                w_den[1, j] = checked((byte)(s.mixw_cb[j] + s.f[i][1].score));
                w_den[2, j] = checked((byte)(s.mixw_cb[j] + s.f[i][2].score));
                w_den[3, j] = checked((byte)(s.mixw_cb[j] + s.f[i][3].score));
                w_den[4, j] = checked((byte)(s.mixw_cb[j] + s.f[i][4].score));
                w_den[5, j] = checked((byte)(s.mixw_cb[j] + s.f[i][5].score));
            }

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];
            pid_cw3 = s.mixw[i][s.f[i][3].codeword];
            pid_cw4 = s.mixw[i][s.f[i][4].codeword];
            pid_cw5 = s.mixw[i][s.f[i][5].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;

                if ((n & 1) != 0)
                {
                    cw = pid_cw0[n / 2] >> 4;
                    tmp = w_den[0, cw];
                    cw = pid_cw1[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1, cw]);
                    cw = pid_cw2[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2, cw]);
                    cw = pid_cw3[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[3, cw]);
                    cw = pid_cw4[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[4, cw]);
                    cw = pid_cw5[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[5, cw]);
                }
                else
                {
                    cw = pid_cw0[n / 2] & 0x0f;
                    tmp = w_den[0, cw];
                    cw = pid_cw1[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1, cw]);
                    cw = pid_cw2[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2, cw]);
                    cw = pid_cw3[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[3, cw]);
                    cw = pid_cw4[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[4, cw]);
                    cw = pid_cw5[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[5, cw]);
                }
                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }
            return 0;
        }

        public static int get_scores_4b_feat_5(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2, pid_cw3, pid_cw4;
            byte[,] w_den = new byte[5, 16];

            /* Precompute scaled densities. */
            for (j = 0; j < 16; ++j)
            {
                w_den[0,j] = checked((byte)(s.mixw_cb[j] + s.f[i][0].score));
                w_den[1,j] = checked((byte)(s.mixw_cb[j] + s.f[i][1].score));
                w_den[2,j] = checked((byte)(s.mixw_cb[j] + s.f[i][2].score));
                w_den[3,j] = checked((byte)(s.mixw_cb[j] + s.f[i][3].score));
                w_den[4,j] = checked((byte)(s.mixw_cb[j] + s.f[i][4].score));
            }

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];
            pid_cw3 = s.mixw[i][s.f[i][3].codeword];
            pid_cw4 = s.mixw[i][s.f[i][4].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;

                if ((n & 1) != 0)
                {
                    cw = pid_cw0[n / 2] >> 4;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                    cw = pid_cw2[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2,cw]);
                    cw = pid_cw3[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[3,cw]);
                    cw = pid_cw4[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[4,cw]);
                }
                else
                {
                    cw = pid_cw0[n / 2] & 0x0f;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                    cw = pid_cw2[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2,cw]);
                    cw = pid_cw3[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[3,cw]);
                    cw = pid_cw4[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[4,cw]);
                }
                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }
            return 0;
        }

        public static int get_scores_4b_feat_4(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2, pid_cw3;
            byte[,] w_den = new byte[4, 16];

            /* Precompute scaled densities. */
            for (j = 0; j < 16; ++j)
            {
                w_den[0,j] = checked((byte)(s.mixw_cb[j] + s.f[i][0].score));
                w_den[1,j] = checked((byte)(s.mixw_cb[j] + s.f[i][1].score));
                w_den[2,j] = checked((byte)(s.mixw_cb[j] + s.f[i][2].score));
                w_den[3,j] = checked((byte)(s.mixw_cb[j] + s.f[i][3].score));
            }

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];
            pid_cw3 = s.mixw[i][s.f[i][3].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;

                if ((n & 1) != 0)
                {
                    cw = pid_cw0[n / 2] >> 4;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                    cw = pid_cw2[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2,cw]);
                    cw = pid_cw3[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[3,cw]);
                }
                else
                {
                    cw = pid_cw0[n / 2] & 0x0f;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                    cw = pid_cw2[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2,cw]);
                    cw = pid_cw3[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[3,cw]);
                }
                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }
            return 0;
        }

        public static int get_scores_4b_feat_3(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1, pid_cw2;
            byte[,] w_den = new byte[3, 16];

            /* Precompute scaled densities. */
            for (j = 0; j < 16; ++j)
            {
                w_den[0,j] = checked((byte)(s.mixw_cb[j] + s.f[i][0].score));
                w_den[1,j] = checked((byte)(s.mixw_cb[j] + s.f[i][1].score));
                w_den[2,j] = checked((byte)(s.mixw_cb[j] + s.f[i][2].score));
            }

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];
            pid_cw2 = s.mixw[i][s.f[i][2].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;

                if ((n & 1) != 0)
                {
                    cw = pid_cw0[n / 2] >> 4;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                    cw = pid_cw2[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2,cw]);
                }
                else
                {
                    cw = pid_cw0[n / 2] & 0x0f;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                    cw = pid_cw2[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[2,cw]);
                }
                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }
            return 0;
        }

        public static int get_scores_4b_feat_2(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0, pid_cw1;
            byte[,] w_den = new byte[2, 16];

            /* Precompute scaled densities. */
            for (j = 0; j < 16; ++j)
            {
                w_den[0,j] = checked((byte)(s.mixw_cb[j] + s.f[i][0].score));
                w_den[1,j] = checked((byte)(s.mixw_cb[j] + s.f[i][1].score));
            }

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];
            pid_cw1 = s.mixw[i][s.f[i][1].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;

                if ((n & 1) != 0)
                {
                    cw = pid_cw0[n / 2] >> 4;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] >> 4;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                }
                else
                {
                    cw = pid_cw0[n / 2] & 0x0f;
                    tmp = w_den[0,cw];
                    cw = pid_cw1[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp, w_den[1,cw]);
                }
                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }
            return 0;
        }

        public static int
        get_scores_4b_feat_1(s2_semi_mgau_t s, int i,
                             Pointer<short> senone_scores, Pointer<byte> senone_active,
                             int n_senone_active)
        {
            int j, l;
            Pointer<byte> pid_cw0;
            byte[] w_den = new byte[16];

            /* Precompute scaled densities. */
            for (j = 0; j < 16; ++j)
            {
                w_den[j] = checked((byte)(s.mixw_cb[j] + s.f[i][0].score));
            }

            pid_cw0 = s.mixw[i][s.f[i][0].codeword];

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;

                if ((n & 1) != 0)
                {
                    cw = pid_cw0[n / 2] >> 4;
                    tmp = w_den[cw];
                }
                else
                {
                    cw = pid_cw0[n / 2] & 0x0f;
                    tmp = w_den[cw];
                }

                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }
            return 0;
        }

        public static int
        get_scores_4b_feat_any(s2_semi_mgau_t s, int i, int topn,
                               Pointer<short> senone_scores, Pointer<byte> senone_active,
                               int n_senone_active)
        {
            int j, k, l;

            for (l = j = 0; j < n_senone_active; j++)
            {
                int n = senone_active[j] + l;
                int tmp, cw;
                Pointer<byte> pid_cw;

                pid_cw = s.mixw[i][s.f[i][0].codeword];
                if ((n & 1) != 0)
                    cw = pid_cw[n / 2] >> 4;
                else
                    cw = pid_cw[n / 2] & 0x0f;
                tmp = s.mixw_cb[cw] + s.f[i][0].score;
                for (k = 1; k < topn; ++k)
                {
                    pid_cw = s.mixw[i][s.f[i][k].codeword];
                    if ((n & 1) != 0)
                        cw = pid_cw[n / 2] >> 4;
                    else
                        cw = pid_cw[n / 2] & 0x0f;
                    tmp = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp,
                                           s.mixw_cb[cw] + s.f[i][k].score);
                }

                senone_scores[n] = checked((short)(senone_scores[n] + tmp));
                l = n;
            }

            return 0;
        }

        public static int
        get_scores_4b_feat(s2_semi_mgau_t s, int i, int topn,
                           Pointer<short> senone_scores, Pointer<byte> senone_active, int n_senone_active)
        {
            switch (topn)
            {
                case 6:
                    return get_scores_4b_feat_6(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 5:
                    return get_scores_4b_feat_5(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 4:
                    return get_scores_4b_feat_4(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 3:
                    return get_scores_4b_feat_3(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 2:
                    return get_scores_4b_feat_2(s, i, senone_scores,
                                                senone_active, n_senone_active);
                case 1:
                    return get_scores_4b_feat_1(s, i, senone_scores,
                                                senone_active, n_senone_active);
                default:
                    return get_scores_4b_feat_any(s, i, topn, senone_scores,
                                                  senone_active, n_senone_active);
            }
        }

        public static int
        get_scores_4b_feat_all(s2_semi_mgau_t s, int i, int topn, Pointer<short> senone_scores)
        {
            int j, last_sen;

            j = 0;
            /* Number of senones is always even, but don't overrun if it isn't. */
            last_sen = s.n_sen & ~1;
            while (j < last_sen)
            {
                Pointer<byte> pid_cw;
                int tmp0, tmp1;
                int k;

                pid_cw = s.mixw[i][s.f[i][0].codeword];
                tmp0 = s.mixw_cb[pid_cw[j / 2] & 0x0f] + s.f[i][0].score;
                tmp1 = s.mixw_cb[pid_cw[j / 2] >> 4] + s.f[i][0].score;
                for (k = 1; k < topn; ++k)
                {
                    int w_den0, w_den1;

                    pid_cw = s.mixw[i][s.f[i][k].codeword];
                    w_den0 = s.mixw_cb[pid_cw[j / 2] & 0x0f] + s.f[i][k].score;
                    w_den1 = s.mixw_cb[pid_cw[j / 2] >> 4] + s.f[i][k].score;
                    tmp0 = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp0, w_den0);
                    tmp1 = tied_mgau_common.fast_logmath_add(s.lmath_8b, tmp1, w_den1);
                }

                senone_scores[j] = checked((short)(senone_scores[j] + tmp0));
                j++;
                senone_scores[j] = checked((short)(senone_scores[j] + tmp1));
                j++;
            }
            return 0;
        }

        /*
         * Compute senone scores for the active senones.
         */
        public static int s2_semi_mgau_frame_eval(ps_mgau_t ps,
                    Pointer<short> senone_scores,
                    Pointer<byte> senone_active,
                    int n_senone_active,
                    Pointer<Pointer<float>> featbuf, int frame,
                    int compallsen)
        {
            s2_semi_mgau_t s = (s2_semi_mgau_t)ps;
            int i, topn_idx;
            int n_feat = s.g.Deref.n_feat;

            senone_scores.MemSet(0, s.n_sen);
            /* No bounds checking is done here, which just means you'll get
             * semi-random crap if you request a frame in the future or one
             * that's too far in the past. */
            topn_idx = frame % s.n_topn_hist;
            s.f = s.topn_hist[topn_idx];
            for (i = 0; i < n_feat; ++i)
            {
                /* For past frames this will already be computed. */
                if (frame >= ps.frame_idx)
                {
                    Pointer< Pointer < vq_feature_t>> lastf;
                    if (topn_idx == 0)
                        lastf = s.topn_hist[s.n_topn_hist - 1];
                    else
                        lastf = s.topn_hist[topn_idx - 1];

                    // LOGAN modified - manual memcopy here
                    lastf[i].MemCopyTo(s.f[i], s.max_topn);
                    //for (int memcpy = 0; memcpy < s.max_topn; memcpy++)
                    //{
                    //    s.f[i].Set(memcpy, lastf[i][memcpy]);
                    //}
                    mgau_dist(s, frame, i, featbuf[i]);
                    s.topn_hist_n[topn_idx].Set(i, checked((byte)(mgau_norm(s, i))));
                }
                if (s.mixw_cb.IsNonNull)
                {
                    if (compallsen != 0)
                        get_scores_4b_feat_all(s, i, s.topn_hist_n[topn_idx][i], senone_scores);
                    else
                        get_scores_4b_feat(s, i, s.topn_hist_n[topn_idx][i], senone_scores,
                                           senone_active, n_senone_active);
                }
                else
                {
                    if (compallsen != 0)
                        get_scores_8b_feat_all(s, i, s.topn_hist_n[topn_idx][i], senone_scores);
                    else
                        get_scores_8b_feat(s, i, s.topn_hist_n[topn_idx][i], senone_scores,
                                           senone_active, n_senone_active);
                }
            }

            return 0;
        }

        public static int read_sendump(s2_semi_mgau_t s, Pointer<bin_mdef_t> mdef, Pointer<byte> file)
        {
            FILE fp;
            Pointer < byte> line = PointerHelpers.Malloc<byte>(1000);
            int i, n, r, c;
            int do_swap;
            uint offset;
            int n_clust = 0;
            int n_feat = s.g.Deref.n_feat;
            int n_density = s.g.Deref.n_density;
            int n_sen = bin_mdef.bin_mdef_n_sen(mdef);
            int n_bits = 8;

            s.n_sen = n_sen; /* FIXME: Should have been done earlier */

            if ((fp = FILE.fopen(file, "rb")) == null)
                return -1;

            byte[] tmp_buf = new byte[4];
            Pointer<byte> tmp_buf_byte = new Pointer<byte>(tmp_buf);
            Pointer<int> tmp_buf_int = tmp_buf_byte.ReinterpretCast<int>();

            err.E_INFO(string.Format("Loading senones from dump file {0}\n", cstring.FromCString(file)));
            /* Read title size, title */
            if (fp.fread(tmp_buf_byte, 4, 1) != 1)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to read title size from {0}", cstring.FromCString(file)));
                goto error_out;
            }
            n = tmp_buf_int.Deref;

            /* This is extremely bogus */
            do_swap = 0;
            if (n < 1 || n > 999)
            {
                n = byteorder.SWAP_INT32(n);
                if (n < 1 || n > 999)
                {
                    err.E_ERROR(string.Format("Title length {0} in dump file {1} out of range\n", n, cstring.FromCString(file)));
                    goto error_out;
                }
                do_swap = 1;
            }
            if (fp.fread(line, 1, (uint)n) != n)
            {
                err.E_ERROR_SYSTEM("Cannot read title");
                goto error_out;
            }
            if (line[n - 1] != '\0')
            {
                err.E_ERROR("Bad title in dump file\n");
                goto error_out;
            }
            err.E_INFO(string.Format("{0}\n", cstring.FromCString(line)));

            /* Read header size, header */
            if (fp.fread(tmp_buf_byte, 4, 1) != 1)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to read header size from {0}", cstring.FromCString(file)));
                goto error_out;
            }
            n = tmp_buf_int.Deref;

            if (do_swap != 0) n = byteorder.SWAP_INT32(n);
            if (fp.fread(line, 1, (uint)n) != n)
            {
                err.E_ERROR_SYSTEM("Cannot read header");
                goto error_out;
            }
            if (line[n - 1] != '\0')
            {
                err.E_ERROR("Bad header in dump file\n");
                goto error_out;
            }

            /* Read other header strings until string length = 0 */
            for (;;)
            {
                if (fp.fread(tmp_buf_byte, 4, 1) != 1)
                {
                    err.E_ERROR_SYSTEM(string.Format("Failed to read header string size from {0}", cstring.FromCString(file)));
                    goto error_out;
                }
                n = tmp_buf_int.Deref;

                if (do_swap != 0) n = byteorder.SWAP_INT32(n);
                if (n == 0)
                    break;
                if (fp.fread(line, 1, (uint)n) != n)
                {
                    err.E_ERROR_SYSTEM("Cannot read header");
                    goto error_out;
                }
                /* Look for a cluster count, if present */
                if (cstring.strncmp(line, cstring.ToCString("feature_count "), cstring.strlen(cstring.ToCString("feature_count "))) == 0)
                {
                    n_feat = cstring.atoi(line + cstring.strlen(cstring.ToCString("feature_count ")));
                }
                if (cstring.strncmp(line, cstring.ToCString("mixture_count "), cstring.strlen(cstring.ToCString("mixture_count "))) == 0)
                {
                    n_density = cstring.atoi(line + cstring.strlen(cstring.ToCString("mixture_count ")));
                }
                if (cstring.strncmp(line, cstring.ToCString("model_count "), cstring.strlen(cstring.ToCString("model_count "))) == 0)
                {
                    n_sen = cstring.atoi(line + cstring.strlen(cstring.ToCString("model_count ")));
                }
                if (cstring.strncmp(line, cstring.ToCString("cluster_count "), cstring.strlen(cstring.ToCString("cluster_count "))) == 0)
                {
                    n_clust = cstring.atoi(line + cstring.strlen(cstring.ToCString("cluster_count ")));
                }
                if (cstring.strncmp(line, cstring.ToCString("cluster_bits "), cstring.strlen(cstring.ToCString("cluster_bits "))) == 0)
                {
                    n_bits = cstring.atoi(line + cstring.strlen(cstring.ToCString("cluster_bits ")));
                }
            }

            /* Defaults for #rows, #columns in mixw array. */
            c = n_sen;
            r = n_density;
            if (n_clust == 0)
            {
                /* Older mixw files have them here, and they might be padded. */
                if (fp.fread(tmp_buf_byte, 4, 1) != 1)
                {
                    err.E_ERROR_SYSTEM("Cannot read #rows");
                    goto error_out;
                }
                r = tmp_buf_int.Deref;

                if (do_swap != 0) r = byteorder.SWAP_INT32(r);
                if (fp.fread(tmp_buf_byte, 4, 1) != 1)
                {
                    err.E_ERROR_SYSTEM("Cannot read #columns");
                    goto error_out;
                }
                c = tmp_buf_int.Deref;

                if (do_swap != 0) c = byteorder.SWAP_INT32(c);
                err.E_INFO(string.Format("Rows: {0}, Columns: {1}\n", r, c));
            }

            if (n_feat != s.g.Deref.n_feat)
            {
                err.E_ERROR(string.Format("Number of feature streams mismatch: {0} != {1}\n",
                        n_feat, s.g.Deref.n_feat));
                goto error_out;
            }
            if (n_density != s.g.Deref.n_density)
            {
                err.E_ERROR(string.Format("Number of densities mismatch: {0} != {1}\n",
                        n_density, s.g.Deref.n_density));
                goto error_out;
            }
            if (n_sen != s.n_sen)
            {
                err.E_ERROR(string.Format("Number of senones mismatch: {0} != {1}\n",
                        n_sen, s.n_sen));
                goto error_out;
            }

            if (!((n_clust == 0) || (n_clust == 15) || (n_clust == 16)))
            {
                err.E_ERROR("Cluster count must be 0, 15, or 16\n");
                goto error_out;
            }
            if (n_clust == 15)
                ++n_clust;

            if (!((n_bits == 8) || (n_bits == 4)))
            {
                err.E_ERROR("Cluster count must be 4 or 8\n");
                goto error_out;
            }
            
            offset = (uint)fp.ftell();

            /* Allocate memory for pdfs (or memory map them) */
            /* Get cluster codebook if any. */
            if (n_clust != 0)
            {
                s.mixw_cb = ckd_alloc.ckd_calloc<byte>(n_clust);
                if (fp.fread(s.mixw_cb, 1, (uint)n_clust) != (uint)n_clust)
                {
                    err.E_ERROR(string.Format("Failed to read {0} bytes from sendump\n", n_clust));
                    goto error_out;
                }
            }

            /* Set up pointers, or read, or whatever */
            s.mixw = ckd_alloc.ckd_calloc_3d<byte>((uint)n_feat, (uint)n_density, (uint)n_sen);
            /* Read pdf values and ids */
            for (n = 0; n < n_feat; n++)
            {
                int step = c;
                if (n_bits == 4)
                    step = (step + 1) / 2;
                for (i = 0; i < r; i++)
                {
                    if (fp.fread(s.mixw[n][i], 1, (uint)step) != (uint)step)
                    {
                        err.E_ERROR(string.Format("Failed to read {0} bytes from sendump\n", step));
                        goto error_out;
                    }
                }
            }

            fp.fclose();
            return 0;
            error_out:
            fp.fclose();
            return -1;
        }

        public static int read_mixw(s2_semi_mgau_t s, Pointer<byte> file_name, double SmoothMin)
        {
            Pointer < Pointer <byte>> argname, argval;
            FILE fp;
            int byteswap, chksum_present;
            BoxedValue<uint> chksum = new BoxedValue<uint>();
            Pointer<float> pdf;
            int i, f, c, n;
            int n_sen;
            int n_feat;
            int n_comp;
            int n_err;

            err.E_INFO(string.Format("Reading mixture weights file '{0}'\n", cstring.FromCString(file_name)));

            if ((fp = FILE.fopen(file_name, "rb")) == null)
                err.E_FATAL_SYSTEM(string.Format("Failed to open mixture weights file '{0}' for reading", cstring.FromCString(file_name)));

            /* Read header, including argument-value info and 32-bit byteorder magic */
            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(fp, boxed_argname, boxed_argval, out byteswap) < 0)
                err.E_FATAL(string.Format("Failed to read header from file '{0}'\n", cstring.FromCString(file_name)));
            argname = boxed_argname.Val;
            argval = boxed_argval.Val;

            /* Parse argument-value list */
            chksum_present = 0;
            for (i = 0; argname[i].IsNonNull; i++)
            {
                if (cstring.strcmp(argname[i], cstring.ToCString("version")) == 0)
                {
                    if (cstring.strcmp(argval[i], tied_mgau_common.MGAU_MIXW_VERSION) != 0)
                        err.E_WARN(string.Format("Version mismatch({0}): {1}, expecting {2}\n",
                               cstring.FromCString(file_name), cstring.FromCString(argval[i]), cstring.FromCString(tied_mgau_common.MGAU_MIXW_VERSION)));
                }
                else if (cstring.strcmp(argname[i], cstring.ToCString("chksum0")) == 0)
                {
                    chksum_present = 1; /* Ignore the associated value */
                }
            }
            bio.bio_hdrarg_free(argname, argval);
            argname = argval = PointerHelpers.NULL<Pointer<byte>>();

            chksum.Val = 0;
            
            byte[] tmp_bytes = new byte[4];
            Pointer<byte> tmp_byte_ptr = new Pointer<byte>(tmp_bytes);
            Pointer<uint> tmp_uint_ptr = tmp_byte_ptr.ReinterpretCast<uint>();
            Pointer<int> tmp_int_ptr = tmp_byte_ptr.ReinterpretCast<int>();
            /* Read #senones, #features, #codewords, arraysize */
            if ((bio.bio_fread(tmp_byte_ptr, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            n_sen = tmp_int_ptr[0];

            if ((bio.bio_fread(tmp_byte_ptr, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            n_feat = tmp_int_ptr[0];

            if ((bio.bio_fread(tmp_byte_ptr, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            n_comp = tmp_int_ptr[0];

            if ((bio.bio_fread(tmp_byte_ptr, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            n = tmp_int_ptr[0];


            if (n_feat != s.g.Deref.n_feat)
                err.E_FATAL(string.Format("#Features streams({0}) != {1}\n", n_feat, s.g.Deref.n_feat));
            if (n != n_sen * n_feat * n_comp)
            {
                err.E_FATAL
                    (string.Format("{0}: #floats({1}) doesn't match header dimensions: {2} x {3} x {4}\n",
                     cstring.FromCString(file_name), i, n_sen, n_feat, n_comp));
            }

            /* n_sen = number of mixture weights per codeword, which is
             * fixed at the number of senones since we have only one codebook.
             */
            s.n_sen = n_sen;

            /* Quantized mixture weight arrays. */
            s.mixw = ckd_alloc.ckd_calloc_3d<byte>((uint)n_feat, (uint)s.g.Deref.n_density, (uint)n_sen);

            /* Temporary structure to read in floats before conversion to (int) logs3 */
            pdf = ckd_alloc.ckd_calloc<float>(n_comp);
            Pointer<byte> pdf_byte = PointerHelpers.Malloc<byte>(n_comp * 4);
            Pointer<float> pdf_float = pdf_byte.ReinterpretCast<float>();

            /* Read senone probs data, normalize, floor, convert to logs3, truncate to 8 bits */
            n_err = 0;
            for (i = 0; i < n_sen; i++)
            {
                for (f = 0; f < n_feat; f++)
                {
                    if (bio.bio_fread(pdf_byte, 4, n_comp, fp, byteswap, chksum) != n_comp)
                    {
                        err.E_FATAL(string.Format("bio_fread({0}) (arraydata) failed\n", cstring.FromCString(file_name)));
                    }
                    pdf_float.MemCopyTo(pdf, n_comp);

                    /* Normalize and floor */
                    if (vector.vector_sum_norm(pdf, n_comp) <= 0.0)
                        n_err++;
                    vector.vector_floor(pdf, n_comp, SmoothMin);
                    vector.vector_sum_norm(pdf, n_comp);

                    /* Convert to LOG, quantize, and transpose */
                    for (c = 0; c < n_comp; c++)
                    {
                        int qscr;

                        qscr = -logmath.logmath_log(s.lmath_8b, pdf[c]);
                        if ((qscr > tied_mgau_common.MAX_NEG_MIXW) || (qscr < 0))
                            qscr = tied_mgau_common.MAX_NEG_MIXW;
                        s.mixw[f][c].Set(i, checked((byte)qscr));
                    }
                }
            }
            if (n_err > 0)
                err.E_WARN(string.Format("Weight normalization failed for {0} mixture weights components\n", n_err));

            ckd_alloc.ckd_free(pdf);

            if (chksum_present != 0)
                bio.bio_verify_chksum(fp, byteswap, chksum.Val);

            if (fp.fread(tmp_byte_ptr, 1, 1) == 1)
                err.E_FATAL(string.Format("More data than expected in {0}\n", cstring.FromCString(file_name)));

            fp.fclose();

            err.E_INFO(string.Format("Read {0} x {1} x {2} mixture weights\n", n_sen, n_feat, n_comp));
            return n_sen;
        }
        
        public static int split_topn(Pointer<byte> str, Pointer<byte> output, int nfeat)
        {
            Pointer<byte> topn_list = ckd_alloc.ckd_salloc(str);
            Pointer<byte> c, cc;
            int i;
            byte maxn;

            c = topn_list;
            i = 0;
            maxn = 0;
            while (i < nfeat && (cc = cstring.strchr(c, (byte)',')).IsNonNull)
            {
                cc.Deref = (byte)'\0';
                output[i] = (byte)cstring.atoi(c);
                if (output[i] > maxn) maxn = output[i];
                c = cc + 1;
                ++i;
            }
            if (i < nfeat && c.Deref != '\0')
            {
                output[i] = (byte)cstring.atoi(c);
                if (output[i] > maxn) maxn = output[i];
                ++i;
            }
            while (i < nfeat)
                output[i++] = maxn;

            ckd_alloc.ckd_free(topn_list);
            return maxn;
        }

        public static ps_mgau_t s2_semi_mgau_init(Pointer<acmod_t> acmod)
        {
            s2_semi_mgau_t s;
            ps_mgau_t ps;
            Pointer<byte> sendump_path;
            int i;
            int n_feat;

            s = new s2_semi_mgau_t();
            s.config = acmod.Deref.config;

            s.lmath = logmath.logmath_retain(acmod.Deref.lmath);
            /* Log-add table. */
            s.lmath_8b = logmath.logmath_init(logmath.logmath_get_base(acmod.Deref.lmath), hmm.SENSCR_SHIFT, 1);
            if (s.lmath_8b.IsNull)
                goto error_out;
            /* Ensure that it is only 8 bits wide so that fast_logmath_add() works. */
            if (logmath.logmath_get_width(s.lmath_8b) != 1)
            {
                err.E_ERROR(string.Format("Log base {0} is too small to represent add table in 8 bits\n",
                        logmath.logmath_get_base(s.lmath_8b)));
                goto error_out;
            }

            /* Read means and variances. */
            if ((s.g = ms_gauden.gauden_init(cmd_ln.cmd_ln_str_r(s.config, cstring.ToCString("_mean")),
                                    cmd_ln.cmd_ln_str_r(s.config, cstring.ToCString("_var")),
                                    (float)cmd_ln.cmd_ln_float_r(s.config, cstring.ToCString("-varfloor")),
                                    s.lmath)).IsNull)
            {
                err.E_ERROR("Failed to read means and variances\n");
                goto error_out;
            }

            /* Currently only a single codebook is supported. */
            if (s.g.Deref.n_mgau != 1)
                goto error_out;

            n_feat = s.g.Deref.n_feat;

            /* Verify n_feat and veclen, against acmod. */
            if (n_feat != feat.feat_dimension1(acmod.Deref.fcb))
            {
                err.E_ERROR(string.Format("Number of streams does not match: {0} != {1}\n",
                        n_feat, feat.feat_dimension1(acmod.Deref.fcb)));
                goto error_out;
            }
            for (i = 0; i < n_feat; ++i)
            {
                if (s.g.Deref.featlen[i] != feat.feat_dimension2(acmod.Deref.fcb, i))
                {
                    err.E_ERROR(string.Format("Dimension of stream {0} does not match: {1} != {2}\n",
                            i, s.g.Deref.featlen[i], feat.feat_dimension2(acmod.Deref.fcb, i)));
                    goto error_out;
                }
            }
            /* Read mixture weights */
            if ((sendump_path = cmd_ln.cmd_ln_str_r(s.config, cstring.ToCString("_sendump"))).IsNonNull)
            {
                if (read_sendump(s, acmod.Deref.mdef, sendump_path) < 0)
                {
                    goto error_out;
                }
            }
            else
            {
                if (read_mixw(s, cmd_ln.cmd_ln_str_r(s.config, cstring.ToCString("_mixw")),
                              cmd_ln.cmd_ln_float_r(s.config, cstring.ToCString("-mixwfloor"))) < 0)
                {
                    goto error_out;
                }
            }
            s.ds_ratio = checked((short)cmd_ln.cmd_ln_int_r(s.config, cstring.ToCString("-ds")));

            /* Determine top-N for each feature */
            s.topn_beam = ckd_alloc.ckd_calloc<byte>(n_feat);
            s.max_topn = checked((short)cmd_ln.cmd_ln_int_r(s.config, cstring.ToCString("-topn")));
            split_topn(cmd_ln.cmd_ln_str_r(s.config, cstring.ToCString("-topn_beam")), s.topn_beam, n_feat);
            err.E_INFO(string.Format("Maximum top-N: {0} ", s.max_topn));
            err.E_INFOCONT("Top-N beams:");
            for (i = 0; i < n_feat; ++i)
            {
                err.E_INFOCONT(string.Format(" {0}", s.topn_beam[i]));
            }
            err.E_INFOCONT("\n");

            /* Top-N scores from recent frames */
            s.n_topn_hist = (int)cmd_ln.cmd_ln_int_r(s.config, cstring.ToCString("-pl_window")) + 2;
            s.topn_hist = ckd_alloc.ckd_calloc_struct_3d< vq_feature_t>((uint)s.n_topn_hist, (uint)n_feat, (uint)s.max_topn);
            s.topn_hist_n = ckd_alloc.ckd_calloc_2d<byte>((uint)s.n_topn_hist, (uint)n_feat);
            for (i = 0; i < s.n_topn_hist; ++i)
            {
                int j;
                for (j = 0; j < n_feat; ++j)
                {
                    int k;
                    for (k = 0; k < s.max_topn; ++k)
                    {
                        s.topn_hist[i][j].Set(k, new vq_feature_t()
                        {
                            score = tied_mgau_common.WORST_DIST,
                            codeword = k
                        });
                    }
                }
            }

            ps = (ps_mgau_t)s;
            ps.vt = s2_semi_mgau_funcs;
            return ps;

            error_out:
            s2_semi_mgau_free(s);
            return null;
        }

        public static int
        s2_semi_mgau_mllr_transform(ps_mgau_t ps,
                                    Pointer<ps_mllr_t> mllr)
        {
            s2_semi_mgau_t s = (s2_semi_mgau_t)ps;
            return ms_gauden.gauden_mllr_transform(s.g, mllr, s.config);
        }

        public static void s2_semi_mgau_free(ps_mgau_t ps)
        {
            s2_semi_mgau_t s = (s2_semi_mgau_t)ps;

            logmath.logmath_free(s.lmath);
            logmath.logmath_free(s.lmath_8b);
            ckd_alloc.ckd_free_3d(s.mixw);
            if (s.mixw_cb.IsNonNull)
                ckd_alloc.ckd_free(s.mixw_cb);
            ms_gauden.gauden_free(s.g);
            ckd_alloc.ckd_free(s.topn_beam);
            ckd_alloc.ckd_free_2d(s.topn_hist_n);
            ckd_alloc.ckd_free_3d(s.topn_hist);
        }
    }
}
