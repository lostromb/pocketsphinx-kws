using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class phone_loop_search
    {
        public static readonly ps_searchfuncs_t phone_loop_search_funcs = new ps_searchfuncs_t()
            {
                start =  phone_loop_search_start,
                step = phone_loop_search_step,
                finish = phone_loop_search_finish,
                reinit = phone_loop_search_reinit,
                free = phone_loop_search_free,
                lattice = null,
                hyp = phone_loop_search_hyp,
                prob = phone_loop_search_prob,
                seg_iter = phone_loop_search_seg_iter,
            };

        public static int phone_loop_search_reinit(ps_search_t search, Pointer<dict_t> dict, Pointer<dict2pid_t> d2p)
        {
            phone_loop_search_t pls = (phone_loop_search_t)search;
            Pointer<cmd_ln_t> config = pocketsphinx.ps_search_config(search);
            Pointer<acmod_t> acmod = pocketsphinx.ps_search_acmod(search);
            int i;

            /* Free old dict2pid, dict, if necessary. */
            pocketsphinx.ps_search_base_reinit(search, dict, d2p);

            /* Initialize HMM context. */
            if (pls.hmmctx.IsNonNull)
                hmm.hmm_context_free(pls.hmmctx);
            pls.hmmctx = hmm.hmm_context_init(bin_mdef.bin_mdef_n_emit_state(acmod.Deref.mdef), acmod.Deref.tmat.Deref.tp, PointerHelpers.NULL<short>(), acmod.Deref.mdef.Deref.sseq);
            if (pls.hmmctx.IsNull)
                return -1;

            /* Initialize penalty storage */
            pls.n_phones = checked((short)bin_mdef.bin_mdef_n_ciphone(acmod.Deref.mdef));
            pls.window = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-pl_window"));
            if (pls.penalties.IsNonNull)
                ckd_alloc.ckd_free(pls.penalties);
            pls.penalties = ckd_alloc.ckd_calloc<int>(pls.n_phones);
            if (pls.pen_buf.IsNonNull)
                ckd_alloc.ckd_free_2d(pls.pen_buf);
            pls.pen_buf = ckd_alloc.ckd_calloc_2d<int>((uint)pls.window, (uint)pls.n_phones);

            /* Initialize phone HMMs. */
            if (pls.hmms.IsNonNull)
            {
                for (i = 0; i < pls.n_phones; ++i)
                    hmm.hmm_deinit(pls.hmms.Point(i));
                ckd_alloc.ckd_free(pls.hmms);
            }
            pls.hmms = ckd_alloc.ckd_calloc_struct<hmm_t>(pls.n_phones);
            for (i = 0; i < pls.n_phones; ++i)
            {
                hmm.hmm_init(pls.hmmctx, pls.hmms.Point(i),
                         0,
                         bin_mdef.bin_mdef_pid2ssid(acmod.Deref.mdef, i),
                         bin_mdef.bin_mdef_pid2tmatid(acmod.Deref.mdef, i));
            }
            pls.penalty_weight = cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-pl_weight"));
            pls.beam = logmath.logmath_log(acmod.Deref.lmath, cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-pl_beam"))) >> hmm.SENSCR_SHIFT;
            pls.pbeam = logmath.logmath_log(acmod.Deref.lmath, cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-pl_pbeam"))) >> hmm.SENSCR_SHIFT;
            pls.pip = logmath.logmath_log(acmod.Deref.lmath, cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-pl_pip"))) >> hmm.SENSCR_SHIFT;
            err.E_INFO(string.Format("State beam {0} Phone exit beam {1} Insertion penalty {2}\n",
                   pls.beam, pls.pbeam, pls.pip));

            return 0;
        }

        public static ps_search_t phone_loop_search_init(
            Pointer<cmd_ln_t> config,
                       Pointer<acmod_t> acmod,
                       Pointer<dict_t> dict)
        {
            phone_loop_search_t pls;

            /* Allocate and initialize. */
            pls = new phone_loop_search_t();
            pocketsphinx.ps_search_init((ps_search_t)pls, phone_loop_search_funcs,
                   pocketsphinx.PS_SEARCH_TYPE_PHONE_LOOP, pocketsphinx.PS_DEFAULT_PL_SEARCH,
                           config, acmod, dict, PointerHelpers.NULL<dict2pid_t>());
            phone_loop_search_reinit((ps_search_t)pls, pls.dict, pls.d2p);

            return (ps_search_t)pls;
        }

        public static void phone_loop_search_free_renorm(phone_loop_search_t pls)
        {
            Pointer<gnode_t> gn;
            for (gn = pls.renorm; gn.IsNonNull; gn = glist.gnode_next(gn))
                ckd_alloc.ckd_free((Pointer<phone_loop_renorm_t>)glist.gnode_ptr(gn));
            glist.glist_free(pls.renorm);
            pls.renorm = PointerHelpers.NULL<gnode_t>();
        }

        public static void phone_loop_search_free(ps_search_t search)
        {
            phone_loop_search_t pls = (phone_loop_search_t)search;
            int i;

            pocketsphinx.ps_search_base_free(search);
            for (i = 0; i < pls.n_phones; ++i)
                hmm.hmm_deinit(pls.hmms.Point(i));
            phone_loop_search_free_renorm(pls);
            ckd_alloc.ckd_free_2d(pls.pen_buf);
            ckd_alloc.ckd_free(pls.hmms);
            ckd_alloc.ckd_free(pls.penalties);
            hmm.hmm_context_free(pls.hmmctx);
        }

        public static int phone_loop_search_start(ps_search_t search)
        {
            phone_loop_search_t pls = (phone_loop_search_t)search;
            int i;

            /* Reset and enter all phone HMMs. */
            for (i = 0; i < pls.n_phones; ++i)
            {
                Pointer < hmm_t> hmmModel = pls.hmms.Point(i);
                hmm.hmm_clear(hmmModel);
                hmm.hmm_enter(hmmModel, 0, -1, 0);
            }

            pls.penalties.MemSet(0, pls.n_phones);
            for (i = 0; i < pls.window; i++)
                pls.pen_buf[i].MemSet(0, pls.n_phones);

            phone_loop_search_free_renorm(pls);
            pls.best_score = 0;
            pls.pen_buf_ptr = 0;

            return 0;
        }

        public static void renormalize_hmms(phone_loop_search_t pls, int frame_idx, int norm)
        {
            Pointer<phone_loop_renorm_t> rn = ckd_alloc.ckd_calloc_struct<phone_loop_renorm_t>(1);
            int i;

            pls.renorm = glist.glist_add_ptr(pls.renorm, rn);
            rn.Deref.frame_idx = frame_idx;
            rn.Deref.norm = norm;

            for (i = 0; i < pls.n_phones; ++i)
            {
                hmm.hmm_normalize(pls.hmms.Point(i), norm);
            }
        }

        public static void evaluate_hmms(phone_loop_search_t pls, Pointer<short> senscr, int frame_idx)
        {
            int bs = hmm.WORST_SCORE;
            int i;

            hmm.hmm_context_set_senscore(pls.hmmctx, senscr);

            for (i = 0; i<pls.n_phones; ++i) {
                Pointer < hmm_t> hmmodel = (Pointer < hmm_t>)pls.hmms.Point(i);
                int score;

                if (hmm.hmm_frame(hmmodel) < frame_idx)
                    continue;
                score = hmm.hmm_vit_eval(hmmodel);
                if (score > bs) {
                    bs = score;
                }
            }

            pls.best_score = bs;
        }

        public static void store_scores(phone_loop_search_t pls, int frame_idx)
        {
            int i, j, itr;

            for (i = 0; i < pls.n_phones; ++i)
            {
                Pointer < hmm_t> hmModel = pls.hmms.Point(i);
                pls.pen_buf[pls.pen_buf_ptr].Set(i, (int)((hmm.hmm_bestscore(hmModel) - pls.best_score) * pls.penalty_weight)); // LOGAN Check wtf is with the implicit cast here?
            }
            pls.pen_buf_ptr++;
            pls.pen_buf_ptr = checked((short)(pls.pen_buf_ptr % pls.window));

            /* update penalties */
            for (i = 0; i < pls.n_phones; ++i)
            {
                pls.penalties[i] = hmm.WORST_SCORE;
                for (j = 0, itr = pls.pen_buf_ptr + 1; j < pls.window; j++, itr++)
                {
                    itr = itr % pls.window;
                    if (pls.pen_buf[itr][i] > pls.penalties[i])
                        pls.penalties[i] = pls.pen_buf[itr][i];
                }
            }
        }

        public static void prune_hmms(phone_loop_search_t pls, int frame_idx)
        {
            int thresh = pls.best_score + pls.beam;
            int nf = frame_idx + 1;
            int i;

            /* Check all phones to see if they remain active in the next frame. */
            for (i = 0; i < pls.n_phones; ++i)
            {
                Pointer<hmm_t> hmModel = pls.hmms.Point(i);

                if (hmm.hmm_frame(hmModel) < frame_idx)
                    continue;
                /* Retain if score better than threshold. */
                if (hmm.hmm_bestscore(hmModel) > thresh)
                {
                    hmModel.Deref.frame = nf;
                }
                else
                {
                    hmm.hmm_clear_scores(hmModel);
                }
            }
        }

        public static void phone_transition(phone_loop_search_t pls, int frame_idx)
        {
            int thresh = pls.best_score + pls.pbeam;
            int nf = frame_idx + 1;
            int i;

            /* Now transition out of phones whose last states are inside the
             * phone transition beam. */
            for (i = 0; i < pls.n_phones; ++i)
            {
                Pointer<hmm_t> hmModel = pls.hmms.Point(i);
                int newphone_score;
                int j;

                if (hmm.hmm_frame(hmModel) != nf)
                    continue;

                newphone_score = hmm.hmm_out_score(hmModel) + pls.pip;
                if (newphone_score > thresh)
                {
                    /* Transition into all phones using the usual Viterbi rule. */
                    for (j = 0; j < pls.n_phones; ++j)
                    {
                        Pointer<hmm_t> nhmModel = pls.hmms.Point(j);

                        if (hmm.hmm_frame(nhmModel) < frame_idx || newphone_score > hmm.hmm_in_score(nhmModel))
                        {
                            hmm.hmm_enter(nhmModel, newphone_score, hmm.hmm_out_history(hmModel), nf);
                        }
                    }
                }
            }
        }

        public static int phone_loop_search_step(ps_search_t search, int frame_idx)
        {
            phone_loop_search_t pls = (phone_loop_search_t)search;
            Pointer<acmod_t> acModel = pocketsphinx.ps_search_acmod(search);
            Pointer<short> senscr;
            int i;

            /* All CI senones are active all the time. */
            if (pocketsphinx.ps_search_acmod(pls).Deref.compallsen == 0)
            {
                acmod.acmod_clear_active(pocketsphinx.ps_search_acmod(pls));
                for (i = 0; i < pls.n_phones; ++i)
                    acmod.acmod_activate_hmm(acModel, pls.hmms.Point(i));
            }

            /* Calculate senone scores for current frame. */
            BoxedValueInt boxed_frame_idx = new BoxedValueInt(frame_idx);
            senscr = acmod.acmod_score(acModel, boxed_frame_idx);
            frame_idx = boxed_frame_idx.Val;

            /* Renormalize, if necessary. */
            if (pls.best_score + (2 * pls.beam) < hmm.WORST_SCORE) {
                err.E_INFO(string.Format("Renormalizing Scores at frame {0}, best score {1}\n",
                       frame_idx, pls.best_score));
                renormalize_hmms(pls, frame_idx, pls.best_score);
            }

            /* Evaluate phone HMMs for current frame. */
            evaluate_hmms(pls, senscr, frame_idx);

            /* Store hmm scores for senone penaly calculation */
            store_scores(pls, frame_idx);

            /* Prune phone HMMs. */
            prune_hmms(pls, frame_idx);

            /* Do phone transitions. */
            phone_transition(pls, frame_idx);

            return 0;
        }

        public static int phone_loop_search_finish(ps_search_t search)
        {
            /* Actually nothing to do here really. */
            return 0;
        }

        public static Pointer<byte> phone_loop_search_hyp(ps_search_t search, BoxedValueInt out_score)
        {
            err.E_WARN("Hypotheses are not returned from phone loop search");
            return PointerHelpers.NULL<byte>();
        }

        public static int phone_loop_search_prob(ps_search_t search)
        {
            /* FIXME: Actually... they ought to be. */
            err.E_WARN("Posterior probabilities are not returned from phone loop search");
            return 0;
        }

        public static ps_seg_t phone_loop_search_seg_iter(ps_search_t search)
        {
            err.E_WARN("Hypotheses are not returned from phone loop search");
            return null;
        }
    }
}
