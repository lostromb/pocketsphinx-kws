using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class bio
    {
        public const int BYTE_ORDER_MAGIC = 0x11223344;
        public const int BIO_HDRARG_MAX = 32;
        public static readonly Pointer<byte> END_COMMENT = cstring.ToCString("*end_comment*\n");

        public static void bcomment_read(FILE fp)
        {
            Pointer<byte> iline = PointerHelpers.Malloc<byte>(16384);

            while (fp.fgets(iline, 16384).IsNonNull)
            {
                if (cstring.strcmp(iline, END_COMMENT) == 0)
                    return;
            }

            err.E_FATAL(string.Format("Missing {0} marker\n", cstring.FromCString(END_COMMENT)));
        }

        public static int swap_check(FILE fp)
        {
            Pointer<byte> magic_buf = PointerHelpers.Malloc<byte>(4);
            Pointer<uint> magic = magic_buf.ReinterpretCast<uint>();

            if (fp.fread(magic_buf, 4, 1) != 1)
            {
                err.E_ERROR("Cannot read BYTEORDER MAGIC NO.\n");
                return -1;
            }

            if (+magic != BYTE_ORDER_MAGIC)
            {
                /* either need to swap or got bogus magic number */
                byteorder.SWAP_INT32(magic);

                if (+magic == BYTE_ORDER_MAGIC)
                    return 1;

                byteorder.SWAP_INT32(magic);
                err.E_ERROR(string.Format("Bad BYTEORDER MAGIC NO: {0:x8}, expecting {1:x8}\n",
                        magic, BYTE_ORDER_MAGIC));
                return -1;
            }

            return 0;
        }


        public static void bio_hdrarg_free(Pointer<Pointer<byte>> argname, Pointer<Pointer<byte>> argval)
        {
            int i;

            if (argname.IsNull)
                return;

            for (i = 0; argname[i].IsNonNull; i++)
            {
                ckd_alloc.ckd_free(argname[i]);
                ckd_alloc.ckd_free(argval[i]);
            }
            ckd_alloc.ckd_free(argname);
            ckd_alloc.ckd_free(argval);
        }

        public static int bio_readhdr(FILE fp, BoxedValue<Pointer<Pointer<byte>>> argname, BoxedValue<Pointer<Pointer<byte>>> argval, out int swap)
        {
            Pointer<byte> line = PointerHelpers.Malloc<byte>(16384);
            Pointer<byte> word = PointerHelpers.Malloc<byte>(4096);
            int i, l;
            int lineno;

            argname.Val = ckd_alloc.ckd_calloc<Pointer<byte>>(BIO_HDRARG_MAX + 1);
            argval.Val = ckd_alloc.ckd_calloc<Pointer<byte>>(BIO_HDRARG_MAX);

            lineno = 0;
            if (fp.fgets(line, 16384).IsNull)
            {
                err.E_ERROR(string.Format("Premature EOF, line {0}\n", lineno));
                goto error_out;
            }
            lineno++;

            if ((line[0] == 's') && (line[1] == '3') && (line[2] == '\n'))
            {
                /* New format (post Dec-1996, including checksums); read argument-value pairs */
                for (i = 0; ;)
                {
                    if (fp.fgets(line, 16384).IsNull)
                    {
                        err.E_ERROR(string.Format("Premature EOF, line {0}\n", lineno));
                        goto error_out;
                    }
                    lineno++;

                    if (stdio.sscanf_s_n(line, word, out l) != 1)
                    {
                        err.E_ERROR(string.Format("Header format error, line {0}\n", lineno));
                        goto error_out;
                    }
                    if (cstring.strcmp(word, cstring.ToCString("endhdr")) == 0)
                        break;
                    if (word[0] == '#') /* Skip comments */
                        continue;

                    if (i >= BIO_HDRARG_MAX)
                    {
                        err.E_ERROR
                            (string.Format("Max arg-value limit({0}) exceeded; increase BIO_HDRARG_MAX\n",
                             BIO_HDRARG_MAX));
                        goto error_out;
                    }

                    argname.Val[i] = ckd_alloc.ckd_salloc(word);
                    if (stdio.sscanf_s(line + l, word) != 1)
                    {      /* Multi-word values not allowed */
                        err.E_ERROR(string.Format("Header format error, line {0}\n", lineno));
                        goto error_out;
                    }
                    argval.Val[i] = ckd_alloc.ckd_salloc(word);
                    i++;
                }
            }
            else
            {
                /* Old format (without checksums); the first entry must be the version# */
                if (stdio.sscanf_s(line, word) != 1)
                {
                    err.E_ERROR(string.Format("Header format error, line {0}\n", lineno));
                    goto error_out;
                }

                argname.Val[0] = ckd_alloc.ckd_salloc(cstring.ToCString("version"));
                argval.Val[0] = ckd_alloc.ckd_salloc(word);
                i = 1;

                bcomment_read(fp);
            }
            argname.Val[i] = PointerHelpers.NULL<byte>();

            if ((swap = swap_check(fp)) < 0)
            {
                err.E_ERROR("swap_check failed\n");
                goto error_out;
            }

            return 0;
            error_out:
            bio_hdrarg_free(argname.Val, argval.Val);
            argname.Val = argval.Val = PointerHelpers.NULL<Pointer<byte>>();
            swap = 0;
            return -1;
        }

        public static uint chksum_accum(Pointer<byte> buf, int el_sz, int n_el, uint sum)
        {
            int i;
            Pointer<byte> i8;
            Pointer<ushort> i16;
            Pointer<uint> i32;

            switch (el_sz)
            {
                case 1:
                    i8 = buf.ReinterpretCast<byte>();
                    for (i = 0; i < n_el; i++)
                        sum = (sum << 5 | sum >> 27) + i8[i];
                    break;
                case 2:
                    i16 = buf.ReinterpretCast<ushort>();
                    for (i = 0; i < n_el; i++)
                        sum = (sum << 10 | sum >> 22) + i16[i];
                    break;
                case 4:
                    i32 = buf.ReinterpretCast<uint>();
                    for (i = 0; i < n_el; i++)
                        sum = (sum << 20 | sum >> 12) + i32[i];
                    break;
                default:
                    err.E_FATAL(string.Format("Unsupported elemsize for checksum: {0}\n", el_sz));
                    break;
            }

            return sum;
        }
        public static void swap_buf(Pointer<byte> buf, int el_sz, int n_el)
        {
            int i;
            Pointer<ushort> buf16;
            Pointer<uint> buf32;

            switch (el_sz)
            {
                case 1:
                    break;
                case 2:
                    buf16 = buf.ReinterpretCast<ushort>();
                    for (i = 0; i < n_el; i++)
                        byteorder.SWAP_INT16(buf16 + i);
                    break;
                case 4:
                    buf32 = buf.ReinterpretCast<uint>();
                    for (i = 0; i < n_el; i++)
                        byteorder.SWAP_INT32(buf32 + i);
                    break;
                default:
                    err.E_FATAL(string.Format("Unsupported elemsize for byteswapping: {0}\n", el_sz));
                    break;
            }
        }

        public static int bio_fread(Pointer<byte> buf, int el_sz, int n_el, FILE fp, int swap, BoxedValue<uint> chksum)
        {
            if (fp.fread(buf, (uint)el_sz, (uint)n_el) != (uint)n_el)
                return -1;

            if (swap != 0)
                swap_buf(buf, el_sz, n_el);

            if (chksum != null)
                chksum.Val = chksum_accum(buf, el_sz, n_el, chksum.Val);

            return n_el;
        }

        public static int bio_fread_1d(BoxedValue<Pointer<byte>> buf, uint el_sz, BoxedValue<uint> n_el, FILE fp, int sw, BoxedValue<uint> ck)
        {
            /* Read 1-d array size */
            Pointer<byte> array_size = PointerHelpers.Malloc<byte>(4);
            if (bio_fread(array_size, 4, 1, fp, sw, ck) != 1)
                err.E_FATAL("fread(arraysize) failed\n");

            n_el.Val = array_size.ReinterpretCast<uint>().Deref;
            if (n_el.Val <= 0)
                err.E_FATAL(string.Format("Bad arraysize: {0}\n", n_el.Val));

            /* Allocate memory for array data */
            buf.Val = ckd_alloc.ckd_calloc<byte>(n_el.Val * el_sz);

            /* Read array data */
            if (bio_fread(buf.Val, (int)el_sz, (int)n_el.Val, fp, sw, ck) != n_el.Val)
                err.E_FATAL("fread(arraydata) failed\n");

            return (int)(n_el.Val);
        }

        public static int bio_fread_3d(BoxedValue<Pointer<Pointer<Pointer<float>>>> arr,
                     BoxedValue<uint> d1,
                     BoxedValue<uint> d2,
                     BoxedValue<uint> d3,
                     FILE fp,
                     uint swap,
                     BoxedValue<uint> chksum)
        {
            MemoryBlock<byte> length_buf = new MemoryBlock<byte>(12);
            Pointer<byte> length = new Pointer<byte>(new BasicMemoryBlockAccess<byte>(length_buf), 0);
            Pointer<uint> l_d1 = new Pointer<uint>(new UpcastingMemoryBlockAccess<uint>(length_buf), 0);
            Pointer<uint> l_d2 = new Pointer<uint>(new UpcastingMemoryBlockAccess<uint>(length_buf), 4);
            Pointer<uint> l_d3 = new Pointer<uint>(new UpcastingMemoryBlockAccess<uint>(length_buf), 8);
            uint n = 0;
            Pointer<byte> raw = PointerHelpers.NULL<byte>();
            uint ret;

            ret = (uint)bio_fread(length.Point(0), 4, 1, fp, (int)swap, chksum);
            if (ret != 1)
            {
                if (ret == 0)
                {
                    err.E_ERROR_SYSTEM("Unable to read complete data");
                }
                else
                {
                    err.E_ERROR_SYSTEM("OS error in bio_fread_3d");
                }
                return -1;
            }
            ret = (uint)bio_fread(length.Point(4), 4, 1, fp, (int)swap, chksum);
            if (ret != 1)
            {
                if (ret == 0)
                {
                    err.E_ERROR_SYSTEM("Unable to read complete data");
                }
                else
                {
                    err.E_ERROR_SYSTEM("OS error in bio_fread_3d");
                }
                return -1;
            }
            ret = (uint)bio_fread(length.Point(8), 4, 1, fp, (int)swap, chksum);
            if (ret != 1)
            {
                if (ret == 0)
                {
                    err.E_ERROR_SYSTEM("Unable to read complete data");
                }
                else
                {
                    err.E_ERROR_SYSTEM("OS error in bio_fread_3d");
                }
                return -1;
            }

            BoxedValue<Pointer<byte>> boxed_raw = new BoxedValue<Pointer<byte>>(raw);
            BoxedValue<uint> boxed_n = new BoxedValue<uint>(n);
            if (bio_fread_1d(boxed_raw, 4, boxed_n, fp, (int)swap, chksum) != n)
            {
                return -1;
            }
            n = boxed_n.Val;
            raw = boxed_raw.Val;

            SphinxAssert.assert(n == +l_d1 * +l_d2 * +l_d3);

            // LOGAN changed
            // Convert byte data to float
            Pointer<float> float_upcast_buf = raw.ReinterpretCast<float>();
            Pointer<float> float_copy_buf = PointerHelpers.Malloc<float>(n);
            float_upcast_buf.MemCopyTo(float_copy_buf, (int)n);

            arr.Val = ckd_alloc.ckd_alloc_3d_ptr<float>(+l_d1, +l_d2, +l_d3, float_copy_buf);
            d1.Val = +l_d1;
            d2.Val = +l_d2;
            d3.Val = +l_d3;

            return (int)n;
        }

        public static void bio_verify_chksum(FILE fp, int byteswap, uint chksum)
        {
            Pointer<byte> file_chksum_array = PointerHelpers.Malloc<byte>(4);
            Pointer<uint> file_chksum = file_chksum_array.ReinterpretCast<uint>();

            if (fp.fread(file_chksum_array, 4, 1) != 1)
                err.E_FATAL("fread(chksum) failed\n");
            
            if (byteswap != 0)
                byteorder.SWAP_INT32(file_chksum);
            if (+file_chksum != chksum)
                err.E_FATAL
                    (string.Format("Checksum error; file-checksum {0:x8}, computed {1:x8}\n", file_chksum, chksum));
        }
    }
}