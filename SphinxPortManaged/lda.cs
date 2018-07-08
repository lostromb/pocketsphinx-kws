using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class lda
    {
        public static readonly Pointer<byte> MATRIX_FILE_VERSION = cstring.ToCString("0.1");

        public static int feat_read_lda(Pointer<feat_t> feat, Pointer<byte> ldafile, int dim)
        {
            FILE fh;
            int byteswap;
            uint chksum, i, m, n;
            Pointer<Pointer<byte>> argname;
            Pointer<Pointer<byte>> argval;

            SphinxAssert.assert(feat.IsNonNull);
            if (feat.Deref.n_stream != 1) {
                err.E_ERROR(string.Format("LDA incompatible with multi-stream features (n_stream = {0})\n",
                        feat.Deref.n_stream));
                return -1;
            }

            if ((fh = FILE.fopen(ldafile, "rb")) == null) {
                err.E_ERROR_SYSTEM(string.Format("Failed to open transform file '{0}' for reading", cstring.FromCString(ldafile)));
                return -1;
            }

            BoxedValue<Pointer<Pointer<byte>>> boxed_argname = new BoxedValue<Pointer<Pointer<byte>>>();
            BoxedValue<Pointer<Pointer<byte>>> boxed_argval = new BoxedValue<Pointer<Pointer<byte>>>();
            if (bio.bio_readhdr(fh, boxed_argname, boxed_argval, out byteswap) < 0) {
                err.E_ERROR(string.Format("Failed to read header from transform file '{0}'\n", cstring.FromCString(ldafile)));
                fh.fclose();
                return -1;
            }
            argname = boxed_argname.Val;
            argval = boxed_argval.Val;

            for (i = 0; argname[i].IsNonNull; i++) {
                if (cstring.strcmp(argname[i], cstring.ToCString("version")) == 0) {
                    if (cstring.strcmp(argval[i], MATRIX_FILE_VERSION) != 0)
                        err.E_WARN(string.Format("{0}: Version mismatch: {1}, expecting {2}\n",
                               cstring.FromCString(ldafile),
                               cstring.FromCString(argval[i]),
                               cstring.FromCString(MATRIX_FILE_VERSION)));
                }
            }

            bio.bio_hdrarg_free(argname, argval);
            argname = argval = PointerHelpers.NULL<Pointer<byte>>();

            chksum = 0;

            if (feat.Deref.lda.IsNonNull)
                ckd_alloc.ckd_free_3d(feat.Deref.lda);

            {
                /* Use a temporary variable to avoid strict-aliasing problems. */
                BoxedValue<Pointer<Pointer<Pointer<float>>>> outlda = new BoxedValue<Pointer<Pointer<Pointer<float>>>>();
                BoxedValue<uint> boxed_n_lda = new BoxedValue<uint>();
                BoxedValue<uint> boxed_m = new BoxedValue<uint>();
                BoxedValue<uint> boxed_n = new BoxedValue<uint>();
                BoxedValue<uint> boxed_checksum = new BoxedValue<uint>();
                if (bio.bio_fread_3d(outlda,
                                 boxed_n_lda, boxed_m, boxed_n,
                                 fh, (uint)byteswap, boxed_checksum) < 0) {
                    err.E_ERROR_SYSTEM(string.Format("{0}: bio_fread_3d(lda) failed\n", cstring.FromCString(ldafile)));
                    fh.fclose();
                    return -1;
                }

                feat.Deref.n_lda = boxed_n_lda.Val;
                m = boxed_m.Val;
                n = boxed_n.Val;
                feat.Deref.lda = outlda.Val;
                chksum = boxed_checksum.Val;
            }
            fh.fclose();
    
            /* Note that SphinxTrain stores the eigenvectors as row vectors. */
            if (n != feat.Deref.stream_len[0])

            err.E_FATAL(string.Format("LDA matrix dimension {0} doesn't match feature stream size %{1}\n", n, feat.Deref.stream_len[0]));
    
            /* Override dim from file if it is 0 or greater than m. */
            if (dim > m || dim <= 0) {
                dim = (int)m;
            }

            feat.Deref.out_dim = (uint)dim;

            return 0;
        }

        public static void
        feat_lda_transform(Pointer<feat_t> fcb, Pointer<Pointer<Pointer<float>>> inout_feat, uint nfr)
        {
            Pointer<float> tmp;
            uint i, j, k;

            tmp = ckd_alloc.ckd_calloc<float>(fcb.Deref.stream_len[0]);
            for (i = 0; i < nfr; ++i)
            {
                /* Do the matrix multiplication inline here since fcb.Deref.lda
                 * is transposed (eigenvectors in rows not columns). */
                /* FIXME: In the future we ought to use the BLAS. */
                tmp.MemSet(0, fcb.Deref.stream_len[0]);
                for (j = 0; j < feat.feat_dimension(fcb); ++j)
                {
                    for (k = 0; k < fcb.Deref.stream_len[0]; ++k)
                    {
                        tmp[j] += (inout_feat[i][0][k] * fcb.Deref.lda[0][j][k]);
                    }
                }

                tmp.MemCopyTo(inout_feat[i][0], (int)fcb.Deref.stream_len[0]);
            }

            ckd_alloc.ckd_free(tmp);
        }
    }
}
