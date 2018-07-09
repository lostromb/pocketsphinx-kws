using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class hmm
    {
        public const int SENSCR_SHIFT = 10;
        public const int WORST_SCORE = unchecked((int)(0xE0000000));
        public const int MAX_N_FRAMES = int.MaxValue;
        public const int TMAT_WORST_SCORE = -255;
        public const int HMM_MAX_NSTATE = 5;

        public static Pointer<hmm_context_t> hmm_context(Pointer<hmm_t> h)
        {
            return h.Deref.ctx;
        }

        public static byte hmm_is_mpx(Pointer<hmm_t> h)
        {
            return h.Deref.mpx;
        }

        public static int hmm_score(Pointer<hmm_t> h, int st)
        {
            return (h).Deref.score[st];
        }

        public static int hmm_in_score(Pointer<hmm_t> h)
        {
            return (h).Deref.score[0];
        }

        public static int hmm_out_score(Pointer<hmm_t> h)
        {
            return (h).Deref.out_score;
        }

        public static int hmm_in_history(Pointer<hmm_t> h)
        {
            return (h).Deref.history[0];
        }

        public static int hmm_history(Pointer<hmm_t> h, int st)
        {
            return (h).Deref.history[st];
        }

        public static int hmm_out_history(Pointer<hmm_t> h)
        {
            return (h).Deref.out_history;
        }

        public static int hmm_bestscore(Pointer<hmm_t> h)
        {
            return (h).Deref.bestscore;
        }

        public static int hmm_frame(Pointer<hmm_t> h)
        {
            return (h).Deref.frame;
        }

        public static int hmm_mpx_ssid(Pointer<hmm_t> h, int st)
        {
            return (h).Deref.senid[st];
        }

        public static int hmm_nonmpx_ssid(Pointer<hmm_t> h)
        {
            return (h).Deref.ssid;
        }

        public static int hmm_ssid(Pointer<hmm_t> h, int st)
        {
            return (hmm_is_mpx(h) != 0 ? hmm_mpx_ssid(h, st) : hmm_nonmpx_ssid(h));
        }

        public static ushort hmm_mpx_senid(Pointer<hmm_t> h, int st)
        {
            return (hmm_mpx_ssid(h, st) == bin_mdef.BAD_SENID ? bin_mdef.BAD_SENID : (h).Deref.ctx.Deref.sseq[hmm_mpx_ssid(h, st)][st]);
        }

        public static ushort hmm_nonmpx_senid(Pointer<hmm_t> h, int st)
        {
            return ((h).Deref.senid[st]);
        }

        public static ushort hmm_senid(Pointer<hmm_t> h, int st)
        {
            return (hmm_is_mpx(h) != 0 ? hmm_mpx_senid(h, st) : hmm_nonmpx_senid(h, st));
        }

        public static int hmm_senscr(Pointer<hmm_t> h, int st)
        {
            return (hmm_senid(h, st) == bin_mdef.BAD_SENID
                                  ? WORST_SCORE
                                  : -(h).Deref.ctx.Deref.senscore[hmm_senid(h, st)]);
        }

        public static short hmm_tmatid(Pointer<hmm_t> h)
        {
            return (h).Deref.tmatid;
        }

        public static int hmm_tprob(Pointer<hmm_t> h, int i, int j)
        {
            return (-(h).Deref.ctx.Deref.tp[hmm_tmatid(h)][i][j]);
        }

        public static byte hmm_n_emit_state(Pointer<hmm_t> h)
        {
            return ((h).Deref.n_emit_state);
        }

        public static byte hmm_n_state(Pointer<hmm_t> h)
        {
            return checked((byte)((h).Deref.n_emit_state + 1));
        }
        
        public static void hmm_context_set_senscore(Pointer<hmm_context_t> ctx, Pointer<short> senscr)
        {
            ctx.Deref.senscore = senscr;
        }

        public static Pointer<hmm_context_t> hmm_context_init(int n_emit_state,
                 Pointer<Pointer<Pointer<byte>>> tp,
                Pointer<short> senscore,
                Pointer<Pointer<ushort>> sseq)
        {
            Pointer<hmm_context_t> ctx;

            SphinxAssert.assert(n_emit_state > 0);
            if (n_emit_state > HMM_MAX_NSTATE) {
                err.E_ERROR(string.Format("Number of emitting states must be <= {0}\n", HMM_MAX_NSTATE));
                return PointerHelpers.NULL<hmm_context_t>();
            }

            ctx = ckd_alloc.ckd_calloc_struct<hmm_context_t>(1);
            ctx.Deref.n_emit_state = n_emit_state;
            ctx.Deref.tp = tp;
            ctx.Deref.senscore = senscore;
            ctx.Deref.sseq = sseq;
            ctx.Deref.st_sen_scr = ckd_alloc.ckd_calloc<int>(n_emit_state);

            return ctx;
        }

        public static void hmm_context_free(Pointer<hmm_context_t> ctx)
        {
            if (ctx.IsNull)
                return;
            ckd_alloc.ckd_free(ctx.Deref.st_sen_scr);
            ckd_alloc.ckd_free(ctx);
        }

        public static void hmm_init(Pointer<hmm_context_t> ctx, Pointer<hmm_t> hmm, byte mpx, int ssid, int tmatid)
        {
            hmm.Deref.ctx = ctx;
            hmm.Deref.mpx = mpx;
            hmm.Deref.n_emit_state = checked((byte)ctx.Deref.n_emit_state);
            if (mpx != 0)
            {
                int i;
                hmm.Deref.ssid = bin_mdef.BAD_SSID;
                hmm.Deref.senid[0] = checked((ushort)ssid);
                for (i = 1; i < hmm_n_emit_state(hmm); ++i)
                {
                    hmm.Deref.senid[i] = bin_mdef.BAD_SSID;
                }
            }
            else
            {
                hmm.Deref.ssid = checked((ushort)ssid);
                ctx.Deref.sseq[ssid].MemCopyTo(hmm.Deref.senid, hmm.Deref.n_emit_state);
            }
            hmm.Deref.tmatid = checked((short)tmatid);
            hmm_clear(hmm);
        }

        public static void hmm_deinit(Pointer<hmm_t> hmm)
        {
        }

        public static void hmm_clear_scores(Pointer<hmm_t> h)
        {
            int i;

            (h).Deref.score[0] = WORST_SCORE;
            for (i = 1; i < hmm_n_emit_state(h); i++)
                h.Deref.score[i] = WORST_SCORE;
            (h).Deref.out_score = WORST_SCORE;

            h.Deref.bestscore = WORST_SCORE;
        }

        public static void hmm_clear(Pointer<hmm_t> h)
        {
            int i;

            (h).Deref.score[0] = WORST_SCORE;
            (h).Deref.history[0] = -1;
            for (i = 1; i < hmm_n_emit_state(h); i++)
            {
                h.Deref.score[i] = WORST_SCORE;
                h.Deref.history[i] = -1;
            }
            (h).Deref.out_score = WORST_SCORE;
            (h).Deref.out_history = -1;

            h.Deref.bestscore = WORST_SCORE;
            h.Deref.frame = -1;
        }

        public static void hmm_enter(Pointer<hmm_t> h, int score, int histid, int frame)
        {
            (h).Deref.score[0] = score;
            (h).Deref.history[0] = histid;
            (h).Deref.frame = frame;
        }

        public static void hmm_normalize(Pointer<hmm_t> h, int bestscr)
        {
            int i;

            for (i = 0; i < hmm_n_emit_state(h); i++)
            {
                if (h.Deref.score[i] > WORST_SCORE)
                    h.Deref.score[i] -= bestscr;
        }
            if ((h).Deref.out_score > WORST_SCORE)
                (h).Deref.out_score -= bestscr;
        }

        public static int hmm_vit_eval_5st_lr(Pointer<hmm_t> hmm)
        {
            Pointer <short> senscore = hmm.Deref.ctx.Deref.senscore;
            Pointer <byte> tp = hmm.Deref.ctx.Deref.tp[hmm.Deref.tmatid][0];
            Pointer <ushort> sseq = hmm.Deref.senid;
            int s5, s4, s3, s2, s1, s0, t2, t1, t0, bestScore;

            /* It was the best of scores, it was the worst of scores. */
            bestScore = WORST_SCORE;

            /* Cache problem here! */
            s4 = hmm_score(hmm, 4) + (-senscore[sseq[4]]);
            s3 = hmm_score(hmm, 3) + (-senscore[sseq[3]]);
            /* Transitions into non-emitting state 5 */
            if (s3 > WORST_SCORE) {
                t1 = s4 + (-tp[(4) * 6 + (5)]);
                t2 = s3 + (-tp[(3) * 6 + (5)]);
                if (t1 > t2) {
                    s5 = t1;
                    (hmm).Deref.out_history = hmm_history(hmm, 4);
                } else {
                    s5 = t2;
                    (hmm).Deref.out_history = hmm_history(hmm, 3);
                }
                if (s5 < WORST_SCORE) s5 = WORST_SCORE;
                (hmm).Deref.out_score = s5;
                bestScore = s5;
            }

            s2 = hmm_score(hmm, 2) + (-senscore[sseq[2]]);
            /* All transitions into state 4 */
            if (s2 > WORST_SCORE) {
                t0 = s4 + (-tp[(4) * 6 + (4)]);
                t1 = s3 + (-tp[(3) * 6 + (4)]);
                t2 = s2 + (-tp[(2) * 6 + (4)]);
                if (t0 > t1) {
                    if (t2 > t0) {
                        s4 = t2;
                        hmm.Deref.history[4] = hmm_history(hmm, 2);
                    } else
                        s4 = t0;
                } else {
                    if (t2 > t1) {
                        s4 = t2;
                        hmm.Deref.history[4] = hmm_history(hmm, 2);
                    } else {
                        s4 = t1;
                        hmm.Deref.history[4] = hmm_history(hmm, 3);
                    }
                }
                if (s4 < WORST_SCORE) s4 = WORST_SCORE;
                if (s4 > bestScore) bestScore = s4;
                hmm.Deref.score[4] = s4;
            }

            s1 = hmm_score(hmm, 1) + (-senscore[sseq[1]]);
            /* All transitions into state 3 */
            if (s1 > WORST_SCORE) {
                t0 = s3 + (-tp[(3) * 6 + (3)]);
                t1 = s2 + (-tp[(2) * 6 + (3)]);
                t2 = s1 + (-tp[(1) * 6 + (3)]);
                if (t0 > t1) {
                    if (t2 > t0) {
                        s3 = t2;
                        hmm.Deref.history[3] = hmm_history(hmm, 1);
                    } else
                        s3 = t0;
                } else {
                    if (t2 > t1) {
                        s3 = t2;
                        hmm.Deref.history[3] = hmm_history(hmm, 1);
                    } else {
                        s3 = t1;
                        hmm.Deref.history[3] = hmm_history(hmm, 2);
                    }
                }
                if (s3 < WORST_SCORE) s3 = WORST_SCORE;
                if (s3 > bestScore) bestScore = s3;
                hmm.Deref.score[3] = s3;
            }

            s0 = (hmm).Deref.score[0] + (-senscore[sseq[0]]);
            /* All transitions into state 2 (state 0 is always active) */
            t0 = s2 + (-tp[(2) * 6 + (2)]);
            t1 = s1 + (-tp[(1) * 6 + (2)]);
            t2 = s0 + (-tp[(0) * 6 + (2)]);
            if (t0 > t1) {
                if (t2 > t0) {
                    s2 = t2;
                    hmm.Deref.history[2] = hmm_in_history(hmm);
                } else
                    s2 = t0;
            } else {
                if (t2 > t1) {
                    s2 = t2;
                    hmm.Deref.history[2] = hmm_in_history(hmm);
                } else {
                    s2 = t1;
                    hmm.Deref.history[2] = hmm_history(hmm, 1);
                }
            }
            if (s2 < WORST_SCORE) s2 = WORST_SCORE;
            if (s2 > bestScore) bestScore = s2;
            hmm.Deref.score[2] = s2;


            /* All transitions into state 1 */
            t0 = s1 + (-tp[(1) * 6 + (1)]);
            t1 = s0 + (-tp[(0) * 6 + (1)]);
            if (t0 > t1) {
                s1 = t0;
            } else {
                s1 = t1;
                hmm.Deref.history[1] = hmm_in_history(hmm);
            }
            if (s1 < WORST_SCORE) s1 = WORST_SCORE;
            if (s1 > bestScore) bestScore = s1;
            hmm.Deref.score[1] = s1;

            /* All transitions into state 0 */
            s0 = s0 + (-tp[(0) * 6 + (0)]);
            if (s0 < WORST_SCORE) s0 = WORST_SCORE;
            if (s0 > bestScore) bestScore = s0;
            (hmm).Deref.score[0] = s0;

            (hmm).Deref.bestscore = bestScore;
            return bestScore;
        }

        public static int hmm_vit_eval_5st_lr_mpx(Pointer<hmm_t> hmm)
        {
            Pointer<byte> tp = hmm.Deref.ctx.Deref.tp[hmm.Deref.tmatid][0];
            Pointer<short> senscore = hmm.Deref.ctx.Deref.senscore;
            Pointer<Pointer<ushort>> sseq = hmm.Deref.ctx.Deref.sseq;
            Pointer <ushort> ssid = hmm.Deref.senid;
            int bestScore;
            int s5, s4, s3, s2, s1, s0, t2, t1, t0;

            /* Don't propagate WORST_SCORE */
            if (ssid[4] == bin_mdef.BAD_SSID)
                s4 = t1 = WORST_SCORE;
            else
            {
                s4 = hmm_score(hmm, 4) + (-senscore[sseq[ssid[4]][4]]);
                t1 = s4 + (-tp[(4) * 6 + (5)]);
            }
            if (ssid[3] == bin_mdef.BAD_SSID)
                s3 = t2 = WORST_SCORE;
            else
            {
                s3 = hmm_score(hmm, 3) + (-senscore[sseq[ssid[3]][3]]);
                t2 = s3 + (-tp[(3) * 6 + (5)]);
            }
            if (t1 > t2) {
                s5 = t1;
                (hmm).Deref.out_history = hmm_history(hmm, 4);
            }
            else {
                s5 = t2;
                (hmm).Deref.out_history = hmm_history(hmm, 3);
            }
            if (s5 < WORST_SCORE) s5 = WORST_SCORE;
            (hmm).Deref.out_score = s5;
            bestScore = s5;

            /* Don't propagate WORST_SCORE */
            if (ssid[2] == bin_mdef.BAD_SSID)
                s2 = t2 = WORST_SCORE;
            else
            {
                s2 = hmm_score(hmm, 2) + (-senscore[sseq[ssid[2]][2]]);
                t2 = s2 + (-tp[(2) * 6 + (4)]);
            }

            t0 = t1 = WORST_SCORE;
            if (s4 != WORST_SCORE)
                t0 = s4 + (-tp[(4) * 6 + (4)]);
            if (s3 != WORST_SCORE)
                t1 = s3 + (-tp[(3) * 6 + (4)]);
            if (t0 > t1) {
                if (t2 > t0) {
                    s4 = t2;
                    (hmm).Deref.history[4] = hmm_history(hmm, 2);
                    ssid[4] = ssid[2];
                }
                else
                    s4 = t0;
            }
            else {
                if (t2 > t1) {
                    s4 = t2;
                    (hmm).Deref.history[4] = hmm_history(hmm, 2);
                    ssid[4] = ssid[2];
                }
                else {
                    s4 = t1;
                    (hmm).Deref.history[4] = hmm_history(hmm, 3);
                    ssid[4] = ssid[3];
                }
            }
            if (s4 < WORST_SCORE) s4 = WORST_SCORE;
            if (s4 > bestScore)
                bestScore = s4;
            (hmm).Deref.history[4] = s4;

            /* Don't propagate WORST_SCORE */
            if (ssid[1] == bin_mdef.BAD_SSID)
                s1 = t2 = WORST_SCORE;
            else
            {
                s1 = hmm_score(hmm, 1) + (-senscore[sseq[ssid[1]][1]]);
                t2 = s1 + (-tp[(1) * 6 + (3)]);
            }
            t0 = t1 = WORST_SCORE;
            if (s3 != WORST_SCORE)
                t0 = s3 + (-tp[(3) * 6 + (3)]);
            if (s2 != WORST_SCORE)
                t1 = s2 + (-tp[(2) * 6 + (3)]);
            if (t0 > t1) {
                if (t2 > t0) {
                    s3 = t2;
                    (hmm).Deref.history[3] = hmm_history(hmm, 1);
                    ssid[3] = ssid[1];
                }
                else
                    s3 = t0;
            }
            else {
                if (t2 > t1) {
                    s3 = t2;
                    (hmm).Deref.history[3] = hmm_history(hmm, 1);
                    ssid[3] = ssid[1];
                }
                else {
                    s3 = t1;
                    (hmm).Deref.history[3] = hmm_history(hmm, 2);
                    ssid[3] = ssid[2];
                }
            }
            if (s3 < WORST_SCORE) s3 = WORST_SCORE;
            if (s3 > bestScore) bestScore = s3;
            (hmm).Deref.history[3] = s3;

            /* State 0 is always active */
            s0 = (hmm).Deref.score[0] + (-senscore[sseq[ssid[0]][0]]);

            /* Don't propagate WORST_SCORE */
            t0 = t1 = WORST_SCORE;
            if (s2 != WORST_SCORE)
                t0 = s2 + (-tp[(2) * 6 + (2)]);
            if (s1 != WORST_SCORE)
                t1 = s1 + (-tp[(1) * 6 + (2)]);
            t2 = s0 + (-tp[(0) * 6 + (2)]);
            if (t0 > t1) {
                if (t2 > t0) {
                    s2 = t2;
                    (hmm).Deref.history[2] = hmm_in_history(hmm);
                    ssid[2] = ssid[0];
                }
                else
                    s2 = t0;
            }
            else {
                if (t2 > t1) {
                    s2 = t2;
                    (hmm).Deref.history[2] = hmm_in_history(hmm);
                    ssid[2] = ssid[0];
                }
                else {
                    s2 = t1;
                    (hmm).Deref.history[2] = hmm_history(hmm, 1);
                    ssid[2] = ssid[1];
                }
            }
            if (s2 < WORST_SCORE) s2 = WORST_SCORE;
            if (s2 > bestScore) bestScore = s2;
            (hmm).Deref.history[2] = s2;

            /* Don't propagate WORST_SCORE */
            t0 = WORST_SCORE;
            if (s1 != WORST_SCORE)
                t0 = s1 + (-tp[(1) * 6 + (1)]);
            t1 = s0 + (-tp[(0) * 6 + (1)]);
            if (t0 > t1) {
                s1 = t0;
            }
            else {
                s1 = t1;
                (hmm).Deref.history[1] = hmm_in_history(hmm);
                ssid[1] = ssid[0];
            }
            if (s1 < WORST_SCORE) s1 = WORST_SCORE;
            if (s1 > bestScore) bestScore = s1;
            (hmm).Deref.score[1] = s1;

            s0 += (-tp[(0) * 6 + (0)]);
            if (s0 < WORST_SCORE) s0 = WORST_SCORE;
            if (s0 > bestScore) bestScore = s0;
            (hmm).Deref.score[0] = s0;

            (hmm).Deref.bestscore = bestScore;
            return bestScore;
        }

        public static int hmm_vit_eval_3st_lr(Pointer<hmm_t> hmm)
        {
            Pointer <short> senscore = hmm.Deref.ctx.Deref.senscore;
            Pointer <byte> tp = hmm.Deref.ctx.Deref.tp[hmm.Deref.tmatid][0];
            Pointer <ushort> sseq = hmm.Deref.senid;
            int s3, s2, s1, s0, t2, t1, t0, bestScore;

            s2 = hmm_score(hmm, 2) + (-senscore[sseq[2]]);
            s1 = hmm_score(hmm, 1) + (-senscore[sseq[1]]);
            s0 = (hmm).Deref.score[0] + (-senscore[sseq[0]]);

            /* It was the best of scores, it was the worst of scores. */
            bestScore = WORST_SCORE;
            t2 = int.MinValue; /* Not used unless skipstate is true */

            /* Transitions into non-emitting state 3 */
            if (s1 > WORST_SCORE) {
                t1 = s2 + (-tp[(2) * 4 + (3)]);
                if ((-tp[(1) * 4 + (3)]) > TMAT_WORST_SCORE)
                    t2 = s1 + (-tp[(1) * 4 + (3)]);
                if (t1 > t2) {
                    s3 = t1;
                    (hmm).Deref.out_history = hmm_history(hmm, 2);
                } else {
                    s3 = t2;
                    (hmm).Deref.out_history = hmm_history(hmm, 1);
                }
                if (s3 < WORST_SCORE) s3 = WORST_SCORE;
                (hmm).Deref.out_score = s3;
                bestScore = s3;
            }

            /* All transitions into state 2 (state 0 is always active) */
            t0 = s2 + (-tp[(2) * 4 + (2)]);
            t1 = s1 + (-tp[(1) * 4 + (2)]);
            if ((-tp[(0) * 4 + (2)]) > TMAT_WORST_SCORE)
                t2 = s0 + (-tp[(0) * 4 + (2)]);
            if (t0 > t1) {
                if (t2 > t0) {
                    s2 = t2;
                    (hmm).Deref.history[2] = hmm_in_history(hmm);
                } else
                    s2 = t0;
            } else {
                if (t2 > t1) {
                    s2 = t2;
                    (hmm).Deref.history[2] = hmm_in_history(hmm);
                } else {
                    s2 = t1;
                    (hmm).Deref.history[2] = hmm_history(hmm, 1);
                }
            }
            if (s2 < WORST_SCORE) s2 = WORST_SCORE;
            if (s2 > bestScore) bestScore = s2;
            (hmm).Deref.score[2] = s2;

            /* All transitions into state 1 */
            t0 = s1 + (-tp[(1) * 4 + (1)]);
            t1 = s0 + (-tp[(0) * 4 + (1)]);
            if (t0 > t1) {
                s1 = t0;
            } else {
                s1 = t1;
                (hmm).Deref.history[1] = hmm_in_history(hmm);
            }
            if (s1 < WORST_SCORE) s1 = WORST_SCORE;
            if (s1 > bestScore) bestScore = s1;
            (hmm).Deref.score[1] = s1;

            /* All transitions into state 0 */
            s0 = s0 + (-tp[(0) * 4 + (0)]);
            if (s0 < WORST_SCORE) s0 = WORST_SCORE;
            if (s0 > bestScore) bestScore = s0;
            (hmm).Deref.score[0] = s0;

            (hmm).Deref.bestscore = bestScore;
            return bestScore;
        }

        public static int hmm_vit_eval_3st_lr_mpx(Pointer<hmm_t> hmm)
        {
            Pointer <byte> tp = hmm.Deref.ctx.Deref.tp[hmm.Deref.tmatid][0];
            Pointer <short> senscore = hmm.Deref.ctx.Deref.senscore;
            Pointer <Pointer<ushort>> sseq = hmm.Deref.ctx.Deref.sseq;
            Pointer <ushort> ssid = hmm.Deref.senid;
            int bestScore;
            int s3, s2, s1, s0, t2, t1, t0;

            /* Don't propagate WORST_SCORE */
            t2 = int.MinValue; /* Not used unless skipstate is true */
            if (ssid[2] == bin_mdef.BAD_SSID)
                s2 = t1 = WORST_SCORE;
            else
            {
                s2 = hmm_score(hmm, 2) + (-senscore[sseq[ssid[2]][2]]);
                t1 = s2 + (-tp[(2) * 4 + (3)]);
            }
            if (ssid[1] == bin_mdef.BAD_SSID)
                s1 = t2 = WORST_SCORE;
            else
            {
                s1 = hmm_score(hmm, 1) + (-senscore[sseq[ssid[1]][1]]);
                if ((-tp[(1) * 4 + (3)]) > TMAT_WORST_SCORE)
                    t2 = s1 + (-tp[(1) * 4 + (3)]);
            }
            if (t1 > t2) {
                s3 = t1;
                (hmm).Deref.out_history = hmm_history(hmm, 2);
            }
            else {
                s3 = t2;
                (hmm).Deref.out_history = hmm_history(hmm, 1);
            }
            if (s3 < WORST_SCORE) s3 = WORST_SCORE;
            (hmm).Deref.out_score = s3;
            bestScore = s3;

            /* State 0 is always active */
            s0 = (hmm).Deref.score[0] + (-senscore[sseq[ssid[0]][0]]);

            /* Don't propagate WORST_SCORE */
            t0 = t1 = WORST_SCORE;
            if (s2 != WORST_SCORE)
                t0 = s2 + (-tp[(2) * 4 + (2)]);
            if (s1 != WORST_SCORE)
                t1 = s1 + (-tp[(1) * 4 + (2)]);
            if ((-tp[(0) * 4 + (2)]) > TMAT_WORST_SCORE)
                t2 = s0 + (-tp[(0) * 4 + (2)]);
            if (t0 > t1) {
                if (t2 > t0) {
                    s2 = t2;
                    (hmm).Deref.history[2] = hmm_in_history(hmm);
                    ssid[2] = ssid[0];
                }
                else
                    s2 = t0;
            }
            else {
                if (t2 > t1) {
                    s2 = t2;
                    (hmm).Deref.history[2] = hmm_in_history(hmm);
                    ssid[2] = ssid[0];
                }
                else {
                    s2 = t1;
                    (hmm).Deref.history[2] = hmm_history(hmm, 1);
                    ssid[2] = ssid[1];
                }
            }
            if (s2 < WORST_SCORE) s2 = WORST_SCORE;
            if (s2 > bestScore) bestScore = s2;
            (hmm).Deref.score[2] = s2;

            /* Don't propagate WORST_SCORE */
            t0 = WORST_SCORE;
            if (s1 != WORST_SCORE)
                t0 = s1 + (-tp[(1) * 4 + (1)]);
            t1 = s0 + (-tp[(0) * 4 + (1)]);
            if (t0 > t1) {
                s1 = t0;
            }
            else {
                s1 = t1;
                (hmm).Deref.history[1] = hmm_in_history(hmm);
                ssid[1] = ssid[0];
            }
            if (s1 < WORST_SCORE) s1 = WORST_SCORE;
            if (s1 > bestScore) bestScore = s1;
            (hmm).Deref.score[1] = s1;

            /* State 0 is always active */
            s0 += (-tp[(0) * 4 + (0)]);
            if (s0 < WORST_SCORE) s0 = WORST_SCORE;
            if (s0 > bestScore) bestScore = s0;
            (hmm).Deref.score[0] = s0;

            (hmm).Deref.bestscore = bestScore;
            return bestScore;
        }

        public static int hmm_vit_eval_anytopo(Pointer<hmm_t> hmm)
        {
            Pointer<hmm_context_t> ctx = hmm.Deref.ctx;
            int to, from, bestfrom;
            int newscr, scr, bestscr;
            int final_state;

            /* Compute previous state-score + observation output prob for each emitting state */
            ctx.Deref.st_sen_scr[0] = (hmm).Deref.score[0] + hmm_senscr(hmm, 0);
            for (from = 1; from < hmm_n_emit_state(hmm); ++from)
            {
                if ((ctx.Deref.st_sen_scr[from] =
                     hmm_score(hmm, from) + hmm_senscr(hmm, from)) < WORST_SCORE)
                    ctx.Deref.st_sen_scr[from] = WORST_SCORE;
        }

        /* FIXME/TODO: Use the BLAS for all this. */
        /* Evaluate final-state first, which does not have a self-transition */
        final_state = hmm_n_emit_state(hmm);
        to = final_state;
            scr = WORST_SCORE;
            bestfrom = -1;
            for (from = to - 1; from >= 0; --from) {
                if ((hmm_tprob(hmm, from, to) > TMAT_WORST_SCORE) &&
                    ((newscr = ctx.Deref.st_sen_scr[from]
                      + hmm_tprob(hmm, from, to)) > scr)) {
                    scr = newscr;
                    bestfrom = from;
                }
            }

            (hmm).Deref.out_score = scr;
            if (bestfrom >= 0)
                (hmm).Deref.out_history = hmm_history(hmm, bestfrom);
            bestscr = scr;

            /* Evaluate all other states, which might have self-transitions */
            for (to = final_state - 1; to >= 0; --to) {
                /* Score from self-transition, if any */
                scr =
                    (hmm_tprob(hmm, to, to) > TMAT_WORST_SCORE)
                    ? ctx.Deref.st_sen_scr[to] + hmm_tprob(hmm, to, to)
                    : WORST_SCORE;

                /* Scores from transitions from other states */
                bestfrom = -1;
                for (from = to - 1; from >= 0; --from) {
                    if ((hmm_tprob(hmm, from, to) > TMAT_WORST_SCORE) &&
                        ((newscr = ctx.Deref.st_sen_scr[from]
                          + hmm_tprob(hmm, from, to)) > scr)) {
                        scr = newscr;
                        bestfrom = from;
                    }
                }

                /* Update new result for state to */
                if (to == 0) {
                    (hmm).Deref.score[0] = scr;
                    if (bestfrom >= 0)
                        (hmm).Deref.history[0] = hmm_history(hmm, bestfrom);
                }
                else {
                    (hmm).Deref.score[to] = scr;
                    if (bestfrom >= 0)
                        (hmm).Deref.history[to] = hmm_history(hmm, bestfrom);
                }
                /* Propagate ssid for multiplex HMMs */
                if (bestfrom >= 0 && hmm_is_mpx(hmm) != 0)
                    hmm.Deref.senid[to] = hmm.Deref.senid[bestfrom];

                if (bestscr < scr)
                    bestscr = scr;
            }

            (hmm).Deref.bestscore = bestscr;
            return bestscr;
        }

        public static int hmm_vit_eval(Pointer<hmm_t> _hmm)
        {
            if (hmm_is_mpx(_hmm) != 0)
            {
                if (hmm_n_emit_state(_hmm) == 5)
                    return hmm_vit_eval_5st_lr_mpx(_hmm);
                else if (hmm_n_emit_state(_hmm) == 3)
                    return hmm_vit_eval_3st_lr_mpx(_hmm);
                else
                    return hmm_vit_eval_anytopo(_hmm);
            }
            else
            {
                if (hmm_n_emit_state(_hmm) == 5)
                    return hmm_vit_eval_5st_lr(_hmm);
                else if (hmm_n_emit_state(_hmm) == 3)
                    return hmm_vit_eval_3st_lr(_hmm);
                else
                    return hmm_vit_eval_anytopo(_hmm);
            }
        }

        public static int hmm_dump_vit_eval(Pointer<hmm_t> _hmm)
        {
            int bs = 0;
            Console.Write("BEFORE:\n");
            hmm_dump(_hmm);
            bs = hmm_vit_eval(_hmm);
            Console.Write("AFTER:\n");
            hmm_dump(_hmm);
            return bs;
        }

        public static void hmm_dump(Pointer<hmm_t> _hmm)
        {
            int i;

            if (hmm_is_mpx(_hmm) != 0)
            {
                Console.Write("MPX   ");
                for (i = 0; i < hmm_n_emit_state(_hmm); i++)
                    Console.Write(" {0}", hmm_senid(_hmm, i));
                Console.Write(" ( ");
                for (i = 0; i < hmm_n_emit_state(_hmm); i++)
                    Console.Write("{0} ", hmm_ssid(_hmm, i));
                Console.Write(")\n");
            }
            else
            {
                Console.Write("SSID  ");
                for (i = 0; i < hmm_n_emit_state(_hmm); i++)
                    Console.Write(" {0}", hmm_senid(_hmm, i));
                Console.Write(" ({0})\n", hmm_ssid(_hmm, 0));
            }

            if (_hmm.Deref.ctx.Deref.senscore.IsNonNull)
            {
                Console.Write("SENSCR");
                for (i = 0; i < hmm_n_emit_state(_hmm); i++)
                    Console.Write(" {0}", hmm_senscr(_hmm, i));
                Console.Write("\n");
            }

            Console.Write("SCORES {0}", hmm_in_score(_hmm));
            for (i = 1; i < hmm_n_emit_state(_hmm); i++)
                Console.Write(" {0}", hmm_score(_hmm, i));
            Console.Write(" {0}", hmm_out_score(_hmm));
            Console.Write("\n");

            Console.Write("HISTID {0}", hmm_in_history(_hmm));
            for (i = 1; i < hmm_n_emit_state(_hmm); i++)
                Console.Write(" {0}", hmm_history(_hmm, i));
            Console.Write(" {0}", hmm_out_history(_hmm));
            Console.Write("\n");

            if (hmm_in_score(_hmm) > 0)
                Console.Write(
                        "ALERT!! The input score {0} is larger than 0. Probably wrap around.\n",
                        hmm_in_score(_hmm));
            if (hmm_out_score(_hmm) > 0)
                Console.Write(
                        "ALERT!! The output score {0} is larger than 0. Probably wrap around\n.",
                        hmm_out_score(_hmm));
        }
    }
}
