using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class bin_mdef
    {
        public const ushort BAD_SSID = 0xFFFF;
        public const ushort BAD_SENID = 0xFFFF;
        public const int BIN_MDEF_FORMAT_VERSION = 1;
        public const int BIN_MDEF_NATIVE_ENDIAN = 0x46444d42 /* 'BMDF' in little-endian order */;
        public const int BIN_MDEF_OTHER_ENDIAN = 0x424d4446 /* 'BMDF' in little-endian order */;

        public static int bin_mdef_is_fillerphone(Pointer<bin_mdef_t> m, int p)
        {
            return (((p) < (m).Deref.n_ciphone)
                                 ? (m).Deref.phone[p].info_ci_filler
                     : (m).Deref.phone[(m).Deref.phone[p].info_cd_ctx[0]].info_ci_filler);
        }

        public static int bin_mdef_is_ciphone(Pointer<bin_mdef_t> m, int p)
        {
            return ((p) < (m).Deref.n_ciphone) ? 1 : 0;
        }

        public static int bin_mdef_n_ciphone(Pointer<bin_mdef_t> m)
        {
            return (m).Deref.n_ciphone;
        }

        public static int bin_mdef_n_phone(Pointer<bin_mdef_t> m)
        {
            return ((m).Deref.n_phone);
        }

        public static int bin_mdef_n_sseq(Pointer<bin_mdef_t> m)
        {
            return ((m).Deref.n_sseq);
        }

        public static int bin_mdef_n_emit_state(Pointer<bin_mdef_t> m)
        {
            return ((m).Deref.n_emit_state);
        }

        public static int bin_mdef_n_emit_state_phone(Pointer<bin_mdef_t> m, int p)
        {
            return ((m).Deref.n_emit_state != 0 ? (m).Deref.n_emit_state
					          : (m).Deref.sseq_len[(m).Deref.phone[p].ssid]);
        }

        public static int bin_mdef_n_sen(Pointer<bin_mdef_t> m)
        {
            return ((m).Deref.n_sen);
        }

        public static int bin_mdef_n_tmat(Pointer<bin_mdef_t> m)
        {
            return ((m).Deref.n_tmat);
        }

        public static int bin_mdef_pid2ssid(Pointer<bin_mdef_t> m, int p)
        {
            return ((m).Deref.phone[p].ssid);
        }

        public static int bin_mdef_pid2tmatid(Pointer<bin_mdef_t> m, int p)
        {
            return ((m).Deref.phone[p].tmat);
        }

        public static int bin_mdef_silphone(Pointer<bin_mdef_t> m)
        {
            return ((m).Deref.sil);
        }

        public static int bin_mdef_sen2cimap(Pointer<bin_mdef_t> m, int s)
        {
            return ((m).Deref.sen2cimap[s]);
        }

        public static ushort bin_mdef_sseq2sen(Pointer<bin_mdef_t> m, int ss, int pos)
        {
            return ((m).Deref.sseq[ss][pos]);
        }

        public static int bin_mdef_pid2ci(Pointer<bin_mdef_t> m, int p)
        {
            return (((p) < (m).Deref.n_ciphone) ? (p) : (m).Deref.phone[p].info_cd_ctx[0]);
        }
        
        public static Pointer<byte> bin_mdef_ciphone_str(Pointer<bin_mdef_t> m, int ci)
        {
            SphinxAssert.assert(m.IsNonNull);
            SphinxAssert.assert(ci < m.Deref.n_ciphone);
            return m.Deref.ciname[ci];
        }
        
        public static Pointer<bin_mdef_t> bin_mdef_read_text(Pointer<cmd_ln_t> config, Pointer<byte> filename)
        {
            Pointer < bin_mdef_t> bmdef;
            Pointer < mdef_t > _mdef;
            int i, nodes, ci_idx, lc_idx, rc_idx;
            int nchars;

            if ((_mdef = mdef.mdef_init((Pointer<byte>) filename, 1)).IsNull)
                return PointerHelpers.NULL<bin_mdef_t>();

            /* Enforce some limits.  */
            if (_mdef.Deref.n_sen > BAD_SENID) {
                err.E_ERROR(string.Format("Number of senones exceeds limit: {0} > {1}\n",
                        _mdef.Deref.n_sen, BAD_SENID));
                mdef.mdef_free(_mdef);
                return PointerHelpers.NULL<bin_mdef_t>();
            }
            if (_mdef.Deref.n_sseq > BAD_SSID) {
                err.E_ERROR(string.Format("Number of senone sequences exceeds limit: {0} > {1}\n",
                        _mdef.Deref.n_sseq, BAD_SSID));
                mdef.mdef_free(_mdef);
                return PointerHelpers.NULL<bin_mdef_t>();
            }
            /* We use uint8 for ciphones */
            if (_mdef.Deref.n_ciphone > 255) {
                err.E_ERROR(string.Format("Number of phones exceeds limit: {0} > {1}\n",
                        _mdef.Deref.n_ciphone, 255));
                mdef.mdef_free(_mdef);
                return PointerHelpers.NULL<bin_mdef_t>();
            }

            bmdef = ckd_alloc.ckd_calloc_struct<bin_mdef_t>(1);
            bmdef.Deref.refcnt = 1;

            /* Easy stuff.  The mdef.c code has done the heavy lifting for us. */
            bmdef.Deref.n_ciphone = _mdef.Deref.n_ciphone;
            bmdef.Deref.n_phone = _mdef.Deref.n_phone;
            bmdef.Deref.n_emit_state = _mdef.Deref.n_emit_state;
            bmdef.Deref.n_ci_sen = _mdef.Deref.n_ci_sen;
            bmdef.Deref.n_sen = _mdef.Deref.n_sen;
            bmdef.Deref.n_tmat = _mdef.Deref.n_tmat;
            bmdef.Deref.n_sseq = _mdef.Deref.n_sseq;
            bmdef.Deref.sseq = _mdef.Deref.sseq;
            bmdef.Deref.cd2cisen = _mdef.Deref.cd2cisen;
            bmdef.Deref.sen2cimap = _mdef.Deref.sen2cimap;
            bmdef.Deref.n_ctx = 3;           /* Triphones only. */
            bmdef.Deref.sil = _mdef.Deref.sil;
            _mdef.Deref.sseq = PointerHelpers.NULL<Pointer<ushort>>();          /* We are taking over this one. */
            _mdef.Deref.cd2cisen = PointerHelpers.NULL<short>();      /* And this one. */
            _mdef.Deref.sen2cimap = PointerHelpers.NULL<short>();     /* And this one. */

            /* Get the phone names.  If they are not sorted
             * ASCII-betically then we are in a world of hurt and
             * therefore will simply refuse to continue. */
            bmdef.Deref.ciname = ckd_alloc.ckd_calloc<Pointer<byte>>(bmdef.Deref.n_ciphone);
            nchars = 0;
            for (i = 0; i<bmdef.Deref.n_ciphone; ++i)
                nchars += (int)cstring.strlen(_mdef.Deref.ciphone[i].name) + 1;
            bmdef.Deref.ciname[0] = ckd_alloc.ckd_calloc<byte>(nchars);
            cstring.strcpy(bmdef.Deref.ciname[0], _mdef.Deref.ciphone[0].name);
            for (i = 1; i<bmdef.Deref.n_ciphone; ++i) {
                bmdef.Deref.ciname[i] =
                    bmdef.Deref.ciname[i - 1] + cstring.strlen(bmdef.Deref.ciname[i - 1]) + 1;
                cstring.strcpy(bmdef.Deref.ciname[i], _mdef.Deref.ciphone[i].name);
                if (cstring.strcmp(bmdef.Deref.ciname[i - 1], bmdef.Deref.ciname[i]) > 0) {
                    /* FIXME: there should be a solution to this, actually. */
                    err.E_ERROR("Phone names are not in sorted order, sorry.");
                    bin_mdef_free(bmdef);
                    return PointerHelpers.NULL<bin_mdef_t>();
                }
            }

            /* Copy over phone information. */
            bmdef.Deref.phone = ckd_alloc.ckd_calloc_struct<mdef_entry_t>(bmdef.Deref.n_phone);
            for (i = 0; i<_mdef.Deref.n_phone; ++i) {
                bmdef.Deref.phone[i].ssid = _mdef.Deref.phone[i].ssid;
                bmdef.Deref.phone[i].tmat = _mdef.Deref.phone[i].tmat;
                if (i<bmdef.Deref.n_ciphone) {
                    bmdef.Deref.phone[i].info_ci_filler = checked((byte)_mdef.Deref.ciphone[i].filler);
                }
                else {
                    bmdef.Deref.phone[i].info_cd_wpos = checked((byte)_mdef.Deref.phone[i].wpos);
                    Pointer<byte> tmp = bmdef.Deref.phone[i].info_cd_ctx;
                    tmp[0] = checked((byte)_mdef.Deref.phone[i].ci);
                    tmp[1] = checked((byte)_mdef.Deref.phone[i].lc);
                    tmp[2] = checked((byte)_mdef.Deref.phone[i].rc);
                }
            }

            /* Walk the wpos_ci_lclist once to find the total number of
             * nodes and the starting locations for each level. */
            nodes = lc_idx = ci_idx = rc_idx = 0;
            for (i = 0; i<mdef.N_WORD_POSN; ++i) {
                int j;
                for (j = 0; j<_mdef.Deref.n_ciphone; ++j) {
                    Pointer<ph_lc_t> lc;

                    for (lc = _mdef.Deref.wpos_ci_lclist[i][j]; lc.IsNonNull; lc = lc.Deref.next) {
                        Pointer<ph_rc_t> rc;
                        for (rc = lc.Deref.rclist; rc.IsNonNull; rc = rc.Deref.next) {
                            ++nodes;    /* RC node */
                        }
                        ++nodes;        /* LC node */
                        ++rc_idx;       /* Start of RC nodes (after LC nodes) */
                    }
                    ++nodes;            /* CI node */
                    ++lc_idx;           /* Start of LC nodes (after CI nodes) */
                    ++rc_idx;           /* Start of RC nodes (after CI and LC nodes) */
                }
                ++nodes;                /* wpos node */
                ++ci_idx;               /* Start of CI nodes (after wpos nodes) */
                ++lc_idx;               /* Start of LC nodes (after CI nodes) */
                ++rc_idx;               /* STart of RC nodes (after wpos, CI, and LC nodes) */
            }
            err.E_INFO(string.Format("Allocating {0} * {1} bytes ({2} KiB) for CD tree\n",
                   nodes, 8, 
                   nodes * 8 / 1024));
            bmdef.Deref.n_cd_tree = nodes;
            bmdef.Deref.cd_tree = ckd_alloc.ckd_calloc_struct<cd_tree_t>(nodes);
            for (i = 0; i<mdef.N_WORD_POSN; ++i) {
                int j;

                bmdef.Deref.cd_tree[i].ctx = (short)i;
                bmdef.Deref.cd_tree[i].n_down = checked((short)_mdef.Deref.n_ciphone);
                bmdef.Deref.cd_tree[i].c_down = ci_idx;

                /* Now we can build the rest of the tree. */
                for (j = 0; j<_mdef.Deref.n_ciphone; ++j) {
                    Pointer<ph_lc_t> lc;

                    bmdef.Deref.cd_tree[ci_idx].ctx = (short)j;
                    bmdef.Deref.cd_tree[ci_idx].c_down = lc_idx;
                    for (lc = _mdef.Deref.wpos_ci_lclist[i][j]; lc.IsNonNull; lc = lc.Deref.next) {
                        Pointer < ph_rc_t> rc;

                        bmdef.Deref.cd_tree[lc_idx].ctx = lc.Deref.lc;
                        bmdef.Deref.cd_tree[lc_idx].c_down = rc_idx;
                        for (rc = lc.Deref.rclist; rc.IsNonNull; rc = rc.Deref.next) {
                            bmdef.Deref.cd_tree[rc_idx].ctx = rc.Deref.rc;
                            bmdef.Deref.cd_tree[rc_idx].n_down = 0;
                            bmdef.Deref.cd_tree[rc_idx].c_pid = rc.Deref.pid;

                            ++bmdef.Deref.cd_tree[lc_idx].n_down;
                            ++rc_idx;
                        }
                        /* If there are no triphones here,
                         * this is considered a leafnode, so
                         * set the pid to -1. */
                        if (bmdef.Deref.cd_tree[lc_idx].n_down == 0)
                            bmdef.Deref.cd_tree[lc_idx].c_pid = -1;

                        ++bmdef.Deref.cd_tree[ci_idx].n_down;
                        ++lc_idx;
                    }

                    /* As above, so below. */
                    if (bmdef.Deref.cd_tree[ci_idx].n_down == 0)
                        bmdef.Deref.cd_tree[ci_idx].c_pid = -1;

                    ++ci_idx;
                }
            }

            mdef.mdef_free(_mdef);

            bmdef.Deref.alloc_mode = alloc_mode.BIN_MDEF_FROM_TEXT;
            return bmdef;
        }

        public static Pointer<bin_mdef_t> bin_mdef_retain(Pointer<bin_mdef_t> m)
        {
            ++m.Deref.refcnt;
            return m;
        }

        public static int bin_mdef_free(Pointer<bin_mdef_t> m)
        {
            if (m.IsNull)
                return 0;
            if (--m.Deref.refcnt > 0)
                return m.Deref.refcnt;

            switch (m.Deref.alloc_mode)
            {
                case alloc_mode.BIN_MDEF_FROM_TEXT:
                    ckd_alloc.ckd_free(m.Deref.ciname[0]);
                    ckd_alloc.ckd_free(m.Deref.sseq[0]);
                    ckd_alloc.ckd_free(m.Deref.phone);
                    ckd_alloc.ckd_free(m.Deref.cd_tree);
                    break;
                case alloc_mode.BIN_MDEF_IN_MEMORY:
                    ckd_alloc.ckd_free(m.Deref.ciname[0]);
                    break;
                case alloc_mode.BIN_MDEF_ON_DISK:
                    break;
            }

            // LOGAN removed mmio
            //if (m.Deref.filemap)
            //    mmio_file_unmap(m.Deref.filemap);
            ckd_alloc.ckd_free(m.Deref.cd2cisen);
            ckd_alloc.ckd_free(m.Deref.sen2cimap);
            ckd_alloc.ckd_free(m.Deref.ciname);
            ckd_alloc.ckd_free(m.Deref.sseq);
            ckd_alloc.ckd_free(m);
            return 0;
        }

        public static int FREAD_SWAP32_CHK(Pointer<bin_mdef_t> m, FILE fh, int swap, out int dest, Pointer<byte> filename)
        {
            Pointer<byte> buf = PointerHelpers.Malloc<byte>(4);
            Pointer<int> buf_int = buf.ReinterpretCast<int>();
            if (fh.fread(buf, 4, 1) != 1)
            {
                dest = 0;
                fh.fclose();
                ckd_alloc.ckd_free(m);
                err.E_ERROR_SYSTEM(string.Format("Failed to read {0} from {1}\n", dest, cstring.FromCString(filename)));
                return -1;
            }

            dest = +buf_int;
            if (swap != 0) dest = byteorder.SWAP_INT32(dest);
            return 0;
        }

        /// <summary>
        /// Reads a cd_tree_t structure from FILE and stores it into the memory location referred to by dest
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="file"></param>
        private static void read_cdtree_t(Pointer<cd_tree_t> dest, FILE file, int swap)
        {
            byte[] buf = new byte[8];
            file.fread(new Pointer<byte>(buf), 1, 8);
            cd_tree_t val = new cd_tree_t();
            val.ctx = BitConverter.ToInt16(buf, 0);
            val.n_down = BitConverter.ToInt16(buf, 2);
            val.c_pid = BitConverter.ToInt32(buf, 4);
            if (swap != 0)
            {
                val.ctx = byteorder.SWAP_INT16(val.ctx);
                val.n_down = byteorder.SWAP_INT16(val.n_down);
                val.c_pid = byteorder.SWAP_INT32(val.c_pid);
            }
            dest.Deref = val;
        }

        /// <summary>
        /// Reads a mdef_entry_t structure from FILE and stores it in the memory location referred to by dest
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="file"></param>
        private static void read_mdef_entry_t(Pointer<mdef_entry_t> dest, FILE file, int swap)
        {
            byte[] buf = new byte[12];
            Pointer<byte> structdata = new Pointer<byte>(buf);
            file.fread(structdata, 1, 12);
            mdef_entry_t val = new mdef_entry_t();
            val.ssid = BitConverter.ToInt32(buf, 0);
            val.tmat = BitConverter.ToInt32(buf, 4);
            val.info_cd_wpos = buf[8];
            val.info_cd_ctx = buf.GetPointer(9);
            if (swap != 0)
            {
                val.ssid = byteorder.SWAP_INT32(val.ssid);
                val.tmat = byteorder.SWAP_INT32(val.tmat);
            }
            dest.Deref = val;
        }

        public static Pointer<bin_mdef_t> bin_mdef_read(Pointer<cmd_ln_t> config, Pointer<byte> filename)
        {
            Pointer<bin_mdef_t> m;
            FILE fh;
            uint tree_start;
            int i, swap;
            long pos, end;
            int sseq_size;

            Pointer<byte> val_bytes = PointerHelpers.Malloc<byte>(4);
            Pointer<int> val = val_bytes.ReinterpretCast<int>();

            /* Try to read it as text first. */
            if (config.IsNonNull && (m = bin_mdef_read_text(config, filename)).IsNonNull)
                return m;

            err.E_INFO(string.Format("Reading binary model definition: {0}\n", cstring.FromCString(filename)));
            if ((fh = FILE.fopen(filename, "rb")) == null)
                return PointerHelpers.NULL<bin_mdef_t>();

            if (fh.fread(val_bytes, 4, 1) != 1)
            {
                fh.fclose();
                err.E_ERROR_SYSTEM(string.Format("Failed to read byte-order marker from {0}\n",
                               cstring.FromCString(filename)));
                return PointerHelpers.NULL<bin_mdef_t>();
            }
            swap = 0;
            if (+val == BIN_MDEF_OTHER_ENDIAN)
            {
                swap = 1;
                err.E_INFO(string.Format("Must byte-swap {0}\n", filename));
            }
            if (fh.fread(val_bytes, 4, 1) != 1)
            {
                fh.fclose();
                err.E_ERROR_SYSTEM(string.Format("Failed to read version from {0}\n", cstring.FromCString(filename)));
                return PointerHelpers.NULL<bin_mdef_t>();
            }
            if (swap != 0)
                byteorder.SWAP_INT32(val);
            if (+val > BIN_MDEF_FORMAT_VERSION)
            {
                err.E_ERROR(string.Format("File format version {0} for {1} is newer than library\n",
                        +val, cstring.FromCString(filename)));
                fh.fclose();
                return PointerHelpers.NULL<bin_mdef_t>();
            }
            if (fh.fread(val_bytes, 4, 1) != 1)
            {
                fh.fclose();
                err.E_ERROR_SYSTEM(string.Format("Failed to read header length from {0}\n", cstring.FromCString(filename)));
                return PointerHelpers.NULL<bin_mdef_t>();
            }
            if (swap != 0)
                byteorder.SWAP_INT32(val);
            /* Skip format descriptor. */
            fh.fseek(+val, FILE.SEEK_CUR);

            /* Finally allocate it. */
            m = ckd_alloc.ckd_calloc_struct<bin_mdef_t>(1);
            m.Deref.refcnt = 1;
            
            int tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_ciphone = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_phone = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_emit_state = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_ci_sen = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_sen = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_tmat = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_sseq = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_ctx = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.n_cd_tree = tmp;
            if (FREAD_SWAP32_CHK(m, fh, swap, out tmp, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            m.Deref.sil = tmp;

            /* CI names are first in the file. */
            m.Deref.ciname = ckd_alloc.ckd_calloc<Pointer<byte>>(m.Deref.n_ciphone);

            /* Decide whether to read in the whole file or mmap it. */
            // LOGAN cut out mmap here
            //do_mmap = config ? cmd_ln.cmd_ln_boolean_r(config, "-mmap") : 1;
            //if (swap != 0)
            //{
            //    E_WARN("-mmap specified, but mdef is other-endian.  Will not memory-map.\n");
            //    do_mmap = 0;
            //}
            ///* Actually try to mmap it. */
            //if (do_mmap)
            //{
            //    m.Deref.filemap = mmio_file_read(filename);
            //    if (m.Deref.filemap == NULL)
            //        do_mmap = 0;
            //}
            //pos = fh.ftell();
            //if (do_mmap)
            //{
            //    /* Get the base pointer from the memory map. */
            //    m.Deref.ciname[0] = (Pointer<byte>)mmio_file_ptr(m.Deref.filemap) + pos;
            //    /* Success! */
            //    m.Deref.alloc_mode = BIN_MDEF_ON_DISK;
            //}
            //else
            //{

            // LOGAN modified
            // The original code would read the entire rest of the file as a single giant memory allocation, and then find the right indexes
            // for all the datastructures to point into.
            // Since we can't do that safely in C#, we have to rewrite the code to read each struct one-by-one
            // The file format is:
            //char ciphones[][];            n_ciphone null-terminated strings
            //char padding[];               0-3 padding bytes to align file to a 4 byte boundary
            //cd_tree_t[];                  n_cd_tree tree structs
            //mdef_entry_t[] phones[];      n_phones phone structs
            //int32 sseq_len                The total size of the sseq[] block, in 16-bit elements
            //short sseq[];                 an set of 16-bit values with size equal to sseq_len
            //int8 sseq_len[];              n_sseq integers, only if n_emit_state is zero

            // Read the cinames
            m.Deref.ciname = PointerHelpers.Malloc<Pointer<byte>>(m.Deref.n_ciphone);
            byte[] cinameBuf = new byte[1000];
            for (int cn = 0; cn < m.Deref.n_ciphone; cn++)
            {
                // Read a single cstring from file
                Pointer<byte> cinameStr = new Pointer<byte>(cinameBuf);
                Pointer<byte> bufferStr = new Pointer<byte>(cinameBuf);
                for (int cinameLen = 0; cinameLen < cinameBuf.Length - 1; cinameLen++)
                {
                    fh.fread(cinameStr, 1, 1);
                    if (cinameStr.Deref == 0)
                    {
                        // Append null terminator
                        cinameStr[1] = 0;
                        // Copy string from buffer to ciname struct
                        m.Deref.ciname[cn] = new Pointer<byte>(cinameLen + 1);
                        cstring.strcpy(m.Deref.ciname[cn], bufferStr);
                        string debugStr = cstring.FromCString(m.Deref.ciname[cn]);
                        break;
                    }

                    cinameStr = cinameStr.Point(1);
                }
            }

            // Seek to the nearest 4-byte alignment, based on our current position in the file
            pos = fh.ftell();
            if (pos % 4 > 0)
                fh.fseek(pos % 4, FILE.SEEK_CUR);

            // Read all the cd trees
            m.Deref.cd_tree = PointerHelpers.Malloc<cd_tree_t>(m.Deref.n_cd_tree);
            for (int cn = 0; cn < m.Deref.n_cd_tree; cn++)
            {
                read_cdtree_t(m.Deref.cd_tree.Point(cn), fh, swap);
            }

            // Read all the mdef entries
            m.Deref.phone = PointerHelpers.Malloc<mdef_entry_t>(m.Deref.n_phone);
            for (int cn = 0; cn < m.Deref.n_phone; cn++)
            {
                read_mdef_entry_t(m.Deref.phone.Point(cn), fh, swap);
            }

            // read sseq_size, which is the number of 16-bit values in the entire sseq data structure
            if (FREAD_SWAP32_CHK(m, fh, swap, out sseq_size, filename) != 0) return PointerHelpers.NULL<bin_mdef_t>();
            if (swap != 0)
                sseq_size = byteorder.SWAP_INT32(sseq_size);

            // Read that many sseqs from the file into a block
            byte[] sseq_block = new byte[sseq_size * 2];
            MemoryBlock<byte> sseqMemoryBlock = new MemoryBlock<byte>(sseq_block);
            Pointer<byte> sseq_block_byte_ptr = new Pointer<byte>(sseq_block);
            Pointer<ushort> sseq_block_ushort_ptr = new Pointer<ushort>(new UpcastingMemoryBlockAccess<ushort>(sseqMemoryBlock), 0);
            fh.fread(sseq_block_byte_ptr, 2, (uint)sseq_size);
            if (swap != 0)
            {
                for (i = 0; i < sseq_size; ++i)
                    byteorder.SWAP_INT16(sseq_block_ushort_ptr + i);
            }

            // Now create pointer indexes into that block
            m.Deref.sseq = ckd_alloc.ckd_calloc<Pointer<ushort>>(m.Deref.n_sseq);
            m.Deref.sseq[0] = sseq_block_ushort_ptr;
                
            if (m.Deref.n_emit_state != 0)
            {
                for (i = 1; i < m.Deref.n_sseq; ++i)
                    m.Deref.sseq[i] = m.Deref.sseq[0] + (i * m.Deref.n_emit_state);
            }
            else
            {
                // The rest of the file in this case is a byte array of sseq_lengths. Read the rest of the file
                long ssqlenstart = fh.ftell();
                fh.fseek(0, FILE.SEEK_END);
                long ssqlenend = fh.ftell();
                fh.fseek(ssqlenstart, FILE.SEEK_SET);
                int sseqLenBlockSize = (int)(ssqlenend - ssqlenstart);
                m.Deref.sseq_len = PointerHelpers.Malloc<byte>(sseqLenBlockSize);
                fh.fread(m.Deref.sseq_len, 1, (uint)sseqLenBlockSize);
                for (i = 1; i < m.Deref.n_sseq; ++i)
                    m.Deref.sseq[i] = m.Deref.sseq[i - 1] + m.Deref.sseq_len[i - 1];
            }

            /* Now build the CD-to-CI mappings using the senone sequences.
             * This is the only really accurate way to do it, though it is
             * still inaccurate in the case of heterogeneous topologies or
             * cross-state tying. */
            m.Deref.cd2cisen = ckd_alloc.ckd_malloc<short>(m.Deref.n_sen );
            m.Deref.sen2cimap = ckd_alloc.ckd_malloc<short>(m.Deref.n_sen);

            /* Default mappings (identity, none) */
            for (i = 0; i < m.Deref.n_ci_sen; ++i)
                m.Deref.cd2cisen[i] = checked((short)i);
            for (; i < m.Deref.n_sen; ++i)
                m.Deref.cd2cisen[i] = -1;
            for (i = 0; i < m.Deref.n_sen; ++i)
                m.Deref.sen2cimap[i] = -1;
            for (i = 0; i < m.Deref.n_phone; ++i)
            {
                int j, ssid = m.Deref.phone[i].ssid;

                for (j = 0; j < bin_mdef_n_emit_state_phone(m, i); ++j)
                {
                    int s = bin_mdef_sseq2sen(m, ssid, j);
                    int ci = bin_mdef_pid2ci(m, i);
                    /* Take the first one and warn if we have cross-state tying. */
                    if (m.Deref.sen2cimap[s] == -1)
                        m.Deref.sen2cimap[s] = checked((short)ci);
                    if (m.Deref.sen2cimap[s] != ci)
                        err.E_WARN(string.Format("Senone {0} is shared between multiple base phones\n",
                             s));

                    if (j > bin_mdef_n_emit_state_phone(m, ci))
                        err.E_WARN(string.Format("CD phone {0} has fewer states than CI phone {1}\n",
                               i, ci));
                    else
                        m.Deref.cd2cisen[s] = (short)bin_mdef_sseq2sen(m, m.Deref.phone[ci].ssid, j);
                }
            }

            /* Set the silence phone. */
            m.Deref.sil = bin_mdef_ciphone_id(m, mdef.S3_SILENCE_CIPHONE);

            err.E_INFO
                (string.Format("{0} CI-phone, {1} CD-phone, {2} emitstate/phone, {3} CI-sen, {4} Sen, {5} Sen-Seq\n",
                 m.Deref.n_ciphone, m.Deref.n_phone - m.Deref.n_ciphone, m.Deref.n_emit_state,
                 m.Deref.n_ci_sen, m.Deref.n_sen, m.Deref.n_sseq));
            fh.fclose();
            return m;
        }

        public static int bin_mdef_ciphone_id(Pointer<bin_mdef_t> m, Pointer<byte> ciphone)
        {
            int low, mid, high;

            /* Exact binary search on m.Deref.ciphone */
            low = 0;
            high = m.Deref.n_ciphone;
            while (low < high)
            {
                int c;

                mid = (low + high) / 2;
                c = (int)cstring.strcmp(ciphone, m.Deref.ciname[mid]);
                if (c == 0)
                    return mid;
                else if (c > 0)
                    low = mid + 1;
                else
                    high = mid;
            }
            return -1;
        }

        public static int bin_mdef_ciphone_id_nocase(Pointer<bin_mdef_t> m, Pointer<byte> ciphone)
        {
            int low, mid, high;

            /* Exact binary search on m.Deref.ciphone */
            low = 0;
            high = m.Deref.n_ciphone;
            while (low < high)
            {
                int c;

                mid = (low + high) / 2;
                c = @case.strcmp_nocase(ciphone, m.Deref.ciname[mid]);
                if (c == 0)
                    return mid;
                else if (c > 0)
                    low = mid + 1;
                else
                    high = mid;
            }
            return -1;
        }

        public static int bin_mdef_phone_id(Pointer<bin_mdef_t> m, int ci, int lc, int rc, int wpos)
        {
            Pointer < cd_tree_t> cd_tree;
            int level, max;
            Pointer<short> ctx = PointerHelpers.Malloc<short>(4);

            SphinxAssert.assert(m.IsNonNull);

            /* In the future, we might back off when context is not available,
             * but for now we'll just return the CI phone. */
            if (lc < 0 || rc < 0)
                return ci;

            SphinxAssert.assert((ci >= 0) && (ci < m.Deref.n_ciphone));
            SphinxAssert.assert((lc >= 0) && (lc < m.Deref.n_ciphone));
            SphinxAssert.assert((rc >= 0) && (rc < m.Deref.n_ciphone));
            SphinxAssert.assert((wpos >= 0) && (wpos < mdef.N_WORD_POSN));

            /* Create a context list, mapping fillers to silence. */
            ctx[0] = (short)wpos;
            ctx[1] = (short)ci;
            ctx[2] = (short)((m.Deref.sil >= 0
                      && m.Deref.phone[lc].info_ci_filler != 0) ? m.Deref.sil : lc);
            ctx[3] = (short)((m.Deref.sil >= 0
                      && m.Deref.phone[rc].info_ci_filler != 0) ? m.Deref.sil : rc);

            /* Walk down the cd_tree. */
            cd_tree = m.Deref.cd_tree;
            level = 0;                  /* What level we are on. */
            max = mdef.N_WORD_POSN;          /* Number of nodes on this level. */
            while (level < 4)
            {
                int i;

                for (i = 0; i < max; ++i)
                {
                    if (cd_tree[i].ctx == ctx[level])
                        break;
                }
                if (i == max)
                    return -1;

                /* Leaf node, stop here. */
                if (cd_tree[i].n_down == 0)
                    return cd_tree[i].c_pid;

                /* Go down one level. */
                max = cd_tree[i].n_down;
                cd_tree = m.Deref.cd_tree + cd_tree[i].c_down;
                ++level;
            }
            /* We probably shouldn't get here. */
            return -1;
        }

        public static int bin_mdef_phone_id_nearest(Pointer<bin_mdef_t> m, int b, int l, int r, int pos)
        {
            int p, tmppos;

            /* In the future, we might back off when context is not available,
             * but for now we'll just return the CI phone. */
            if (l < 0 || r < 0)
                return b;

            p = bin_mdef_phone_id(m, b, l, r, pos);
            if (p >= 0)
                return p;

            /* Exact triphone not found; backoff to other word positions */
            for (tmppos = 0; tmppos < mdef.N_WORD_POSN; tmppos++)
            {
                if (tmppos != pos)
                {
                    p = bin_mdef_phone_id(m, b, l, r, tmppos);
                    if (p >= 0)
                        return p;
                }
            }

            /* Nothing yet; backoff to silence phone if non-silence filler context */
            /* In addition, backoff to silence phone on left/right if in beginning/end position */
            if (m.Deref.sil >= 0)
            {
                int newl = l, newr = r;
                if (m.Deref.phone[(int)l].info_ci_filler != 0
                    || pos == word_posn_t.WORD_POSN_BEGIN || pos == word_posn_t.WORD_POSN_SINGLE)
                    newl = m.Deref.sil;
                if (m.Deref.phone[(int)r].info_ci_filler != 0
                    || pos == word_posn_t.WORD_POSN_END || pos == word_posn_t.WORD_POSN_SINGLE)
                    newr = m.Deref.sil;
                if ((newl != l) || (newr != r))
                {
                    p = bin_mdef_phone_id(m, b, newl, newr, pos);
                    if (p >= 0)
                        return p;

                    for (tmppos = 0; tmppos < mdef.N_WORD_POSN; tmppos++)
                    {
                        if (tmppos != pos)
                        {
                            p = bin_mdef_phone_id(m, b, newl, newr, tmppos);
                            if (p >= 0)
                                return p;
                        }
                    }
                }
            }

            /* Nothing yet; backoff to base phone */
            return b;
        }
    }
}
