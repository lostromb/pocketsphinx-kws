using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class mdef
    {
        public static readonly Pointer<byte> MODEL_DEF_VERSION = cstring.ToCString("0.3");
        public const int N_WORD_POSN = 4;
        public static readonly Pointer<byte> WPOS_NAME = cstring.ToCString("ibesu");
        public static readonly Pointer<byte> S3_SILENCE_CIPHONE = cstring.ToCString("SIL");

        public static void ciphone_add(Pointer<mdef_t> m, Pointer<byte> ci, int p)
        {
            SphinxAssert.assert(p < m.Deref.n_ciphone);

            m.Deref.ciphone[p].name = ckd_alloc.ckd_salloc(ci);       /* freed in mdef_free */
            if (hash_table.hash_table_enter(m.Deref.ciphone_ht, m.Deref.ciphone[p].name, p) != p)
                err.E_FATAL(string.Format("hash_table_enter({0}) failed; duplicate CIphone?\n",
                    cstring.FromCString(m.Deref.ciphone[p].name)));
        }


        public static Pointer<ph_lc_t> find_ph_lc(Pointer<ph_lc_t> lclist, int lc)
        {
            Pointer<ph_lc_t> lcptr;

            for (lcptr = lclist; lcptr.IsNonNull && (lcptr.Deref.lc != lc); lcptr = lcptr.Deref.next) ;
            return lcptr;
        }


        public static Pointer<ph_rc_t> find_ph_rc(Pointer<ph_rc_t> rclist, int rc)
        {
            Pointer<ph_rc_t> rcptr;

            for (rcptr = rclist; rcptr.IsNonNull && (rcptr.Deref.rc != rc); rcptr = rcptr.Deref.next) ;
            return rcptr;
        }


        public static void triphone_add(Pointer<mdef_t> m,
                short ci, short lc, short rc, int wpos,
                int p)
        {
            Pointer<ph_lc_t> lcptr;
            Pointer<ph_rc_t> rcptr;

            SphinxAssert.assert(p < m.Deref.n_phone);

            /* Fill in phone[p] information (state and tmat mappings added later) */
            m.Deref.phone[p].ci = ci;
            m.Deref.phone[p].lc = lc;
            m.Deref.phone[p].rc = rc;
            m.Deref.phone[p].wpos = wpos;

            /* Create <ci,lc,rc,wpos> .Deref. p mapping if not a CI phone */
            if (p >= m.Deref.n_ciphone)
            {
                if ((lcptr = find_ph_lc(m.Deref.wpos_ci_lclist[wpos][(int)ci], lc)).IsNull)
                {
                    lcptr = ckd_alloc.ckd_calloc_struct<ph_lc_t>(1); /* freed at mdef_free, I believe */
                    lcptr.Deref.lc = lc;
                    lcptr.Deref.next = m.Deref.wpos_ci_lclist[wpos][(int)ci];
                    Pointer<Pointer<ph_lc_t>> tmp = m.Deref.wpos_ci_lclist[wpos];
                    tmp[(int)ci] = lcptr;  /* This is what needs to be freed */
                }
                if ((rcptr = find_ph_rc(lcptr.Deref.rclist, rc)).IsNonNull)
                {
                    Pointer<byte> buf = PointerHelpers.Malloc<byte>(4096);
                    mdef_phone_str(m, rcptr.Deref.pid, buf);
                    err.E_FATAL(string.Format("Duplicate triphone: {0}\n", cstring.FromCString(buf)));
                }

                rcptr = ckd_alloc.ckd_calloc_struct<ph_rc_t>(1);     /* freed in mdef_free, I believe */
                rcptr.Deref.rc = rc;
                rcptr.Deref.pid = p;
                rcptr.Deref.next = lcptr.Deref.rclist;
                lcptr.Deref.rclist = rcptr;
            }
        }

        public static int mdef_ciphone_id(Pointer<mdef_t> m, Pointer<byte> ci)
        {
            BoxedValueInt id = new BoxedValueInt();
            if (hash_table.hash_table_lookup_int32(m.Deref.ciphone_ht, ci, id) < 0)
                return -1;
            return id.Val;
        }

        public static Pointer<byte> mdef_ciphone_str(Pointer<mdef_t> m, int id)
        {
            SphinxAssert.assert(m.IsNonNull);
            SphinxAssert.assert((id >= 0) && (id < m.Deref.n_ciphone));

            return (m.Deref.ciphone[id].name);
        }

        public static int mdef_phone_str(Pointer<mdef_t> m, int pid, Pointer<byte> buf)
        {
            Pointer<byte> wpos_name;

            SphinxAssert.assert(m.IsNonNull);
            SphinxAssert.assert((pid >= 0) && (pid < m.Deref.n_phone));
            wpos_name = WPOS_NAME;

            buf[0] = (byte)'\0';
            if (pid < m.Deref.n_ciphone)
            {
                stdio.sprintf(buf, string.Format("{0}", cstring.FromCString(mdef_ciphone_str(m, pid))));
            }
            else
            {
                stdio.sprintf(buf, string.Format("{0} {1} {2} {3}",
                    cstring.FromCString(mdef_ciphone_str(m, m.Deref.phone[pid].ci)),
                    cstring.FromCString(mdef_ciphone_str(m, m.Deref.phone[pid].lc)),
                    cstring.FromCString(mdef_ciphone_str(m, m.Deref.phone[pid].rc)),
                    (char)wpos_name[m.Deref.phone[pid].wpos]));
            }
            return 0;
        }

        /* Parse tmat and state.Deref.senone mappings for phone p and fill in structure */
        public static void parse_tmat_senmap(Pointer<mdef_t> m, Pointer<byte> line, long off, int p)
        {
            int wlen, n, s;
            Pointer<byte> lp;
            Pointer<byte> word = PointerHelpers.Malloc<byte>(1024);

            lp = line + (int)off;

            /* Read transition matrix id */
            if ((stdio.sscanf_d_n(lp, out n, out wlen) != 1) || (n < 0))
                err.E_FATAL(string.Format("Missing or bad transition matrix id: {0}\n", cstring.FromCString(line)));
            m.Deref.phone[p].tmat = n;
            if (m.Deref.n_tmat <= n)
                err.E_FATAL(string.Format("tmat-id({0}) > #tmat in header({1}): {2}\n", n, m.Deref.n_tmat,
                    cstring.FromCString(line)));
            lp += wlen;

            /* Read senone mappings for each emitting state */
            for (n = 0; n < m.Deref.n_emit_state; n++)
            {
                if ((stdio.sscanf_d_n(lp, out s, out wlen) != 1) || (s < 0))
                    err.E_FATAL(string.Format("Missing or bad state[{0}].Deref.senone mapping: {1}\n", n,
                        cstring.FromCString(line)));

                if ((p < m.Deref.n_ciphone) && (m.Deref.n_ci_sen <= s))
                    err.E_FATAL(string.Format("CI-senone-id({0}) > #CI-senones({1}): {2}\n", s,
                        m.Deref.n_ci_sen, cstring.FromCString(line)));
                if (m.Deref.n_sen <= s)
                    err.E_FATAL(string.Format("Senone-id({0}) > #senones({1}): {2}\n", s, m.Deref.n_sen,
                        cstring.FromCString(line)));

                Pointer<ushort> tmp = m.Deref.sseq[p];
                tmp[n] = (ushort)s;
                lp += wlen;
            }

            /* Check for the last non-emitting state N */
            if ((stdio.sscanf_s_n(lp, word, out wlen) != 1) || (cstring.strcmp(word, cstring.ToCString("N")) != 0))
                err.E_FATAL(string.Format("Missing non-emitting state spec: {0}\n", cstring.FromCString(line)));
            lp += wlen;

            /* Check for end of line */
            if (stdio.sscanf_s_n(lp, word, out wlen) == 1)
                err.E_FATAL(string.Format("Non-empty beyond non-emitting final state: {0}\n", cstring.FromCString(line)));
        }


        public static void parse_base_line(Pointer<mdef_t> m, Pointer<byte> line, int p)
        {
            int wlen, n;
            Pointer<byte> word = PointerHelpers.Malloc<byte>(1024);
            Pointer<byte> lp;
            int ci;

            lp = line;

            /* Read base phone name */
            if (stdio.sscanf_s_n(lp, word, out wlen) != 1)
                err.E_FATAL(string.Format("Missing base phone name: {0}\n", cstring.FromCString(line)));
            lp += wlen;

            /* Make sure it's not a duplicate */
            ci = mdef_ciphone_id(m, word);
            if (ci >= 0)
                err.E_FATAL(string.Format("Duplicate base phone: {0}\n", cstring.FromCString(line)));

            /* Add ciphone to ciphone table with id p */
            ciphone_add(m, word, p);
            ci = (int)p;

            /* Read and skip "-" for lc, rc, wpos */
            for (n = 0; n < 3; n++)
            {
                if ((stdio.sscanf_s_n(lp, word, out wlen) != 1)
                    || (cstring.strcmp(word, cstring.ToCString("-")) != 0))
                    err.E_FATAL(string.Format("Bad context info for base phone: {0}\n", cstring.FromCString(line)));
                lp += wlen;
            }

            /* Read filler attribute, if present */
            if (stdio.sscanf_s_n(lp, word, out wlen) != 1)
                err.E_FATAL(string.Format("Missing filler attribute field: {0}\n", cstring.FromCString(line)));
            lp += wlen;
            if (cstring.strcmp(word, cstring.ToCString("filler")) == 0)
                m.Deref.ciphone[(int)ci].filler = 1;
            else if (cstring.strcmp(word, cstring.ToCString("n/a")) == 0)
                m.Deref.ciphone[(int)ci].filler = 0;
            else
                err.E_FATAL(string.Format("Bad filler attribute field: {0}\n", cstring.FromCString(line)));

            triphone_add(m, (short)ci, -1, -1, word_posn_t.WORD_POSN_UNDEFINED, p);

            /* Parse remainder of line: transition matrix and state.Deref.senone mappings */
            parse_tmat_senmap(m, line, lp - line, p);
        }


        public static void parse_tri_line(Pointer<mdef_t> m, Pointer<byte> line, int p)
        {
            int wlen;
            Pointer<byte> word = PointerHelpers.Malloc<byte>(1024);
            Pointer<byte> lp;
            int ci, lc, rc;
            int wpos = word_posn_t.WORD_POSN_BEGIN;

            lp = line;

            /* Read base phone name */
            if (stdio.sscanf_s_n(lp, word, out wlen) != 1)
                err.E_FATAL(string.Format("Missing base phone name: {0}\n", cstring.FromCString(line)));
            lp += wlen;

            ci = mdef_ciphone_id(m, word);
            if (ci < 0)
                err.E_FATAL(string.Format("Unknown base phone: {0}\n", cstring.FromCString(line)));

            /* Read lc */
            if (stdio.sscanf_s_n(lp, word, out wlen) != 1)
                err.E_FATAL(string.Format("Missing left context: {0}\n", cstring.FromCString(line)));
            lp += wlen;
            lc = mdef_ciphone_id(m, word);
            if (lc < 0)
                err.E_FATAL(string.Format("Unknown left context: {0}\n", cstring.FromCString(line)));

            /* Read rc */
            if (stdio.sscanf_s_n(lp, word, out wlen) != 1)
                err.E_FATAL(string.Format("Missing right context: {0}\n", cstring.FromCString(line)));
            lp += wlen;
            rc = mdef_ciphone_id(m, word);
            if (rc < 0)
                err.E_FATAL(string.Format("Unknown right  context: {0}\n", cstring.FromCString(line)));

            /* Read tripone word-position within word */
            if ((stdio.sscanf_s_n(lp, word, out wlen) != 1) || (word[1] != '\0'))
                err.E_FATAL(string.Format("Missing or bad word-position spec: {0}\n", cstring.FromCString(line)));
            lp += wlen;
            switch (word[0])
            {
                case (byte)'b':
                    wpos = word_posn_t.WORD_POSN_BEGIN;
                    break;
                case (byte)'e':
                    wpos = word_posn_t.WORD_POSN_END;
                    break;
                case (byte)'s':
                    wpos = word_posn_t.WORD_POSN_SINGLE;
                    break;
                case (byte)'i':
                    wpos = word_posn_t.WORD_POSN_INTERNAL;
                    break;
                default:
                    err.E_FATAL(string.Format("Bad word-position spec: {0}\n", cstring.FromCString(line)));
                    break;
            }

            /* Read filler attribute, if present.  Must match base phone attribute */
            if (stdio.sscanf_s_n(lp, word, out wlen) != 1)
                err.E_FATAL(string.Format("Missing filler attribute field: {0}\n", cstring.FromCString(line)));
            lp += wlen;
            if (((cstring.strcmp(word, cstring.ToCString("filler")) == 0) && (m.Deref.ciphone[(int)ci].filler != 0)) ||
                ((cstring.strcmp(word, cstring.ToCString("n/a")) == 0) && (m.Deref.ciphone[(int)ci].filler == 0)))
            {
                /* Everything is fine */
            }
            else
                err.E_FATAL(string.Format("Bad filler attribute field: {0}\n", cstring.FromCString(line)));

            triphone_add(m, (short)ci, (short)lc, (short)rc, wpos, p);

            /* Parse remainder of line: transition matrix and state.Deref.senone mappings */
            parse_tmat_senmap(m, line, lp - line, p);
        }


        public static void sseq_compress(Pointer<mdef_t> m)
        {
            Pointer<hash_table_t> h;
            Pointer<Pointer<ushort>> sseq;
            int n_sseq;
            int p;
            uint k;
            BoxedValue<int> j = new BoxedValue<int>();
            Pointer<gnode_t> g;
            Pointer<gnode_t> gn;
            Pointer<hash_entry_t> he;

            k = (uint)(m.Deref.n_emit_state);

            h = hash_table.hash_table_new(m.Deref.n_phone, hash_table.HASH_CASE_YES);
            n_sseq = 0;

            /* Identify unique senone-sequence IDs.  BUG: tmat-id not being considered!! */
            for (p = 0; p < m.Deref.n_phone; p++)
            {
                /* Add senone sequence to hash table */
                if (n_sseq
                    == (j.Val = hash_table.hash_table_enter_bkey_int32(h, m.Deref.sseq[p].ReinterpretCast<byte>(), k * 2 /*sizeof(short)*/, n_sseq)))
                    n_sseq++;

                m.Deref.phone[p].ssid = j.Val;
            }

            /* Generate compacted sseq table */
            sseq = ckd_alloc.ckd_calloc_2d<ushort>((uint)n_sseq, (uint)m.Deref.n_emit_state); /* freed in mdef_free() */

            g = hash_table.hash_table_tolist(h, j);
            SphinxAssert.assert(j.Val == n_sseq);

            for (gn = g; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                he = (Pointer<hash_entry_t>)glist.gnode_ptr(gn);
                j.Val = (int)hash_table.hash_entry_val(he);
                hash_table.hash_entry_key(he).ReinterpretCast<ushort>().MemCopyTo(sseq[j.Val], (int)k);
            }
            glist.glist_free(g);

            /* Free the old, temporary senone sequence table, replace with compacted one */
            ckd_alloc.ckd_free_2d(m.Deref.sseq);
            m.Deref.sseq = sseq;
            m.Deref.n_sseq = n_sseq;

            hash_table.hash_table_free(h);
        }

        public static int noncomment_line(Pointer<byte> line, int size, FILE fp)
        {
            while (fp.fgets(line, size).IsNonNull)
            {
                if (line[0] != '#')
                    return 0;
            }
            return -1;
        }

        /*
		* Initialize phones (ci and triphones) and state.Deref.senone mappings from .mdef file.
		*/
        public static Pointer<mdef_t> mdef_init(Pointer<byte> mdeffile, int breport)
        {
            FILE fp;
            int n_ci, n_tri, n_map, n;
            Pointer<byte> tag = PointerHelpers.Malloc<byte>(1024);
            Pointer<byte> buf = PointerHelpers.Malloc<byte>(1024);
            Pointer<Pointer<ushort>> senmap;
            int p;
            int s, ci, cd;
            Pointer<mdef_t> m;

            if (mdeffile.IsNull)
                err.E_FATAL("No mdef-file\n");

            if (breport != 0)
                err.E_INFO(string.Format("Reading model definition: {0}\n", cstring.FromCString(mdeffile)));

            m = ckd_alloc.ckd_calloc_struct<mdef_t>(1);       /* freed in mdef_free */

            if ((fp = FILE.fopen(mdeffile, "r")) == null)
                err.E_FATAL_SYSTEM(string.Format("Failed to open mdef file '{0} for reading", cstring.FromCString(mdeffile)));

            if (noncomment_line(buf, 1024, fp) < 0)
                err.E_FATAL(string.Format("Empty file: {0}\n", cstring.FromCString(mdeffile)));

            if (cstring.strncmp(buf, cstring.ToCString("BMDF"), 4) == 0 || cstring.strncmp(buf, cstring.ToCString("FDMB"), 4) == 0)
            {
                err.E_INFO(string.Format("Found byte-order mark {0:x4}, assuming this is a binary mdef file\n", buf));
                fp.fclose();
                ckd_alloc.ckd_free(m);
                return PointerHelpers.NULL<mdef_t>();
            }

            if (cstring.strncmp(buf, MODEL_DEF_VERSION, cstring.strlen(MODEL_DEF_VERSION)) != 0)
                err.E_FATAL(string.Format("Version error: Expecing {0}, but read {1}\n",
                    cstring.FromCString(MODEL_DEF_VERSION), cstring.FromCString(buf)));

            /* Read #base phones, #triphones, #senone mappings defined in header */
            n_ci = -1;
            n_tri = -1;
            n_map = -1;
            m.Deref.n_ci_sen = -1;
            m.Deref.n_sen = -1;
            m.Deref.n_tmat = -1;
            do
            {
                if (noncomment_line(buf, 1024, fp) < 0)
                    err.E_FATAL("Incomplete header\n");

                if ((stdio.sscanf_d_s(buf, out n, tag) != 2) || (n < 0))
                    err.E_FATAL(string.Format("Error in header: %s\n", cstring.FromCString(buf)));

                if (cstring.strcmp(tag, cstring.ToCString("n_base")) == 0)
                    n_ci = n;
                else if (cstring.strcmp(tag, cstring.ToCString("n_tri")) == 0)
                    n_tri = n;
                else if (cstring.strcmp(tag, cstring.ToCString("n_state_map")) == 0)
                    n_map = n;
                else if (cstring.strcmp(tag, cstring.ToCString("n_tied_ci_state")) == 0)
                    m.Deref.n_ci_sen = n;
                else if (cstring.strcmp(tag, cstring.ToCString("n_tied_state")) == 0)
                    m.Deref.n_sen = n;
                else if (cstring.strcmp(tag, cstring.ToCString("n_tied_tmat")) == 0)
                    m.Deref.n_tmat = n;
                else
                    err.E_FATAL(string.Format("Unknown header line: {0}\n", cstring.FromCString(buf)));
            } while ((n_ci < 0) || (n_tri < 0) || (n_map < 0) ||
                (m.Deref.n_ci_sen < 0) || (m.Deref.n_sen < 0) || (m.Deref.n_tmat < 0));

            if ((n_ci == 0) || (m.Deref.n_ci_sen == 0) || (m.Deref.n_tmat == 0)
                || (m.Deref.n_ci_sen > m.Deref.n_sen))
                err.E_FATAL(string.Format("{0}: Error in header\n", cstring.FromCString(mdeffile)));

            /* Check typesize limits */
            if (n_ci >= short.MaxValue)
                err.E_FATAL(string.Format("{0}: #CI phones ({1}) exceeds limit ({2})\n", cstring.FromCString(mdeffile), n_ci,
                    short.MaxValue));
            if (n_ci + n_tri >= int.MaxValue) /* Comparison is always false... */
                err.E_FATAL(string.Format("{0}: #Phones ({1}) exceeds limit ({2})\n", cstring.FromCString(mdeffile),
                    n_ci + n_tri, int.MaxValue));
            if (m.Deref.n_sen >= short.MaxValue)
                err.E_FATAL(string.Format("{0}: #senones ({1}) exceeds limit ({2})\n", cstring.FromCString(mdeffile),
                    m.Deref.n_sen, short.MaxValue));
            if (m.Deref.n_tmat >= int.MaxValue) /* Comparison is always false... */
                err.E_FATAL(string.Format("{0}: #tmats ({1}) exceeds limit ({2})\n", cstring.FromCString(mdeffile),
                    m.Deref.n_tmat, int.MaxValue));

            m.Deref.n_emit_state = (n_map / (n_ci + n_tri)) - 1;
            if ((m.Deref.n_emit_state + 1) * (n_ci + n_tri) != n_map)
                err.E_FATAL
                ("Header error: n_state_map not a multiple of n_ci*n_tri\n");

            /* Initialize ciphone info */
            m.Deref.n_ciphone = n_ci;
            m.Deref.ciphone_ht = hash_table.hash_table_new(n_ci, hash_table.HASH_CASE_YES);  /* With case-insensitive string names *//* freed in mdef_free */
            m.Deref.ciphone = ckd_alloc.ckd_calloc_struct<ciphone_t>(n_ci);     /* freed in mdef_free */

            /* Initialize phones info (ciphones + triphones) */
            m.Deref.n_phone = n_ci + n_tri;
            m.Deref.phone = ckd_alloc.ckd_calloc_struct<phone_t>(m.Deref.n_phone);     /* freed in mdef_free */

            /* Allocate space for state.Deref.senone map for each phone */
            senmap = ckd_alloc.ckd_calloc_2d<ushort>((uint)m.Deref.n_phone, (uint)m.Deref.n_emit_state);      /* freed in mdef_free */
            m.Deref.sseq = senmap;           /* TEMPORARY; until it is compressed into just the unique ones */

            /* Allocate initial space for <ci,lc,rc,wpos> .Deref. pid mapping */
            m.Deref.wpos_ci_lclist = ckd_alloc.ckd_calloc_2d<Pointer<ph_lc_t>>((uint)N_WORD_POSN, (uint)m.Deref.n_ciphone);      /* freed in mdef_free */

            /*
            * Read base phones and triphones.  They'll simply be assigned a running sequence
            * number as their "phone-id".  If the phone-id < n_ci, it's a ciphone.
            */

            /* Read base phones */
            for (p = 0; p < n_ci; p++)
            {
                if (noncomment_line(buf, 1024, fp) < 0)
                    err.E_FATAL(string.Format("Premature EOF reading CIphone {0}\n", p));
                parse_base_line(m, buf, p);
            }
            m.Deref.sil = (short)mdef_ciphone_id(m, S3_SILENCE_CIPHONE);

            /* Read triphones, if any */
            for (; p < m.Deref.n_phone; p++)
            {
                if (noncomment_line(buf, 1024, fp) < 0)
                    err.E_FATAL(string.Format("Premature EOF reading phone {0}\n", p));
                parse_tri_line(m, buf, p);
            }

            if (noncomment_line(buf, 1024, fp) >= 0)
                err.E_ERROR(string.Format("Non-empty file beyond expected #phones ({0})\n",
                    m.Deref.n_phone));

            /* Build CD senones to CI senones map */
            if (m.Deref.n_ciphone * m.Deref.n_emit_state != m.Deref.n_ci_sen)
                err.E_FATAL
                (string.Format("#CI-senones({0}) != #CI-phone({1}) x #emitting-states({2})\n",
                    m.Deref.n_ci_sen, m.Deref.n_ciphone, m.Deref.n_emit_state));
            m.Deref.cd2cisen = ckd_alloc.ckd_calloc<short>(m.Deref.n_sen); /* freed in mdef_free */

            m.Deref.sen2cimap = ckd_alloc.ckd_calloc<short>(m.Deref.n_sen); /* freed in mdef_free */

            for (s = 0; s < m.Deref.n_sen; s++)
                m.Deref.sen2cimap[s] = -1;
            for (s = 0; s < m.Deref.n_ci_sen; s++)
            { /* CI senones */
                m.Deref.cd2cisen[s] = (short)s;
                m.Deref.sen2cimap[s] = (short)(s / m.Deref.n_emit_state);
            }
            for (p = n_ci; p < m.Deref.n_phone; p++)
            {       /* CD senones */
                for (s = 0; s < m.Deref.n_emit_state; s++)
                {
                    cd = m.Deref.sseq[p][s];
                    ci = m.Deref.sseq[m.Deref.phone[p].ci][s];
                    m.Deref.cd2cisen[cd] = (short)ci;
                    m.Deref.sen2cimap[cd] = m.Deref.phone[p].ci;
                }
            }

            sseq_compress(m);
            fp.fclose();

            return m;
        }

        public static void mdef_free_recursive_lc(Pointer<ph_lc_t> lc)
        {
            if (lc.IsNull)
                return;

            if (lc.Deref.rclist.IsNonNull)
                mdef_free_recursive_rc(lc.Deref.rclist);

            if (lc.Deref.next.IsNonNull)
                mdef_free_recursive_lc(lc.Deref.next);

            ckd_alloc.ckd_free(lc);
        }

        public static void mdef_free_recursive_rc(Pointer<ph_rc_t> rc)
        {
            if (rc.IsNull)
                return;

            if (rc.Deref.next.IsNonNull)
                mdef_free_recursive_rc(rc.Deref.next);

            ckd_alloc.ckd_free(rc);
        }

        public static void mdef_free(Pointer<mdef_t> m)
        {
            int i, j;

            if (m.IsNonNull)
            {
                if (m.Deref.sen2cimap.IsNonNull)
                    ckd_alloc.ckd_free(m.Deref.sen2cimap);
                if (m.Deref.cd2cisen.IsNonNull)
                    ckd_alloc.ckd_free(m.Deref.cd2cisen);

                /* RAH, go down the .Deref.next list and delete all the pieces */
                for (i = 0; i < N_WORD_POSN; i++)
                    for (j = 0; j < m.Deref.n_ciphone; j++)
                        if (m.Deref.wpos_ci_lclist[i][j].IsNonNull)
                        {
                            mdef_free_recursive_lc(m.Deref.wpos_ci_lclist[i][j].Deref.next);
                            mdef_free_recursive_rc(m.Deref.wpos_ci_lclist[i][j].Deref.
                                rclist);
                        }

                for (i = 0; i < N_WORD_POSN; i++)
                    for (j = 0; j < m.Deref.n_ciphone; j++)
                        if (m.Deref.wpos_ci_lclist[i][j].IsNonNull)
                            ckd_alloc.ckd_free(m.Deref.wpos_ci_lclist[i][j]);


                if (m.Deref.wpos_ci_lclist.IsNonNull)
                    ckd_alloc.ckd_free_2d(m.Deref.wpos_ci_lclist);
                if (m.Deref.sseq.IsNonNull)
                    ckd_alloc.ckd_free_2d(m.Deref.sseq);
                /* Free phone context */
                if (m.Deref.phone.IsNonNull)
                    ckd_alloc.ckd_free(m.Deref.phone);
                if (m.Deref.ciphone_ht.IsNonNull)
                    hash_table.hash_table_free(m.Deref.ciphone_ht);

                for (i = 0; i < m.Deref.n_ciphone; i++)
                {
                    if (m.Deref.ciphone[i].name.IsNonNull)
                        ckd_alloc.ckd_free(m.Deref.ciphone[i].name);
                }


                if (m.Deref.ciphone.IsNonNull)
                    ckd_alloc.ckd_free(m.Deref.ciphone);

                ckd_alloc.ckd_free(m);
            }
        }
    };
}
