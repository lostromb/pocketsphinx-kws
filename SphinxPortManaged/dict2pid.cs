using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class dict2pid
    {
        public static void compress_table(Pointer<ushort> uncomp_tab, Pointer<ushort> com_tab,
               Pointer<short> ci_map, int n_ci)
        {
            int found;
            int r;
            int tmp_r;

            for (r = 0; r < n_ci; r++)
            {
                com_tab[r] = s3types.BAD_S3SSID;
                ci_map[r] = s3types.BAD_S3CIPID;
            }
            /** Compress this map */
            for (r = 0; r < n_ci; r++)
            {

                found = 0;
                for (tmp_r = 0; tmp_r < r && com_tab[tmp_r] != s3types.BAD_S3SSID; tmp_r++)
                {   /* If it appears before, just filled in cimap; */
                    if (uncomp_tab[r] == com_tab[tmp_r])
                    {
                        found = 1;
                        ci_map[r] = checked((short)tmp_r);
                        break;
                    }
                }

                if (found == 0)
                {
                    com_tab[tmp_r] = uncomp_tab[r];
                    ci_map[r] = checked((short)tmp_r);
                }
            }
        }


        public static void compress_right_context_tree(Pointer<dict2pid_t> d2p,
                                    Pointer<Pointer<Pointer<ushort>>> rdiph_rc)
        {
            int n_ci;
            int b, l, r;
            Pointer<ushort> rmap;
            Pointer<ushort> tmpssid;
            Pointer<short> tmpcimap;
            Pointer<bin_mdef_t> mdef = d2p.Deref.mdef;
            uint alloc;

            n_ci = mdef.Deref.n_ciphone;

            tmpssid = ckd_alloc.ckd_calloc<ushort>(n_ci);
            tmpcimap = ckd_alloc.ckd_calloc<short>(n_ci);

            d2p.Deref.rssid =
                (Pointer < Pointer < xwdssid_t >>)ckd_alloc.ckd_calloc<Pointer<xwdssid_t>>(mdef.Deref.n_ciphone);
            alloc = (uint)(mdef.Deref.n_ciphone * 8);

            for (b = 0; b < n_ci; b++)
            {
                d2p.Deref.rssid[b] =
                    (Pointer<xwdssid_t>)ckd_alloc.ckd_calloc_struct<xwdssid_t>(mdef.Deref.n_ciphone);
                alloc += (uint)(mdef.Deref.n_ciphone * 20);

                for (l = 0; l < n_ci; l++)
                {
                    rmap = rdiph_rc[b][l];
                    compress_table(rmap, tmpssid, tmpcimap, mdef.Deref.n_ciphone);

                    for (r = 0; r < mdef.Deref.n_ciphone && tmpssid[r] != s3types.BAD_S3SSID;
                         r++) ;

                    if (tmpssid[0] != s3types.BAD_S3SSID)
                    {
                        d2p.Deref.rssid[b][l].ssid = ckd_alloc.ckd_calloc<ushort>(r);
                        tmpssid.MemCopyTo(d2p.Deref.rssid[b][l].ssid, r);
                        d2p.Deref.rssid[b][l].cimap =
                            ckd_alloc.ckd_calloc<short>(mdef.Deref.n_ciphone);
                        tmpcimap.MemCopyTo(d2p.Deref.rssid[b][l].cimap, (mdef.Deref.n_ciphone));
                        d2p.Deref.rssid[b][l].n_ssid = r;
                    }
                    else
                    {
                        d2p.Deref.rssid[b][l].ssid = PointerHelpers.NULL<ushort>();
                        d2p.Deref.rssid[b][l].cimap = PointerHelpers.NULL<short>();
                        d2p.Deref.rssid[b][l].n_ssid = 0;
                    }
                }
            }

            err.E_INFO(string.Format("Allocated {0} bytes ({1} KiB) for word-final triphones\n",
                   (int)alloc, (int)alloc / 1024));
            ckd_alloc.ckd_free(tmpssid);
            ckd_alloc.ckd_free(tmpcimap);
        }

        public static void compress_left_right_context_tree(Pointer<dict2pid_t> d2p)
        {
            int n_ci;
            int b, l, r;
            Pointer<ushort> rmap;
            Pointer<ushort> tmpssid;
            Pointer<short> tmpcimap;
            Pointer<bin_mdef_t> mdef = d2p.Deref.mdef;
            uint alloc;

            n_ci = mdef.Deref.n_ciphone;

            tmpssid = ckd_alloc.ckd_calloc<ushort>(n_ci);
            tmpcimap = ckd_alloc.ckd_calloc<short>(n_ci);

            SphinxAssert.assert(d2p.Deref.lrdiph_rc.IsNonNull);

            d2p.Deref.lrssid =
                (Pointer<Pointer<xwdssid_t>>)ckd_alloc.ckd_calloc<Pointer<xwdssid_t>>(mdef.Deref.n_ciphone);
            alloc = (uint)(mdef.Deref.n_ciphone * 8);

            for (b = 0; b < n_ci; b++)
            {

                d2p.Deref.lrssid[b] =
                    (Pointer<xwdssid_t>)ckd_alloc.ckd_calloc_struct<xwdssid_t>(mdef.Deref.n_ciphone);
                alloc += (uint)(mdef.Deref.n_ciphone * 20);
                
                for (l = 0; l < n_ci; l++)
                {
                    rmap = d2p.Deref.lrdiph_rc[b][l];

                    compress_table(rmap, tmpssid, tmpcimap, mdef.Deref.n_ciphone);

                    for (r = 0; r < mdef.Deref.n_ciphone && tmpssid[r] != s3types.BAD_S3SSID;
                         r++) ;

                    if (tmpssid[0] != s3types.BAD_S3SSID)
                    {
                        d2p.Deref.lrssid[b][l].ssid = ckd_alloc.ckd_calloc<ushort>(r);
                        tmpssid.MemCopyTo(d2p.Deref.lrssid[b][l].ssid, r);
                        d2p.Deref.lrssid[b][l].cimap =
                            ckd_alloc.ckd_calloc<short>(mdef.Deref.n_ciphone);
                        tmpcimap.MemCopyTo(d2p.Deref.lrssid[b][l].cimap, mdef.Deref.n_ciphone);
                        d2p.Deref.lrssid[b][l].n_ssid = r;
                    }
                    else
                    {
                        d2p.Deref.lrssid[b][l].ssid = PointerHelpers.NULL<ushort>();
                        d2p.Deref.lrssid[b][l].cimap = PointerHelpers.NULL<short>();
                        d2p.Deref.lrssid[b][l].n_ssid = 0;
                    }
                }
            }

            /* Try to compress lrdiph_rc into lrdiph_rc_compressed */
            ckd_alloc.ckd_free(tmpssid);
            ckd_alloc.ckd_free(tmpcimap);

            err.E_INFO(string.Format("Allocated {0} bytes ({1} KiB) for single-phone word triphones\n",
                   (int)alloc, (int)alloc / 1024));
        }

        public static void free_compress_map(Pointer<Pointer<xwdssid_t>> tree, int n_ci)
        {
            int b, l;
            for (b = 0; b < n_ci; b++)
            {
                for (l = 0; l < n_ci; l++)
                {
                    ckd_alloc.ckd_free(tree[b][l].ssid);
                    ckd_alloc.ckd_free(tree[b][l].cimap);
                }
                ckd_alloc.ckd_free(tree[b]);
            }
            ckd_alloc.ckd_free(tree);
        }

        public static void populate_lrdiph(Pointer<dict2pid_t> d2p, Pointer<Pointer<Pointer<ushort>>> rdiph_rc, short b)
        {
            Pointer<bin_mdef_t> mdef = d2p.Deref.mdef;
            short l, r;

            for (l = 0; l < bin_mdef.bin_mdef_n_ciphone(mdef); l++)
            {
                for (r = 0; r < bin_mdef.bin_mdef_n_ciphone(mdef); r++)
                {
                    int p;
                    p = bin_mdef.bin_mdef_phone_id_nearest(mdef, (short)b,
                                                  (short)l,
                                                  (short)r,
                                                  word_posn_t.WORD_POSN_SINGLE);
                    d2p.Deref.lrdiph_rc[b][l].Set(r, checked((ushort)bin_mdef.bin_mdef_pid2ssid(mdef, p)));
                    if (r == bin_mdef.bin_mdef_silphone(mdef))
                    {
                        d2p.Deref.ldiph_lc[b][r].Set(l, checked((ushort)bin_mdef.bin_mdef_pid2ssid(mdef, p)));
                    }
                    if (rdiph_rc.IsNonNull && l == bin_mdef.bin_mdef_silphone(mdef))
                    {
                        rdiph_rc[b][l].Set(r, checked((ushort)bin_mdef.bin_mdef_pid2ssid(mdef, p)));
                    }
                    SphinxAssert.assert(s3types.IS_S3SSID(bin_mdef.bin_mdef_pid2ssid(mdef, p)) != 0);
                    // LOGAN this dumped way too much
                    //err.E_DEBUG(string.Format("{0}({1},{2}) => {3} / {4}\n",
                    //        cstring.FromCString(bin_mdef.bin_mdef_ciphone_str(mdef, b)),
                    //        cstring.FromCString(bin_mdef.bin_mdef_ciphone_str(mdef, l)),
                    //        cstring.FromCString(bin_mdef.bin_mdef_ciphone_str(mdef, r)),
                    //        p, bin_mdef.bin_mdef_pid2ssid(mdef, p)));
                }
            }
        }

        public static ushort dict2pid_internal(Pointer<dict2pid_t> d2p,
                          int wid,
                          int pos)
        {
            int b, l, r, p;
            Pointer<dict_t> dictionary = d2p.Deref.dict;
            Pointer<bin_mdef_t> mdef = d2p.Deref.mdef;

            if (pos == 0 || pos == dict.dict_pronlen(dictionary, wid))
                return s3types.BAD_S3SSID;

            b = dict.dict_pron(dictionary, wid, pos);
            l = dict.dict_pron(dictionary, wid, pos - 1);
            r = dict.dict_pron(dictionary, wid, pos + 1);
            p = bin_mdef.bin_mdef_phone_id_nearest(mdef, (short)b,
                                          (short)l, (short)r,
                                          word_posn_t.WORD_POSN_INTERNAL);
            return checked((ushort)bin_mdef.bin_mdef_pid2ssid(mdef, p));
        }

        public static Pointer<dict2pid_t> dict2pid_build(Pointer<bin_mdef_t> mdef, Pointer<dict_t> dictionary)
        {
            Pointer<dict2pid_t> returnVal;
            Pointer<Pointer<Pointer <ushort>>> rdiph_rc;
            Pointer<uint> ldiph;
            Pointer<uint> rdiph;
            Pointer<uint> single;
            int pronlen;
            int b, l, r, w, p;

            err.E_INFO("Building PID tables for dictionary\n");
            SphinxAssert.assert(mdef.IsNonNull);
            SphinxAssert.assert(dictionary.IsNonNull);

            returnVal = (Pointer<dict2pid_t>)ckd_alloc.ckd_calloc_struct<dict2pid_t>(1);
            returnVal.Deref.refcount = 1;
            returnVal.Deref.mdef = bin_mdef.bin_mdef_retain(mdef);
            returnVal.Deref.dict = dict.dict_retain(dictionary);
            err.E_INFO(string.Format("Allocating {0}^3 * {1} bytes ({2} KiB) for word-initial triphones\n",
                   mdef.Deref.n_ciphone, sizeof(ushort),
                   mdef.Deref.n_ciphone * mdef.Deref.n_ciphone * mdef.Deref.n_ciphone * sizeof(ushort) / 1024));
            returnVal.Deref.ldiph_lc = ckd_alloc.ckd_calloc_3d<ushort>((uint)mdef.Deref.n_ciphone, (uint)mdef.Deref.n_ciphone,
                                             (uint)mdef.Deref.n_ciphone);
            /* Only used internally to generate rssid */
            rdiph_rc = ckd_alloc.ckd_calloc_3d<ushort>((uint)mdef.Deref.n_ciphone, (uint)mdef.Deref.n_ciphone,
                                             (uint)mdef.Deref.n_ciphone);

            returnVal.Deref.lrdiph_rc = ckd_alloc.ckd_calloc_3d<ushort>((uint)mdef.Deref.n_ciphone,
                                                                       (uint)mdef.Deref.n_ciphone,
                                                                       (uint)mdef.Deref.n_ciphone);
            /* Actually could use memset for this, if s3types.BAD_S3SSID is guaranteed
             * to be 65535... */
            for (b = 0; b < mdef.Deref.n_ciphone; ++b)
            {
                for (r = 0; r < mdef.Deref.n_ciphone; ++r)
                {
                    for (l = 0; l < mdef.Deref.n_ciphone; ++l)
                    {
                        returnVal.Deref.ldiph_lc[b][r].Set(l, s3types.BAD_S3SSID);
                        returnVal.Deref.lrdiph_rc[b][l].Set(r, s3types.BAD_S3SSID);
                        rdiph_rc[b][l].Set(r, s3types.BAD_S3SSID);
                    }
                }
            }

            /* Track which diphones / ciphones have been seen. */
            ldiph = bitvec.bitvec_alloc(mdef.Deref.n_ciphone * mdef.Deref.n_ciphone);
            rdiph = bitvec.bitvec_alloc(mdef.Deref.n_ciphone * mdef.Deref.n_ciphone);
            single = bitvec.bitvec_alloc(mdef.Deref.n_ciphone);

            for (w = 0; w < dict.dict_size(returnVal.Deref.dict); w++)
            {
                pronlen = dict.dict_pronlen(dictionary, w);

                if (pronlen >= 2)
                {
                    b = dict.dict_first_phone(dictionary, w);
                    r = dict.dict_second_phone(dictionary, w);
                    /* Populate ldiph_lc */
                    if (bitvec.bitvec_is_clear(ldiph, b * mdef.Deref.n_ciphone + r) != 0)
                    {
                        /* Mark this diphone as done */
                        bitvec.bitvec_set(ldiph, b * mdef.Deref.n_ciphone + r);

                        /* Record all possible ssids for b(?,r) */
                        for (l = 0; l < bin_mdef.bin_mdef_n_ciphone(mdef); l++)
                        {
                            p = bin_mdef.bin_mdef_phone_id_nearest(mdef, (short)b,
                                                      (short)l, (short)r,
                                                      word_posn_t.WORD_POSN_BEGIN);
                            returnVal.Deref.ldiph_lc[b][r].Set(l, checked((ushort)bin_mdef.bin_mdef_pid2ssid(mdef, p)));
                        }
                    }


                    /* Populate rdiph_rc */
                    l = dict.dict_second_last_phone(dictionary, w);
                    b = dict.dict_last_phone(dictionary, w);
                    if (bitvec.bitvec_is_clear(rdiph, b * mdef.Deref.n_ciphone + l) != 0)
                    {
                        /* Mark this diphone as done */
                        bitvec.bitvec_set(rdiph, b * mdef.Deref.n_ciphone + l);

                        for (r = 0; r < bin_mdef.bin_mdef_n_ciphone(mdef); r++)
                        {
                            p = bin_mdef.bin_mdef_phone_id_nearest(mdef, (short)b,
                                                      (short)l, (short)r,
                                                      word_posn_t.WORD_POSN_END);
                            rdiph_rc[b][l].Set(r, checked((ushort)bin_mdef.bin_mdef_pid2ssid(mdef, p)));
                        }
                    }
                }
                else if (pronlen == 1)
                {
                    b = dict.dict_pron(dictionary, w, 0);
                    err.E_DEBUG(string.Format("Building tables for single phone word {0} phone {1} = {2}\n",
                               cstring.FromCString(dict.dict_wordstr(dictionary, w)), b, cstring.FromCString(bin_mdef.bin_mdef_ciphone_str(mdef, b))));
                    /* Populate lrdiph_rc (and also ldiph_lc, rdiph_rc if needed) */
                    if (bitvec.bitvec_is_clear(single, b) != 0)
                    {
                        populate_lrdiph(returnVal, rdiph_rc, checked((short)b));
                        bitvec.bitvec_set(single, b);
                    }
                }
            }

            bitvec.bitvec_free(ldiph);
            bitvec.bitvec_free(rdiph);
            bitvec.bitvec_free(single);

            /* Try to compress rdiph_rc into rdiph_rc_compressed */
            compress_right_context_tree(returnVal, rdiph_rc);
            compress_left_right_context_tree(returnVal);

            ckd_alloc.ckd_free_3d(rdiph_rc);

            dict2pid_report(returnVal);
            return returnVal;
        }

        public static Pointer<dict2pid_t> dict2pid_retain(Pointer<dict2pid_t> d2p)
        {
            ++d2p.Deref.refcount;
            return d2p;
        }

        public static int dict2pid_free(Pointer<dict2pid_t> d2p)
        {
            if (d2p.IsNull)
                return 0;
            if (--d2p.Deref.refcount > 0)
                return d2p.Deref.refcount;

            if (d2p.Deref.ldiph_lc.IsNonNull)
                ckd_alloc.ckd_free_3d(d2p.Deref.ldiph_lc);

            if (d2p.Deref.lrdiph_rc.IsNonNull)
                ckd_alloc.ckd_free_3d(d2p.Deref.lrdiph_rc);

            if (d2p.Deref.rssid.IsNonNull)
                free_compress_map(d2p.Deref.rssid, bin_mdef.bin_mdef_n_ciphone(d2p.Deref.mdef));

            if (d2p.Deref.lrssid.IsNonNull)
                free_compress_map(d2p.Deref.lrssid, bin_mdef.bin_mdef_n_ciphone(d2p.Deref.mdef));

            bin_mdef.bin_mdef_free(d2p.Deref.mdef);
            dict.dict_free(d2p.Deref.dict);
            ckd_alloc.ckd_free(d2p);
            return 0;
        }

        public static void dict2pid_report(Pointer<dict2pid_t> d2p)
        {
        }
    }
}
