using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class hash_table
    {
        /*
         * HACK!!  Initial hash table size is restricted by this set of primes.  (Of course,
         * collision resolution by chaining will accommodate more entries indefinitely, but
         * efficiency will drop.)
         */
        private static readonly int[] prime = {
            101, 211, 307, 401, 503, 601, 701, 809, 907,
            1009, 1201, 1601, 2003, 2411, 3001, 4001, 5003, 6007, 7001, 8009,
            9001,
            10007, 12007, 16001, 20011, 24001, 30011, 40009, 50021, 60013,
            70001, 80021, 90001,
            100003, 120011, 160001, 200003, 240007, 300007, 400009, 500009,
            600011, 700001, 800011, 900001,
            -1
        };

        public const int HASH_CASE_YES = 0;
        public const int HASH_CASE_NO = 1;

        public static object hash_entry_val(Pointer<hash_entry_t> e)
        {
            return e.Deref.val;
        }

        public static Pointer<byte> hash_entry_key(Pointer<hash_entry_t> e)
        {
            return e.Deref.key;
        }

        public static uint hash_entry_len(Pointer<hash_entry_t> e)
        {
            return e.Deref.len;
        }

        public static int hash_table_inuse(Pointer<hash_table_t> e)
        {
            return e.Deref.inuse;
        }

        public static int hash_table_size(Pointer<hash_table_t> e)
        {
            return e.Deref.size;
        }

        /*
         * This function returns a very large prime.
         */
        public static int prime_size(int size)
        {
            int i;

            for (i = 0; (prime[i] > 0) && (prime[i] < size); i++) ;
            if (prime[i] <= 0)
            {
                err.E_WARN(string.Format("Very large hash table requested ({0} entries)\n", size));
                --i;
            }
            return (prime[i]);
        }
        
        public static Pointer<hash_table_t> hash_table_new(int size, int casearg)
        {
            Pointer<hash_table_t> h;

            h = ckd_alloc.ckd_calloc_struct<hash_table_t>(1);
            h.Deref.size = prime_size(size + (size >> 1));
            h.Deref.nocase = (casearg == HASH_CASE_NO) ? 1 : 0;
            h.Deref.table = ckd_alloc.ckd_calloc_struct<hash_entry_t>(h.Deref.size);
            /* The above calloc clears h.Deref.table[*].key and .next to NULL, i.e. an empty table */

            return h;
        }
        
        /*
         * Compute hash value for given key string.
         * Somewhat tuned for English text word strings.
         */
        public static uint key2hash(Pointer<hash_table_t> h, Pointer<byte> key)
        {
            Pointer<byte> cp;

            /* This is a hack because the best way to solve it is to make sure 
               all byteacter representation is unsigned byteacter in the first place.        
               (or better unicode.) */
            byte c;
            int s;
            uint hash;

            hash = 0;
            s = 0;

            if (h.Deref.nocase != 0)
            {
                for (cp = key; cp.IsNonNull; cp++)
                {
                    c = +cp;
                    c = @case.UPPER_CASE(c);
                    hash += (uint)c << s;
                    s += 5;
                    if (s >= 25)
                        s -= 24;
                }
            }
            else
            {
                for (cp = key; cp.Deref != 0; cp++)
                {
                    hash += ((uint)cp.Deref) << s;
                    s += 5;
                    if (s >= 25)
                        s -= 24;
                }
            }

            return (hash % (uint)h.Deref.size);
        }


        public static Pointer<byte> makekey(Pointer<byte> data, uint len, Pointer<byte> key)
        {
            uint i, j;

            if (key.IsNull)
                key = ckd_alloc.ckd_calloc<byte>(len * 2 + 1);

            for (i = 0, j = 0; i < len; i++, j += 2)
            {
                key[j] = (byte)('A' + (data[i] & 0x000f));
                key[j + 1] = (byte)('J' + ((data[i] >> 4) & 0x000f));
            }
            key[j] = (byte)('\0');

            return key;
        }


        public static int keycmp_nocase(Pointer<hash_entry_t> entry, Pointer<byte> key)
        {
            byte c1, c2;
            int i;
            Pointer<byte> str;

            str = entry.Deref.key;
            for (i = 0; i < entry.Deref.len; i++)
            {
                str = str.Iterate(out c1);
                c1 = @case.UPPER_CASE(c1);
                key = key.Iterate(out c2);
                c2 = @case.UPPER_CASE(c2);
                if (c1 != c2)
                    return (c1 - c2);
            }

            return 0;
        }


        public static int keycmp_case(Pointer<hash_entry_t> entry, Pointer<byte> key)
        {
            byte c1, c2;
            int i;
            Pointer<byte> str;
            str = entry.Deref.key;
            for (i = 0; i < entry.Deref.len; i++)
            {
                str = str.Iterate(out c1);
                key = key.Iterate(out c2);
                if (c1 != c2)
                    return (c1 - c2);
            }

            return 0;
        }


        /*
         * Lookup entry with hash-value hash in table h for given key
         * Return value: hash_entry_t for key
         */
        public static Pointer<hash_entry_t> lookup(Pointer<hash_table_t> h, uint hash, Pointer<byte> key, uint len)
        {
            Pointer<hash_entry_t> entry;

            entry = h.Deref.table.Point(hash);
            if (!entry.Deref.key.IsNonNull)
                return default(Pointer<hash_entry_t>);

            if (h.Deref.nocase != 0)
            {
                while (entry.IsNonNull && ((entry.Deref.len != len)
                                 || (keycmp_nocase(entry, key) != 0)))
                    entry = entry.Deref.next;
            }
            else
            {
                while (entry.IsNonNull && ((entry.Deref.len != len)
                                 || (keycmp_case(entry, key) != 0)))
                    entry = entry.Deref.next;
            }

            return entry;
        }


        public static int hash_table_lookup(Pointer<hash_table_t> h, Pointer<byte> key, BoxedValue<object> val)
        {
            Pointer<hash_entry_t> entry;
            uint hash;
            uint len;

            hash = key2hash(h, key);
            len = cstring.strlen(key);

            entry = lookup(h, hash, key, len);
            if (entry.IsNonNull)
            {
                if (val != null)
                    val.Val = entry.Deref.val;
                return 0;
            }
            else
                return -1;
        }

        public static int hash_table_lookup_int32(Pointer<hash_table_t> h, Pointer<byte> key, BoxedValueInt val)
        {
            BoxedValue<object> vval = new BoxedValue<object>();
            int rv;

            rv = hash_table_lookup(h, key, vval);
            if (rv != 0)
                return rv;
            if (val != null)
            {
                if (vval.Val is int)
                {
                    val.Val = (int)vval.Val;
                }
                else if (vval.Val is long)
                {
                    val.Val = (int)(long)vval.Val;
                }
                else throw new Exception("what");
            }
            return 0;
        }
        
        public static int hash_table_lookup_bkey(Pointer<hash_table_t> h, Pointer<byte> key, uint len, BoxedValue<object> val)
        {
            Pointer<hash_entry_t> entry;
            uint hash;
            Pointer<byte> str;

            str = makekey((Pointer<byte>)key, len, PointerHelpers.NULL<byte>());
            hash = key2hash(h, str);
            ckd_alloc.ckd_free(str);

            entry = lookup(h, hash, key, len);
            if (entry.IsNonNull)
            {
                if (val != null)
                    val.Val = entry.Deref.val;
                return 0;
            }
            else
                return -1;
        }

        public static int hash_table_lookup_bkey_int(Pointer<hash_table_t> h, Pointer<byte> key, uint len, BoxedValueInt val)
        {
            BoxedValue<object> vval = new BoxedValue<object>();
            int rv;

            rv = hash_table_lookup_bkey(h, key, len, vval);
            if (rv != 0)
                return rv;
            if (val != null)
                val.Val = (int)(long)vval.Val;
            return 0;
        }


        public static T enter<T>(Pointer<hash_table_t> h, uint hash, Pointer<byte> key, uint len, T val, int replace)
        {
            Pointer<hash_entry_t> cur, _new;
            if ((cur = lookup(h, hash, key, len)).IsNonNull)
            {
                object oldval;
                /* Key already exists. */
                oldval = cur.Deref.val;
                if (replace != 0)
                {
                    /* Replace the pointer if replacement is requested,
                     * because this might be a different instance of the same
                     * string (this verges on magic, sorry) */
                    cur.Deref.key = key;
                    cur.Deref.val = val;
                }
                return (T)oldval;
            }

            cur = h.Deref.table.Point(hash);
            if (!cur.Deref.key.IsNonNull)
            {
                /* Empty slot at hashed location; add this entry */
                cur.Deref.key = key;
                cur.Deref.len = len;
                cur.Deref.val = val;

                /* Added by ARCHAN at 20050515. This allows deletion could work. */
                cur.Deref.next = PointerHelpers.NULL<hash_entry_t>();

            }
            else
            {
                /* Key collision; create new entry and link to hashed location */
                _new = ckd_alloc.ckd_calloc_struct<hash_entry_t>(1);
                _new.Deref.key = key;
                _new.Deref.len = len;
                _new.Deref.val = val;
                _new.Deref.next = cur.Deref.next;
                cur.Deref.next = _new;
            }
            ++h.Deref.inuse;

            return val;
        }

        /* 20050523 Added by ARCHAN  to delete a key from a hash table */
        public static object delete(Pointer<hash_table_t> h, uint hash, Pointer<byte> key, uint len)
        {
            Pointer<hash_entry_t> entry, prev;
            object val;

            prev = PointerHelpers.NULL<hash_entry_t>();
            entry = h.Deref.table.Point(hash);
            if (!entry.Deref.key.IsNonNull)
                return null;

            if (h.Deref.nocase != 0)
            {
                while (entry.IsNonNull && ((entry.Deref.len != len)
                                 || (keycmp_nocase(entry, key) != 0)))
                {
                    prev = entry;
                    entry = entry.Deref.next;
                }
            }
            else
            {
                while (entry.IsNonNull && ((entry.Deref.len != len)
                                 || (keycmp_case(entry, key) != 0)))
                {
                    prev = entry;
                    entry = entry.Deref.next;
                }
            }

            if (!entry.IsNonNull)
                return null;

            /* At this point, entry will be the one required to be deleted, prev
               will contain the previous entry
             */
            val = entry.Deref.val;

            if (!prev.IsNonNull)
            {
                /* That is to say the entry in the hash table (not the chain) matched the key. */
                /* We will then copy the things from the next entry to the hash table */
                prev = entry;
                if (entry.Deref.next.IsNonNull)
                {      /* There is a next entry, great, copy it. */
                    entry = entry.Deref.next;
                    prev.Deref.key = entry.Deref.key;
                    prev.Deref.len = entry.Deref.len;
                    prev.Deref.val = entry.Deref.val;
                    prev.Deref.next = entry.Deref.next;
                    ckd_alloc.ckd_free(entry);
                }
                else
                {                  /* There is not a next entry, just set the key to null */
                    prev.Deref.key = PointerHelpers.NULL<byte>();
                    prev.Deref.len = 0;
                    prev.Deref.next = PointerHelpers.NULL<hash_entry_t>();
                }

            }
            else
            {                      /* This case is simple */
                prev.Deref.next = entry.Deref.next;
                ckd_alloc.ckd_free(entry);
            }

            /* Do wiring and free the entry */

            --h.Deref.inuse;

            return val;
        }

        public static void hash_table_empty(Pointer<hash_table_t> h)
        {
            Pointer<hash_entry_t> e, e2;
            int i;

            for (i = 0; i < h.Deref.size; i++)
            {
                /* Free collision lists. */
                for (e = h.Deref.table[i].next; e.IsNonNull; e = e2)
                {
                    e2 = e.Deref.next;
                    ckd_alloc.ckd_free(e);
                }

                PointerHelpers.ZeroOutStruct(h.Deref.table + i, 1);
            }
            h.Deref.inuse = 0;
        }
        
        public static int hash_table_enter_int32(Pointer<hash_table_t> h, Pointer<byte> key, int val)
        {
            return hash_table_enter(h, key, val);
        }

        public static T hash_table_enter<T>(Pointer<hash_table_t> h, Pointer<byte> key, T val)
        {
            uint hash;
            uint len;

            hash = key2hash(h, key);
            len = cstring.strlen(key);
            return (enter(h, hash, key, len, val, 0));
        }

        public static object hash_table_replace(Pointer<hash_table_t> h, Pointer<byte> key, object val)
        {
            uint hash;
            uint len;

            hash = key2hash(h, key);
            len = cstring.strlen(key);
            return (enter(h, hash, key, len, val, 1));
        }

        public static object hash_table_delete(Pointer<hash_table_t> h, Pointer<byte> key)
        {
            uint hash;
            uint len;

            hash = key2hash(h, key);
            len = cstring.strlen(key);

            return (delete(h, hash, key, len));
        }

        public static int hash_table_enter_bkey_int32(Pointer<hash_table_t> h, Pointer<byte> key, uint len, int val)
        {
            return hash_table_enter_bkey(h, key, len, val);
        }

        public static T hash_table_enter_bkey<T>(Pointer<hash_table_t> h, Pointer<byte> key, uint len, T val)
        {
            uint hash;
            Pointer<byte> str;

            str = makekey(key, len, PointerHelpers.NULL<byte>());
            hash = key2hash(h, str);
            ckd_alloc.ckd_free(str);

            return (enter(h, hash, key, len, val, 0));
        }

        public static object hash_table_replace_bkey(Pointer<hash_table_t> h, Pointer<byte> key, uint len, object val)
        {
            uint hash;
            Pointer<byte> str;

            str = makekey(key, len, PointerHelpers.NULL<byte>());
            hash = key2hash(h, str);
            ckd_alloc.ckd_free(str);

            return (enter(h, hash, key, len, val, 1));
        }

        public static object hash_table_delete_bkey(Pointer<hash_table_t> h, Pointer<byte> key, uint len)
        {
            uint hash;
            Pointer<byte> str;

            str = makekey(key, len, PointerHelpers.NULL<byte>());
            hash = key2hash(h, str);
            ckd_alloc.ckd_free(str);

            return (delete(h, hash, key, len));
        }

        public static Pointer<gnode_t> hash_table_tolist(Pointer<hash_table_t> h, BoxedValue<int> count)
        {
            Pointer<gnode_t> g;
            Pointer<hash_entry_t> e;
            int i, j;

            g = PointerHelpers.NULL<gnode_t>();

            j = 0;
            for (i = 0; i < h.Deref.size; i++)
            {
                e = h.Deref.table.Point(i);

                if (e.Deref.key.IsNonNull)
                {
                    g = glist.glist_add_ptr(g, (object)e);
                    j++;

                    for (e = e.Deref.next; e.IsNonNull; e = e.Deref.next)
                    {
                        g = glist.glist_add_ptr(g, (object)e);
                        j++;
                    }
                }
            }

            if (count != null)
                count.Val = j;

            return g;
        }

        public static Pointer<hash_iter_t> hash_table_iter(Pointer<hash_table_t> h)
        {
            Pointer<hash_iter_t> itor;
            itor = ckd_alloc.ckd_calloc_struct<hash_iter_t>(1);
            itor.Deref.ht = h;
            return hash_table_iter_next(itor);
        }

        public static Pointer<hash_iter_t> hash_table_iter_next(Pointer<hash_iter_t> itor)
        {
            /* If there is an entry, walk down its list. */
            if (itor.Deref.ent.IsNonNull)
                itor.Deref.ent = itor.Deref.ent.Deref.next;
            /* If we got to the end of the chain, or we had no entry, scan
	         * forward in the table to find the next non-empty bucket. */
            if (itor.Deref.ent.IsNull)
            {
                while (itor.Deref.idx < itor.Deref.ht.Deref.size
                       && itor.Deref.ht.Deref.table[itor.Deref.idx].key.IsNull)
                    ++itor.Deref.idx;
                /* If we did not find one then delete the iterator and
		         * return NULL. */
                if (itor.Deref.idx == itor.Deref.ht.Deref.size)
                {
                    hash_table_iter_free(itor);
                    return PointerHelpers.NULL<hash_iter_t>();
                }
                /* Otherwise use this next entry. */
                itor.Deref.ent = itor.Deref.ht.Deref.table + itor.Deref.idx;
                /* Increase idx for the next time around. */
                ++itor.Deref.idx;
            }
            return itor;
        }

        public static void hash_table_iter_free(Pointer<hash_iter_t> itor)
        {
            ckd_alloc.ckd_free(itor);
        }

        public static void hash_table_free(Pointer<hash_table_t> h)
        {
            Pointer<hash_entry_t> e, e2;
            int i;

            if (!h.IsNonNull)
                return;

            /* Free additional entries created for key collision cases */
            for (i = 0; i < h.Deref.size; i++)
            {
                for (e = h.Deref.table[i].next; e.IsNonNull; e = e2)
                {
                    e2 = e.Deref.next;
                    ckd_alloc.ckd_free(e);
                }
            }

            ckd_alloc.ckd_free(h.Deref.table);
            ckd_alloc.ckd_free(h);
        }

    }
}