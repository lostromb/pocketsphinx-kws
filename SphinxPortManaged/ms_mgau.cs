using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ms_mgau
    {
        public static ps_mgaufuncs_t ms_mgau_funcs = new ps_mgaufuncs_t()
        {
            name = cstring.ToCString("ms"),
            frame_eval = ms_cont_mgau_frame_eval,
            transform = ms_mgau_mllr_transform,
            free = ms_mgau_free
        };

        public static Pointer<gauden_t> ms_mgau_gauden(ms_mgau_model_t msg)
        {
            return msg.g;
        }

        public static Pointer<senone_t> ms_mgau_senone(ms_mgau_model_t msg)
        {
            return msg.s;
        }

        public static int ms_mgau_topn(ms_mgau_model_t msg)
        {
            return msg.topn;
        }

        public static ps_mgau_t ms_mgau_init(Pointer<acmod_t> acmod, Pointer<logmath_t> lmath, Pointer<bin_mdef_t> mdef)
        {
            /* Codebooks */
            ms_mgau_model_t msg;
            ps_mgau_t mg;
            Pointer<gauden_t> g;
            Pointer<senone_t> s;
            Pointer<cmd_ln_t> config;
            int i;

            config = acmod.Deref.config;

            msg = new ms_mgau_model_t();
            msg.config = config;
            msg.g = PointerHelpers.NULL<gauden_t>();
            msg.s = PointerHelpers.NULL<senone_t>();

            if ((g = msg.g = ms_gauden.gauden_init(cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_mean")),
                                     cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_var")),
                                     (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-varfloor")),
                                     lmath)).IsNull)
            {
                err.E_ERROR("Failed to read means and variances\n");
                goto error_out;
            }

            /* Verify n_feat and veclen, against acmod. */
            if (g.Deref.n_feat != feat.feat_dimension1(acmod.Deref.fcb))
            {
                err.E_ERROR(string.Format("Number of streams does not match: {0} != {1}\n",
                        g.Deref.n_feat, feat.feat_dimension1(acmod.Deref.fcb)));
                goto error_out;
            }
            for (i = 0; i < g.Deref.n_feat; ++i)
            {
                if (g.Deref.featlen[i] != feat.feat_dimension2(acmod.Deref.fcb, i))
                {
                    err.E_ERROR(string.Format("Dimension of stream {0} does not match: {1} != {2}\n", i,
                            g.Deref.featlen[i], feat.feat_dimension2(acmod.Deref.fcb, i)));
                    goto error_out;
                }
            }

            s = msg.s = ms_senone.senone_init(msg.g,
                                     cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_mixw")),
                                     cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_senmgau")),
                                     (float)cmd_ln.cmd_ln_float_r(config, cstring.ToCString("-mixwfloor")),
                                     lmath, mdef);

            s.Deref.aw = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-aw"));

            /* Verify senone parameters against gauden parameters */
            if (s.Deref.n_feat != g.Deref.n_feat)
                err.E_FATAL(string.Format("#Feature mismatch: gauden= {0}, senone= {1}\n", g.Deref.n_feat,
                        s.Deref.n_feat));
            if (s.Deref.n_cw != g.Deref.n_density)
                err.E_FATAL(string.Format("#Densities mismatch: gauden= {0}, senone= {1}\n",
                        g.Deref.n_density, s.Deref.n_cw));
            if (s.Deref.n_gauden > g.Deref.n_mgau)
                err.E_FATAL(string.Format("Senones need more codebooks ({0}) than present ({1})\n",
                        s.Deref.n_gauden, g.Deref.n_mgau));
            if (s.Deref.n_gauden < g.Deref.n_mgau)
                err.E_ERROR(string.Format("Senones use fewer codebooks ({0}) than present ({1})\n",
                        s.Deref.n_gauden, g.Deref.n_mgau));

            msg.topn = (int)cmd_ln.cmd_ln_int_r(config, cstring.ToCString("-topn"));
            err.E_INFO(string.Format("The value of topn: {0}\n", msg.topn));
            if (msg.topn == 0 || msg.topn > msg.g.Deref.n_density)
            {
                err.E_WARN
                    (string.Format("-topn argument ({0}) invalid or > #density codewords ({1}); set to latter\n",
                     msg.topn, msg.g.Deref.n_density));
                msg.topn = msg.g.Deref.n_density;
            }

            msg.dist =  ckd_alloc.ckd_calloc_struct_3d<gauden_dist_t>((uint)g.Deref.n_mgau, (uint)g.Deref.n_feat, (uint)msg.topn);
            msg.mgau_active = ckd_alloc.ckd_calloc<byte>(g.Deref.n_mgau);

            mg = (ps_mgau_t)msg;
            mg.vt = ms_mgau_funcs;
            return mg;

            error_out:
            ms_mgau_free(msg);
            return null;
        }

        public static void ms_mgau_free(ps_mgau_t mg)
        {
            ms_mgau_model_t msg = (ms_mgau_model_t)mg;
            if (msg == null)
                return;

            if (msg.g.IsNonNull)
                ms_gauden.gauden_free(msg.g);
            if (msg.s.IsNonNull)
                ms_senone.senone_free(msg.s);
            if (msg.dist.IsNonNull)
                ckd_alloc.ckd_free_3d(msg.dist);
            if (msg.mgau_active.IsNonNull)
                ckd_alloc.ckd_free(msg.mgau_active);
        }

        public static int ms_mgau_mllr_transform(ps_mgau_t s,
                       Pointer<ps_mllr_t> mllr)
        {
            ms_mgau_model_t msg = (ms_mgau_model_t)s;
            return ms_gauden.gauden_mllr_transform(msg.g, mllr, msg.config);
        }

        public static int ms_cont_mgau_frame_eval(ps_mgau_t mg,
                    Pointer<short> senscr,
                    Pointer<byte> senone_active,
                    int n_senone_active,
                    Pointer<Pointer<float>> feats,
                    int frame,
                    int compallsen)
        {
            ms_mgau_model_t msg = (ms_mgau_model_t)mg;
            int gid;
            int topn;
            int best;
            Pointer < gauden_t> g;
            Pointer < senone_t >sen;

            topn = ms_mgau_topn(msg);
            g = ms_mgau_gauden(msg);
            sen = ms_mgau_senone(msg);

            if (compallsen != 0)
            {
                int s;

                for (gid = 0; gid < g.Deref.n_mgau; gid++)
                    ms_gauden.gauden_dist(g, gid, topn, feats, msg.dist[gid]);

                best = (int)0x7fffffff;
                for (s = 0; s < sen.Deref.n_sen; s++)
                {
                    senscr[s] = checked((short)(ms_senone.senone_eval(sen, s, msg.dist[sen.Deref.mgau[s]], topn)));
                    if (best > senscr[s])
                    {
                        best = senscr[s];
                    }
                }

                /* Normalize senone scores */
                for (s = 0; s < sen.Deref.n_sen; s++)
                {
                    int bs = senscr[s] - best;
                    if (bs > 32767)
                        bs = 32767;
                    if (bs < -32768)
                        bs = -32768;
                    senscr[s] = checked((short)bs);
                }
            }
            else
            {
                int i, n;
                /* Flag all active mixture-gaussian codebooks */
                for (gid = 0; gid < g.Deref.n_mgau; gid++)
                    msg.mgau_active[gid] = 0;

                n = 0;
                for (i = 0; i < n_senone_active; i++)
                {
                    /* senone_active consists of deltas. */
                    int s = senone_active[i] + n;
                    msg.mgau_active[sen.Deref.mgau[s]] = 1;
                    n = s;
                }

                /* Compute topn gaussian density values (for active codebooks) */
                for (gid = 0; gid < g.Deref.n_mgau; gid++)
                {
                    if (msg.mgau_active[gid] != 0)
                        ms_gauden.gauden_dist(g, gid, topn, feats, msg.dist[gid]);
                }

                best = (int)0x7fffffff;
                n = 0;
                for (i = 0; i < n_senone_active; i++)
                {
                    int s = senone_active[i] + n;
                    senscr[s] = checked((short)ms_senone.senone_eval(sen, s, msg.dist[sen.Deref.mgau[s]], topn));
                    if (best > senscr[s])
                    {
                        best = senscr[s];
                    }
                    n = s;
                }

                /* Normalize senone scores */
                n = 0;
                for (i = 0; i < n_senone_active; i++)
                {
                    int s = senone_active[i] + n;
                    int bs = senscr[s] - best;
                    if (bs > 32767)
                        bs = 32767;
                    if (bs < -32768)
                        bs = -32768;
                    senscr[s] = checked((short)bs);
                    n = s;
                }
            }

            return 0;
        }
    }
}
