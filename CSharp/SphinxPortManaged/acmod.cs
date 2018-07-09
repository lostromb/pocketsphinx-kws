using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class acmod
    {
        public const short SENSCR_DUMMY = 0x7fff;

        public static int acmod_init_am(Pointer<acmod_t> acmod)
        {
           Pointer<byte> mdeffn, tmatfn, mllrfn, hmmdir;

            /* Read model definition. */
            if ((mdeffn = cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_mdef"))).IsNull)
            {
                if ((hmmdir = cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-hmm"))).IsNull)
                    err.E_ERROR("Acoustic model definition is not specified either with -mdef option or with -hmm\n");
                else
                    err.E_ERROR(string.Format("Folder '{0}' does not contain acoustic model definition 'mdef'\n", cstring.FromCString(hmmdir)));

                return -1;
            }

            if ((acmod.Deref.mdef = bin_mdef.bin_mdef_read(acmod.Deref.config, mdeffn)).IsNull)
            {
                err.E_ERROR(string.Format("Failed to read acoustic model definition from {0}\n", cstring.FromCString(mdeffn)));
                return -1;
            }

            /* Read transition matrices. */
            if ((tmatfn = cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_tmat"))).IsNull)
            {
                err.E_ERROR("No tmat file specified\n");
                return -1;
            }
            acmod.Deref.tmat = tmat.tmat_init(tmatfn, acmod.Deref.lmath,
                                    cmd_ln.cmd_ln_float_r(acmod.Deref.config, cstring.ToCString("-tmatfloor")),
                                    1);

            /* Read the acoustic models. */
            if ((cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_mean")).IsNull)
                || (cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_var")).IsNull)
                || (cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_tmat")).IsNull))
            {
                err.E_ERROR("No mean/var/tmat files specified\n");
                return -1;
            }

            if (cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_senmgau")).IsNonNull)
            {
                err.E_INFO("Using general multi-stream GMM computation\n");
                acmod.Deref.mgau = ms_mgau.ms_mgau_init(acmod, acmod.Deref.lmath, acmod.Deref.mdef);
                if (acmod.Deref.mgau == null)
                    return -1;
            }
            else
            {
                err.E_INFO("Attempting to use PTM computation module\n");
                if ((acmod.Deref.mgau = ptm_mgau.ptm_mgau_init(acmod, acmod.Deref.mdef)) == null)
                {
                    err.E_INFO("Attempting to use semi-continuous computation module\n");
                    if ((acmod.Deref.mgau = s2_semi_mgau.s2_semi_mgau_init(acmod)) == null)
                    {
                        err.E_INFO("Falling back to general multi-stream GMM computation\n");
                        acmod.Deref.mgau = ms_mgau.ms_mgau_init(acmod, acmod.Deref.lmath, acmod.Deref.mdef);
                        if (acmod.Deref.mgau == null)
                        {
                            err.E_ERROR("Failed to read acoustic model\n");
                            return -1;
                        }
                    }
                }
            }

            /* If there is an MLLR transform, apply it. */
            if ((mllrfn = cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-mllr"))).IsNonNull)
            {
                Pointer<ps_mllr_t> mllr = ps_mllr.ps_mllr_read(mllrfn);
                if (mllr.IsNull)
                    return -1;
                acmod_update_mllr(acmod, mllr);
            }

            return 0;
        }

        public static int acmod_init_feat(Pointer<acmod_t> acmod)
        {
            acmod.Deref.fcb =
                feat.feat_init(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-feat")),
                          cmn.cmn_type_from_str(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-cmn"))),
                          cmd_ln.cmd_ln_boolean_r(acmod.Deref.config, cstring.ToCString("-varnorm")),
                          agc.agc_type_from_str(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-agc"))),
                          1,
                          (int)cmd_ln.cmd_ln_int_r(acmod.Deref.config, cstring.ToCString("-ceplen")));
            if (acmod.Deref.fcb.IsNull)
                return -1;

            if (cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_lda")).IsNonNull)
            {
                err.E_INFO(string.Format("Reading linear feature transformation from {0}\n",
                       cstring.FromCString(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_lda")))));
                if (lda.feat_read_lda(acmod.Deref.fcb,
                                  cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("_lda")),
                                  (int)cmd_ln.cmd_ln_int_r(acmod.Deref.config, cstring.ToCString("-ldadim"))) < 0)
                    return -1;
            }

            if (cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-svspec")).IsNonNull)
            {
                Pointer<Pointer<int>> subvecs;
                err.E_INFO(string.Format("Using subvector specification {0}\n", cstring.FromCString(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-svspec")))));
                if ((subvecs = feat.parse_subvecs(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-svspec")))).IsNull)
                    return -1;
                if ((feat.feat_set_subvecs(acmod.Deref.fcb, subvecs)) < 0)
                    return -1;
            }

            if (cmd_ln.cmd_ln_exists_r(acmod.Deref.config, cstring.ToCString("-agcthresh")) != 0
                && 0 != cstring.strcmp(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-agc")), cstring.ToCString("none")))
            {
                agc.agc_set_threshold(acmod.Deref.fcb.Deref.agc_struct, (float)cmd_ln.cmd_ln_float_r(acmod.Deref.config, cstring.ToCString("-agcthresh")));
            }

            if (acmod.Deref.fcb.Deref.cmn_struct.IsNonNull
                && cmd_ln.cmd_ln_exists_r(acmod.Deref.config, cstring.ToCString("-cmninit")) != 0)
            {
                // LOGAN modified - this old method was way too obtuse
                //string valList = cstring.FromCString(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-cmninit")));
                //string[] parts = valList.Split(',');
                //for (int c = 0; c < parts.Length; c++)
                //{
                //    acmod.Deref.fcb.Deref.cmn_struct.Deref.cmn_mean[c] = float.Parse(parts[c]);
                //}

                Pointer<byte> c, cc, vallist;
                int nvals;

                vallist = ckd_alloc.ckd_salloc(cmd_ln.cmd_ln_str_r(acmod.Deref.config, cstring.ToCString("-cmninit")));
                c = vallist;
                nvals = 0;
                while (nvals < acmod.Deref.fcb.Deref.cmn_struct.Deref.veclen
                       && (cc = cstring.strchr(c, (byte)',')).IsNonNull)
                {
                    cc[0] = (byte)'\0';
                    acmod.Deref.fcb.Deref.cmn_struct.Deref.cmn_mean[nvals] = (float)(strfuncs.atof_c(c));
                    c = cc + 1;
                    ++nvals;
                }
                if (nvals < acmod.Deref.fcb.Deref.cmn_struct.Deref.veclen && c[0] != '\0')
                {
                    acmod.Deref.fcb.Deref.cmn_struct.Deref.cmn_mean[nvals] = (float)(strfuncs.atof_c(c));
                }
                ckd_alloc.ckd_free(vallist);
            }
            return 0;
        }

        public static int acmod_fe_mismatch(Pointer<acmod_t> _acmod, Pointer<fe_t> _fe)
        {
            /* Output vector dimension needs to be the same. */
            if (cmd_ln.cmd_ln_int_r(_acmod.Deref.config, cstring.ToCString("-ceplen")) != fe_interface.fe_get_output_size(_fe))
            {
                err.E_ERROR(string.Format("Configured feature length {0} doesn't match feature extraction output size {1}\n",
                        cmd_ln.cmd_ln_int_r(_acmod.Deref.config, cstring.ToCString("-ceplen")),
                        fe_interface.fe_get_output_size(_fe)));
                return 1;
            }
            /* Feature parameters need to be the same. */
            /* ... */
            return 0;
        }

        public static int acmod_feat_mismatch(Pointer<acmod_t> _acmod, Pointer<feat_t> fcb)
        {
            /* Feature type needs to be the same. */
            if (0 != cstring.strcmp(cmd_ln.cmd_ln_str_r(_acmod.Deref.config, cstring.ToCString("-feat")), feat.feat_name(fcb)))
                return 1;
            /* Input vector dimension needs to be the same. */
            if (cmd_ln.cmd_ln_int_r(_acmod.Deref.config, cstring.ToCString("-ceplen")) != feat.feat_cepsize(fcb))
                return 1;
            /* FIXME: Need to check LDA and stuff too. */
            return 0;
        }

        public static Pointer<acmod_t> acmod_init(Pointer<cmd_ln_t> config, Pointer<logmath_t> lmath, Pointer<fe_t> fe, Pointer<feat_t> fcb)
        {
            Pointer<acmod_t> acmod;

            acmod = ckd_alloc.ckd_calloc_struct<acmod_t>(1);
            acmod.Deref.config = cmd_ln.cmd_ln_retain(config);
            acmod.Deref.lmath = lmath;
            acmod.Deref.state = acmod_state_e.ACMOD_IDLE;

            /* Initialize feature computation. */
            if (fe.IsNonNull)
            {
                if (acmod_fe_mismatch(acmod, fe) != 0)
                    goto error_out;
                fe_interface.fe_retain(fe);
                acmod.Deref.fe = fe;
            }
            else
            {
                /* Initialize a new front end. */
                acmod.Deref.fe = fe_interface.fe_init_auto_r(config);
                if (acmod.Deref.fe.IsNull)
                    goto error_out;
                if (acmod_fe_mismatch(acmod, acmod.Deref.fe) != 0)
                    goto error_out;
            }
            if (fcb.IsNonNull)
            {
                if (acmod_feat_mismatch(acmod, fcb) != 0)
                    goto error_out;
                feat.feat_retain(fcb);
                acmod.Deref.fcb = fcb;
            }
            else
            {
                /* Initialize a new fcb. */
                if (acmod_init_feat(acmod) < 0)
                    goto error_out;
            }

            /* Load acoustic model parameters. */
            if (acmod_init_am(acmod) < 0)
                goto error_out;


            /* The MFCC buffer needs to be at least as large as the dynamic
             * feature window.  */
            acmod.Deref.n_mfc_alloc = acmod.Deref.fcb.Deref.window_size * 2 + 1;
            acmod.Deref.mfc_buf = (Pointer<Pointer<float>>)
                ckd_alloc.ckd_calloc_2d<float>((uint)acmod.Deref.n_mfc_alloc, (uint)acmod.Deref.fcb.Deref.cepsize);

            /* Feature buffer has to be at least as large as MFCC buffer. */
            acmod.Deref.n_feat_alloc = checked((int)(acmod.Deref.n_mfc_alloc + cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-pl_window"))));
            acmod.Deref.feat_buf = feat.feat_array_alloc(acmod.Deref.fcb, acmod.Deref.n_feat_alloc);
            acmod.Deref.framepos = ckd_alloc.ckd_calloc<long>(acmod.Deref.n_feat_alloc);

            acmod.Deref.utt_start_frame = 0;

            /* Senone computation stuff. */
            acmod.Deref.senone_scores = ckd_alloc.ckd_calloc<short>(bin_mdef.bin_mdef_n_sen(acmod.Deref.mdef));
            acmod.Deref.senone_active_vec = bitvec.bitvec_alloc(bin_mdef.bin_mdef_n_sen(acmod.Deref.mdef));
            acmod.Deref.senone_active = ckd_alloc.ckd_calloc<byte>(bin_mdef.bin_mdef_n_sen(acmod.Deref.mdef));
            acmod.Deref.log_zero = logmath.logmath_get_zero(acmod.Deref.lmath);
            acmod.Deref.compallsen = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-compallsen"));
            return acmod;

            error_out:
            acmod_free(acmod);
            return PointerHelpers.NULL<acmod_t>();
        }

        public static void acmod_free(Pointer<acmod_t> _acmod)
        {
            if (_acmod.IsNull)
                return;

            feat.feat_free(_acmod.Deref.fcb);
            fe_interface.fe_free(_acmod.Deref.fe);
            cmd_ln.cmd_ln_free_r(_acmod.Deref.config);

            if (_acmod.Deref.mfc_buf.IsNonNull)
                ckd_alloc.ckd_free_2d(_acmod.Deref.mfc_buf);
            if (_acmod.Deref.feat_buf.IsNonNull)
                feat.feat_array_free(_acmod.Deref.feat_buf);

            if (_acmod.Deref.mfcfh != null)
                _acmod.Deref.mfcfh.fclose();
            if (_acmod.Deref.rawfh != null)
                _acmod.Deref.rawfh.fclose();
            if (_acmod.Deref.senfh != null)
                _acmod.Deref.senfh.fclose();

            ckd_alloc.ckd_free(_acmod.Deref.framepos);
            ckd_alloc.ckd_free(_acmod.Deref.senone_scores);
            ckd_alloc.ckd_free(_acmod.Deref.senone_active_vec);
            ckd_alloc.ckd_free(_acmod.Deref.senone_active);
            ckd_alloc.ckd_free(_acmod.Deref.rawdata);

            if (_acmod.Deref.mdef.IsNonNull)
                bin_mdef.bin_mdef_free(_acmod.Deref.mdef);
            if (_acmod.Deref.tmat.IsNonNull)
                tmat.tmat_free(_acmod.Deref.tmat);
            if (_acmod.Deref.mgau != null)
                _acmod.Deref.mgau.vt.free(_acmod.Deref.mgau);
            if (_acmod.Deref.mllr.IsNonNull)
                ps_mllr.ps_mllr_free(_acmod.Deref.mllr);

            ckd_alloc.ckd_free(_acmod);
        }

        public static Pointer<ps_mllr_t> acmod_update_mllr(Pointer<acmod_t> _acmod, Pointer<ps_mllr_t> _mllr)
        {
            if (_acmod.Deref.mllr.IsNonNull)
                ps_mllr.ps_mllr_free(_acmod.Deref.mllr);
            _acmod.Deref.mllr = _mllr;
            _acmod.Deref.mgau.vt.transform(_acmod.Deref.mgau, _mllr);

            return _mllr;
        }

        public static void acmod_grow_feat_buf(Pointer<acmod_t> _acmod, int nfr)
        {
            if (nfr > hmm.MAX_N_FRAMES)
                err.E_FATAL(string.Format("Decoder can not process more than {0} frames at once, requested {1}\n", hmm.MAX_N_FRAMES, nfr));

            _acmod.Deref.feat_buf = feat.feat_array_realloc(_acmod.Deref.fcb, _acmod.Deref.feat_buf,
                                                 _acmod.Deref.n_feat_alloc, nfr);
            _acmod.Deref.framepos = ckd_alloc.ckd_realloc<long>(_acmod.Deref.framepos, nfr);
            _acmod.Deref.n_feat_alloc = nfr;
        }

        public static int acmod_set_grow(Pointer<acmod_t> _acmod, int grow_feat)
        {
            int tmp = _acmod.Deref.grow_feat;
            _acmod.Deref.grow_feat = (byte)grow_feat;

            /* Expand feat_buf to a reasonable size to start with. */
            if (grow_feat != 0 && _acmod.Deref.n_feat_alloc < 128)
                acmod_grow_feat_buf(_acmod, 128);

            return tmp;
        }

        public static int acmod_start_utt(Pointer<acmod_t> acmod)
        {
            fe_interface.fe_start_utt(acmod.Deref.fe);
            acmod.Deref.state = acmod_state_e.ACMOD_STARTED;
            acmod.Deref.n_mfc_frame = 0;
            acmod.Deref.n_feat_frame = 0;
            acmod.Deref.mfc_outidx = 0;
            acmod.Deref.feat_outidx = 0;
            acmod.Deref.output_frame = 0;
            acmod.Deref.senscr_frame = -1;
            acmod.Deref.n_senone_active = 0;
            acmod.Deref.mgau.frame_idx = 0;
            acmod.Deref.rawdata_pos = 0;

            return 0;
        }

        public static int acmod_end_utt(Pointer<acmod_t> _acmod)
        {
            int nfr = 0;

            _acmod.Deref.state = acmod_state_e.ACMOD_ENDED;
            if (_acmod.Deref.n_mfc_frame < _acmod.Deref.n_mfc_alloc)
            {
                int inptr;
                /* Where to start writing them (circular buffer) */
                inptr = (_acmod.Deref.mfc_outidx + _acmod.Deref.n_mfc_frame) % _acmod.Deref.n_mfc_alloc;
                /* nfr is always either zero or one. */
                BoxedValueInt boxed_nfr = new BoxedValueInt(nfr);
                fe_interface.fe_end_utt(_acmod.Deref.fe, _acmod.Deref.mfc_buf[inptr], boxed_nfr);
                nfr = boxed_nfr.Val;
                _acmod.Deref.n_mfc_frame += nfr;

                /* Process whatever's left, and any leadout or update stats if needed. */
                if (nfr != 0)
                    nfr = acmod_process_mfcbuf(_acmod);
                else
                    feat.feat_update_stats(_acmod.Deref.fcb);
            }
            if (_acmod.Deref.mfcfh != null)
            {
                long outlen;
                int rv;
                outlen = (_acmod.Deref.mfcfh.ftell() - 4) / 4;
                /* Try to seek and write */
                if ((rv = _acmod.Deref.mfcfh.fseek(0, FILE.SEEK_SET)) == 0)
                {
                    _acmod.Deref.mfcfh.fwrite(outlen, 4, 1);
                }

                _acmod.Deref.mfcfh.fclose();
                _acmod.Deref.mfcfh = null;
            }
            if (_acmod.Deref.rawfh != null)
            {
                _acmod.Deref.rawfh.fclose();
                _acmod.Deref.rawfh = null;
            }

            if (_acmod.Deref.senfh != null)
            {
                _acmod.Deref.senfh.fclose();
                _acmod.Deref.senfh = null;
            }

            return nfr;
        }

        public static int acmod_log_mfc(Pointer<acmod_t> _acmod,
                      Pointer<Pointer<float>> cep, int n_frames)
        {
            int n = n_frames * feat.feat_cepsize(_acmod.Deref.fcb);
            /* Write features. */
            if (_acmod.Deref.mfcfh.fwrite(cep[0], 4, n) != n)
            {
                err.E_ERROR_SYSTEM(string.Format("Failed to write {0} values to file", n));
            }
            return 0;
        }

        public static int acmod_process_full_cep(Pointer<acmod_t> _acmod,
                               BoxedValue<Pointer<Pointer<float>>> inout_cep,
                               BoxedValueInt inout_n_frames)
        {
            int nfr;

            /* Write to file. */
            if (_acmod.Deref.mfcfh != null)
                acmod_log_mfc(_acmod, inout_cep.Val, inout_n_frames.Val);

            /* Resize feat_buf to fit. */
            if (_acmod.Deref.n_feat_alloc < inout_n_frames.Val)
            {

                if (inout_n_frames.Val > hmm.MAX_N_FRAMES)
                    err.E_FATAL(string.Format("Batch processing can not process more than {0} frames at once, requested {1}\n", hmm.MAX_N_FRAMES, inout_n_frames.Val));

                feat.feat_array_free(_acmod.Deref.feat_buf);
                _acmod.Deref.feat_buf = feat.feat_array_alloc(_acmod.Deref.fcb, inout_n_frames.Val);
                _acmod.Deref.n_feat_alloc = inout_n_frames.Val;
                _acmod.Deref.n_feat_frame = 0;
                _acmod.Deref.feat_outidx = 0;
            }
            /* Make dynamic features. */
            nfr = feat.feat_s2mfc2feat_live(_acmod.Deref.fcb, inout_cep.Val, inout_n_frames,
                                       1, 1, _acmod.Deref.feat_buf);
            _acmod.Deref.n_feat_frame = nfr;
            SphinxAssert.assert(_acmod.Deref.n_feat_frame <= _acmod.Deref.n_feat_alloc);
            inout_cep.Val += inout_n_frames.Val;
            inout_n_frames.Val = 0;

            return nfr;
        }

        public static int acmod_process_full_raw(Pointer<acmod_t> _acmod,
                               BoxedValue<Pointer<short>> inout_raw,
                               BoxedValue<uint> inout_n_samps)
        {
            int nfr, ntail;
            Pointer<Pointer<float>> cepptr;

            /* Write to logging file if any. */
            if (inout_n_samps.Val + _acmod.Deref.rawdata_pos < _acmod.Deref.rawdata_size)
            {
                inout_raw.Val.MemCopyTo(_acmod.Deref.rawdata + _acmod.Deref.rawdata_pos, (int)inout_n_samps.Val);
                _acmod.Deref.rawdata_pos += checked((int)inout_n_samps.Val);
            }
            if (_acmod.Deref.rawfh != null)
                _acmod.Deref.rawfh.fwrite(inout_raw, 2, (int)inout_n_samps.Val);

            /* Resize mfc_buf to fit. */
            // LOGAN fixme: bad conversion between boxed integer types here
            BoxedValueInt boxed_n_samps = new BoxedValueInt((int)inout_n_samps.Val);
            BoxedValueInt boxed_nfr = new BoxedValueInt();
            if (fe_interface.fe_process_frames(_acmod.Deref.fe, null, boxed_n_samps, PointerHelpers.NULL<Pointer<float>>(), boxed_nfr, null) < 0)
                return -1;
            inout_n_samps.Val = (uint)boxed_n_samps.Val;
            nfr = boxed_nfr.Val;

            if (_acmod.Deref.n_mfc_alloc < nfr + 1)
            {
                ckd_alloc.ckd_free_2d(_acmod.Deref.mfc_buf);
                _acmod.Deref.mfc_buf = ckd_alloc.ckd_calloc_2d<float>((uint)(nfr + 1), (uint)fe_interface.fe_get_output_size(_acmod.Deref.fe));
                _acmod.Deref.n_mfc_alloc = nfr + 1;
            }
            _acmod.Deref.n_mfc_frame = 0;
            _acmod.Deref.mfc_outidx = 0;
            fe_interface.fe_start_utt(_acmod.Deref.fe);

            boxed_n_samps.Val = (int)inout_n_samps.Val;
            boxed_nfr.Val = nfr;
            if (fe_interface.fe_process_frames(_acmod.Deref.fe, inout_raw, boxed_n_samps,
                                  _acmod.Deref.mfc_buf, boxed_nfr, null) < 0)
                return -1;
            nfr = boxed_nfr.Val;
            inout_n_samps.Val = (uint)boxed_n_samps.Val;

            BoxedValueInt boxed_ntail = new BoxedValueInt();
            fe_interface.fe_end_utt(_acmod.Deref.fe, _acmod.Deref.mfc_buf[nfr], boxed_ntail);
            ntail = boxed_ntail.Val;
            nfr += ntail;

            cepptr = _acmod.Deref.mfc_buf;
            BoxedValue<Pointer<Pointer<float>>> boxed_cepptr = new BoxedValue<Pointer<Pointer<float>>>(cepptr);
            boxed_nfr.Val = nfr;
            nfr = acmod_process_full_cep(_acmod, boxed_cepptr, boxed_nfr);
            cepptr = boxed_cepptr.Val;
            nfr = boxed_nfr.Val;
            _acmod.Deref.n_mfc_frame = 0;
            return nfr;
        }

        /**
         * Process MFCCs that are in the internal buffer into features.
         */
        public static int acmod_process_mfcbuf(Pointer<acmod_t> _acmod)
        {
            // LOGAN OPT lots of boxing and unboxing here
            BoxedValue<Pointer<Pointer<float>>> mfcptr = new BoxedValue<Pointer<Pointer<float>>>();
            BoxedValueInt ncep = new BoxedValueInt();
            BoxedValueInt ncep1 = new BoxedValueInt();

            ncep.Val = _acmod.Deref.n_mfc_frame;
            /* Also do this in two parts because of the circular mfc_buf. */
            if (_acmod.Deref.mfc_outidx + ncep.Val > _acmod.Deref.n_mfc_alloc)
            {
                ncep1.Val = _acmod.Deref.n_mfc_alloc - _acmod.Deref.mfc_outidx;
                int saved_state = _acmod.Deref.state;

                /* Make sure we don't end the utterance here. */
                if (_acmod.Deref.state == acmod_state_e.ACMOD_ENDED)
                    _acmod.Deref.state = acmod_state_e.ACMOD_PROCESSING;
                mfcptr.Val = _acmod.Deref.mfc_buf + _acmod.Deref.mfc_outidx;
                ncep1.Val = acmod_process_cep(_acmod, mfcptr, ncep1, 0);
                /* It's possible that not all available frames were filled. */
                ncep.Val -= ncep1.Val;
                _acmod.Deref.n_mfc_frame -= ncep1.Val;
                _acmod.Deref.mfc_outidx += ncep1.Val;
                _acmod.Deref.mfc_outidx %= _acmod.Deref.n_mfc_alloc;
                /* Restore original state (could this really be the end) */
                _acmod.Deref.state = checked((byte)saved_state);
            }
            mfcptr.Val = _acmod.Deref.mfc_buf + _acmod.Deref.mfc_outidx;
            ncep.Val = acmod_process_cep(_acmod, mfcptr, ncep, 0);
            _acmod.Deref.n_mfc_frame -= ncep.Val;
            _acmod.Deref.mfc_outidx += ncep.Val;
            _acmod.Deref.mfc_outidx %= _acmod.Deref.n_mfc_alloc;
            return ncep.Val;
        }

        public static int acmod_process_raw(Pointer<acmod_t> _acmod,
                          BoxedValue<Pointer<short>> inout_raw,
                          BoxedValue<uint> inout_n_samps,
                          int full_utt)
        {
            int ncep;
            BoxedValueInt boxed_n_samps = new BoxedValueInt();
            BoxedValueInt out_frameidx = new BoxedValueInt();
            Pointer<short> prev_audio_inptr;

            /* If this is a full utterance, process it all at once. */
            if (full_utt != 0)
                return acmod_process_full_raw(_acmod, inout_raw, inout_n_samps);

            /* Append MFCCs to the end of any that are previously in there
             * (in practice, there will probably be none) */
            if (inout_n_samps != null && inout_n_samps.Val != 0)
            {
                int inptr;
                int processed_samples;

                prev_audio_inptr = inout_raw.Val;
                /* Total number of frames available. */
                ncep = _acmod.Deref.n_mfc_alloc - _acmod.Deref.n_mfc_frame;
                /* Where to start writing them (circular buffer) */
                inptr = (_acmod.Deref.mfc_outidx + _acmod.Deref.n_mfc_frame) % _acmod.Deref.n_mfc_alloc;

                /* Write them in two (or more) parts if there is wraparound. */
                while (inptr + ncep > _acmod.Deref.n_mfc_alloc)
                {
                    int ncep1 = _acmod.Deref.n_mfc_alloc - inptr;
                    BoxedValueInt boxed_ncep1 = new BoxedValueInt(ncep1);
                    boxed_n_samps.Val = checked((int)inout_n_samps.Val);
                    if (fe_interface.fe_process_frames(_acmod.Deref.fe, inout_raw, boxed_n_samps,
                                          _acmod.Deref.mfc_buf + inptr, boxed_ncep1, out_frameidx) < 0)
                    {
                        ncep1 = boxed_ncep1.Val;
                        inout_n_samps.Val = checked((uint)boxed_n_samps.Val);
                        return -1;
                    }
                    ncep1 = boxed_ncep1.Val;
                    inout_n_samps.Val = checked((uint)boxed_n_samps.Val);

                    if (out_frameidx.Val > 0)
                        _acmod.Deref.utt_start_frame = out_frameidx.Val;

                    processed_samples = inout_raw.Val - prev_audio_inptr;
                    if (processed_samples + _acmod.Deref.rawdata_pos < _acmod.Deref.rawdata_size)
                    {
                        prev_audio_inptr.MemCopyTo(_acmod.Deref.rawdata + _acmod.Deref.rawdata_pos, processed_samples);
                        _acmod.Deref.rawdata_pos += processed_samples;
                    }
                    /* Write to logging file if any. */
                    if (_acmod.Deref.rawfh != null)
                    {
                        _acmod.Deref.rawfh.fwrite(prev_audio_inptr, 2, processed_samples);
                    }
                    prev_audio_inptr = inout_raw.Val;

                    /* ncep1 now contains the number of frames actually
                     * processed.  This is a good thing, but it means we
                     * actually still might have some room left at the end of
                     * the buffer, hence the while loop.  Unfortunately it
                     * also means that in the case where we are really
                     * actually done, we need to get out totally, hence the
                     * goto. */
                    _acmod.Deref.n_mfc_frame += ncep1;
                    ncep -= ncep1;
                    inptr += ncep1;
                    inptr %= _acmod.Deref.n_mfc_alloc;
                    if (ncep1 == 0)
                        goto alldone;
                }

                SphinxAssert.assert(inptr + ncep <= _acmod.Deref.n_mfc_alloc);
                boxed_n_samps.Val = checked((int)inout_n_samps.Val);
                BoxedValueInt boxed_ncep = new BoxedValueInt(ncep);
                if (fe_interface.fe_process_frames(_acmod.Deref.fe, inout_raw, boxed_n_samps,
                                      _acmod.Deref.mfc_buf + inptr, boxed_ncep, out_frameidx) < 0)
                {
                    inout_n_samps.Val = checked((uint)boxed_n_samps.Val);
                    ncep = boxed_ncep.Val;
                    return -1;
                }
                inout_n_samps.Val = checked((uint)boxed_n_samps.Val);
                ncep = boxed_ncep.Val;

                if (out_frameidx.Val > 0)
                    _acmod.Deref.utt_start_frame = out_frameidx.Val;


                processed_samples = inout_raw.Val - prev_audio_inptr;
                if (processed_samples + _acmod.Deref.rawdata_pos < _acmod.Deref.rawdata_size)
                {
                    prev_audio_inptr.MemCopyTo(_acmod.Deref.rawdata + _acmod.Deref.rawdata_pos, processed_samples);
                    _acmod.Deref.rawdata_pos += processed_samples;
                }
                if (_acmod.Deref.rawfh != null)
                {
                    _acmod.Deref.rawfh.fwrite(prev_audio_inptr, 2, processed_samples);
                }
                prev_audio_inptr = inout_raw.Val;
                _acmod.Deref.n_mfc_frame += ncep;
                alldone:
                ;
            }

            /* Hand things off to acmod_process_cep. */
            return acmod_process_mfcbuf(_acmod);
        }

        public static int acmod_process_cep(Pointer<acmod_t> _acmod,
                          BoxedValue<Pointer<Pointer<float>>> inout_cep,
                          BoxedValueInt inout_n_frames,
                          int full_utt)
        {
            int nfeat, ncep, inptr;
            int orig_n_frames;

            /* If this is a full utterance, process it all at once. */
            if (full_utt != 0)
                return acmod_process_full_cep(_acmod, inout_cep, inout_n_frames);

            /* Write to file. */
            if (_acmod.Deref.mfcfh != null)
                acmod_log_mfc(_acmod, inout_cep.Val, inout_n_frames.Val);

            /* Maximum number of frames we're going to generate. */
            orig_n_frames = ncep = nfeat = inout_n_frames.Val;

            /* FIXME: This behaviour isn't guaranteed... */
            if (_acmod.Deref.state == acmod_state_e.ACMOD_ENDED)
                nfeat += feat.feat_window_size(_acmod.Deref.fcb);
            else if (_acmod.Deref.state == acmod_state_e.ACMOD_STARTED)
                nfeat -= feat.feat_window_size(_acmod.Deref.fcb);

            /* Clamp number of features to fit available space. */
            if (nfeat > _acmod.Deref.n_feat_alloc - _acmod.Deref.n_feat_frame)
            {
                /* Grow it as needed - we have to grow it at the end of an
                 * utterance because we can't return a short read there. */
                if (_acmod.Deref.grow_feat != 0 || _acmod.Deref.state == acmod_state_e.ACMOD_ENDED)
                    acmod_grow_feat_buf(_acmod, _acmod.Deref.n_feat_alloc + nfeat);
                else
                    ncep -= (nfeat - (_acmod.Deref.n_feat_alloc - _acmod.Deref.n_feat_frame));
            }

            /* Where to start writing in the feature buffer. */
            if (_acmod.Deref.grow_feat != 0)
            {
                /* Grow to avoid wraparound if grow_feat == TRUE. */
                inptr = _acmod.Deref.feat_outidx + _acmod.Deref.n_feat_frame;
                while (inptr + nfeat >= _acmod.Deref.n_feat_alloc)
                    acmod_grow_feat_buf(_acmod, _acmod.Deref.n_feat_alloc * 2);
            }
            else
            {
                inptr = (_acmod.Deref.feat_outidx + _acmod.Deref.n_feat_frame) % _acmod.Deref.n_feat_alloc;
            }


            /* FIXME: we can't split the last frame drop properly to be on the bounary,
             *        so just return
             */
            if (inptr + nfeat > _acmod.Deref.n_feat_alloc && _acmod.Deref.state == acmod_state_e.ACMOD_ENDED)
            {
                inout_n_frames.Val -= ncep;
                inout_cep.Val += ncep;
                return 0;
            }

            /* Write them in two parts if there is wraparound. */
            if (inptr + nfeat > _acmod.Deref.n_feat_alloc)
            {
                BoxedValueInt ncep1 = new BoxedValueInt(_acmod.Deref.n_feat_alloc - inptr);

                /* Make sure we don't end the utterance here. */
                nfeat = feat.feat_s2mfc2feat_live(_acmod.Deref.fcb, inout_cep.Val,
                                             ncep1,
                                             (_acmod.Deref.state == acmod_state_e.ACMOD_STARTED) ? 1 : 0,
                                             0,
                                             _acmod.Deref.feat_buf + inptr);
                if (nfeat < 0)
                    return -1;
                /* Move the output feature pointer forward. */
                _acmod.Deref.n_feat_frame += nfeat;
                SphinxAssert.assert(_acmod.Deref.n_feat_frame <= _acmod.Deref.n_feat_alloc);
                inptr += nfeat;
                inptr %= _acmod.Deref.n_feat_alloc;
                /* Move the input feature pointers forward. */
                inout_n_frames.Val -= ncep1.Val;
                inout_cep.Val += ncep1.Val;
                ncep -= ncep1.Val;
            }

            BoxedValueInt boxed_ncep = new BoxedValueInt(ncep);
            nfeat = feat.feat_s2mfc2feat_live(_acmod.Deref.fcb, inout_cep.Val,
                                         boxed_ncep,
                                         (_acmod.Deref.state == acmod_state_e.ACMOD_STARTED) ? 1 : 0,
                                         (_acmod.Deref.state == acmod_state_e.ACMOD_ENDED) ? 1 : 0,
                                         _acmod.Deref.feat_buf + inptr);
            ncep = boxed_ncep.Val;

            if (nfeat < 0)
                return -1;
            _acmod.Deref.n_feat_frame += nfeat;
            SphinxAssert.assert(_acmod.Deref.n_feat_frame <= _acmod.Deref.n_feat_alloc);
            /* Move the input feature pointers forward. */
            inout_n_frames.Val -= ncep;
            inout_cep.Val += ncep;
            if (_acmod.Deref.state == acmod_state_e.ACMOD_STARTED)
                _acmod.Deref.state = acmod_state_e.ACMOD_PROCESSING;

            return orig_n_frames - inout_n_frames.Val;
        }

        public static int acmod_process_feat(Pointer<acmod_t> _acmod,
                           Pointer<Pointer<float>> feats)
        {
            int i, inptr;

            if (_acmod.Deref.n_feat_frame == _acmod.Deref.n_feat_alloc)
            {
                if (_acmod.Deref.grow_feat != 0)
                    acmod_grow_feat_buf(_acmod, _acmod.Deref.n_feat_alloc * 2);
                else
                    return 0;
            }

            if (_acmod.Deref.grow_feat != 0)
            {
                /* Grow to avoid wraparound if grow_feat == TRUE. */
                inptr = _acmod.Deref.feat_outidx + _acmod.Deref.n_feat_frame;
                while (inptr + 1 >= _acmod.Deref.n_feat_alloc)
                    acmod_grow_feat_buf(_acmod, _acmod.Deref.n_feat_alloc * 2);
            }
            else
            {
                inptr = (_acmod.Deref.feat_outidx + _acmod.Deref.n_feat_frame) % _acmod.Deref.n_feat_alloc;
            }
            for (i = 0; i < feat.feat_dimension1(_acmod.Deref.fcb); ++i)
                feats[i].MemCopyTo(_acmod.Deref.feat_buf[inptr][i], (int)feat.feat_dimension2(_acmod.Deref.fcb, i)); // LOGAN watch out, this is a memcopy of pointers
            ++_acmod.Deref.n_feat_frame;
            SphinxAssert.assert(_acmod.Deref.n_feat_frame <= _acmod.Deref.n_feat_alloc);

            return 1;
        }

        public static int acmod_read_senfh_header(Pointer<acmod_t> _acmod)
        {
            Pointer<Pointer<byte>> name;
            Pointer<Pointer<byte>> val;
            int swap;
            int i;

            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(_acmod.Deref.insenfh, boxed_argname, boxed_argval, out swap) < 0)
                return -1;

            name = boxed_argname.Val;
            val = boxed_argval.Val;

            for (i = 0; name[i].IsNonNull; ++i)
            {
                if (cstring.strcmp(name[i], cstring.ToCString("n_sen")) == 0)
                {
                    if (cstring.atoi(val[i]) != bin_mdef.bin_mdef_n_sen(_acmod.Deref.mdef))
                    {
                        err.E_ERROR(string.Format("Number of senones in senone file ({0}) does not match mdef ({1})\n", cstring.atoi(val[i]),
                                bin_mdef.bin_mdef_n_sen(_acmod.Deref.mdef)));
                        return -1;
                    }
                }

                if (cstring.strcmp(name[i], cstring.ToCString("logbase")) == 0)
                {
                    if (Math.Abs(strfuncs.atof_c(val[i]) - logmath.logmath_get_base(_acmod.Deref.lmath)) > 0.001)
                    {
                        err.E_ERROR(string.Format("Logbase in senone file ({0}) does not match acmod ({1})\n", strfuncs.atof_c(val[i]),
                                logmath.logmath_get_base(_acmod.Deref.lmath)));
                        return -1;
                    }
                }
            }

            _acmod.Deref.insen_swap = checked((byte)swap);
            bio.bio_hdrarg_free(name, val);
            return 0;
        }

        public static int acmod_set_insenfh(Pointer<acmod_t> _acmod, FILE senfh)
        {
            _acmod.Deref.insenfh = senfh;
            if (senfh == null)
            {
                _acmod.Deref.n_feat_frame = 0;
                _acmod.Deref.compallsen = cmd_ln.cmd_ln_boolean_r(_acmod.Deref.config, cstring.ToCString("-compallsen"));
                return 0;
            }
            _acmod.Deref.compallsen = 1;
            return acmod_read_senfh_header(_acmod);
        }

        public static int acmod_rewind(Pointer<acmod_t> _acmod)
        {
            /* If the feature buffer is circular, this is not possible. */
            if (_acmod.Deref.output_frame > _acmod.Deref.n_feat_alloc)
            {
                err.E_ERROR(string.Format("Circular feature buffer cannot be rewound (output frame {0}, alloc {1})\n", _acmod.Deref.output_frame, _acmod.Deref.n_feat_alloc));
                return -1;
            }

            /* Frames consumed + frames available */
            _acmod.Deref.n_feat_frame = _acmod.Deref.output_frame + _acmod.Deref.n_feat_frame;

            /* Reset output pointers. */
            _acmod.Deref.feat_outidx = 0;
            _acmod.Deref.output_frame = 0;
            _acmod.Deref.senscr_frame = -1;
            _acmod.Deref.mgau.frame_idx = 0;

            return 0;
        }

        public static int acmod_advance(Pointer<acmod_t> acmod)
        {
            /* Advance the output pointers. */
            if (++acmod.Deref.feat_outidx == acmod.Deref.n_feat_alloc)
                acmod.Deref.feat_outidx = 0;
            --acmod.Deref.n_feat_frame;
            ++acmod.Deref.mgau.frame_idx;

            return ++acmod.Deref.output_frame;
        }

        /**
         * Internal version, used for reading previous frames in acmod_score()
         */
        public static int acmod_read_scores_internal(Pointer<acmod_t> _acmod)
        {
            FILE senfh = _acmod.Deref.insenfh;
            short n_active;
            uint rv;

            if (_acmod.Deref.n_feat_frame == _acmod.Deref.n_feat_alloc)
            {
                if (_acmod.Deref.grow_feat != 0)
                    acmod_grow_feat_buf(_acmod, _acmod.Deref.n_feat_alloc * 2);
                else
                    return 0;
            }

            if (senfh == null)
                return -1;

            byte[] buf = new byte[2];
            Pointer<byte> temp_byte_ptr = new Pointer<byte>(buf);
            Pointer<short> temp_short_ptr = temp_byte_ptr.ReinterpretCast<short>();

            if ((rv = senfh.fread(temp_byte_ptr, 2, 1)) != 1)
                goto error_out;
            n_active = temp_short_ptr.Deref;
            
            _acmod.Deref.n_senone_active = n_active;

            byte[] big_buf = new byte[_acmod.Deref.n_senone_active * 2];
            Pointer<byte> big_byte_ptr = new Pointer<byte>(big_buf);
            Pointer<short> big_short_ptr = big_byte_ptr.ReinterpretCast<short>();

            if (_acmod.Deref.n_senone_active == bin_mdef.bin_mdef_n_sen(_acmod.Deref.mdef))
            {
                if ((rv = senfh.fread(big_byte_ptr, 2, (uint)_acmod.Deref.n_senone_active)) != _acmod.Deref.n_senone_active)
                    goto error_out;

                big_short_ptr.MemCopyTo(_acmod.Deref.senone_scores, _acmod.Deref.n_senone_active);
            }
            else
            {
                int i, n;

                if ((rv = senfh.fread(_acmod.Deref.senone_active, 1, (uint)_acmod.Deref.n_senone_active)) != _acmod.Deref.n_senone_active)
                    goto error_out;

                for (i = 0, n = 0; i < _acmod.Deref.n_senone_active; ++i)
                {
                    int j, sen = n + _acmod.Deref.senone_active[i];
                    for (j = n + 1; j < sen; ++j)
                        _acmod.Deref.senone_scores[j] = SENSCR_DUMMY;

                    if ((rv = senfh.fread(temp_byte_ptr, 2, 1)) != 1)
                        goto error_out;
                    _acmod.Deref.senone_scores[sen] = temp_short_ptr.Deref;

                    n = sen;
                }

                n++;
                while (n < bin_mdef.bin_mdef_n_sen(_acmod.Deref.mdef))
                    _acmod.Deref.senone_scores[n++] = SENSCR_DUMMY;
            }
            return 1;

            error_out:
            if (senfh.ferror() != 0)
            {
                err.E_ERROR_SYSTEM("Failed to read frame from senone file");
                return -1;
            }
            return 0;
        }

        public static int acmod_read_scores(Pointer<acmod_t> acmod)
        {
            int inptr, rv;

            if (acmod.Deref.grow_feat != 0)
            {
                /* Grow to avoid wraparound if grow_feat == TRUE. */
                inptr = acmod.Deref.feat_outidx + acmod.Deref.n_feat_frame;
                /* Has to be +1, otherwise, next time acmod_advance() is
                 * called, this will wrap around. */
                while (inptr + 1 >= acmod.Deref.n_feat_alloc)
                    acmod_grow_feat_buf(acmod, acmod.Deref.n_feat_alloc * 2);
            }
            else
            {
                inptr = (acmod.Deref.feat_outidx + acmod.Deref.n_feat_frame) %
                        acmod.Deref.n_feat_alloc;
            }

            if ((rv = acmod_read_scores_internal(acmod)) != 1)
                return rv;

            /* Set acmod.Deref.senscr_frame appropriately so that these scores
               get reused below in acmod_score(). */
            acmod.Deref.senscr_frame = acmod.Deref.output_frame + acmod.Deref.n_feat_frame;

            err.E_DEBUG(string.Format("Frame {0} has {1} active states\n",
                    acmod.Deref.senscr_frame, acmod.Deref.n_senone_active));

            /* Increment the "feature frame counter" and record the file
             * position for the relevant frame in the (possibly circular)
             * buffer. */
            ++acmod.Deref.n_feat_frame;
            acmod.Deref.framepos[inptr] = acmod.Deref.insenfh.ftell();

            return 1;
        }

        public static int calc_frame_idx(Pointer<acmod_t> acmod, BoxedValueInt inout_frame_idx)
        {
            int frame_idx;

            /* Calculate the absolute frame index to be scored. */
            if (inout_frame_idx == null)
                frame_idx = acmod.Deref.output_frame;
            else if (inout_frame_idx.Val < 0)
                frame_idx = acmod.Deref.output_frame + 1 + inout_frame_idx.Val;
            else
                frame_idx = inout_frame_idx.Val;

            return frame_idx;
        }

        public static int calc_feat_idx(Pointer<acmod_t> acmod, int frame_idx)
        {
            int n_backfr, feat_idx;

            n_backfr = acmod.Deref.n_feat_alloc - acmod.Deref.n_feat_frame;
            if (frame_idx < 0 || acmod.Deref.output_frame - frame_idx > n_backfr)
            {
                err.E_ERROR(string.Format("Frame {0} outside queue of {1} frames, {2} alloc ({3} > {4}), cannot score\n", frame_idx, acmod.Deref.n_feat_frame,
                        acmod.Deref.n_feat_alloc, acmod.Deref.output_frame - frame_idx,
                        n_backfr));
                return -1;
            }

            /* Get the index in feat_buf/framepos of the frame to be scored. */
            feat_idx = (acmod.Deref.feat_outidx + frame_idx - acmod.Deref.output_frame) %
                       acmod.Deref.n_feat_alloc;
            if (feat_idx < 0)
                feat_idx += acmod.Deref.n_feat_alloc;

            return feat_idx;
        }

        public static Pointer<Pointer<float>> acmod_get_frame(Pointer<acmod_t> acmod, BoxedValueInt inout_frame_idx)
        {
            int frame_idx, feat_idx;

            /* Calculate the absolute frame index requested. */
            frame_idx = calc_frame_idx(acmod, inout_frame_idx);

            /* Calculate position of requested frame in circular buffer. */
            if ((feat_idx = calc_feat_idx(acmod, frame_idx)) < 0)
                return PointerHelpers.NULL<Pointer<float>>();

            if (inout_frame_idx != null)
                inout_frame_idx.Val = frame_idx;

            return acmod.Deref.feat_buf[feat_idx];
        }

        public static Pointer<short> acmod_score(Pointer<acmod_t> _acmod, BoxedValueInt inout_frame_idx)
        {
            int frame_idx, feat_idx;

            /* Calculate the absolute frame index to be scored. */
            frame_idx = calc_frame_idx(_acmod, inout_frame_idx);

            /* If all senones are being computed, or we are using a senone file,
               then we can reuse existing scores. */
            if ((_acmod.Deref.compallsen != 0 || _acmod.Deref.insenfh != null)
                && frame_idx == _acmod.Deref.senscr_frame)
            {
                if (inout_frame_idx != null)
                    inout_frame_idx.Val = frame_idx;
                return _acmod.Deref.senone_scores;
            }

            /* Calculate position of requested frame in circular buffer. */
            if ((feat_idx = calc_feat_idx(_acmod, frame_idx)) < 0)
                return PointerHelpers.NULL<short>();

            /*
             * If there is an input senone file locate the appropriate frame and read
             * it.
             */
            if (_acmod.Deref.insenfh != null)
            {
                _acmod.Deref.insenfh.fseek(_acmod.Deref.framepos[feat_idx], FILE.SEEK_SET);
                if (acmod_read_scores_internal(_acmod) < 0)
                    return PointerHelpers.NULL<short>();
            }
            else
            {
                /* Build active senone list. */
                acmod_flags2list(_acmod);

                /* Generate scores for the next available frame */
                _acmod.Deref.mgau.vt.frame_eval(_acmod.Deref.mgau,
                                   _acmod.Deref.senone_scores,
                                   _acmod.Deref.senone_active,
                                   _acmod.Deref.n_senone_active,
                                   _acmod.Deref.feat_buf[feat_idx],
                                   frame_idx,
                                   _acmod.Deref.compallsen);
            }

            if (inout_frame_idx != null)
                inout_frame_idx.Val = frame_idx;
            _acmod.Deref.senscr_frame = frame_idx;

            /* Dump scores to the senone dump file if one exists. */
            // LOGAN cut this part out
            //if (_acmod.Deref.senfh != null)
            //{
            //    if (acmod_write_scores(_acmod, _acmod.Deref.n_senone_active,
            //                           _acmod.Deref.senone_active,
            //                           _acmod.Deref.senone_scores,
            //                           _acmod.Deref.senfh) < 0)
            //        return PointerHelpers.NULL<short>();
            //    err.E_DEBUG(string.Format("Frame {0} has {1} active states\n", frame_idx,
            //            _acmod.Deref.n_senone_active));
            //}

            return _acmod.Deref.senone_scores;
        }

        public static int acmod_best_score(Pointer<acmod_t> _acmod, BoxedValueInt out_best_senid)
        {
            int i, best;

            best = SENSCR_DUMMY;
            if (_acmod.Deref.compallsen != 0)
            {
                for (i = 0; i < bin_mdef.bin_mdef_n_sen(_acmod.Deref.mdef); ++i)
                {
                    if (_acmod.Deref.senone_scores[i] < best)
                    {
                        best = _acmod.Deref.senone_scores[i];
                        out_best_senid.Val = i;
                    }
                }
            }
            else
            {
                Pointer<short> senscr;
                senscr = _acmod.Deref.senone_scores;
                for (i = 0; i < _acmod.Deref.n_senone_active; ++i)
                {
                    senscr += _acmod.Deref.senone_active[i];
                    if (senscr.Deref < best)
                    {
                        best = senscr.Deref;
                        out_best_senid.Val = i;
                    }
                }
            }
            return best;
        }


        public static void acmod_clear_active(Pointer<acmod_t> acmod)
        {
            if (acmod.Deref.compallsen != 0)
                return;
            bitvec.bitvec_clear_all(acmod.Deref.senone_active_vec, bin_mdef.bin_mdef_n_sen(acmod.Deref.mdef));
            acmod.Deref.n_senone_active = 0;
        }
        
        public static void acmod_activate_hmm(Pointer<acmod_t> _acmod, Pointer<hmm_t> _hmm)
        {
            int i;

            if (_acmod.Deref.compallsen != 0)
                return;

            // LOGAN OPT The C code unrolled this loop a little bit, treating 3 and 5 as special cases

            if (hmm.hmm_is_mpx(_hmm) != 0)
            {
                        for (i = 0; i < hmm.hmm_n_emit_state(_hmm); ++i)
                        {
                    if (hmm.hmm_mpx_ssid(_hmm, i) != bin_mdef.BAD_SSID)
                        bitvec.bitvec_set(_acmod.Deref.senone_active_vec, hmm.hmm_mpx_senid(_hmm, i));
                        }
                }
            else
            {
                        for (i = 0; i < hmm.hmm_n_emit_state(_hmm); ++i)
                        {
                    bitvec.bitvec_set(_acmod.Deref.senone_active_vec, hmm.hmm_nonmpx_senid(_hmm, i));
                        }
                }
            }

        public static int acmod_flags2list(Pointer<acmod_t> _acmod)
        {
            int w, l, n, b, total_dists, total_words, extra_bits;
            Pointer<uint> flagptr;

            total_dists = bin_mdef.bin_mdef_n_sen(_acmod.Deref.mdef);
            if (_acmod.Deref.compallsen != 0)
            {
                _acmod.Deref.n_senone_active = total_dists;
                return total_dists;
            }
            total_words = total_dists / bitvec.BITVEC_BITS;
            extra_bits = total_dists % bitvec.BITVEC_BITS;
            w = n = l = 0;
            for (flagptr = _acmod.Deref.senone_active_vec; w < total_words; ++w, ++flagptr)
            {
                if (flagptr.Deref == 0)
                    continue;
                for (b = 0; b < bitvec.BITVEC_BITS; ++b)
                {
                    if ((flagptr.Deref & (1UL << b)) != 0)
                    {
                        int sen = w * bitvec.BITVEC_BITS + b;
                        int delta = sen - l;
                        /* Handle excessive deltas "lossily" by adding a few
                           extra senones to bridge the gap. */
                        while (delta > 255)
                        {
                            _acmod.Deref.senone_active[n++] = 255;
                            delta -= 255;
                        }
                        _acmod.Deref.senone_active[n++] = checked((byte)delta);
                        l = sen;
                    }
                }
            }

            for (b = 0; b < extra_bits; ++b)
            {
                if ((flagptr.Deref & (1UL << b)) != 0)
                {
                    int sen = w * bitvec.BITVEC_BITS + b;
                    int delta = sen - l;
                    /* Handle excessive deltas "lossily" by adding a few
                       extra senones to bridge the gap. */
                    while (delta > 255)
                    {
                        _acmod.Deref.senone_active[n++] = 255;
                        delta -= 255;
                    }
                    _acmod.Deref.senone_active[n++] = checked((byte)delta);
                    l = sen;
                }
            }

            _acmod.Deref.n_senone_active = n;
            //err.E_DEBUG(string.Format("acmod_flags2list: {0} active in frame {1}\n",
            //        _acmod.Deref.n_senone_active, _acmod.Deref.output_frame));
            return n;
        }

        public static int acmod_stream_offset(Pointer<acmod_t> acmod)
        {
            return acmod.Deref.utt_start_frame;
        }
    }
}
