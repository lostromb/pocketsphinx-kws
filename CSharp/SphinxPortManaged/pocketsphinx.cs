using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class pocketsphinx
    {
        /* Search names*/
        public static readonly Pointer<byte> PS_DEFAULT_SEARCH = cstring.ToCString("_default");
        public static readonly Pointer<byte> PS_DEFAULT_PL_SEARCH = cstring.ToCString("_default_pl");

        /* Search types */
        public static readonly Pointer<byte> PS_SEARCH_TYPE_KWS = cstring.ToCString("kws");
        public static readonly Pointer<byte> PS_SEARCH_TYPE_FSG = cstring.ToCString("fsg");
        public static readonly Pointer<byte> PS_SEARCH_TYPE_NGRAM = cstring.ToCString("ngram");
        public static readonly Pointer<byte> PS_SEARCH_TYPE_ALLPHONE = cstring.ToCString("allphone");
        public static readonly Pointer<byte> PS_SEARCH_TYPE_STATE_ALIGN = cstring.ToCString("state_align");
        public static readonly Pointer<byte> PS_SEARCH_TYPE_PHONE_LOOP = cstring.ToCString("phone_loop");

        public static Pointer<arg_t> ps_args()
        {
            return ps_args_def();
        }

        public static Pointer<arg_t> ps_args_def()
        {
            List<arg_t> args = cmdln_macro.POCKETSPHINX_OPTIONS();
            return new Pointer<arg_t>(args.ToArray());
        }

        public static Pointer<arg_t> feat_defn()
        {
            List<arg_t> args = new List<arg_t>();
            args.AddRange(cmdln_macro.waveform_to_cepstral_command_line_macro());
            args.AddRange(cmdln_macro.cepstral_to_feature_command_line_macro());
            args.Add(null);
            return new Pointer<arg_t>(args.ToArray());
        }

        public static void ps_expand_file_config(Pointer<ps_decoder_t> ps, Pointer<byte> arg, Pointer<byte> extra_arg,
                          Pointer<byte> hmmdir, Pointer<byte> file)
        {
            Pointer<byte> val;
            if ((val = cmd_ln.cmd_ln_str_r(ps.Deref.config, arg)).IsNonNull)
            {
                cmd_ln.cmd_ln_set_str_extra_r(ps.Deref.config, extra_arg, val);
            }
            else if (hmmdir.IsNull)
            {
                cmd_ln.cmd_ln_set_str_extra_r(ps.Deref.config, extra_arg, PointerHelpers.NULL<byte>());
            }
            else
            {
                string path = System.IO.Path.Combine(cstring.FromCString(hmmdir), cstring.FromCString(file));
                Pointer<byte> tmp = cstring.ToCString(path);
                if (FILE.file_exists(tmp))
                    cmd_ln.cmd_ln_set_str_extra_r(ps.Deref.config, extra_arg, tmp);
                else
                    cmd_ln.cmd_ln_set_str_extra_r(ps.Deref.config, extra_arg, PointerHelpers.NULL<byte>());
                ckd_alloc.ckd_free(tmp);
            }
        }
        
        public static void ps_expand_model_config(Pointer<ps_decoder_t> ps)
        {
            Pointer<byte> hmmdir;
            Pointer <byte> featparams;
            /* Get acoustic model filenames and add them to the command-line */
            hmmdir = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("-hmm"));
            ps_expand_file_config(ps, cstring.ToCString("-mdef"), cstring.ToCString("_mdef"), hmmdir, cstring.ToCString("mdef"));
            ps_expand_file_config(ps, cstring.ToCString("-mean"), cstring.ToCString("_mean"), hmmdir, cstring.ToCString("means"));
            ps_expand_file_config(ps, cstring.ToCString("-var"), cstring.ToCString("_var"), hmmdir, cstring.ToCString("variances"));
            ps_expand_file_config(ps, cstring.ToCString("-tmat"), cstring.ToCString("_tmat"), hmmdir, cstring.ToCString("transition_matrices"));
            ps_expand_file_config(ps, cstring.ToCString("-mixw"), cstring.ToCString("_mixw"), hmmdir, cstring.ToCString("mixture_weights"));
            ps_expand_file_config(ps, cstring.ToCString("-sendump"), cstring.ToCString("_sendump"), hmmdir, cstring.ToCString("sendump"));
            ps_expand_file_config(ps, cstring.ToCString("-fdict"), cstring.ToCString("_fdict"), hmmdir, cstring.ToCString("noisedict"));
            ps_expand_file_config(ps, cstring.ToCString("-lda"), cstring.ToCString("_lda"), hmmdir, cstring.ToCString("feature_transform"));
            ps_expand_file_config(ps, cstring.ToCString("-featparams"), cstring.ToCString("_featparams"), hmmdir, cstring.ToCString("feat.params"));
            ps_expand_file_config(ps, cstring.ToCString("-senmgau"), cstring.ToCString("_senmgau"), hmmdir, cstring.ToCString("senmgau"));

            /* Look for feat.params in acoustic model dir. */
            if ((featparams = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("_featparams"))).IsNonNull)
            {
                if (cmd_ln.cmd_ln_parse_file_r(ps.Deref.config, feat_defn(), featparams, 0).IsNonNull)
                    err.E_INFO(string.Format("Parsed model-specific feature parameters from {0}\n",
                            cstring.FromCString(featparams)));
            }

            /* Print here because acmod_init might load feat.params file */
            //if (err_get_logfp() != NULL)
            //{
            //    cmd_ln.cmd_ln_print_values_r(ps.Deref.config, err_get_logfp(), ps_args());
            //}
        }

        public static void ps_free_searches(Pointer<ps_decoder_t> ps)
        {
            if (ps.Deref.searches.IsNonNull)
            {
                Pointer<hash_iter_t> search_it;
                for (search_it = hash_table.hash_table_iter(ps.Deref.searches); search_it.IsNonNull;
                     search_it = hash_table.hash_table_iter_next(search_it))
                {
                    ps_search_free((ps_search_t)hash_table.hash_entry_val(search_it.Deref.ent));
                }

                hash_table.hash_table_free(ps.Deref.searches);
            }

            ps.Deref.searches = PointerHelpers.NULL<hash_table_t>();
            ps.Deref.search = null;
        }

        public static ps_search_t ps_find_search(Pointer<ps_decoder_t> ps, Pointer<byte> name)
        {
            BoxedValue<object> search = new BoxedValue<object>();
            hash_table.hash_table_lookup(ps.Deref.searches, name, search);

            return (ps_search_t)search.Val;
        }

        public static int ps_reinit(Pointer<ps_decoder_t> ps, Pointer<cmd_ln_t> config)
        {
            Pointer<byte> path;
            Pointer<byte> keyphrase;
            int lw;

            if (config.IsNonNull && config != ps.Deref.config)
            {
                cmd_ln.cmd_ln_free_r(ps.Deref.config);
                ps.Deref.config = cmd_ln.cmd_ln_retain(config);
            }

            /* Set up logging. We need to do this earlier because we want to dump
             * the information to the configured log, not to the stderr. */

            ps.Deref.mfclogdir = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("-mfclogdir"));
            ps.Deref.rawlogdir = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("-rawlogdir"));
            ps.Deref.senlogdir = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("-senlogdir"));

            /* Fill in some default arguments. */
            ps_expand_model_config(ps);

            /* Free old searches (do this before other reinit) */
            ps_free_searches(ps);
            ps.Deref.searches = hash_table.hash_table_new(3, hash_table.HASH_CASE_YES);

            /* Free old acmod. */
            acmod.acmod_free(ps.Deref.acmod);
            ps.Deref.acmod = PointerHelpers.NULL<acmod_t>();

            /* Free old dictionary (must be done after the two things above) */
            dict.dict_free(ps.Deref.dict);
            ps.Deref.dict = PointerHelpers.NULL<dict_t>();

            /* Free d2p */
            dict2pid.dict2pid_free(ps.Deref.d2p);
            ps.Deref.d2p = PointerHelpers.NULL<dict2pid_t>();

            /* Logmath computation (used in acmod and search) */
            if (ps.Deref.lmath.IsNull
                || (logmath.logmath_get_base(ps.Deref.lmath) !=
                    (double)cmd_ln.cmd_ln_float_r(ps.Deref.config, cstring.ToCString("-logbase"))))
            {
                if (ps.Deref.lmath.IsNonNull)
                    logmath.logmath_free(ps.Deref.lmath);
                ps.Deref.lmath = logmath.logmath_init
                    ((double)cmd_ln.cmd_ln_float_r(ps.Deref.config, cstring.ToCString("-logbase")), 0,
                     cmd_ln.cmd_ln_boolean_r(ps.Deref.config, cstring.ToCString("-bestpath")));
            }

            /* Acoustic model (this is basically everything that
             * uttproc.c, senscr.c, and others used to do) */
            if ((ps.Deref.acmod = acmod.acmod_init(ps.Deref.config, ps.Deref.lmath, PointerHelpers.NULL<fe_t>(), PointerHelpers.NULL<feat_t>())).IsNull)
                return -1;



            if (cmd_ln.cmd_ln_int_r(ps.Deref.config, cstring.ToCString("-pl_window")) > 0)
            {
                /* Initialize an auxiliary phone loop search, which will run in
                 * "parallel" with FSG or N-Gram search. */
                if ((ps.Deref.phone_loop = phone_loop_search.phone_loop_search_init(ps.Deref.config, ps.Deref.acmod, ps.Deref.dict)) == null)
                    return -1;
                hash_table.hash_table_enter(ps.Deref.searches,
                                 ps_search_name(ps.Deref.phone_loop),
                                 ps.Deref.phone_loop);
            }

            /* Dictionary and triphone mappings (depends on acmod). */
            /* FIXME: pass config, change arguments, implement LTS, etc. */
            if ((ps.Deref.dict = dict.dict_init(ps.Deref.config, ps.Deref.acmod.Deref.mdef)).IsNull)
                return -1;
            if ((ps.Deref.d2p = dict2pid.dict2pid_build(ps.Deref.acmod.Deref.mdef, ps.Deref.dict)).IsNull)
                return -1;

            // LOGAN cast from double to int here?
            lw = (int)cmd_ln.cmd_ln_float_r(ps.Deref.config, cstring.ToCString("-lw"));

            /* Determine whether we are starting out in FSG or N-Gram search mode.
             * If neither is used skip search initialization. */

            /* Load KWS if one was specified in config */
            if ((keyphrase = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("-keyphrase"))).IsNonNull)
            {
                if (ps_set_keyphrase(ps, PS_DEFAULT_SEARCH, keyphrase) != 0)
                    return -1;
                ps_set_search(ps, PS_DEFAULT_SEARCH);
            }

            if ((path = cmd_ln.cmd_ln_str_r(ps.Deref.config, cstring.ToCString("-kws"))).IsNonNull)
            {
                if (ps_set_kws(ps, PS_DEFAULT_SEARCH, path) != 0)
                    return -1;
                ps_set_search(ps, PS_DEFAULT_SEARCH);
            }

            // LOGAN cut this out
            /* Load an FSG if one was specified in config */
            /*if ((path = cmd_ln.cmd_ln_str_r(ps.Deref.config, "-fsg"))) {
                fsg_model_t *fsg = fsg_model_readfile(path, ps.Deref.lmath, lw);
                if (!fsg)
                    return -1;
                if (ps_set_fsg(ps, PS_DEFAULT_SEARCH, fsg)) {
                    fsg_model_free(fsg);
                    return -1;
                }
                fsg_model_free(fsg);
                ps_set_search(ps, PS_DEFAULT_SEARCH);
            }*/

            /* Or load a JSGF grammar */
            /*if ((path = cmd_ln.cmd_ln_str_r(ps.Deref.config, "-jsgf"))) {
                if (ps_set_jsgf_file(ps, PS_DEFAULT_SEARCH, path)
                    || ps_set_search(ps, PS_DEFAULT_SEARCH))
                    return -1;
            }

            if ((path = cmd_ln.cmd_ln_str_r(ps.Deref.config, "-allphone"))) {
                if (ps_set_allphone_file(ps, PS_DEFAULT_SEARCH, path)
                        || ps_set_search(ps, PS_DEFAULT_SEARCH))
                        return -1;
            }

            if ((path = cmd_ln.cmd_ln_str_r(ps.Deref.config, "-lm")) && 
                !cmd_ln.cmd_ln_boolean_r(ps.Deref.config, "-allphone")) {
                if (ps_set_lm_file(ps, PS_DEFAULT_SEARCH, path)
                    || ps_set_search(ps, PS_DEFAULT_SEARCH))
                    return -1;
            }

            if ((path = cmd_ln.cmd_ln_str_r(ps.Deref.config, "-lmctl"))) {
                const char *name;
                ngram_model_t *lmset;
                ngram_model_set_iter_t *lmset_it;

                if (!(lmset = ngram_model_set_read(ps.Deref.config, path, ps.Deref.lmath))) {
                    E_ERROR("Failed to read language model control file: %s\n", path);
                    return -1;
                }

                for(lmset_it = ngram_model_set_iter(lmset);
                    lmset_it; lmset_it = ngram_model_set_iter_next(lmset_it)) {    
                    ngram_model_t *lm = ngram_model_set_iter_model(lmset_it, &name);            
                    E_INFO("adding search %s\n", name);
                    if (ps_set_lm(ps, name, lm)) {
                        ngram_model_set_iter_free(lmset_it);
                    ngram_model_free(lmset);
                        return -1;
                    }
                }
                ngram_model_free(lmset);

                name = cmd_ln.cmd_ln_str_r(ps.Deref.config, "-lmname");
                if (name)
                    ps_set_search(ps, name);
                else {
                    E_ERROR("No default LM name (-lmname) for `-lmctl'\n");
                    return -1;
                }
            }*/

            /* Initialize performance timer. */
            ps.Deref.perf.name = "decode";
            profile.ptmr_init(ps.Deref.perf);

            return 0;
        }

        public static Pointer<ps_decoder_t> ps_init(Pointer<cmd_ln_t> config)
        {
            Pointer<ps_decoder_t> ps;

            if (config.IsNull)
            {
                err.E_ERROR("No configuration specified");
                return PointerHelpers.NULL<ps_decoder_t>();
            }

            ps = ckd_alloc.ckd_calloc_struct<ps_decoder_t>(1);
            ps.Deref.refcount = 1;
            if (ps_reinit(ps, config) < 0)
            {
                ps_free(ps);
                return PointerHelpers.NULL<ps_decoder_t>();
            }
            return ps;
        }

        public static int ps_free(Pointer<ps_decoder_t> ps)
        {
            if (ps.IsNull)
                return 0;
            if (--ps.Deref.refcount > 0)
                return ps.Deref.refcount;
            ps_free_searches(ps);
            dict.dict_free(ps.Deref.dict);
            dict2pid.dict2pid_free(ps.Deref.d2p);
            acmod.acmod_free(ps.Deref.acmod);
            logmath.logmath_free(ps.Deref.lmath);
            cmd_ln.cmd_ln_free_r(ps.Deref.config);
            ckd_alloc.ckd_free(ps);
            return 0;
        }

        public static Pointer<logmath_t> ps_get_logmath(Pointer<ps_decoder_t> ps)
        {
            return ps.Deref.lmath;
        }

        public static Pointer<ps_mllr_t> ps_update_mllr(Pointer<ps_decoder_t> ps, Pointer<ps_mllr_t> mllr)
        {
            return acmod.acmod_update_mllr(ps.Deref.acmod, mllr);
        }

        public static int ps_set_search(Pointer<ps_decoder_t> ps, Pointer<byte> name)
        {
            ps_search_t search;

            if (ps.Deref.acmod.Deref.state != acmod_state_e.ACMOD_ENDED && ps.Deref.acmod.Deref.state != acmod_state_e.ACMOD_IDLE)
            {
                err.E_ERROR("Cannot change search while decoding, end utterance first\n");
                return -1;
            }

            if ((search = ps_find_search(ps, name)) == null)
            {
                return -1;
            }

            ps.Deref.search = search;
            /* Set pl window depending on the search */
            if (cstring.strcmp(PS_SEARCH_TYPE_NGRAM, ps_search_type(search)) == 0)
            {
                ps.Deref.pl_window = (int)cmd_ln.cmd_ln_int_r(ps.Deref.config, cstring.ToCString("-pl_window"));
            }
            else
            {
                ps.Deref.pl_window = 0;
            }

            return 0;
        }

        public static int set_search_internal(Pointer<ps_decoder_t> ps, ps_search_t search)
        {
            ps_search_t old_search;

            if (search == null)
                return -1;

            search.pls = ps.Deref.phone_loop;
            old_search = (ps_search_t)hash_table.hash_table_replace(ps.Deref.searches, ps_search_name(search), search);
            if (old_search != search)
                ps_search_free(old_search);

            return 0;
        }

        public static int ps_set_kws(Pointer<ps_decoder_t> ps, Pointer<byte> name, Pointer<byte> keyfile)
        {
            ps_search_t search;
            search = kws_search.kws_search_init(name, PointerHelpers.NULL<byte>(), keyfile, ps.Deref.config, ps.Deref.acmod, ps.Deref.dict, ps.Deref.d2p);
            return set_search_internal(ps, search);
        }

        public static int ps_set_keyphrase(Pointer<ps_decoder_t> ps, Pointer<byte> name, Pointer<byte> keyphrase)
        {
            ps_search_t search;
            search = kws_search.kws_search_init(name, keyphrase, PointerHelpers.NULL<byte>(), ps.Deref.config, ps.Deref.acmod, ps.Deref.dict, ps.Deref.d2p);
            return set_search_internal(ps, search);
        }

        public static int ps_start_utt(Pointer<ps_decoder_t> ps)
        {
            int rv;
            Pointer<byte> uttid = PointerHelpers.Malloc<byte>(16);

            if (ps.Deref.acmod.Deref.state == acmod_state_e.ACMOD_STARTED || ps.Deref.acmod.Deref.state == acmod_state_e.ACMOD_PROCESSING)
            {
                err.E_ERROR("Utterance already started\n");
                return -1;
            }

            if (ps.Deref.search == null)
            {
                err.E_ERROR("No search module is selected, did you forget to specify a language model or grammar?\n");
                return -1;
            }

            profile.ptmr_reset(ps.Deref.perf);
            profile.ptmr_start(ps.Deref.perf);

            stdio.sprintf(uttid, string.Format("{0}", ps.Deref.uttno));
            ++ps.Deref.uttno;

            /* Remove any residual word lattice and hypothesis. */
            ps_lattice.ps_lattice_free(ps.Deref.search.dag);
            ps.Deref.search.dag = PointerHelpers.NULL<ps_lattice_t>();
            ps.Deref.search.last_link = PointerHelpers.NULL<ps_latlink_t>();
            ps.Deref.search.post = 0;
            ckd_alloc.ckd_free(ps.Deref.search.hyp_str);
            ps.Deref.search.hyp_str = PointerHelpers.NULL<byte>();
            if ((rv = acmod.acmod_start_utt(ps.Deref.acmod)) < 0)
                return rv;

            /* Start logging features and audio if requested. */
            // LOGAN cut out logging
            /*if (ps.Deref.mfclogdir) {
                char *logfn = string_join(ps.Deref.mfclogdir, "/",
                                          uttid, ".mfc", NULL);
                FILE *mfcfh;
                E_INFO("Writing MFCC file: %s\n", logfn);
                if ((mfcfh = fopen(logfn, "wb")) == NULL) {
                    E_ERROR_SYSTEM("Failed to open MFCC file %s", logfn);
                    ckd_alloc.ckd_free(logfn);
                    return -1;
                }
                ckd_alloc.ckd_free(logfn);
                acmod_set_mfcfh(ps.Deref.acmod, mfcfh);
            }
            if (ps.Deref.rawlogdir) {
                char *logfn = string_join(ps.Deref.rawlogdir, "/",
                                          uttid, ".raw", NULL);
                FILE *rawfh;
                E_INFO("Writing raw audio file: %s\n", logfn);
                if ((rawfh = fopen(logfn, "wb")) == NULL) {
                    E_ERROR_SYSTEM("Failed to open raw audio file %s", logfn);
                    ckd_alloc.ckd_free(logfn);
                    return -1;
                }
                ckd_alloc.ckd_free(logfn);
                acmod_set_rawfh(ps.Deref.acmod, rawfh);
            }
            if (ps.Deref.senlogdir) {
                char *logfn = string_join(ps.Deref.senlogdir, "/",
                                          uttid, ".sen", NULL);
                FILE *senfh;
                E_INFO("Writing senone score file: %s\n", logfn);
                if ((senfh = fopen(logfn, "wb")) == NULL) {
                    E_ERROR_SYSTEM("Failed to open senone score file %s", logfn);
                    ckd_alloc.ckd_free(logfn);
                    return -1;
                }
                ckd_alloc.ckd_free(logfn);
                acmod_set_senfh(ps.Deref.acmod, senfh);
            }*/

            /* Start auxiliary phone loop search. */
            if (ps.Deref.phone_loop != null)
                ps_search_start(ps.Deref.phone_loop);

            return ps_search_start(ps.Deref.search);
        }

        public static int ps_search_forward(Pointer<ps_decoder_t> ps)
        {
            int nfr;

            nfr = 0;
            while (ps.Deref.acmod.Deref.n_feat_frame > 0)
            {
                int k;
                if (ps.Deref.pl_window > 0)
                    if ((k = ps_search_step(ps.Deref.phone_loop, ps.Deref.acmod.Deref.output_frame)) < 0)
                        return k;
                if (ps.Deref.acmod.Deref.output_frame >= ps.Deref.pl_window)
                    if ((k = ps_search_step(ps.Deref.search,
                                            ps.Deref.acmod.Deref.output_frame - ps.Deref.pl_window)) < 0)
                        return k;
                acmod.acmod_advance(ps.Deref.acmod);
                ++ps.Deref.n_frame;
                ++nfr;
            }
            return nfr;
        }

        public static int ps_process_raw(Pointer<ps_decoder_t> ps,
                       Pointer<short> data,
                       uint n_samples,
                       int no_search,
                       int full_utt)
        {
            int n_searchfr = 0;

            if (ps.Deref.acmod.Deref.state == acmod_state_e.ACMOD_IDLE)
            {
                err.E_ERROR("Failed to process data, utterance is not started. Use start_utt to start it\n");
                return 0;
            }

            if (no_search != 0)
                acmod.acmod_set_grow(ps.Deref.acmod, 1);

            while (n_samples != 0)
            {
                int nfr;

                /* Process some data into features. */
                BoxedValue<Pointer<short>> boxed_data = new BoxedValue<Pointer<short>>(data);
                BoxedValue<uint> boxed_n_samples = new BoxedValue<uint>(n_samples);
                if ((nfr = acmod.acmod_process_raw(ps.Deref.acmod, boxed_data,
                                             boxed_n_samples, full_utt)) < 0)
                {
                    data = boxed_data.Val;
                    n_samples = boxed_n_samples.Val;
                    return nfr;
                }

                data = boxed_data.Val;
                n_samples = boxed_n_samples.Val;

                /* Score and search as much data as possible */
                if (no_search != 0)
                    continue;
                if ((nfr = ps_search_forward(ps)) < 0)
                    return nfr;
                n_searchfr += nfr;
            }

            return n_searchfr;
        }

        public static int ps_end_utt(Pointer<ps_decoder_t> ps)
        {
            int rv, i;

            if (ps.Deref.acmod.Deref.state == acmod_state_e.ACMOD_ENDED || ps.Deref.acmod.Deref.state == acmod_state_e.ACMOD_IDLE)
            {
                err.E_ERROR("Utterance is not started\n");
                return -1;
            }
            acmod.acmod_end_utt(ps.Deref.acmod);

            /* Search any remaining frames. */
            if ((rv = ps_search_forward(ps)) < 0)
            {
                profile.ptmr_stop(ps.Deref.perf);
                return rv;
            }
            /* Finish phone loop search. */
            if (ps.Deref.phone_loop != null)
            {
                if ((rv = ps_search_finish(ps.Deref.phone_loop)) < 0)
                {
                    profile.ptmr_stop(ps.Deref.perf);
                    return rv;
                }
            }
            /* Search any frames remaining in the lookahead window. */
            if (ps.Deref.acmod.Deref.output_frame >= ps.Deref.pl_window)
            {
                for (i = ps.Deref.acmod.Deref.output_frame - ps.Deref.pl_window;
                     i < ps.Deref.acmod.Deref.output_frame; ++i)
                    ps_search_step(ps.Deref.search, i);
            }
            /* Finish main search. */
            if ((rv = ps_search_finish(ps.Deref.search)) < 0)
            {
                profile.ptmr_stop(ps.Deref.perf);
                return rv;
            }

            profile.ptmr_stop(ps.Deref.perf);

            /* Log a backtrace if requested. */
            if (cmd_ln.cmd_ln_boolean_r(ps.Deref.config, cstring.ToCString("-backtrace")) != 0)
            {
                Pointer<byte> hyp;
                ps_seg_t seg;
                BoxedValueInt score = new BoxedValueInt();

                hyp = ps_get_hyp(ps, score);

                if (hyp.IsNonNull)
                {
                    err.E_INFO(string.Format("{0} ({1})\n", cstring.FromCString(hyp), score.Val));
                    err.E_INFO_NOFN(string.Format("{0,-20} {1,-5} {2,-5} {3,-5} {4,-10} {5,-10} {6,-3}\n",
                            "word", "start", "end", "pprob", "ascr", "lscr", "lback"));
                    for (seg = ps_seg_iter(ps); seg != null; seg = ps_seg_next(seg))
                    {
                        Pointer <byte> word;
                        BoxedValueInt sf = new BoxedValueInt();
                        BoxedValueInt ef = new BoxedValueInt();
                        int post;
                        BoxedValueInt lscr = new BoxedValueInt();
                        BoxedValueInt ascr = new BoxedValueInt();
                        BoxedValueInt lback = new BoxedValueInt();

                        word = ps_seg_word(seg);
                        ps_seg_frames(seg, sf, ef);
                        post = ps_seg_prob(seg, ascr, lscr, lback);
                        err.E_INFO_NOFN(string.Format("{0,-20} {1,-5} {2,-5} {3,-5:F3} {4,-10} {5,-10} {6,-3}\n",
                                        cstring.FromCString(word), sf.Val, ef.Val, logmath.logmath_exp(ps_get_logmath(ps), post), ascr.Val, lscr.Val, lback.Val));
                    }
                }
            }
            return rv;
        }

        public static Pointer<byte> ps_get_hyp(Pointer<ps_decoder_t> ps, BoxedValueInt out_best_score)
        {
            Pointer<byte> hyp;
            profile.ptmr_start(ps.Deref.perf);
            hyp = ps_search_hyp(ps.Deref.search, out_best_score);
            profile.ptmr_stop(ps.Deref.perf);
            return hyp;
        }

        public static ps_seg_t ps_seg_iter(Pointer<ps_decoder_t> ps)
        {
            ps_seg_t itor;
            profile.ptmr_start(ps.Deref.perf);
            itor = ps_search_seg_iter(ps.Deref.search);
            profile.ptmr_stop(ps.Deref.perf);
            return itor;
        }

        public static ps_seg_t ps_seg_next(ps_seg_t seg)
        {
            return ps_search_seg_next(seg);
        }

        public static Pointer<byte> ps_seg_word(ps_seg_t seg)
        {
            return seg.word;
        }

        public static void ps_seg_frames(ps_seg_t seg, BoxedValueInt out_sf, BoxedValueInt out_ef)
        {
            int uf;
            uf = acmod.acmod_stream_offset(seg.search.acmod);
            if (out_sf != null) out_sf.Val = seg.sf + uf;
            if (out_ef != null) out_ef.Val = seg.ef + uf;
        }

        public static int ps_seg_prob(ps_seg_t seg, BoxedValueInt out_ascr, BoxedValueInt out_lscr, BoxedValueInt out_lback)
        {
            if (out_ascr != null) out_ascr.Val = seg.ascr;
            if (out_lscr != null) out_lscr.Val = seg.lscr;
            if (out_lback != null) out_lback.Val = seg.lback;
            return seg.prob;
        }

        public static byte ps_get_in_speech(Pointer<ps_decoder_t> ps)
        {
            return fe_interface.fe_get_vad_state(ps.Deref.acmod.Deref.fe);
        }

        public static void ps_search_init(
            ps_search_t search,
            ps_searchfuncs_t vt,
            Pointer<byte> type,
            Pointer<byte> name,
            Pointer<cmd_ln_t> config,
            Pointer<acmod_t> acousticmod,
            Pointer<dict_t> dictionary,
            Pointer<dict2pid_t> d2p)
        {
            search.vt = vt;
            search.name = ckd_alloc.ckd_salloc(name);
            search.type = ckd_alloc.ckd_salloc(type);

            search.config = config;
            search.acmod = acousticmod;
            if (d2p.IsNonNull)
                search.d2p = dict2pid.dict2pid_retain(d2p);
            else
                search.d2p = PointerHelpers.NULL<dict2pid_t>();
            if (dictionary.IsNonNull)
            {
                search.dict = dict.dict_retain(dictionary);
                search.start_wid = dict.dict_startwid(dictionary);
                search.finish_wid = dict.dict_finishwid(dictionary);
                search.silence_wid = dict.dict_silwid(dictionary);
                search.n_words = dict.dict_size(dictionary);
            }
            else
            {
                search.dict = PointerHelpers.NULL<dict_t>();
                search.start_wid = search.finish_wid = search.silence_wid = -1;
                search.n_words = 0;
            }
        }

        public static void ps_search_base_free(ps_search_t search)
        {
            /* FIXME: We will have refcounting on acmod, config, etc, at which
             * point we will free them here too. */
            ckd_alloc.ckd_free(search.name);
            ckd_alloc.ckd_free(search.type);
            dict.dict_free(search.dict);
            dict2pid.dict2pid_free(search.d2p);
            ckd_alloc.ckd_free(search.hyp_str);
            ps_lattice.ps_lattice_free(search.dag);
        }

        public static void ps_search_base_reinit(ps_search_t search, Pointer<dict_t> dictionary,
                              Pointer<dict2pid_t> d2p)
        {
            dict.dict_free(search.dict);
            dict2pid.dict2pid_free(search.d2p);
            /* FIXME: _retain() should just return NULL if passed NULL. */
            if (dictionary.IsNonNull)
            {
                search.dict = dict.dict_retain(dictionary);
                search.start_wid = dict.dict_startwid(dictionary);
                search.finish_wid = dict.dict_finishwid(dictionary);
                search.silence_wid = dict.dict_silwid(dictionary);
                search.n_words = dict.dict_size(dictionary);
            }
            else
            {
                search.dict = PointerHelpers.NULL<dict_t>();
                search.start_wid = search.finish_wid = search.silence_wid = -1;
                search.n_words = 0;
            }
            if (d2p.IsNonNull)
                search.d2p = dict2pid.dict2pid_retain(d2p);
            else
                search.d2p = PointerHelpers.NULL<dict2pid_t>();
        }

        public static Pointer<cmd_ln_t> ps_search_config(ps_search_t s)
        {
            return s.config;
        }

        public static Pointer<acmod_t> ps_search_acmod(ps_search_t s)
        {
            return s.acmod;
        }

        public static Pointer<dict_t> ps_search_dict(ps_search_t s)
        {
            return s.dict;
        }

        public static Pointer<dict2pid_t> ps_search_dict2pid(ps_search_t s)
        {
            return s.d2p;
        }

        public static Pointer<ps_lattice_t> ps_search_dag(ps_search_t s)
        {
            return s.dag;
        }

        public static Pointer<ps_latlink_t> ps_search_last_link(ps_search_t s)
        {
            return s.last_link;
        }

        public static int ps_search_post(ps_search_t s)
        {
            return s.post;
        }

        public static ps_search_t ps_search_lookahead(ps_search_t s)
        {
            return s.pls;
        }

        public static int ps_search_n_words(ps_search_t s)
        {
            return s.n_words;
        }

        public static Pointer<byte> ps_search_type(ps_search_t s)
        {
            return s.type;
        }

        public static Pointer<byte> ps_search_name(ps_search_t s)
        {
            return s.name;
        }

        public static int ps_search_start(ps_search_t s)
        {
            return s.vt.start(s);
        }

        public static int ps_search_step(ps_search_t s, int i)
        {
            return s.vt.step(s, i);
        }

        public static int ps_search_finish(ps_search_t s)
        {
            return s.vt.finish(s);
        }

        public static int ps_search_reinit(ps_search_t s, Pointer<dict_t> dict, Pointer<dict2pid_t> d2p)
        {
            return s.vt.reinit(s, dict, d2p);
        }

        public static void ps_search_free(ps_search_t s)
        {
            s.vt.free(s);
        }

        public static Pointer<ps_lattice_t> ps_search_lattice(ps_search_t s)
        {
            return s.vt.lattice(s);
        }

        public static Pointer<byte> ps_search_hyp(ps_search_t s, BoxedValueInt out_score)
        {
            return s.vt.hyp(s, out_score);
        }

        public static int ps_search_prob(ps_search_t s)
        {
            return s.vt.prob(s);
        }

        public static ps_seg_t ps_search_seg_iter(ps_search_t s)
        {
            return s.vt.seg_iter(s);
        }

        public static ps_seg_t ps_search_seg_next(ps_seg_t seg)
        {
            return seg.vt.seg_next(seg);
        }

        public static void ps_search_seg_free(ps_seg_t seg)
        {
            seg.vt.seg_free(seg);
        }

        /* For convenience... */
        //#define ps_search_silence_wid(s) ps_search_base(s)->silence_wid
        //#define ps_search_start_wid(s) ps_search_base(s)->start_wid
        //#define ps_search_finish_wid(s) ps_search_base(s)->finish_wid
    }
}
