using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ptm_mgau
    {
        public static readonly ps_mgaufuncs_t ptm_mgau_funcs = new ps_mgaufuncs_t()
        {
            name = cstring.ToCString("ptm"),
            frame_eval = ptm_mgau_frame_eval,
            transform = ptm_mgau_mllr_transform,
            free = ptm_mgau_free
        };

        public static void insertion_sort_topn(Pointer<ptm_topn_t> topn, int i, int d)
        {
            ptm_topn_t vtmp;
            int j;

            topn.Set(i, new ptm_topn_t()
            {
                score = d,
                cw = topn[i].cw
            });
            if (i == 0)
                return;
            vtmp = topn[i];
            for (j = i - 1; j >= 0 && d > topn[j].score; j--)
            {
                topn[j + 1] = topn[j];
            }
            topn[j + 1] = vtmp;
        }

        public static int eval_topn(ptm_mgau_t s, int cb, int feat, Pointer<float> z)
        {
            Pointer<ptm_topn_t> topn;
            int i, ceplen;

            topn = s.f.Deref.topn[cb][feat];
            ceplen = s.g.Deref.featlen[feat];

            for (i = 0; i < s.max_topn; i++)
            {
                Pointer<float> mean;
                Pointer<float> diff = PointerHelpers.Malloc<float>(4);
                Pointer<float> sqdiff = PointerHelpers.Malloc<float>(4);
                Pointer<float> compl = PointerHelpers.Malloc<float>(4); /* diff, diff^2, component likelihood */
                Pointer<float> var;
                float d;
                Pointer<float> obs;
                int cw, j;

                cw = topn[i].cw;
                mean = s.g.Deref.mean[cb][feat][0] + cw * ceplen;
                var = s.g.Deref.var[cb][feat][0] + cw * ceplen;
                d = s.g.Deref.det[cb][feat][cw];
                obs = z;
                for (j = 0; j < ceplen % 4; ++j)
                {
                    diff[0] = (obs.Deref) - (mean.Deref);
                    obs++;
                    mean++;
                    sqdiff[0] = (diff[0] * diff[0]);
                    compl[0] = (sqdiff[0] * var.Deref);
                    d = (d - compl[0]);
                    ++var;
                }
                /* We could vectorize this but it's unlikely to make much
                 * difference as the outer loop here isn't very big. */
                for (; j < ceplen; j += 4)
                {
                    diff[0] = obs[0] - mean[0];
                    sqdiff[0] = (diff[0] * diff[0]);
                    compl[0] = (sqdiff[0] * var[0]);

                    diff[1] = obs[1] - mean[1];
                    sqdiff[1] = (diff[1] * diff[1]);
                    compl[1] = (sqdiff[1] * var[1]);

                    diff[2] = obs[2] - mean[2];
                    sqdiff[2] = (diff[2] * diff[2]);
                    compl[2] = (sqdiff[2] * var[2]);

                    diff[3] = obs[3] - mean[3];
                    sqdiff[3] = (diff[3] * diff[3]);
                    compl[3] = (sqdiff[3] * var[3]);

                    d = (d - compl[0]);
                    d = (d - compl[1]);
                    d = (d - compl[2]);
                    d = (d - compl[3]);
                    var += 4;
                    obs += 4;
                    mean += 4;
                }
                insertion_sort_topn(topn, i, (int)d);
            }

            return topn[0].score;
        }

        public static int eval_cb(ptm_mgau_t s, int cb, int feat, Pointer<float> z)
        {
            Pointer<ptm_topn_t> topn;
            Pointer<float> mean;
            Pointer<float> var;
            Pointer<float> det;
            int i, ceplen;

            int detP_ptr;
            int detE_ptr;

            int best_ptr = 0;
            int worst_ptr = 0;

            topn = s.f.Deref.topn[cb][feat];
            best_ptr = 0;
            worst_ptr = (s.max_topn - 1);
            mean = s.g.Deref.mean[cb][feat][0];
            var = s.g.Deref.var[cb][feat][0];
            det = s.g.Deref.det[cb][feat];
            detE_ptr = s.g.Deref.n_density;
            ceplen = s.g.Deref.featlen[feat];

            for (detP_ptr = 0; detP_ptr < detE_ptr; ++detP_ptr)
            {
                Pointer<float> diff = PointerHelpers.Malloc<float>(4);
                Pointer<float> sqdiff = PointerHelpers.Malloc<float>(4);
                Pointer<float> compl = PointerHelpers.Malloc<float>(4); /* diff, diff^2, component likelihood */
                float d, thresh;
                Pointer<float> obs;
                int cur_ptr;
                int cw, j;

                d = det[detP_ptr];
                thresh = (float)topn[worst_ptr].score; /* Avoid int-to-float conversions */
                obs = z;
                cw = detP_ptr;

                /* Unroll the loop starting with the first dimension(s).  In
                 * theory this might be a bit faster if this Gaussian gets
                 * "knocked out" by C0. In practice not. */
                for (j = 0; (j < ceplen % 4) && (d >= thresh); ++j)
                {
                    diff[0] = (obs.Deref) - (mean.Deref);
                    obs++;
                    mean++;
                    sqdiff[0] = (diff[0] * diff[0]);
                    compl[0] = (sqdiff[0] * (var.Deref));
                    var++;
                    d = (d - compl[0]);
                }
                /* Now do 4 dimensions at a time.  You'd think that GCC would
                 * vectorize this?  Apparently not.  And it's right, because
                 * that won't make this any faster, at least on x86-64. */
                for (; j < ceplen && d >= thresh; j += 4)
                {
                    diff[0] = obs[0] - mean[0];
                    sqdiff[0] = (diff[0] * diff[0]);
                    compl[0] = (sqdiff[0] * var[0]);

                    diff[1] = obs[1] - mean[1];
                    sqdiff[1] = (diff[1] * diff[1]);
                    compl[1] = (sqdiff[1] * var[1]);

                    diff[2] = obs[2] - mean[2];
                    sqdiff[2] = (diff[2] * diff[2]);
                    compl[2] = (sqdiff[2] * var[2]);

                    diff[3] = obs[3] - mean[3];
                    sqdiff[3] = (diff[3] * diff[3]);
                    compl[3] = (sqdiff[3] * var[3]);

                    d = (d - compl[0]);
                    d = (d - compl[1]);
                    d = (d - compl[2]);
                    d = (d - compl[3]);
                    var += 4;
                    obs += 4;
                    mean += 4;
                }
                if (j < ceplen)
                {
                    /* terminated early, so not in topn */
                    mean += (ceplen - j);
                    var += (ceplen - j);
                    continue;
                }
                if (d < thresh)
                    continue;
                for (i = 0; i < s.max_topn; i++)
                {
                    /* already there, so don't need to insert */
                    if (topn[i].cw == cw)
                        break;
                }
                if (i < s.max_topn)
                    continue;       /* already there.  Don't insert */

                /* This looks bad, but it actually isn't.  Less than 1% of eval_cb's
                    * time is spent doing this. */
                for (cur_ptr = worst_ptr - 1; cur_ptr >= best_ptr && d >= topn[cur_ptr].score; --cur_ptr)
                {
                    topn.Set(cur_ptr + 1, new ptm_topn_t()
                    {
                        cw = topn[cur_ptr].cw,
                        score = topn[cur_ptr].score
                    });
                }

                ++cur_ptr;
                topn.Set(cur_ptr, new ptm_topn_t()
                {
                    cw = cw,
                    score = (int)d
                });
            }

            return topn[best_ptr].score;
        }

        /**
         * Compute top-N densities for active codebooks (and prune)
         */
        public static int ptm_mgau_codebook_eval(ptm_mgau_t s, Pointer<Pointer<float>> z, int frame)
        { 
            int i, j;

            /* First evaluate top-N from previous frame. */
            for (i = 0; i < s.g.Deref.n_mgau; ++i)
                for (j = 0; j < s.g.Deref.n_feat; ++j)
                    eval_topn(s, i, j, z[j]);

            /* If frame downsampling is in effect, possibly do nothing else. */
            if (frame % s.ds_ratio != 0)
                return 0;

            /* Evaluate remaining codebooks. */
            for (i = 0; i < s.g.Deref.n_mgau; ++i)
            {
                if (bitvec.bitvec_is_clear(s.f.Deref.mgau_active, i) != 0)
                    continue;
                for (j = 0; j < s.g.Deref.n_feat; ++j)
                {
                    eval_cb(s, i, j, z[j]);
                }
            }
            return 0;
        }

        /**
         * Normalize densities to produce "posterior probabilities",
         * i.e. things with a reasonable dynamic range, then scale and
         * clamp them to the acceptable range.  This is actually done
         * solely to ensure that we can use fast_logmath_add().  Note that
         * unless we share the same normalizer across all codebooks for
         * each feature stream we get defective scores (that's why these
         * loops are inside out - doing it per-feature should give us
         * greater precision). */
        public static int ptm_mgau_codebook_norm(ptm_mgau_t s, Pointer<Pointer<float>> z, int frame)
        {
            int i, j;

            for (j = 0; j < s.g.Deref.n_feat; ++j)
            {
                int norm = hmm.WORST_SCORE;
                for (i = 0; i < s.g.Deref.n_mgau; ++i)
                {
                    if (bitvec.bitvec_is_clear(s.f.Deref.mgau_active, i) != 0)
                        continue;
                    if (norm < s.f.Deref.topn[i][j][0].score >> hmm.SENSCR_SHIFT)
                        norm = s.f.Deref.topn[i][j][0].score >> hmm.SENSCR_SHIFT;
                }
                SphinxAssert.assert(norm != hmm.WORST_SCORE);
                for (i = 0; i < s.g.Deref.n_mgau; ++i)
                {
                    int k;
                    if (bitvec.bitvec_is_clear(s.f.Deref.mgau_active, i) != 0)
                        continue;
                    for (k = 0; k < s.max_topn; ++k)
                    {
                        // LOGAN modified this func to avoid constant dereferencing of an inaccessible field
                        int scr = s.f.Deref.topn[i][j][k].score;
                        scr >>= hmm.SENSCR_SHIFT;
                        scr -= norm;
                        scr = -scr;
                        if (scr > tied_mgau_common.MAX_NEG_ASCR)
                            scr = tied_mgau_common.MAX_NEG_ASCR;

                        s.f.Deref.topn[i][j].Set(k, new ptm_topn_t()
                        {
                            score = scr,
                            cw = s.f.Deref.topn[i][j][k].cw
                        });
                    }
                }
            }

            return 0;
        }

        public static int ptm_mgau_calc_cb_active(ptm_mgau_t s, Pointer<byte> senone_active,
                                int n_senone_active, int compallsen)
        {
            int i, lastsen;

            if (compallsen != 0)
            {
                bitvec.bitvec_set_all(s.f.Deref.mgau_active, s.g.Deref.n_mgau);
                return 0;
            }
            bitvec.bitvec_clear_all(s.f.Deref.mgau_active, s.g.Deref.n_mgau);
            for (lastsen = i = 0; i < n_senone_active; ++i)
            {
                int sen = senone_active[i] + lastsen;
                int cb = s.sen2cb[sen];
                bitvec.bitvec_set(s.f.Deref.mgau_active, cb);
                lastsen = sen;
            }
            err.E_DEBUG("Active codebooks:");
            for (i = 0; i < s.g.Deref.n_mgau; ++i)
            {
                if (bitvec.bitvec_is_clear(s.f.Deref.mgau_active, i) != 0)
                    continue;
                err.E_DEBUG(string.Format(" {0}", i));
            }
            return 0;
        }

        /**
         * Compute senone scores from top-N densities for active codebooks.
         */
        public static int ptm_mgau_senone_eval(ptm_mgau_t s, Pointer<short> senone_scores,
                             Pointer<byte> senone_active, int n_senone_active,
                             int compall)
        {
            int i, lastsen, bestscore;

            senone_scores.MemSet(0, s.n_sen);
            /* FIXME: This is the non-cache-efficient way to do this.  We want
             * to evaluate one codeword at a time but this requires us to have
             * a reverse codebook to senone mapping, which we don't have
             * (yet), since different codebooks have different top-N
             * codewords. */
            if (compall != 0)
                n_senone_active = s.n_sen;
            bestscore = 0x7fffffff;
            for (lastsen = i = 0; i < n_senone_active; ++i)
            {
                int sen, f, cb;
                int ascore;

                if (compall != 0)
                    sen = i;
                else
                    sen = senone_active[i] + lastsen;
                lastsen = sen;
                cb = s.sen2cb[sen];

                if (bitvec.bitvec_is_clear(s.f.Deref.mgau_active, cb) != 0)
                {
                    int j;
                    /* Because senone_active is deltas we can't really "knock
                     * out" senones from pruned codebooks, and in any case,
                     * it wouldn't make any difference to the search code,
                     * which doesn't expect senone_active to change. */
                    for (f = 0; f < s.g.Deref.n_feat; ++f)
                    {
                        for (j = 0; j < s.max_topn; ++j)
                        {
                            s.f.Deref.topn[cb][f].Set(j, new ptm_topn_t()
                            {
                                cw = s.f.Deref.topn[cb][f][j].cw,
                                score = tied_mgau_common.MAX_NEG_ASCR
                            });
                        }
                    }
                }
                /* For each feature, log-sum codeword scores + mixw to get
                 * feature density, then sum (multiply) to get ascore */
                ascore = 0;
                for (f = 0; f < s.g.Deref.n_feat; ++f)
                {
                    Pointer<ptm_topn_t> topn;
                    int j, fden = 0;
                    topn = s.f.Deref.topn[cb][f];
                    for (j = 0; j < s.max_topn; ++j)
                    {
                        int mixw;
                        /* Find mixture weight for this codeword. */
                        if (s.mixw_cb.IsNonNull)
                        {
                            int dcw = s.mixw[f][topn[j].cw][sen / 2];
                            dcw = (dcw & 1) != 0 ? dcw >> 4 : dcw & 0x0f;
                            mixw = s.mixw_cb[dcw];
                        }
                        else
                        {
                            mixw = s.mixw[f][topn[j].cw][sen];
                        }
                        if (j == 0)
                            fden = mixw + topn[j].score;
                        else
                            fden = tied_mgau_common.fast_logmath_add(s.lmath_8b, fden,
                                               mixw + topn[j].score);
                        err.E_DEBUG(string.Format("fden[{0}][{1}] l+= {2} + {3} = {4}\n",
                                sen, f, mixw, topn[j].score, fden));
                    }
                    ascore += fden;
                }
                if (ascore < bestscore) bestscore = ascore;
                senone_scores[sen] = checked((short)ascore);
            }
            /* Normalize the scores again (finishing the job we started above
             * in ptm_mgau_codebook_eval...) */
            for (i = 0; i < s.n_sen; ++i)
            {
                senone_scores[i] = checked((short)(senone_scores[i] - bestscore));
            }

            return 0;
        }

        /**
         * Compute senone scores for the active senones.
         */
        public static int ptm_mgau_frame_eval(ps_mgau_t ps,
                            Pointer<short> senone_scores,
                            Pointer<byte> senone_active,
                            int n_senone_active,
                            Pointer<Pointer<float>> featbuf,
                            int frame,
                            int compallsen)
        {
            ptm_mgau_t s = (ptm_mgau_t)ps;
            int fast_eval_idx;

            /* Find the appropriate frame in the rotating history buffer
             * corresponding to the requested input frame.  No bounds checking
             * is done here, which just means you'll get semi-random crap if
             * you request a frame in the future or one that's too far in the
             * past.  Since the history buffer is just used for fast match
             * that might not be fatal. */
            fast_eval_idx = frame % s.n_fast_hist;
            s.f = s.hist + fast_eval_idx;
            /* Compute the top-N codewords for every codebook, unless this
             * is a past frame, in which case we already have them (we
             * hope!) */
            if (frame >= ps.frame_idx)
            {
                Pointer<ptm_fast_eval_t> lastf;
                /* Get the previous frame's top-N information (on the
                 * first frame of the input this is just all WORST_DIST,
                 * no harm in that) */
                if (fast_eval_idx == 0)
                    lastf = s.hist + s.n_fast_hist - 1;
                else
                    lastf = s.hist + fast_eval_idx - 1;
                /* Copy in initial top-N info */
                // LOGAN modified - need to do a deep copy of the data here because it is a struct
                for (int c = 0; c < s.g.Deref.n_mgau * s.g.Deref.n_feat * s.max_topn; c++)
                {
                    s.f.Deref.topn[0][0].Set(c, new ptm_topn_t()
                    {
                        cw = lastf.Deref.topn[0][0][c].cw,
                        score = lastf.Deref.topn[0][0][c].score
                    });
                }

                /* Generate initial active codebook list (this might not be
                 * necessary) */
                ptm_mgau_calc_cb_active(s, senone_active, n_senone_active, compallsen);
                /* Now evaluate top-N, prune, and evaluate remaining codebooks. */
                ptm_mgau_codebook_eval(s, featbuf, frame);
                ptm_mgau_codebook_norm(s, featbuf, frame);
            }
            /* Evaluate intersection of active senones and active codebooks. */
            ptm_mgau_senone_eval(s, senone_scores, senone_active,
                                 n_senone_active, compallsen);

            return 0;
        }

        public static int read_sendump(ptm_mgau_t s, Pointer<bin_mdef_t> mdef, Pointer<byte> file)
        {
            FILE fp;
            Pointer<byte> line = PointerHelpers.Malloc<byte>(1000);
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


            byte[] tmp_bytes = new byte[4];
            Pointer<byte> tmp_byte_ptr = new Pointer<byte>(tmp_bytes);
            Pointer<uint> tmp_uint_ptr = tmp_byte_ptr.ReinterpretCast<uint>();
            Pointer<int> tmp_int_ptr = tmp_byte_ptr.ReinterpretCast<int>();
            err.E_INFO(string.Format("Loading senones from dump file {0}\n", cstring.FromCString(file)));
            /* Read title size, title */
            if (fp.fread(tmp_byte_ptr, 4, 1) != 1)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to read title size from {0}", cstring.FromCString(file)));
                goto error_out;
            }
            n = tmp_int_ptr[0];

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
            if (fp.fread(tmp_byte_ptr, 4, 1) != 1)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to read header size from {0}", cstring.FromCString(file)));
                goto error_out;
            }
            n = tmp_int_ptr[0];

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
                if (fp.fread(tmp_byte_ptr, 4, 1) != 1)
                {
                    err.E_ERROR_SYSTEM(string.Format("Failed to read header string size from {0}", cstring.FromCString(file)));
                    goto error_out;
                }
                n = tmp_int_ptr[0];

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
                if (fp.fread(tmp_byte_ptr, 4, 1) != 1)
                {
                    err.E_ERROR_SYSTEM("Cannot read #rows");
                    goto error_out;
                }
                r = tmp_int_ptr[0];

                if (do_swap != 0) r = byteorder.SWAP_INT32(r);
                if (fp.fread(tmp_byte_ptr, 4, 1) != 1)
                {
                    err.E_ERROR_SYSTEM("Cannot read #columns");
                    goto error_out;
                }
                c = tmp_int_ptr[0];

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

        public static int read_mixw(ptm_mgau_t s, Pointer<byte> file_name, double SmoothMin)
        {
            Pointer<Pointer<byte>> argname;
            Pointer<Pointer<byte>> argval;
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
                err.E_FATAL_SYSTEM(string.Format("Failed to open mixture file '{0}' for reading", cstring.FromCString(file_name)));

            /* Read header, including argument-value info and 32-bit byteorder magic */
            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(fp, boxed_argname, boxed_argval, out byteswap) < 0)
                err.E_FATAL(string.Format("Failed to read header from '{0}'\n", cstring.FromCString(file_name)));

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
                    (string.Format("{0}: #float32s({1}) doesn't match header dimensions: {2} x {3} x {4}\n",
                     cstring.FromCString(file_name), i, n_sen, n_feat, n_comp));
            }

            /* n_sen = number of mixture weights per codeword, which is
             * fixed at the number of senones since we have only one codebook.
             */
            s.n_sen = n_sen;

            /* Quantized mixture weight arrays. */
            s.mixw = ckd_alloc.ckd_calloc_3d<byte>((uint)s.g.Deref.n_feat, (uint)s.g.Deref.n_density, (uint)n_sen);

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

        public static ps_mgau_t ptm_mgau_init(Pointer<acmod_t> acmod, Pointer<bin_mdef_t> mdef)
        {
            ptm_mgau_t s;
            ps_mgau_t ps;
            Pointer <byte> sendump_path;
            int i;

            s = new ptm_mgau_t();
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

            /* We only support 256 codebooks or less (like 640k or 2GB, this
             * should be enough for anyone) */
            if (s.g.Deref.n_mgau > 256)
            {
                err.E_INFO(string.Format("Number of codebooks exceeds 256: {0}\n", s.g.Deref.n_mgau));
                goto error_out;
            }
            if (s.g.Deref.n_mgau != bin_mdef.bin_mdef_n_ciphone(mdef))
            {
                err.E_INFO(string.Format("Number of codebooks doesn't match number of ciphones, doesn't look like PTM: {0} != {1}\n", s.g.Deref.n_mgau, bin_mdef.bin_mdef_n_ciphone(mdef)));
                goto error_out;
            }
            /* Verify n_feat and veclen, against acmod. */
            if (s.g.Deref.n_feat != feat.feat_dimension1(acmod.Deref.fcb))
            {
                err.E_ERROR(string.Format("Number of streams does not match: {0} != {1}\n",
                        s.g.Deref.n_feat, feat.feat_dimension1(acmod.Deref.fcb)));
                goto error_out;
            }
            for (i = 0; i < s.g.Deref.n_feat; ++i)
            {
                if (s.g.Deref.featlen[i] != feat.feat_dimension2(acmod.Deref.fcb, i))
                {
                    err.E_ERROR(string.Format("Dimension of stream {0} does not match: {1} != {2}\n",
                            s.g.Deref.featlen[i], feat.feat_dimension2(acmod.Deref.fcb, i)));
                    goto error_out;
                }
            }
            /* Read mixture weights. */
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
            s.max_topn = checked((short)cmd_ln.cmd_ln_int_r(s.config, cstring.ToCString("-topn")));
            err.E_INFO(string.Format("Maximum top-N: {0}\n", s.max_topn));

            /* Assume mapping of senones to their base phones, though this
             * will become more flexible in the future. */
            s.sen2cb = ckd_alloc.ckd_calloc<byte>(s.n_sen);
            for (i = 0; i < s.n_sen; ++i)
                s.sen2cb[i] = checked((byte)bin_mdef.bin_mdef_sen2cimap(acmod.Deref.mdef, i));

            /* Allocate fast-match history buffers.  We need enough for the
             * phoneme lookahead window, plus the current frame, plus one for
             * good measure? (FIXME: I don't remember why) */
            s.n_fast_hist = (int)cmd_ln.cmd_ln_int_r(s.config, cstring.ToCString("-pl_window")) + 2;
            s.hist = ckd_alloc.ckd_calloc_struct<ptm_fast_eval_t>(s.n_fast_hist);
            /* s.f will be a rotating pointer into s.hist. */
            s.f = s.hist;
            for (i = 0; i < s.n_fast_hist; ++i)
            {
                int j, k, m;
                /* Top-N codewords for every codebook and feature. */
                s.hist[i].topn = ckd_alloc.ckd_calloc_struct_3d<ptm_topn_t>((uint)s.g.Deref.n_mgau, (uint)s.g.Deref.n_feat, (uint)s.max_topn);
                /* Initialize them to sane (yet arbitrary) defaults. */
                for (j = 0; j < s.g.Deref.n_mgau; ++j)
                {
                    for (k = 0; k < s.g.Deref.n_feat; ++k)
                    {
                        for (m = 0; m < s.max_topn; ++m)
                        {
                            s.hist[i].topn[j][k].Set(m, new ptm_topn_t()
                            {
                                cw = m,
                                score = tied_mgau_common.WORST_DIST
                            });
                        }
                    }
                }
                /* Active codebook mapping (just codebook, not features,
                   at least not yet) */
                s.hist[i].mgau_active = bitvec.bitvec_alloc(s.g.Deref.n_mgau);
                /* Start with them all on, prune them later. */
                bitvec.bitvec_set_all(s.hist[i].mgau_active, s.g.Deref.n_mgau);
            }

            ps = (ps_mgau_t)s;
            ps.vt = ptm_mgau_funcs;
            return ps;

            error_out:
            ptm_mgau_free((ps_mgau_t)s);
            return null;
        }

        public static int ptm_mgau_mllr_transform(ps_mgau_t ps,
                                    Pointer<ps_mllr_t> mllr)
        {
            ptm_mgau_t s = (ptm_mgau_t)ps;
            return ms_gauden.gauden_mllr_transform(s.g, mllr, s.config);
        }

        public static void ptm_mgau_free(ps_mgau_t ps)
        {
            int i;
            ptm_mgau_t s = (ptm_mgau_t)ps;

            logmath.logmath_free(s.lmath);
            logmath.logmath_free(s.lmath_8b);
            ckd_alloc.ckd_free_3d(s.mixw);
            ckd_alloc.ckd_free(s.sen2cb);

            for (i = 0; i < s.n_fast_hist; i++)
            {
                ckd_alloc.ckd_free_3d(s.hist[i].topn);
                bitvec.bitvec_free(s.hist[i].mgau_active);
            }
            ckd_alloc.ckd_free(s.hist);

            ms_gauden.gauden_free(s.g);
        }
    }
}
