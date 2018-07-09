using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class kws_search
    {
        public const int KWS_MAX = 1500;

        public static Pointer<ps_lattice_t> kws_search_lattice(ps_search_t search)
        {
            return PointerHelpers.NULL<ps_lattice_t>();
        }

        public static int kws_search_prob(ps_search_t search)
        {
            return 0;
        }

        public static void kws_seg_free(ps_seg_t seg)
        {
        }

        public static int hmm_is_active(Pointer<hmm_t> _hmm)
        {
            return _hmm.Deref.frame > 0 ? 1 : 0;
        }

        public static Pointer<hmm_t> kws_nth_hmm(Pointer<kws_keyphrase_t> keyphrase, int n)
        {
            return keyphrase.Deref.hmms.Point(n);
        }

        public static void kws_seg_fill(kws_seg_t itor)
        {
            Pointer<kws_detection_t> detection = (Pointer<kws_detection_t>)glist.gnode_ptr(itor.detection);

            itor.word = detection.Deref.keyphrase;
            itor.sf = detection.Deref.sf;
            itor.ef = detection.Deref.ef;
            itor.prob = detection.Deref.prob;
            itor.ascr = detection.Deref.ascr;
            itor.lscr = 0;
        }

        public static ps_seg_t kws_seg_next(ps_seg_t seg)
        {
            kws_seg_t itor = (kws_seg_t)seg;

            Pointer<gnode_t> detect_head = glist.gnode_next(itor.detection);
            while (detect_head.IsNonNull && ((Pointer<kws_detection_t>)glist.gnode_ptr(detect_head)).Deref.ef > itor.last_frame)
                detect_head = glist.gnode_next(detect_head);
            itor.detection = detect_head;

            if (itor.detection.IsNull)
            {
                kws_seg_free(seg);
                return null;
            }

            kws_seg_fill(itor);

            return seg;
        }

        public static ps_segfuncs_t kws_segfuncs = new ps_segfuncs_t()
        {
            seg_next = kws_seg_next,
            seg_free = kws_seg_free
        };

        public static ps_seg_t kws_search_seg_iter(ps_search_t search)
        {
            kws_search_t kwss = (kws_search_t)search;
            kws_seg_t itor;
            Pointer<gnode_t> detect_head = kwss.detections.Deref.detect_list;

            while (detect_head.IsNonNull && ((Pointer<kws_detection_t>)glist.gnode_ptr(detect_head)).Deref.ef > kwss.frame - kwss.delay)
                detect_head = glist.gnode_next(detect_head);

            if (detect_head.IsNull)
                return null;

            itor = new kws_seg_t();
            itor.vt = kws_segfuncs;
            itor.search = search;
            itor.lwf = 1.0f;
            itor.detection = detect_head;
            itor.last_frame = kwss.frame - kwss.delay;
            kws_seg_fill(itor);
            return (ps_seg_t)itor;
        }

        public static ps_searchfuncs_t kws_funcs = new ps_searchfuncs_t()
        {
            start = kws_search_start,
            step = kws_search_step,
            finish = kws_search_finish,
            reinit = kws_search_reinit,
            free = kws_search_free,
            lattice = kws_search_lattice,
            hyp = kws_search_hyp,
            prob = kws_search_prob,
            seg_iter = kws_search_seg_iter,
        };


        /* Activate senones for scoring */
        public static void kws_search_sen_active(kws_search_t kwss)
        {
            int i;
            Pointer<gnode_t> gn;

            acmod.acmod_clear_active(pocketsphinx.ps_search_acmod(kwss));

            /* active phone loop hmms */
            for (i = 0; i < kwss.n_pl; i++)
                acmod.acmod_activate_hmm(pocketsphinx.ps_search_acmod(kwss), kwss.pl_hmms.Point(i));

            /* activate hmms in active nodes */
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer<kws_keyphrase_t> keyphrase = (Pointer<kws_keyphrase_t>)glist.gnode_ptr(gn);
                for (i = 0; i < keyphrase.Deref.n_hmms; i++)
                {
                    if (hmm_is_active(kws_nth_hmm(keyphrase, i)) != 0)
                        acmod.acmod_activate_hmm(pocketsphinx.ps_search_acmod(kwss), kws_nth_hmm(keyphrase, i));
                }
            }
        }

        /*
        * Evaluate all the active HMMs.
        * (Executed once per frame.)
        */
        public static void kws_search_hmm_eval(kws_search_t kwss, Pointer<short> senscr)
        {
            int i;
            Pointer<gnode_t> gn;
            int bestscore = hmm.WORST_SCORE;
            
            hmm.hmm_context_set_senscore(kwss.hmmctx, senscr);

            /* evaluate hmms from phone loop */
            for (i = 0; i < kwss.n_pl; ++i)
            {
                Pointer<hmm_t> _hmm = kwss.pl_hmms.Point(i);
                int score;

                score = hmm.hmm_vit_eval(_hmm);
                if (score > bestscore)
                    bestscore = score;
            }
            /* evaluate hmms for active nodes */
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer<kws_keyphrase_t> keyphrase = (Pointer<kws_keyphrase_t>)glist.gnode_ptr(gn);
                for (i = 0; i < keyphrase.Deref.n_hmms; i++)
                {
                    Pointer<hmm_t> _hmm = kws_nth_hmm(keyphrase, i);

                    if (hmm_is_active(_hmm) != 0)
                    {
                        int score;
                        score = hmm.hmm_vit_eval(_hmm);
                        //Console.Write("HMM Eval {0} Score {1}\n", cstring.FromCString(keyphrase.Deref.word), score);
                        if (score > bestscore)
                            bestscore = score;
                    }
                }
            }

            kwss.bestscore = bestscore;
        }

        /*
        * (Beam) prune the just evaluated HMMs, determine which ones remain
        * active. Executed once per frame.
        */
        public static void kws_search_hmm_prune(kws_search_t kwss)
        {
            int thresh, i;
            Pointer<gnode_t> gn;

            thresh = kwss.bestscore + kwss.beam;

            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer<kws_keyphrase_t> keyphrase = (Pointer<kws_keyphrase_t>)glist.gnode_ptr(gn);
                for (i = 0; i < keyphrase.Deref.n_hmms; i++)
                {
                    Pointer < hmm_t> _hmm = kws_nth_hmm(keyphrase, i);
                    if (hmm_is_active(_hmm) != 0 && hmm.hmm_bestscore(_hmm) < thresh)
                        hmm.hmm_clear(_hmm);
                }
            }
        }


        /**
        * Do phone transitions
*/
        public static void kws_search_trans(kws_search_t kwss)
        {
            Pointer < hmm_t> pl_best_hmm = PointerHelpers.NULL<hmm_t>();
            int best_out_score = hmm.WORST_SCORE;
            int i;
            Pointer < gnode_t> gn;

            /* select best hmm in phone-loop to be a predecessor */
            for (i = 0; i < kwss.n_pl; i++)
                if (hmm.hmm_out_score(kwss.pl_hmms.Point(i)) > best_out_score)
                {
                    best_out_score = hmm.hmm_out_score(kwss.pl_hmms.Point(i));
                    pl_best_hmm = kwss.pl_hmms.Point(i);
                }

            /* out probs are not ready yet */
            if (pl_best_hmm.IsNull)
                return;

            /* Check whether keyphrase wasn't spotted yet */
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer < kws_keyphrase_t> keyphrase = (Pointer <kws_keyphrase_t>)glist.gnode_ptr(gn);
                Pointer < hmm_t> last_hmm;

                if (keyphrase.Deref.n_hmms < 1)
                    continue;

                last_hmm = kws_nth_hmm(keyphrase, keyphrase.Deref.n_hmms - 1);

                if (hmm_is_active(last_hmm) != 0
                    && hmm.hmm_out_score(pl_best_hmm) > hmm.WORST_SCORE)
                {

                    if (hmm.hmm_out_score(last_hmm) - hmm.hmm_out_score(pl_best_hmm)
                        >= keyphrase.Deref.threshold)
                    {

                        int prob = hmm.hmm_out_score(last_hmm) - hmm.hmm_out_score(pl_best_hmm) - KWS_MAX;
                        kws_detections.kws_detections_add(kwss.detections, keyphrase.Deref.word,
                                          hmm.hmm_out_history(last_hmm),
                                          kwss.frame, prob,
                                          hmm.hmm_out_score(last_hmm));
                    } /* keyphrase is spotted */
                } /* last hmm of keyphrase is active */
            } /* keyphrase loop */

            /* Make transition for all phone loop hmms */
            for (i = 0; i < kwss.n_pl; i++)
            {
                if (hmm.hmm_out_score(pl_best_hmm) + kwss.plp >
                    hmm.hmm_in_score(kwss.pl_hmms.Point(i)))
                {
                    hmm.hmm_enter(kwss.pl_hmms.Point(i),
                              hmm.hmm_out_score(pl_best_hmm) + kwss.plp,
                              hmm.hmm_out_history(pl_best_hmm), kwss.frame + 1);
                }
            }

            /* Activate new keyphrase nodes, enter their hmms */
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer < kws_keyphrase_t> keyphrase = (Pointer < kws_keyphrase_t>)glist.gnode_ptr(gn);

                if (keyphrase.Deref.n_hmms < 1)
                    continue;

                for (i = keyphrase.Deref.n_hmms - 1; i > 0; i--)
                {
                    Pointer < hmm_t> pred_hmm = kws_nth_hmm(keyphrase, i - 1);
                    Pointer < hmm_t> _hmm = kws_nth_hmm(keyphrase, i);

                    if (hmm_is_active(pred_hmm) != 0)
                    {
                        if (hmm_is_active(_hmm) == 0
                            || hmm.hmm_out_score(pred_hmm) >
                            hmm.hmm_in_score(_hmm))
                            hmm.hmm_enter(_hmm, hmm.hmm_out_score(pred_hmm),
                                      hmm.hmm_out_history(pred_hmm), kwss.frame + 1);
                    }
                }

                /* Enter keyphrase start node from phone loop */
                if (hmm.hmm_out_score(pl_best_hmm) >
                    hmm.hmm_in_score(kws_nth_hmm(keyphrase, 0)))
                    hmm.hmm_enter(kws_nth_hmm(keyphrase, 0), hmm.hmm_out_score(pl_best_hmm),
                        kwss.frame, kwss.frame + 1);
            }
        }

        public static int kws_search_read_list(kws_search_t kwss, Pointer<byte> keyfile)
        {
            // LOGAN modified this whole routine
            // It's easier in C#, honestly
            string keyFile = cstring.FromCString(keyfile);
            string[] lines = keyFile.Split('\n');
            int n_keyphrases = lines.Length;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split('/');
                string word = parts[0];
                string thresh = parts[1];
                double parsedThresh = strfuncs.atof_c(cstring.ToCString(thresh));
                Pointer<kws_keyphrase_t> keyphrase = ckd_alloc.ckd_calloc_struct<kws_keyphrase_t>(1);
                keyphrase.Deref.threshold = logmath.logmath_log(kwss.acmod.Deref.lmath, parsedThresh) >> hmm.SENSCR_SHIFT;
                keyphrase.Deref.word = cstring.ToCString(word);
                kwss.keyphrases = glist.glist_add_ptr(kwss.keyphrases, keyphrase);
            }

            return 0;
        }

        public static ps_search_t kws_search_init(Pointer<byte> name,
                        Pointer<byte> keyphrase,
                        Pointer<byte> keyfile,
                        Pointer<cmd_ln_t> config,
                        Pointer<acmod_t> acmod, Pointer<dict_t> dict, Pointer<dict2pid_t> d2p)
        {
            kws_search_t kwss = new kws_search_t();
            pocketsphinx.ps_search_init(kwss, kws_funcs, pocketsphinx.PS_SEARCH_TYPE_KWS, name, config, acmod, dict, d2p);

            kwss.detections = ckd_alloc.ckd_calloc_struct< kws_detections_t>(1);

            kwss.beam =
                (int)logmath.logmath_log(acmod.Deref.lmath,
                                    cmd_ln.cmd_ln_float_r(config,
                                                     cstring.ToCString("-beam"))) >> hmm.SENSCR_SHIFT;

            kwss.plp =
                (int)logmath.logmath_log(acmod.Deref.lmath,
                                    cmd_ln.cmd_ln_float_r(config,
                                                     cstring.ToCString("-kws_plp"))) >> hmm.SENSCR_SHIFT;


            kwss.def_threshold =
                (int)logmath.logmath_log(acmod.Deref.lmath,
                                    cmd_ln.cmd_ln_float_r(config,
                                                     cstring.ToCString("-kws_threshold"))) >>
                hmm.SENSCR_SHIFT;

            kwss.delay = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-kws_delay"));

            err.E_INFO(string.Format("KWS(beam: {0}, plp: {1}, default threshold {2}, delay {3})\n",
                   kwss.beam, kwss.plp, kwss.def_threshold, kwss.delay));

            if (keyfile.IsNonNull)
            {
                if (kws_search_read_list(kwss, keyfile) < 0)
                {
                    err.E_ERROR("Failed to create kws search\n");
                    kws_search_free(kwss);
                    return null;
                }
            }
            else
            {
                Pointer<kws_keyphrase_t> k = ckd_alloc.ckd_calloc_struct< kws_keyphrase_t>(1);
                k.Deref.threshold = kwss.def_threshold;
                k.Deref.word = ckd_alloc.ckd_salloc(keyphrase);
                kwss.keyphrases = glist.glist_add_ptr(PointerHelpers.NULL<gnode_t>(), k);
            }

            /* Reinit for provided keyphrase */
            if (kws_search_reinit(kwss,
                                  pocketsphinx.ps_search_dict(kwss),
                                  pocketsphinx.ps_search_dict2pid(kwss)) < 0)
            {
                pocketsphinx.ps_search_free(kwss);
                return null;
            }

            profile.ptmr_init(kwss.perf);

            return kwss;
        }

        public static void kws_search_free(ps_search_t search)
        {
            kws_search_t kwss;
            double n_speech;
            Pointer < gnode_t> gn;

            kwss = (kws_search_t)search;

            n_speech = (double)kwss.n_tot_frame / cmd_ln.cmd_ln_int_r(pocketsphinx.ps_search_config(kwss), cstring.ToCString("-frate"));

            // LOGAN reimplement this
            //err.E_INFO("TOTAL kws %.2f CPU %.3f xRT\n",
            //       kwss.perf.t_tot_cpu,
            //       kwss.perf.t_tot_cpu / n_speech);
            //err.E_INFO("TOTAL kws %.2f wall %.3f xRT\n",
            //       kwss.perf.t_tot_elapsed,
            //       kwss.perf.t_tot_elapsed / n_speech);
            
            pocketsphinx.ps_search_base_free(search);
            hmm.hmm_context_free(kwss.hmmctx);
            kws_detections.kws_detections_reset(kwss.detections);
            ckd_alloc.ckd_free(kwss.detections);

            ckd_alloc.ckd_free(kwss.pl_hmms);
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer < kws_keyphrase_t> keyphrase = (Pointer < kws_keyphrase_t>)glist.gnode_ptr(gn);
                ckd_alloc.ckd_free(keyphrase.Deref.hmms);
                ckd_alloc.ckd_free(keyphrase.Deref.word);
                ckd_alloc.ckd_free(keyphrase);
            }

            glist.glist_free(kwss.keyphrases);
        }

        public static int kws_search_reinit(ps_search_t search, Pointer<dict_t> dictionary, Pointer<dict2pid_t> d2p)
        {
            Pointer < Pointer <byte>>  wrdptr;
            Pointer <byte> tmp_keyphrase;
            int wid, pronlen, in_dict;
            int n_hmms, n_wrds;
            int ssid, tmatid;
            int i, j, p;
            kws_search_t kwss = (kws_search_t)search;
            Pointer < bin_mdef_t> mdef = search.acmod.Deref.mdef;
            int silcipid = bin_mdef.bin_mdef_silphone(mdef);
            Pointer < gnode_t> gn;

            /* Free old dict2pid, dict */
            pocketsphinx.ps_search_base_reinit(search, dictionary, d2p);

            /* Initialize HMM context. */
            if (kwss.hmmctx.IsNonNull)
                hmm.hmm_context_free(kwss.hmmctx);
            kwss.hmmctx =
                hmm.hmm_context_init(bin_mdef.bin_mdef_n_emit_state(search.acmod.Deref.mdef),
                                 search.acmod.Deref.tmat.Deref.tp, PointerHelpers.NULL<short>(),
                                 search.acmod.Deref.mdef.Deref.sseq);
            if (kwss.hmmctx.IsNull)
                return -1;

            /* Initialize phone loop HMMs. */
            if (kwss.pl_hmms.IsNonNull)
            {
                for (i = 0; i < kwss.n_pl; ++i)
                    hmm.hmm_deinit((Pointer < hmm_t>)kwss.pl_hmms.Point(i));
                ckd_alloc.ckd_free(kwss.pl_hmms);
            }
            kwss.n_pl = bin_mdef.bin_mdef_n_ciphone(search.acmod.Deref.mdef);
            kwss.pl_hmms = ckd_alloc.ckd_calloc_struct<hmm_t>(kwss.n_pl);
            for (i = 0; i < kwss.n_pl; ++i)
            {
                hmm.hmm_init(kwss.hmmctx,
                    kwss.pl_hmms.Point(i),
                    0,
                    bin_mdef.bin_mdef_pid2ssid(search.acmod.Deref.mdef, i),
                    bin_mdef.bin_mdef_pid2tmatid(search.acmod.Deref.mdef, i));
            }

            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer < kws_keyphrase_t> keyphrase = (Pointer < kws_keyphrase_t>)glist.gnode_ptr(gn);

                /* Initialize keyphrase HMMs */
                tmp_keyphrase = ckd_alloc.ckd_salloc(keyphrase.Deref.word);
                n_wrds = strfuncs.str2words(tmp_keyphrase, PointerHelpers.NULL<Pointer<byte>>(), 0);
                wrdptr = ckd_alloc.ckd_calloc<Pointer<byte>>(n_wrds);
                strfuncs.str2words(tmp_keyphrase, wrdptr, n_wrds);

                /* count amount of hmms */
                n_hmms = 0;
                in_dict = 1;
                for (i = 0; i < n_wrds; i++)
                {
                    wid = dict.dict_wordid(dictionary, wrdptr[i]);
                    if (wid == s3types.BAD_S3WID)
                    {
                        err.E_ERROR(string.Format("Word '{0}' in phrase '{1}' is missing in the dictionary\n", cstring.FromCString(wrdptr[i]), cstring.FromCString(keyphrase.Deref.word)));
                        in_dict = 0;
                        break;
                    }
                    pronlen = dict.dict_pronlen(dictionary, wid);
                    n_hmms += pronlen;
                }

                if (in_dict == 0)
                {
                    ckd_alloc.ckd_free(wrdptr);
                    ckd_alloc.ckd_free(tmp_keyphrase);
                    continue;
                }

                /* allocate node array */
                if (keyphrase.Deref.hmms.IsNonNull)
                    ckd_alloc.ckd_free(keyphrase.Deref.hmms);
                keyphrase.Deref.hmms = ckd_alloc.ckd_calloc_struct<hmm_t>(n_hmms);
                keyphrase.Deref.n_hmms = n_hmms;

                /* fill node array */
                j = 0;
                for (i = 0; i < n_wrds; i++)
                {
                    wid = dict.dict_wordid(dictionary, wrdptr[i]);
                    pronlen = dict.dict_pronlen(dictionary, wid);
                    for (p = 0; p < pronlen; p++)
                    {
                        int ci = dict.dict_pron(dictionary, wid, p);
                        if (p == 0)
                        {
                            /* first phone of word */
                            int rc =
                                pronlen > 1 ? dict.dict_pron(dictionary, wid, 1) : silcipid;
                            ssid = d2p.Deref.ldiph_lc[ci][rc][silcipid];
                        }
                        else if (p == pronlen - 1)
                        {
                            /* last phone of the word */
                            int lc = dict.dict_pron(dictionary, wid, p - 1);
                            Pointer < xwdssid_t> rssid = d2p.Deref.rssid[ci].Point(lc);
                            int jjj = rssid.Deref.cimap[silcipid];
                            ssid = rssid.Deref.ssid[jjj]; // LOGAN WTF? Why does C allow you to declare the same variable twice in nested scopes? TODO file a bug on sphinx
                        }
                        else
                        {
                            /* word internal phone */
                            ssid = dict2pid.dict2pid_internal(d2p, wid, p);
                        }
                        tmatid = bin_mdef.bin_mdef_pid2tmatid(mdef, ci);
                        hmm.hmm_init(kwss.hmmctx, keyphrase.Deref.hmms.Point(j), 0, ssid, tmatid);
                        j++;
                    }
                }

                ckd_alloc.ckd_free(wrdptr);
                ckd_alloc.ckd_free(tmp_keyphrase);
            }


            return 0;
        }

        public static int kws_search_start(ps_search_t search)
        {
            int i;
            kws_search_t kwss = (kws_search_t)search;

            kwss.frame = 0;
            kwss.bestscore = 0;
            kws_detections.kws_detections_reset(kwss.detections);

            /* Reset and enter all phone-loop HMMs. */
            for (i = 0; i < kwss.n_pl; ++i)
            {
                Pointer<hmm_t> _hmm = (Pointer < hmm_t>)kwss.pl_hmms.Point(i);
                hmm.hmm_clear(_hmm);
                hmm.hmm_enter(_hmm, 0, -1, 0);
            }

            profile.ptmr_reset(kwss.perf);
            profile.ptmr_start(kwss.perf);

            return 0;
        }

        public static int kws_search_step(ps_search_t search, int frame_idx)
        {
            Pointer <short> senscr;
            kws_search_t kwss = (kws_search_t)search;
            Pointer<acmod_t> _acmod = search.acmod;

            /* Activate senones */
            if (_acmod.Deref.compallsen == 0)
                kws_search_sen_active(kwss);

            /* Calculate senone scores for current frame. */
            BoxedValueInt boxed_frame_idx = new BoxedValueInt(frame_idx);
            senscr = acmod.acmod_score(_acmod, boxed_frame_idx);
            frame_idx = boxed_frame_idx.Val;

            /* Evaluate hmms in phone loop and in active keyphrase nodes */
            kws_search_hmm_eval(kwss, senscr);

            /* Prune hmms with low prob */
            kws_search_hmm_prune(kwss);

            /* Do hmms transitions */
            kws_search_trans(kwss);

            ++kwss.frame;
            return 0;
        }

        public static int kws_search_finish(ps_search_t search)
        {
            kws_search_t kwss;
            int cf;

            kwss = (kws_search_t)search;

            kwss.n_tot_frame += kwss.frame;

            /* Print out some statistics. */
            profile.ptmr_stop(kwss.perf);
            /* This is the number of frames processed. */
            cf = pocketsphinx.ps_search_acmod(kwss).Deref.output_frame;
            if (cf > 0)
            {
                double n_speech = (double)(cf + 1) / cmd_ln.cmd_ln_int_r(pocketsphinx.ps_search_config(kwss), cstring.ToCString("-frate"));
                // LOGAN reimplement this
                //err.E_INFO("kws %.2f CPU %.3f xRT\n",
                //       kwss.perf.t_cpu, kwss.perf.t_cpu / n_speech);
                //err.E_INFO("kws %.2f wall %.3f xRT\n",
                //       kwss.perf.t_elapsed, kwss.perf.t_elapsed / n_speech);
            }

            return 0;
        }

        public static Pointer<byte> kws_search_hyp(ps_search_t search, BoxedValueInt out_score)
        {
            kws_search_t kwss = (kws_search_t)search;
            if (out_score != null)
                out_score.Val = 0;

            if (search.hyp_str.IsNonNull)
                ckd_alloc.ckd_free(search.hyp_str);
            search.hyp_str = kws_detections.kws_detections_hyp_str(kwss.detections, kwss.frame, kwss.delay);

            return search.hyp_str;
        }

        public static Pointer<byte> kws_search_get_keyphrases(ps_search_t search)
        {
            int c, len;
            kws_search_t kwss;
            Pointer <byte> line;
            Pointer < gnode_t> gn;

            kwss = (kws_search_t)search;

            len = 0;
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
                len += (int)cstring.strlen(((Pointer<kws_keyphrase_t>)glist.gnode_ptr(gn)).Deref.word) + 1;

            c = 0;
            line = ckd_alloc.ckd_calloc<byte>(len);
            for (gn = kwss.keyphrases; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer<byte> str = ((Pointer<kws_keyphrase_t>)glist.gnode_ptr(gn)).Deref.word;
                str.MemCopyTo(line.Point(c), (int)cstring.strlen(str));
                c += (int)cstring.strlen(str);
                line[c++] = (byte)'\n';
            }
            line[--c] = (byte)'\0';

            return line;
        }
    }
}
