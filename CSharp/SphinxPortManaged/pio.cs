using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class pio
    {
        private const int COMP_NONE = 0;
        private const int COMP_COMPRESS = 1;
        private const int COMP_GZIP = 2;
        private const int COMP_BZIP2 = 3;

        private const int FREAD_RETRY_COUNT = 60;
        private const int STAT_RETRY_COUNT = 10;

        public static Pointer<lineiter_t> lineiter_start(FILE fh)
        {
            Pointer<lineiter_t> li;

            li = ckd_alloc.ckd_calloc_struct<lineiter_t>(1);
            li.Deref.buf = ckd_alloc.ckd_malloc<byte>(128);
            li.Deref.buf[0] = (byte)'\0';
            li.Deref.bsiz = 128;
            li.Deref.len = 0;
            li.Deref.fh = fh;

            li = lineiter_next(li);

            /* Strip the UTF-8 BOM */

            if (li.IsNonNull && 0 == cstring.strncmp(li.Deref.buf, cstring.ToCString("\xef\xbb\xbf"), 3))
            {
                li.Deref.buf.Point(3).MemMove(-3, (int)cstring.strlen(li.Deref.buf + 1));
                li.Deref.len -= 3;
            }

            return li;
        }

        public static Pointer<lineiter_t> lineiter_start_clean(FILE fh)
        {
            Pointer<lineiter_t> li;

            li = lineiter_start(fh);

            if (li.IsNull)
                return li;

            li.Deref.clean = 1;

            if (li.Deref.buf.IsNonNull && li.Deref.buf[0] == '#')
            {
                li = lineiter_next(li);
            }
            else
            {
                strfuncs.string_trim(li.Deref.buf, string_edge_e.STRING_BOTH);
            }

            return li;
        }

        public static Pointer<lineiter_t> lineiter_next_plain(Pointer<lineiter_t> li)
        {
            /* We are reading the next line */
            li.Deref.lineno++;

            /* Read a line and check for EOF. */
            if (li.Deref.fh.fgets(li.Deref.buf, li.Deref.bsiz).IsNull)
            {
                lineiter_free(li);
                return PointerHelpers.NULL<lineiter_t>();
            }
            /* If we managed to read the whole thing, then we are done
             * (this will be by far the most common result). */
            li.Deref.len = (int)cstring.strlen(li.Deref.buf);
            if (li.Deref.len < li.Deref.bsiz - 1 || li.Deref.buf[li.Deref.len - 1] == '\n')
                return li;

            /* Otherwise we have to reallocate and keep going. */
            while (true)
            {
                li.Deref.bsiz *= 2;
                li.Deref.buf = ckd_alloc.ckd_realloc(li.Deref.buf, (uint)li.Deref.bsiz);
                /* If we get an EOF, we are obviously done. */
                if (li.Deref.fh.fgets(li.Deref.buf + li.Deref.len, li.Deref.bsiz - li.Deref.len).IsNull)
                {
                    li.Deref.len += (int)cstring.strlen(li.Deref.buf + li.Deref.len);
                    return li;
                }
                li.Deref.len += (int)cstring.strlen(li.Deref.buf + li.Deref.len);
                /* If we managed to read the whole thing, then we are done. */
                if (li.Deref.len < li.Deref.bsiz - 1 || li.Deref.buf[li.Deref.len - 1] == '\n')
                    return li;
            }

            /* Shouldn't get here. */
            return li;
        }
        public static Pointer<lineiter_t> lineiter_next(Pointer<lineiter_t> li)
        {
            if (li.Deref.clean == 0)
                return lineiter_next_plain(li);

            for (li = lineiter_next_plain(li); li.IsNonNull; li = lineiter_next_plain(li))
            {
                if (li.Deref.buf.IsNonNull)
                {
                    li.Deref.buf = strfuncs.string_trim(li.Deref.buf, string_edge_e.STRING_BOTH);
                    if (li.Deref.buf[0] != 0 && li.Deref.buf[0] != '#')
                        break;
                }
            }
            return li;
        }

        public static void lineiter_free(Pointer<lineiter_t> li)
        {
            if (li.IsNull)
                return;

            ckd_alloc.ckd_free<byte>(li.Deref.buf);
            ckd_alloc.ckd_free<lineiter_t>(li);
        }

        public static int fread_retry(Pointer<byte> pointer, int size, int num_items, FILE stream)
        {
            Pointer<byte> data;
            uint n_items_read;
            uint n_items_rem;
            uint n_retry_rem;
            int loc;

            n_retry_rem = FREAD_RETRY_COUNT;

            data = pointer;
            loc = 0;
            n_items_rem = (uint)num_items;

            do
            {
                n_items_read = stream.fread(data.Point(loc), (uint)size, n_items_rem);

                n_items_rem -= n_items_read;

                if (n_items_rem > 0)
                {
                    /* an incomplete read occurred */

                    if (n_retry_rem == 0)
                        return -1;

                    if (n_retry_rem == FREAD_RETRY_COUNT)
                    {
                        err.E_ERROR_SYSTEM("fread() failed; retrying...\n");
                    }

                    --n_retry_rem;

                    loc += (int)n_items_read * size;
                }
            } while (n_items_rem > 0);

            return num_items;
        }

        public static int stat_retry(Pointer<byte> file, BoxedValue<stat_t> statbuf)
        {
            int i;

            for (i = 0; i < STAT_RETRY_COUNT; i++)
            {
                if (FILE.stat(file, statbuf) == 0)
                    return 0;
                if (i == 0)
                {
                    err.E_ERROR_SYSTEM(string.Format("Failed to stat file '{0}'; retrying...", cstring.FromCString(file)));
                }
            }

            return -1;
        }

        public static long stat_mtime(Pointer<byte> file)
        {
            BoxedValue<stat_t> statbuf = new BoxedValue<stat_t>();

            if (FILE.stat(file, statbuf) != 0)
                return -1;

            return (statbuf.Val.st_mtime);
        }
    }
}
