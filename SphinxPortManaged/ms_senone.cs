using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ms_senone
    {
        public static readonly Pointer<byte> MIXW_PARAM_VERSION = cstring.ToCString("1.0");
        public static readonly Pointer<byte> SPDEF_PARAM_VERSION = cstring.ToCString("1.2");

        public static int senone_mgau_map_read(Pointer<senone_t> s, Pointer<byte> file_name)
        {
            FILE fp;
            int byteswap, chksum_present, n_gauden_present;
            BoxedValue<uint> chksum = new BoxedValue<uint>();
            int i;
            Pointer<Pointer<byte>> argname;
            Pointer<Pointer<byte>> argval;
            object ptr;
            double v;

            err.E_INFO(string.Format("Reading senone gauden-codebook map file: {0}\n", cstring.FromCString(file_name)));

            if ((fp = FILE.fopen(file_name, "rb")) == null)
                err.E_FATAL_SYSTEM(string.Format("Failed to open map file '{0}' for reading", cstring.FromCString(file_name)));

            /* Read header, including argument-value info and 32-bit byteorder magic */
            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(fp, boxed_argname, boxed_argval, out byteswap) < 0)
                err.E_FATAL(string.Format("Failed to read header from file '{0}'\n", cstring.FromCString(file_name)));
            argname = boxed_argname.Val;
            argval = boxed_argval.Val;
            
            /* Parse argument-value list */
            chksum_present = 0;
            n_gauden_present = 0;
            for (i = 0; argname[i].IsNonNull; i++)
            {
                if (cstring.strcmp(argname[i], cstring.ToCString("version")) == 0)
                {
                    if (cstring.strcmp(argval[i], SPDEF_PARAM_VERSION) != 0)
                    {
                        err.E_WARN(string.Format("Version mismatch({0}): {1}, expecting {2}\n", file_name, argval[i], SPDEF_PARAM_VERSION));
                    }

                    /* HACK!! Convert version# to float32 and take appropriate action */
                    if (stdio.sscanf_f(argval[i], out v) != 1)
                        err.E_FATAL(string.Format("{0}: Bad version no. string: {1}\n", cstring.FromCString(file_name),
                        cstring.FromCString(argval[i])));

                    n_gauden_present = (v > 1.1) ? 1 : 0;
                }
                else if (cstring.strcmp(argname[i], cstring.ToCString("chksum0")) == 0)
                {
                    chksum_present = 1; /* Ignore the associated value */
                }
            }
            bio.bio_hdrarg_free(argname, argval);
            argname = argval = PointerHelpers.NULL<Pointer<byte>>();

            chksum.Val = 0;

            /* Read #gauden (if version matches) */
            byte[] read_single_buf = new byte[4];
            Pointer<byte> read_byte_ptr = new Pointer<byte>(read_single_buf);
            Pointer<uint> read_uint_ptr = read_byte_ptr.ReinterpretCast<uint>();

            if (n_gauden_present != 0)
            {
                err.E_INFO(string.Format("Reading number of codebooks from {0}\n", cstring.FromCString(file_name)));
                if (bio.bio_fread(read_byte_ptr, 4, 1, fp, byteswap, chksum) != 1)
                    err.E_FATAL(string.Format("fread({0}) (#gauden) failed\n", cstring.FromCString(file_name)));
                s.Deref.n_gauden = read_uint_ptr[0];
            }

            /* Read 1d array data */
            BoxedValue<uint> n_el = new BoxedValue<uint>(s.Deref.n_sen);
            BoxedValue<Pointer<byte>> boxed_ptr = new BoxedValue<Pointer<byte>>();
            if (bio.bio_fread_1d(boxed_ptr, 4, n_el, fp, byteswap, chksum) < 0)
            {
                err.E_FATAL(string.Format("bio_fread_1d({0}) failed\n", cstring.FromCString(file_name)));
            }
            s.Deref.n_sen = n_el.Val;
            Pointer<uint> data_as_uint = boxed_ptr.Val.ReinterpretCast<uint>();
            Pointer<uint> native_uint_data = PointerHelpers.Malloc<uint>(s.Deref.n_sen);
            data_as_uint.MemCopyTo(native_uint_data, (int)s.Deref.n_sen);
            s.Deref.mgau = native_uint_data;

            err.E_INFO(string.Format("Mapping {0} senones to {1} codebooks\n", s.Deref.n_sen, s.Deref.n_gauden));

            /* Infer n_gauden if not present in this version */
            if (n_gauden_present == 0)
            {
                s.Deref.n_gauden = 1;
                for (i = 0; i < s.Deref.n_sen; i++)
                    if (s.Deref.mgau[i] >= s.Deref.n_gauden)
                        s.Deref.n_gauden = s.Deref.mgau[i] + 1;
            }

            if (chksum_present != 0)
                bio.bio_verify_chksum(fp, byteswap, chksum.Val);

            if (fp.fread(read_byte_ptr, 1, 1) == 1)
                err.E_FATAL(string.Format("More data than expected in {0}: {1}\n", cstring.FromCString(file_name), read_single_buf[0]));

            fp.fclose();

            err.E_INFO(string.Format("Read {0}->{1} senone-codebook mappings\n", s.Deref.n_sen,
                   s.Deref.n_gauden));

            return 1;
        }


        public static int senone_mixw_read(Pointer<senone_t> s, Pointer<byte> file_name, Pointer<logmath_t> lmath)
        {
            FILE fp;
            int byteswap, chksum_present;
            BoxedValue<uint> chksum = new BoxedValue<uint>();
            Pointer<float> pdf;
            int i, f, c, p, n_err;
            Pointer<Pointer<byte>> argname;
            Pointer<Pointer<byte>> argval;

            err.E_INFO(string.Format("Reading senone mixture weights: {0}\n", cstring.FromCString(file_name)));

            if ((fp = FILE.fopen(file_name, "rb")) == null)
                err.E_FATAL_SYSTEM(string.Format("Failed to open mixture weights file '{0}' for reading", cstring.FromCString(file_name)));

            /* Read header, including argument-value info and 32-bit byteorder magic */
            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(fp, boxed_argname, boxed_argval, out byteswap) < 0)
            {
                err.E_FATAL(string.Format("Failed to read header from file '{0}'\n", cstring.FromCString(file_name)));
            }
            argname = boxed_argname.Val;
            argval = boxed_argval.Val;

            /* Parse argument-value list */
            chksum_present = 0;
            for (i = 0; argname[i].IsNonNull; i++)
            {
                if (cstring.strcmp(argname[i], cstring.ToCString("version")) == 0)
                {
                    if (cstring.strcmp(argval[i], MIXW_PARAM_VERSION) != 0)
                        err.E_WARN(string.Format("Version mismatch({0}): {1}, expecting {2}\n",
                               cstring.FromCString(file_name), cstring.FromCString(argval[i]), cstring.FromCString(MIXW_PARAM_VERSION)));
                }
                else if (cstring.strcmp(argname[i], cstring.ToCString("chksum0")) == 0)
                {
                    chksum_present = 1; /* Ignore the associated value */
                }
            }
            bio.bio_hdrarg_free(argname, argval);
            argname = argval = PointerHelpers.NULL<Pointer<byte>>();

            chksum.Val = 0;

            /* Read #senones, #features, #codewords, arraysize */
            byte[] temp_read_buf = new byte[4];
            Pointer<byte> temp_read_ptr_byte = new Pointer<byte>(temp_read_buf);
            Pointer<uint> temp_read_ptr_uint = temp_read_ptr_byte.ReinterpretCast<uint>();

            if ((bio.bio_fread(temp_read_ptr_byte, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            s.Deref.n_sen = temp_read_ptr_uint[0];

            if ((bio.bio_fread(temp_read_ptr_byte, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            s.Deref.n_feat = temp_read_ptr_uint[0];

            if ((bio.bio_fread(temp_read_ptr_byte, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            s.Deref.n_cw = temp_read_ptr_uint[0];

            if ((bio.bio_fread(temp_read_ptr_byte, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("bio_fread({0}) (arraysize) failed\n", cstring.FromCString(file_name)));
            }
            i = (int)temp_read_ptr_uint[0];
            
            if (i != s.Deref.n_sen * s.Deref.n_feat * s.Deref.n_cw)
            {
                err.E_FATAL
                    (string.Format("{0}: #float32s({1}) doesn't match dimensions: {2} x {3} x {4}\n",
                     cstring.FromCString(file_name), i, s.Deref.n_sen, s.Deref.n_feat, s.Deref.n_cw));
            }

            /*
             * Compute #LSB bits to be dropped to represent mixwfloor with 8 bits.
             * All PDF values will be truncated (in the LSB positions) by these many bits.
             */
            if ((s.Deref.mixwfloor <= 0.0) || (s.Deref.mixwfloor >= 1.0))
                err.E_FATAL(string.Format("mixwfloor ({0}) not in range (0, 1)\n", s.Deref.mixwfloor));

            /* Use a fixed shift for compatibility with everything else. */
            err.E_INFO(string.Format("Truncating senone logs3(pdf) values by {0} bits\n", hmm.SENSCR_SHIFT));

            /*
             * Allocate memory for senone PDF data.  Organize normally or transposed depending on
             * s.Deref.n_gauden.
             */
            if (s.Deref.n_gauden > 1)
            {
                err.E_INFO("Not transposing mixture weights in memory\n");
                s.Deref.pdf = ckd_alloc.ckd_calloc_3d<byte>(s.Deref.n_sen, s.Deref.n_feat, s.Deref.n_cw);
            }
            else
            {
                err.E_INFO("Transposing mixture weights in memory\n");
                s.Deref.pdf = ckd_alloc.ckd_calloc_3d<byte>(s.Deref.n_feat, s.Deref.n_cw, s.Deref.n_sen);
            }

            /* Temporary structure to read in floats */
            pdf = ckd_alloc.ckd_calloc<float>(s.Deref.n_cw);
            Pointer<byte> bytebuf = PointerHelpers.Malloc<byte>(4 * s.Deref.n_cw);
            Pointer<float> floatbuf = bytebuf.ReinterpretCast<float>();

            /* Read senone probs data, normalize, floor, convert to logs3, truncate to 8 bits */
            n_err = 0;
            for (i = 0; i < s.Deref.n_sen; i++)
            {
                for (f = 0; f < s.Deref.n_feat; f++)
                {
                    if (bio.bio_fread(bytebuf, 4, (int)s.Deref.n_cw, fp, byteswap, chksum) != s.Deref.n_cw)
                    {
                        err.E_FATAL(string.Format("bio_fread({0}) (arraydata) failed\n", file_name));
                    }
                    floatbuf.MemCopyTo(pdf, (int)s.Deref.n_cw);

                    /* Normalize and floor */
                    if (vector.vector_sum_norm(pdf, checked((int)s.Deref.n_cw)) <= 0.0)
                        n_err++;
                    vector.vector_floor(pdf, checked((int)s.Deref.n_cw), s.Deref.mixwfloor);
                    vector.vector_sum_norm(pdf, checked((int)s.Deref.n_cw));

                    /* Convert to logs3, truncate to 8 bits, and store in s.Deref.pdf */
                    for (c = 0; c < s.Deref.n_cw; c++)
                    {
                        p = -(logmath.logmath_log(lmath, pdf[c]));
                        p += (1 << (hmm.SENSCR_SHIFT - 1)) - 1; /* Rounding before truncation */

                        if (s.Deref.n_gauden > 1)
                        {
                            s.Deref.pdf[i][f].Set(c, checked((byte)((p < (255 << hmm.SENSCR_SHIFT)) ? (p >> hmm.SENSCR_SHIFT) : 255)));
                        }
                        else
                        {
                            s.Deref.pdf[f][c].Set(i, checked((byte)((p < (255 << hmm.SENSCR_SHIFT)) ? (p >> hmm.SENSCR_SHIFT) : 255)));
                        }
                    }
                }
            }
            if (n_err > 0)
                err.E_WARN(string.Format("Weight normalization failed for %d mixture weights components\n", n_err));

            ckd_alloc.ckd_free(pdf);

            if (chksum_present != 0)
                bio.bio_verify_chksum(fp, byteswap, chksum.Val);

            if (fp.fread(temp_read_ptr_byte, 1, 1) == 1)
                err.E_FATAL(string.Format("More data than expected in {0}\n", cstring.FromCString(file_name)));

            fp.fclose();

            err.E_INFO
                (string.Format("Read mixture weights for {0} senones: {1} features x {2} codewords\n",
                 s.Deref.n_sen, s.Deref.n_feat, s.Deref.n_cw));

            return 1;
        }


        public static Pointer<senone_t>
        senone_init(Pointer<gauden_t> g, Pointer<byte> mixwfile, Pointer<byte> sen2mgau_map_file,
                float mixwfloor, Pointer<logmath_t> lmath, Pointer<bin_mdef_t> mdef)
        {
            Pointer<senone_t> s;
            int n = 0, i;

            s = ckd_alloc.ckd_calloc_struct<senone_t>(1);
            s.Deref.lmath = logmath.logmath_init(logmath.logmath_get_base(lmath), hmm.SENSCR_SHIFT, 1);
            s.Deref.mixwfloor = mixwfloor;

            s.Deref.n_gauden = checked((uint)g.Deref.n_mgau);
            if (sen2mgau_map_file.IsNonNull)
            {
                if (!(cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".semi.")) == 0
                      || cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".ptm.")) == 0
                      || cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".cont.")) == 0))
                {
                    senone_mgau_map_read(s, sen2mgau_map_file);
                    n = checked((int)s.Deref.n_sen);
                }
            }
            else
            {
                if (s.Deref.n_gauden == 1)
                    sen2mgau_map_file = cstring.ToCString(".semi.");
                else if (s.Deref.n_gauden == bin_mdef.bin_mdef_n_ciphone(mdef))
                    sen2mgau_map_file = cstring.ToCString(".ptm.");
                else
                    sen2mgau_map_file = cstring.ToCString(".cont.");
            }

            senone_mixw_read(s, mixwfile, lmath);

            if (cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".semi.")) == 0)
            {
                /* All-to-1 senones-codebook mapping */
                err.E_INFO(string.Format("Mapping all senones to one codebook\n"));
                s.Deref.mgau = ckd_alloc.ckd_calloc<uint>(s.Deref.n_sen);
            }
            else if (cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".ptm.")) == 0)
            {
                /* All-to-ciphone-id senones-codebook mapping */
                err.E_INFO(string.Format("Mapping senones to context-independent phone codebooks\n"));
                s.Deref.mgau = ckd_alloc.ckd_calloc<uint>(s.Deref.n_sen);
                for (i = 0; i < s.Deref.n_sen; i++)
                    s.Deref.mgau[i] = checked((uint)bin_mdef.bin_mdef_sen2cimap(mdef, i));
            }
            else if (cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".cont.")) == 0
                     || cstring.strcmp(sen2mgau_map_file, cstring.ToCString(".s3cont.")) == 0)
            {
                /* 1-to-1 senone-codebook mapping */
                err.E_INFO("Mapping senones to individual codebooks\n");
                if (s.Deref.n_sen <= 1)
                    err.E_FATAL(string.Format("#senone={0}; must be >1\n", s.Deref.n_sen));

                s.Deref.mgau = ckd_alloc.ckd_calloc<uint>(s.Deref.n_sen);
                for (i = 0; i < s.Deref.n_sen; i++)
                    s.Deref.mgau[i] = (uint)i;
                /* Not sure why this is here, it probably does nothing. */
                s.Deref.n_gauden = s.Deref.n_sen;
            }
            else
            {
                if (s.Deref.n_sen != n)
                    err.E_FATAL(string.Format("#senones inconsistent: {0} in {1}; {2} in {3}\n",
                            n, cstring.FromCString(sen2mgau_map_file), s.Deref.n_sen, cstring.FromCString(mixwfile)));
            }

            s.Deref.featscr = PointerHelpers.NULL<int>();
            return s;
        }

        public static void senone_free(Pointer<senone_t> s)
        {
            if (s.IsNull)
                return;
            if (s.Deref.pdf.IsNonNull)
                ckd_alloc.ckd_free_3d(s.Deref.pdf);
            if (s.Deref.mgau.IsNonNull)
                ckd_alloc.ckd_free(s.Deref.mgau);
            if (s.Deref.featscr.IsNonNull)
                ckd_alloc.ckd_free(s.Deref.featscr);
            logmath.logmath_free(s.Deref.lmath);
            ckd_alloc.ckd_free(s);
        }


        /*
         * Compute senone score for one senone.
         * NOTE:  Remember that senone PDF tables contain SCALED, NEGATED logs3 values.
         * NOTE:  Remember also that PDF data may be transposed or not depending on s.Deref.n_gauden.
         */
        public static int senone_eval(Pointer<senone_t> s, int id, Pointer<Pointer<gauden_dist_t>> dist, int n_top)
        {
            int scr;                  /* total senone score */
            int fden;                 /* Gaussian density */
            int fscr;                 /* senone score for one feature */
            int fwscr;                /* senone score for one feature, one codeword */
            int f, t;
            int top;
            Pointer<gauden_dist_t> fdist;

            SphinxAssert.assert((id >= 0) && (id < s.Deref.n_sen));
            SphinxAssert.assert((n_top > 0) && (n_top <= s.Deref.n_cw));

            scr = 0;

            for (f = 0; f < s.Deref.n_feat; f++)
            {
                fdist = dist[f];

                /* Top codeword for feature f */
                top = fden = ((int)fdist[0].dist + ((1 << hmm.SENSCR_SHIFT) - 1)) >> hmm.SENSCR_SHIFT;
                fscr = (s.Deref.n_gauden > 1)
                ? (fden + -s.Deref.pdf[id][f][fdist[0].id])  /* untransposed */
                : (fden + -s.Deref.pdf[f][fdist[0].id][id]); /* transposed */
                err.E_DEBUG(string.Format("fden[{0}][{1}] l+= {2} + {3} = {4}\n",
                            id, f, -(fscr - fden), -(fden - top), -(fscr - top)));
                /* Remaining of n_top codewords for feature f */
                for (t = 1; t < n_top; t++)
                {
                    fden = ((int)fdist[t].dist + ((1 << hmm.SENSCR_SHIFT) - 1)) >> hmm.SENSCR_SHIFT;
                    fwscr = (s.Deref.n_gauden > 1) ?
                        (fden + -s.Deref.pdf[id][f][fdist[t].id]) :
                        (fden + -s.Deref.pdf[f][fdist[t].id][id]);
                    fscr = logmath.logmath_add(s.Deref.lmath, fscr, fwscr);
                    err.E_DEBUG(string.Format("fden[{0}][{1}] l+= {2} + {3} = {4}\n",
                                id, f, -(fwscr - fden), -(fden - top), -(fscr - top)));
                }
                /* Senone scores are also scaled, negated logs3 values.  Hence
                 * we have to negate the stuff we calculated above. */
                scr -= fscr;
            }
            /* Downscale scores. */
            scr /= s.Deref.aw;

            /* Avoid overflowing int16 */
            if (scr > 32767)
                scr = 32767;
            if (scr < -32768)
                scr = -32768;
            return scr;
        }
    }
}
