using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class dict
    {
        public static readonly Pointer<byte> DELIM = cstring.ToCString(" \t\n");
        public static readonly Pointer<byte> S3_START_WORD = cstring.ToCString("<s>");
        public static readonly Pointer<byte> S3_FINISH_WORD = cstring.ToCString("</s>");
        public static readonly Pointer<byte> S3_SILENCE_WORD = cstring.ToCString("<sil>");
        public static readonly Pointer<byte> S3_UNKNOWN_WORD = cstring.ToCString("<UNK>");
        public static readonly Pointer<byte> HASHES = cstring.ToCString("##");
        public static readonly Pointer<byte> SEMICOLONS = cstring.ToCString(";;");

        public const int DEFAULT_NUM_PHONE = s3types.MAX_S3CIPID + 1;
        public const int S3DICT_INC_SZ = 4096;

        public static int dict_size(Pointer<dict_t> d)
        {
            return d.Deref.n_word;
        }

        public static int dict_num_fillers(Pointer<dict_t> d)
        {
            return dict_filler_end(d) - dict_filler_start(d);
        }

        public static int dict_num_real_words(Pointer<dict_t> d)
        {
            return (dict_size(d) - (dict_filler_end(d) - dict_filler_start(d)) - 2);
        }

        public static int dict_basewid(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].basewid);
        }

        public static Pointer<byte> dict_wordstr(Pointer<dict_t> d, int w)
        {
            return ((w) < 0 ? PointerHelpers.NULL<byte>() : (d).Deref.word[w].word);
        }

        public static Pointer<byte> dict_basestr(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[dict_basewid(d, w)].word);
        }

        public static int dict_nextalt(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].alt);
        }

        public static int dict_pronlen(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].pronlen);
        }

        public static short dict_pron(Pointer<dict_t> d, int w, int p)
        {
            return ((d).Deref.word[w].ciphone[p]);
        }

        public static int dict_filler_start(Pointer<dict_t> d)
        {
            return ((d).Deref.filler_start);
        }

        public static int dict_filler_end(Pointer<dict_t> d)
        {
            return ((d).Deref.filler_end);
        }

        public static int dict_startwid(Pointer<dict_t> d)
        {
            return ((d).Deref.startwid);
        }

        public static int dict_finishwid(Pointer<dict_t> d)
        {
            return ((d).Deref.finishwid);
        }

        public static int dict_silwid(Pointer<dict_t> d)
        {
            return ((d).Deref.silwid);
        }

        public static int dict_is_single_phone(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].pronlen == 1) ? 1 : 0;
        }

        public static short dict_first_phone(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].ciphone[0]);
        }

        public static short dict_second_phone(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].ciphone[1]);
        }

        public static short dict_second_last_phone(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].ciphone[(d).Deref.word[w].pronlen - 2]);
        }

        public static short dict_last_phone(Pointer<dict_t> d, int w)
        {
            return ((d).Deref.word[w].ciphone[(d).Deref.word[w].pronlen - 1]);
        }

        public static short dict_ciphone_id(Pointer<dict_t> d, Pointer<byte> str)
        {
            if (d.Deref.nocase != 0)
                return checked((short)bin_mdef.bin_mdef_ciphone_id_nocase(d.Deref.mdef, str));
            else
                return checked((short)bin_mdef.bin_mdef_ciphone_id(d.Deref.mdef, str));
        }

        public static int dict_add_word(Pointer<dict_t> d, Pointer<byte> word, Pointer<short> p, int np)
        {
            int len;
            Pointer<dictword_t> wordp;
            int newwid;
            Pointer<byte> wword;

            if (d.Deref.n_word >= d.Deref.max_words)
            {
                err.E_INFO(string.Format("Reallocating to {0} KiB for word entries\n",
                       (d.Deref.max_words + S3DICT_INC_SZ) * 28 / 1024));
                d.Deref.word = ckd_alloc.ckd_realloc(d.Deref.word, (d.Deref.max_words + S3DICT_INC_SZ));
                d.Deref.max_words = d.Deref.max_words + S3DICT_INC_SZ;
            }

            wordp = d.Deref.word + d.Deref.n_word;
            wordp.Deref.word = (Pointer<byte>)ckd_alloc.ckd_salloc(word);    /* Freed in dict_free */

            /* Determine base/alt wids */
            wword = ckd_alloc.ckd_salloc(word);
            if ((len = dict_word2basestr(wword)) > 0)
            {
                BoxedValueInt w = new BoxedValueInt();

                /* Truncated to a baseword string; find its ID */
                if (hash_table.hash_table_lookup_int32(d.Deref.ht, wword, w) < 0)
                {
                    err.E_ERROR(string.Format("Missing base word for: {0}\n", cstring.FromCString(word)));
                    ckd_alloc.ckd_free(wword);
                    ckd_alloc.ckd_free(wordp.Deref.word);
                    wordp.Deref.word = PointerHelpers.NULL<byte>();
                    return s3types.BAD_S3WID;
                }

                /* Link into alt list */
                wordp.Deref.basewid = w.Val;
                wordp.Deref.alt = d.Deref.word[w.Val].alt;
                d.Deref.word[w.Val].alt = d.Deref.n_word;
            }
            else
            {
                wordp.Deref.alt = s3types.BAD_S3WID;
                wordp.Deref.basewid = d.Deref.n_word;
            }
            ckd_alloc.ckd_free(wword);

            /* Associate word string with d.Deref.n_word in hash table */
            if (hash_table.hash_table_enter_int32(d.Deref.ht, wordp.Deref.word, d.Deref.n_word) != d.Deref.n_word)
            {
                ckd_alloc.ckd_free(wordp.Deref.word);
                wordp.Deref.word = PointerHelpers.NULL<byte>();
                return s3types.BAD_S3WID;
            }

            /* Fill in word entry, and set defaults */
            if (p.IsNonNull && (np > 0))
            {
                wordp.Deref.ciphone = ckd_alloc.ckd_malloc<short>(np);      /* Freed in dict_free */
                p.MemCopyTo(wordp.Deref.ciphone, np);
                wordp.Deref.pronlen = np;
            }
            else
            {
                wordp.Deref.ciphone = PointerHelpers.NULL<short>();
                wordp.Deref.pronlen = 0;
            }

            newwid = d.Deref.n_word++;

            return newwid;
        }

        public static int dict_read(FILE fp, Pointer<dict_t> d)
        {
            Pointer<lineiter_t> li;
            Pointer<Pointer<byte>> wptr;
            Pointer<short> p;
            int lineno, nwd;
            int w;
            int i, maxwd;
            uint stralloc, phnalloc;

            maxwd = 512;
            p = ckd_alloc.ckd_calloc<short>(maxwd + 4);
            wptr = ckd_alloc.ckd_calloc<Pointer<byte>>(maxwd); /* Freed below */

            lineno = 0;
            stralloc = phnalloc = 0;
            for (li = pio.lineiter_start(fp); li.IsNonNull; li = pio.lineiter_next(li))
            {
                lineno++;
                if (0 == cstring.strncmp(li.Deref.buf, HASHES, 2)
                    || 0 == cstring.strncmp(li.Deref.buf, SEMICOLONS, 2))
                    continue;

                if ((nwd = strfuncs.str2words(li.Deref.buf, wptr, maxwd)) < 0)
                {
                    /* Increase size of p, wptr. */
                    nwd = strfuncs.str2words(li.Deref.buf, PointerHelpers.NULL<Pointer<byte>>(), 0);
                    SphinxAssert.assert(nwd > maxwd); /* why else would it fail? */
                    maxwd = nwd;
                    p = ckd_alloc.ckd_realloc(p, (uint)(maxwd + 4));
                    wptr = (Pointer<Pointer<byte>>)ckd_alloc.ckd_realloc(wptr, maxwd/* * sizeof(*wptr)*/);
                }

                if (nwd == 0)           /* Empty line */
                    continue;
                /* wptr[0] is the word-string and wptr[1..nwd-1] the pronunciation sequence */
                if (nwd == 1)
                {
                    err.E_ERROR(string.Format("Line {0}: No pronunciation for word '{1}'; ignored\n",
                            lineno, cstring.FromCString(wptr[0])));
                    continue;
                }


                /* Convert pronunciation string to CI-phone-ids */
                for (i = 1; i < nwd; i++)
                {
                    p[i - 1] = dict_ciphone_id(d, wptr[i]);
                    if (s3types.NOT_S3CIPID(p[i - 1]) != 0)
                    {
                        err.E_ERROR(string.Format("Line {0}: Phone '{1}' is mising in the acoustic model; word '{2}' ignored\n",
                                lineno, cstring.FromCString(wptr[i]), cstring.FromCString(wptr[0])));
                        break;
                    }
                }

                if (i == nwd)
                {         /* All CI-phones successfully converted to IDs */
                    w = dict_add_word(d, wptr[0], p, nwd - 1);
                    if (s3types.NOT_S3WID(w) != 0)
                        err.E_ERROR
                            (string.Format("Line {0}: Failed to add the word '{1}' (duplicate?); ignored\n",
                             lineno, cstring.FromCString(wptr[0])));
                    else
                    {
                        stralloc += cstring.strlen(d.Deref.word[w].word);
                        phnalloc += (uint)d.Deref.word[w].pronlen * 2;
                    }
                }
            }
            err.E_INFO(string.Format("Dictionary size {0}, allocated {1} KiB for strings, {2} KiB for phones\n",
                   dict_size(d), (int)stralloc / 1024, (int)phnalloc / 1024));
            ckd_alloc.ckd_free(p);
            ckd_alloc.ckd_free(wptr);

            return 0;
        }

        public static Pointer<dict_t> dict_init(Pointer<cmd_ln_t> config, Pointer<bin_mdef_t> mdef)
        {
            FILE fp, fp2;
            int n;
            Pointer<lineiter_t> li;
            Pointer<dict_t> d;
            Pointer<short> sil = PointerHelpers.Malloc<short>(1);
            Pointer<byte> dictfile = PointerHelpers.NULL<byte>();
            Pointer<byte> fillerfile = PointerHelpers.NULL<byte>();

            if (config.IsNonNull)
            {
                dictfile = cmd_ln.cmd_ln_str_r(config, cstring.ToCString("-dict"));
                fillerfile = cmd_ln.cmd_ln_str_r(config, cstring.ToCString("_fdict"));
            }

            /*
             * First obtain #words in dictionary (for hash table allocation).
             * Reason: The PC NT system doesn't like to grow memory gradually.  Better to allocate
             * all the required memory in one go.
             */
            fp = null;
            n = 0;
            if (dictfile.IsNonNull)
            {
                if ((fp = FILE.fopen(dictfile, "r")) == null)
                {
                    err.E_ERROR_SYSTEM(string.Format("Failed to open dictionary file '{0}' for reading", cstring.FromCString(dictfile)));
                    return PointerHelpers.NULL<dict_t>();
                }
                for (li = pio.lineiter_start(fp); li.IsNonNull; li = pio.lineiter_next(li))
                {
                    if (0 != cstring.strncmp(li.Deref.buf, HASHES, 2)
                        && 0 != cstring.strncmp(li.Deref.buf, SEMICOLONS, 2))
                        n++;
                }

                fp.fseek(0L, FILE.SEEK_SET);
            }

            fp2 = null;
            if (fillerfile.IsNonNull)
            {
                if ((fp2 = FILE.fopen(fillerfile, "r")) == null)
                {
                    err.E_ERROR_SYSTEM(string.Format("Failed to open filler dictionary file '{0}' for reading", cstring.FromCString(fillerfile)));
                    fp.fclose();
                    return PointerHelpers.NULL<dict_t>();
                }
                for (li = pio.lineiter_start(fp2); li.IsNonNull; li = pio.lineiter_next(li))
                {
                    if (0 != cstring.strncmp(li.Deref.buf, HASHES, 2)
                            && 0 != cstring.strncmp(li.Deref.buf, SEMICOLONS, 2))
                        n++;
                }

                fp2.fseek(0L, FILE.SEEK_SET);
            }

            /*
             * Allocate dict entries.  HACK!!  Allow some extra entries for words not in file.
             * Also check for type size restrictions.
             */
            d = ckd_alloc.ckd_calloc_struct<dict_t>(1);       /* freed in dict_free() */
            d.Deref.refcnt = 1;
            d.Deref.max_words =
                (n + S3DICT_INC_SZ < s3types.MAX_S3WID) ? n + S3DICT_INC_SZ : s3types.MAX_S3WID;
            if (n >= s3types.MAX_S3WID)
            {
                err.E_ERROR(string.Format("Number of words in dictionaries ({0}) exceeds limit (%d)\n", n,
                        s3types.MAX_S3WID));
                if (fp != null) fp.fclose();
                if (fp2 != null) fp2.fclose();
                ckd_alloc.ckd_free(d);
                return PointerHelpers.NULL<dict_t>();
            }

            err.E_INFO(string.Format("Allocating {0} * {1} bytes ({2} KiB) for word entries\n",
                   d.Deref.max_words, 28,
                   d.Deref.max_words * 28 / 1024));
            d.Deref.word = ckd_alloc.ckd_calloc_struct<dictword_t>(d.Deref.max_words);      /* freed in dict_free() */
            d.Deref.n_word = 0;
            if (mdef.IsNonNull)
                d.Deref.mdef = bin_mdef.bin_mdef_retain(mdef);

            /* Create new hash table for word strings; case-insensitive word strings */
            if (config.IsNonNull && cmd_ln.cmd_ln_exists_r(config, cstring.ToCString("-dictcase")) != 0)
                d.Deref.nocase = cmd_ln.cmd_ln_boolean_r(config, cstring.ToCString("-dictcase"));
            d.Deref.ht = hash_table.hash_table_new(d.Deref.max_words, d.Deref.nocase);

            /* Digest main dictionary file */
            if (fp != null)
            {
                err.E_INFO(string.Format("Reading main dictionary: {0}\n", cstring.FromCString(dictfile)));
                dict_read(fp, d);
                fp.fclose();
                err.E_INFO(string.Format("{0} words read\n", d.Deref.n_word));
            }

            if (dict_wordid(d, S3_START_WORD) != s3types.BAD_S3WID)
            {
                err.E_ERROR("Remove sentence start word '<s>' from the dictionary\n");
                dict_free(d);
                return PointerHelpers.NULL<dict_t>();
            }
            if (dict_wordid(d, S3_FINISH_WORD) != s3types.BAD_S3WID)
            {
                err.E_ERROR("Remove sentence start word '</s>' from the dictionary\n");
                dict_free(d);
                return PointerHelpers.NULL<dict_t>();
            }
            if (dict_wordid(d, S3_SILENCE_WORD) != s3types.BAD_S3WID)
            {
                err.E_ERROR("Remove silence word '<sil>' from the dictionary\n");
                dict_free(d);
                return PointerHelpers.NULL<dict_t>();
            }

            /* Now the filler dictionary file, if it exists */
            d.Deref.filler_start = d.Deref.n_word;
            if (fp2 != null)
            {
                err.E_INFO(string.Format("Reading filler dictionary: {0}\n", cstring.FromCString(fillerfile)));
                dict_read(fp2, d);
                fp2.fclose();
                err.E_INFO(string.Format("{0} words read\n", d.Deref.n_word - d.Deref.filler_start));
            }
            if (mdef.IsNonNull)
                sil.Deref = checked((short)bin_mdef.bin_mdef_silphone(mdef));
            else
                sil.Deref = 0;
            if (dict_wordid(d, S3_START_WORD) == s3types.BAD_S3WID)
            {
                dict_add_word(d, S3_START_WORD, sil, 1);
            }
            if (dict_wordid(d, S3_FINISH_WORD) == s3types.BAD_S3WID)
            {
                dict_add_word(d, S3_FINISH_WORD, sil, 1);
            }
            if (dict_wordid(d, S3_SILENCE_WORD) == s3types.BAD_S3WID)
            {
                dict_add_word(d, S3_SILENCE_WORD, sil, 1);
            }

            d.Deref.filler_end = d.Deref.n_word - 1;

            /* Initialize distinguished word-ids */
            d.Deref.startwid = dict_wordid(d, S3_START_WORD);
            d.Deref.finishwid = dict_wordid(d, S3_FINISH_WORD);
            d.Deref.silwid = dict_wordid(d, S3_SILENCE_WORD);

            if ((d.Deref.filler_start > d.Deref.filler_end)
                || (dict_filler_word(d, d.Deref.silwid) == 0))
            {
                err.E_ERROR(string.Format("Word '{0}' must occur (only) in filler dictionary\n",
                        cstring.FromCString(S3_SILENCE_WORD)));
                dict_free(d);
                return PointerHelpers.NULL<dict_t>();
            }

            /* No check that alternative pronunciations for filler words are in filler range!! */

            return d;
        }

        public static int dict_wordid(Pointer<dict_t> d, Pointer<byte> word)
        {
            BoxedValueInt w = new BoxedValueInt();

            SphinxAssert.assert(d.IsNonNull);
            SphinxAssert.assert(word.IsNonNull);

            if (hash_table.hash_table_lookup_int32(d.Deref.ht, word, w) < 0)
                return (s3types.BAD_S3WID);
            return w.Val;
        }

        public static int dict_filler_word(Pointer<dict_t> d, int w)
        {
            SphinxAssert.assert(d.IsNonNull);
            SphinxAssert.assert((w >= 0) && (w < d.Deref.n_word));

            w = dict_basewid(d, w);
            if ((w == d.Deref.startwid) || (w == d.Deref.finishwid))
                return 0;
            if ((w >= d.Deref.filler_start) && (w <= d.Deref.filler_end))
                return 1;
            return 0;
        }

        public static int dict_word2basestr(Pointer<byte> word)
        {
            int i, len;

            len = (int)cstring.strlen(word);
            if (word[len - 1] == ')')
            {
                for (i = len - 2; (i > 0) && (word[i] != '('); --i) ;

                if (i > 0)
                {
                    /* The word is of the form <baseword>(...); strip from left-paren */
                    word[i] = (byte)'\0';
                    return i;
                }
            }

            return -1;
        }

        public static Pointer<dict_t> dict_retain(Pointer<dict_t> d)
        {
            ++d.Deref.refcnt;
            return d;
        }

        public static int dict_free(Pointer<dict_t> d)
        {
            int i;
            Pointer<dictword_t> word;

            if (d.IsNull)
                return 0;
            if (--d.Deref.refcnt > 0)
                return d.Deref.refcnt;

            /* First Step, free all memory allocated for each word */
            for (i = 0; i < d.Deref.n_word; i++)
            {
                word = d.Deref.word.Point(i);
                if (word.Deref.word.IsNonNull)
                    ckd_alloc.ckd_free(word.Deref.word);
                if (word.Deref.ciphone.IsNonNull)
                    ckd_alloc.ckd_free(word.Deref.ciphone);
            }

            if (d.Deref.word.IsNonNull)
                ckd_alloc.ckd_free(d.Deref.word);
            if (d.Deref.ht.IsNonNull)
                hash_table.hash_table_free(d.Deref.ht);
            if (d.Deref.mdef.IsNonNull)
                bin_mdef.bin_mdef_free(d.Deref.mdef);
            ckd_alloc.ckd_free(d);

            return 0;
        }
    }
}
