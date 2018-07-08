using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class tmat
    {
        public static readonly Pointer<byte> TMAT_PARAM_VERSION = cstring.ToCString("1.0");

        public static int tmat_chk_uppertri(Pointer<tmat_t> tmat, Pointer<logmath_t> lmath)
        {
            int i, src, dst;

            /* Check that each tmat is upper-triangular */
            for (i = 0; i < tmat.Deref.n_tmat; i++)
            {
                for (dst = 0; dst < tmat.Deref.n_state; dst++)
                    for (src = dst + 1; src < tmat.Deref.n_state; src++)
                        if (tmat.Deref.tp[i][src][dst] < 255)
                        {
                            err.E_ERROR(string.Format("tmat[{0}][{1}][{2}] = {3}\n",
                                    i, src, dst, tmat.Deref.tp[i][src][dst]));
                            return -1;
                        }
            }

            return 0;
        }

        public static int tmat_chk_1skip(Pointer<tmat_t> tmat, Pointer<logmath_t> lmath)
        {
            int i, src, dst;

            for (i = 0; i < tmat.Deref.n_tmat; i++)
            {
                for (src = 0; src < tmat.Deref.n_state; src++)
                    for (dst = src + 3; dst <= tmat.Deref.n_state; dst++)
                        if (tmat.Deref.tp[i][src][dst] < 255)
                        {
                            err.E_ERROR(string.Format("tmat[{0}][{1}][{2}] = {3}\n",
                                    i, src, dst, tmat.Deref.tp[i][src][dst]));
                            return -1;
                        }
            }

            return 0;
        }

        public static Pointer<tmat_t> tmat_init(Pointer<byte> file_name, Pointer<logmath_t> lmath, double tpfloor, int breport)
        {
            int n_src, n_dst, n_tmat;
            FILE fp;
            int byteswap, chksum_present;
            BoxedValue<uint> chksum = new BoxedValue<uint>();
            Pointer<Pointer<float>> tp;
            int i, j, k, tp_per_tmat;
            Pointer<Pointer<byte>> argname;
            Pointer<Pointer<byte>> argval;
            Pointer<tmat_t> t;

            if (breport != 0)
            {
                err.E_INFO(string.Format("Reading HMM transition probability matrices: {0}\n",
                       cstring.FromCString(file_name)));
            }

            t = ckd_alloc.ckd_calloc_struct<tmat_t>(1);

            if ((fp = FILE.fopen(file_name, "rb")) == null)
                err.E_FATAL_SYSTEM(string.Format("Failed to open transition file '{0}' for reading", cstring.FromCString(file_name)));

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
                    if (cstring.strcmp(argval[i], TMAT_PARAM_VERSION) != 0)
                        err.E_WARN(string.Format("Version mismatch({0}): }1}, expecting {2}\n",
                               cstring.FromCString(file_name), cstring.FromCString(argval[i]), cstring.FromCString(TMAT_PARAM_VERSION)));
                }
                else if (cstring.strcmp(argname[i], cstring.ToCString("chksum0")) == 0)
                {
                    chksum_present = 1; /* Ignore the associated value */
                }
            }
            bio.bio_hdrarg_free(argname, argval);
            argname = argval = PointerHelpers.NULL<Pointer<byte>>();

            chksum.Val = 0;

            Pointer<byte> fread_buf = PointerHelpers.Malloc<byte>(4);
            Pointer<int> fread_buf_int = fread_buf.ReinterpretCast<int>();

            /* Read #tmat, #from-states, #to-states, arraysize */
            if ((bio.bio_fread(fread_buf, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("Failed to read header from '{0}'\n", cstring.FromCString(file_name)));
            }
            n_tmat = fread_buf_int.Deref;

            if ((bio.bio_fread(fread_buf, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("Failed to read header from '{0}'\n", cstring.FromCString(file_name)));
            }
            n_src = fread_buf_int.Deref;

            if ((bio.bio_fread(fread_buf, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("Failed to read header from '{0}'\n", cstring.FromCString(file_name)));
            }
            n_dst = fread_buf_int.Deref;

            if ((bio.bio_fread(fread_buf, 4, 1, fp, byteswap, chksum) != 1))
            {
                err.E_FATAL(string.Format("Failed to read header from '{0}'\n", cstring.FromCString(file_name)));
            }
            i = fread_buf_int.Deref;
            
            if (n_tmat >= short.MaxValue)
                err.E_FATAL(string.Format("{0}: Number of transition matrices ({1}) exceeds limit ({2})\n", cstring.FromCString(file_name),
                n_tmat, short.MaxValue));
            t.Deref.n_tmat = checked((short)n_tmat);

            if (n_dst != n_src + 1)
                err.E_FATAL(string.Format("{0}: Unsupported transition matrix. Number of source states ({1}) != number of target states ({2})-1\n", cstring.FromCString(file_name), n_src, n_dst));
            t.Deref.n_state = checked((short)n_src);

            if (i != t.Deref.n_tmat * n_src * n_dst)
            {
                err.E_FATAL(string.Format("{0}: Invalid transitions. Number of coefficients ({1}) doesn't match expected array dimension: {2} x {3} x {4}\n", cstring.FromCString(file_name), i, t.Deref.n_tmat, n_src, n_dst));
            }

            /* Allocate memory for tmat data */
            t.Deref.tp = ckd_alloc.ckd_calloc_3d<byte>((uint)t.Deref.n_tmat, (uint)n_src, (uint)n_dst);

            /* Temporary structure to read in the float data */
            tp = ckd_alloc.ckd_calloc_2d<float>((uint)n_src, (uint)n_dst);

            /* Read transition matrices, normalize and floor them, and convert to log domain */
            tp_per_tmat = n_src * n_dst;
            for (i = 0; i < t.Deref.n_tmat; i++)
            {
                byte[] rawData = new byte[tp_per_tmat * 4];
                Pointer<byte> rawData_ptr = new Pointer<byte>(rawData);
                if (bio.bio_fread(rawData_ptr, 4, tp_per_tmat, fp,
                              byteswap, chksum) != tp_per_tmat)
                {
                    err.E_FATAL(string.Format("Failed to read transition matrix {0} from '{1}'\n", i, cstring.FromCString(file_name)));
                }

                // LOGAN modified - need to convert byte array to float
                Pointer<float> tp_tmp = tp[0];
                for (int c = 0; c < tp_per_tmat; c++)
                {
                    tp_tmp[c] = BitConverter.ToSingle(rawData, c * 4);
                }

                /* Normalize and floor */
                for (j = 0; j < n_src; j++)
                {
                    if (vector.vector_sum_norm(tp[j], n_dst) == 0.0)
                        err.E_WARN(string.Format("Normalization failed for transition matrix {0} from state {1}\n",
                               i, j));
                    vector.vector_nz_floor(tp[j], n_dst, tpfloor);
                    vector.vector_sum_norm(tp[j], n_dst);

                    /* Convert to logs3. */
                    for (k = 0; k < n_dst; k++)
                    {
                        int ltp;
                        /* Log and quantize them. */
                        ltp = -logmath.logmath_log(lmath, tp[j][k]) >> hmm.SENSCR_SHIFT;
                        if (ltp > 255) ltp = 255;
                        t.Deref.tp[i][j].Set(k, (byte)ltp);
                    }
                }
            }

            ckd_alloc.ckd_free_2d(tp);

            if (chksum_present != 0)
                bio.bio_verify_chksum(fp, byteswap, chksum.Val);

            if (fp.fread(fread_buf, 1, 1) == 1)
                err.E_ERROR("Non-empty file beyond end of data\n");

            fp.fclose();

            if (tmat_chk_uppertri(t, lmath) < 0)
                err.E_FATAL("Tmat not upper triangular\n");
            if (tmat_chk_1skip(t, lmath) < 0)
                err.E_FATAL("Topology not Left-to-Right or Bakis\n");

            return t;
        }

        public static void tmat_free(Pointer<tmat_t> t)
        {
            if (t.IsNonNull)
            {
                if (t.Deref.tp.IsNonNull)
                    ckd_alloc.ckd_free_3d(t.Deref.tp);
                ckd_alloc.ckd_free(t);
            }
        }
    }
}
