using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class cmd_ln
    {
        public const int ARG_REQUIRED = 1 << 0;
        public const int ARG_INTEGER = 1 << 1;
        public const int ARG_FLOATING = 1 << 2;
        public const int ARG_STRING = 1 << 3;
        public const int ARG_BOOLEAN = 1 << 4;
        public const int ARG_STRING_LIST = 1 << 5;
        public const int REQARG_INTEGER = ARG_INTEGER | ARG_REQUIRED;
        public const int REQARG_FLOATING = ARG_FLOATING | ARG_REQUIRED;
        public const int REQARG_STRING = ARG_STRING | ARG_REQUIRED;
        public const int REQARG_BOOLEAN = ARG_BOOLEAN | ARG_REQUIRED;
        
        public static uint strnappend(BoxedValue<Pointer<byte>> dest, BoxedValue<uint> dest_allocation,
               Pointer<byte> source, uint n)
        {
            uint source_len, required_allocation;

            if (dest == null || dest_allocation == null)
                return uint.MaxValue;
            if (dest.Val.IsNull && dest_allocation.Val != 0)
                return uint.MaxValue;
            if (source.IsNull)
                return dest_allocation.Val;

            source_len = cstring.strlen(source);
            if (n != 0 && n<source_len)
                source_len = n;

            required_allocation = (dest.Val.IsNonNull ? cstring.strlen(dest.Val) : 0) + source_len + 1;
            if (dest_allocation.Val < required_allocation) {
                if (dest_allocation.Val == 0) {
                    dest.Val = ckd_alloc.ckd_calloc<byte>(required_allocation* 2);
            } else {
                    dest.Val = ckd_alloc.ckd_realloc(dest.Val, required_allocation * 2);
        }
                dest_allocation.Val = required_allocation * 2;
            }

            cstring.strncat(dest.Val, source, source_len);

            return dest_allocation.Val;
        }

        public static uint strappend(BoxedValue<Pointer<byte>> dest, BoxedValue<uint> dest_allocation,
               Pointer<byte> source)
        {
            return strnappend(dest, dest_allocation, source, 0);
        }

        public static Pointer<byte> arg_resolve_env(Pointer<byte> str)
        {
            BoxedValue<Pointer<byte>> resolved_str = new BoxedValue<Pointer<byte>>(PointerHelpers.NULL<byte>());
            Pointer<byte> env_name = PointerHelpers.Malloc<byte>(100);
            Pointer<byte> env_val;
            BoxedValue<uint> alloced = new BoxedValue<uint>(0);
            Pointer<byte> i = str;
            Pointer<byte> j;

            /* calculate required resolved_str size */
            do
            {
                j = cstring.strstr(i, cstring.ToCString("$("));
                if (j.IsNonNull)
                {
                    if (j != i)
                    {
                        strnappend(resolved_str, alloced, i, checked((uint)(j - i)));
                        i = j;
                    }
                    j = cstring.strchr(i + 2, (byte)')');
                    if (j.IsNonNull)
                    {
                        if (j - (i + 2) < 100)
                        {
                            cstring.strncpy(env_name, i + 2, checked((uint)(j - (i + 2))));
                            env_name[j - (i + 2)] = (byte)'\0';
                            env_val = PointerHelpers.NULL<byte>();
                        }
                        i = j + 1;
                    }
                    else
                    {
                        /* unclosed, copy and skip */
                        j = i + 2;
                        strnappend(resolved_str, alloced, i, checked((uint)(j - i)));
                        i = j;
                    }
                }
                else
                {
                    strappend(resolved_str, alloced, i);
                }
            } while (j.IsNonNull);

            return resolved_str.Val;
        }

        public static Pointer<Pointer<byte>> parse_string_list(Pointer<byte> str)
        {
            int count, i, j;
            Pointer<byte> p;
            Pointer<Pointer<byte>> result;

            p = str;
            count = 1;
            while (p.IsNonNull)
            {
                if (p.Deref == ',')
                    count++;
                p++;
            }
            /* Should end with NULL */
            result = ckd_alloc.ckd_calloc<Pointer<byte>>(count + 1);
            p = str;
            for (i = 0; i < count; i++)
            {
                for (j = 0; p[j] != ',' && p[j] != 0; j++) ;
                result[i] = (Pointer<byte>)ckd_alloc.ckd_calloc<byte>(j + 1);
                cstring.strncpy(result[i], p, checked((uint)(j)));
                p = p + j + 1;
            }
            return result;
        }

        public static Pointer<cmd_ln_val_t> cmd_ln_val_init(int t, Pointer<byte> name, Pointer<byte> str)
        {
            Pointer<cmd_ln_val_t> v;
            object val = null;
            Pointer<byte> e_str;

            if (str.IsNull)
            {
                /* For lack of a better default value. */
                val = null;
            }
            else
            {
                int valid = 1;
                e_str = arg_resolve_env(str);

                switch (t)
                {
                    case ARG_INTEGER:
                    case REQARG_INTEGER:
                        int pval;
                        if (stdio.sscanf_d(e_str, out pval) != 1)
                            valid = 0;
                        val = pval;
                        break;
                    case ARG_FLOATING:
                    case REQARG_FLOATING:
                        if (e_str.IsNull || e_str[0] == 0)
                            valid = 0;
                        val = strfuncs.atof_c(e_str);
                        break;
                    case ARG_BOOLEAN:
                    case REQARG_BOOLEAN:
                        if ((e_str[0] == 'y') || (e_str[0] == 't') ||
                            (e_str[0] == 'Y') || (e_str[0] == 'T') || (e_str[0] == '1'))
                        {
                            val = 1;
                        }
                        else if ((e_str[0] == 'n') || (e_str[0] == 'f') ||
                                 (e_str[0] == 'N') || (e_str[0] == 'F') |
                                 (e_str[0] == '0'))
                        {
                            val = 0;
                        }
                        else
                        {
                            err.E_ERROR(string.Format("Unparsed boolean value '{0}'\n", cstring.FromCString(str)));
                            valid = 0;
                        }
                        break;
                    case ARG_STRING:
                    case REQARG_STRING:
                        val = ckd_alloc.ckd_salloc(e_str);
                        break;
                    case ARG_STRING_LIST:
                        val = parse_string_list(e_str);
                        break;
                    default:
                        err.E_ERROR(string.Format("Unknown argument type: {0}\n", t));
                        valid = 0;
                        break;
                }

                ckd_alloc.ckd_free(e_str);
                if (valid == 0)
                    return PointerHelpers.NULL<cmd_ln_val_t>();
            }

            v = ckd_alloc.ckd_calloc_struct<cmd_ln_val_t>(1);
            v.Deref.val = val;
            v.Deref.type = t;
            v.Deref.name = ckd_alloc.ckd_salloc(name);

            return v;
        }

        public static Pointer<cmd_ln_t> parse_options(Pointer<cmd_ln_t> cmdln, Pointer<arg_t> defn, int argc, Pointer<Pointer<byte>> argv, int strict)
        {
            Pointer<cmd_ln_t> new_cmdln;

            new_cmdln = cmd_ln_parse_r(cmdln, defn, argc, argv, strict);
            /* If this failed then clean up and return NULL. */
            if (new_cmdln.IsNull)
            {
                int i;
                for (i = 0; i < argc; ++i)
                    ckd_alloc.ckd_free(argv[i]);
                ckd_alloc.ckd_free(argv);
                return PointerHelpers.NULL<cmd_ln_t>();
            }

            /* Otherwise, we need to add the contents of f_argv to the new object. */
            if (new_cmdln == cmdln)
            {
                /* If we are adding to a previously passed-in cmdln, then
                 * store our allocated strings in its f_argv. */
                new_cmdln.Deref.f_argv = ckd_alloc.ckd_realloc(new_cmdln.Deref.f_argv, checked((int)(new_cmdln.Deref.f_argc + argc)));
                argv.MemCopyTo(new_cmdln.Deref.f_argv + new_cmdln.Deref.f_argc, argc);
                ckd_alloc.ckd_free(argv);
                new_cmdln.Deref.f_argc += checked((uint)argc);
            }
            else
            {
                /* Otherwise, store f_argc and f_argv. */
                new_cmdln.Deref.f_argc = checked((uint)argc);
                new_cmdln.Deref.f_argv = argv;
            }

            return new_cmdln;
        }

        public static void cmd_ln_val_free(Pointer<cmd_ln_val_t> val)
        {
            int i;
            if ((val.Deref.type & ARG_STRING_LIST) != 0)
            {
                Pointer<Pointer<byte>> array = (Pointer<Pointer<byte>>)val.Deref.val;
                if (array.IsNonNull)
                {
                    for (i = 0; array[i].IsNonNull; i++)
                    {
                        ckd_alloc.ckd_free(array[i]);
                    }
                    ckd_alloc.ckd_free(array);
                }
            }

            if ((val.Deref.type & ARG_STRING) != 0 && val.Deref.val != null)
            {
                ckd_alloc.ckd_free((Pointer<byte>)val.Deref.val);
            }

            ckd_alloc.ckd_free(val.Deref.name);
            ckd_alloc.ckd_free(val);
        }

        public static Pointer<cmd_ln_t> cmd_ln_parse_r(Pointer<cmd_ln_t> inout_cmdln, Pointer<arg_t> defn, int argc, Pointer<Pointer<byte>> argv, int strict)
        {
            int i, j, n, argstart;
            Pointer<hash_table_t> defidx = PointerHelpers.NULL<hash_table_t>();
            Pointer<cmd_ln_t> cmdln;

            /* Construct command-line object */
            if (inout_cmdln.IsNull)
            {
                cmdln = ckd_alloc.ckd_calloc_struct<cmd_ln_t>(1);
                cmdln.Deref.refcount = 1;
            }
            else
                cmdln = inout_cmdln;

            /* Build a hash table for argument definitions */
            defidx = hash_table.hash_table_new(50, 0);
            if (defn.IsNonNull)
            {
                for (n = 0; defn[n] != null; n++)
                {
                    object v;
                    v = hash_table.hash_table_enter<Pointer<arg_t>>(defidx, defn[n].name, defn.Point(n));
                    if (strict != 0 && (!v.Equals(defn.Point(n))))
                    {
                        err.E_ERROR(string.Format("Duplicate argument name in definition: {0}\n", cstring.FromCString(defn[n].name)));
                        goto error;
                    }
                }
            }
            else
            {
                /* No definitions. */
                n = 0;
            }

            /* Allocate memory for argument values */
            if (cmdln.Deref.ht.IsNull)
                cmdln.Deref.ht = hash_table.hash_table_new(n, 0 /* argument names are case-sensitive */ );


            /* skip argv[0] if it doesn't start with dash */
            argstart = 0;
            if (argc > 0 && argv[0][0] != '-')
            {
                argstart = 1;
            }

            /* Parse command line arguments (name-value pairs) */
            for (j = argstart; j < argc; j += 2)
            {
                Pointer<arg_t> argdef;
                Pointer<cmd_ln_val_t> val;
                object v;
                BoxedValue<object> boxed_v = new BoxedValue<object>();
                if (hash_table.hash_table_lookup(defidx, argv[j], boxed_v) < 0)
                {
                    if (strict != 0)
                    {
                        err.E_ERROR(string.Format("Unknown argument name '{0}'\n", cstring.FromCString(argv[j])));
                        goto error;
                    }
                    else if (defn.IsNull)
                        v = null;
                    else
                        continue;
                }
                v = boxed_v.Val;
                argdef = (Pointer<arg_t>)v;

                /* Enter argument value */
                if (j + 1 >= argc)
                {
                    cmd_ln_print_help_r(cmdln, defn);
                    err.E_ERROR(string.Format("Argument value for '{0}' missing\n", cstring.FromCString(argv[j])));
                    goto error;
                }

                if (argdef.IsNull)
                    val = cmd_ln_val_init(ARG_STRING, argv[j], argv[j + 1]);
                else
                {
                    if ((val = cmd_ln_val_init(argdef.Deref.type, argv[j], argv[j + 1])).IsNull)
                    {
                        cmd_ln_print_help_r(cmdln, defn);
                        err.E_ERROR(string.Format("Bad argument value for {0}: {1}\n", cstring.FromCString(argv[j]), cstring.FromCString(argv[j + 1])));
                        goto error;
                    }
                }

                if (!(v = hash_table.hash_table_enter(cmdln.Deref.ht, val.Deref.name, val)).Equals(val))
                {
                    if (strict != 0)
                    {
                        cmd_ln_val_free(val);
                        err.E_ERROR(string.Format("Duplicate argument name in arguments: {0}\n", cstring.FromCString(argdef.Deref.name)));
                        goto error;
                    }
                    else
                    {
                        v = hash_table.hash_table_replace(cmdln.Deref.ht, val.Deref.name, val);
                        cmd_ln_val_free((Pointer<cmd_ln_val_t>)v);
                    }
                }
            }

            /* Fill in default values, if any, for unspecified arguments */
            for (i = 0; i < n; i++)
            {
                Pointer <cmd_ln_val_t> val;
                BoxedValue<object> v = new BoxedValue<object>();
                if (hash_table.hash_table_lookup(cmdln.Deref.ht, defn[i].name, v) < 0)
                {
                    if ((val = cmd_ln_val_init(defn[i].type, defn[i].name, defn[i].deflt)).IsNull)
                    {
                        err.E_ERROR
                            (string.Format("Bad default argument value for {0}: {1}\n",
                             cstring.FromCString(defn[i].name), cstring.FromCString(defn[i].deflt)));
                        goto error;
                    }
                    hash_table.hash_table_enter(cmdln.Deref.ht, val.Deref.name, val);
                }
            }

            /* Check for required arguments; exit if any missing */
            j = 0;
            for (i = 0; i < n; i++)
            {
                if ((defn[i].type & ARG_REQUIRED) != 0)
                {
                    BoxedValue<object> v = new BoxedValue<object>();
                    if (hash_table.hash_table_lookup(cmdln.Deref.ht, defn[i].name, v) != 0)
                        err.E_ERROR(string.Format("Missing required argument %s\n", defn[i].name));
                }
            }
            if (j > 0)
            {
                cmd_ln_print_help_r(cmdln, defn);
                goto error;
            }

            if (strict != 0 && argc == 1)
            {
                err.E_ERROR("No arguments given, available options are:\n");
                cmd_ln_print_help_r(cmdln, defn);
                if (defidx.IsNonNull)
                    hash_table.hash_table_free(defidx);
                if (inout_cmdln.IsNull)
                    PointerHelpers.NULL<cmd_ln_t>();
            }

            /* If we use it from something except pocketsphinx, print current values */
            if (cmd_ln_exists_r(cmdln, cstring.ToCString("-logfn")) == 0)
            {
                cmd_ln_print_values_r(cmdln, defn);
            }

            hash_table.hash_table_free(defidx);
            return cmdln;

            error:
            if (defidx.IsNonNull)
                hash_table.hash_table_free(defidx);
            if (inout_cmdln.IsNull)
                cmd_ln_free_r(cmdln);
            err.E_ERROR("Failed to parse arguments list\n");
            return PointerHelpers.NULL<cmd_ln_t>();
        }

        public static Pointer<cmd_ln_t> cmd_ln_init(Pointer<cmd_ln_t> inout_cmdln, Pointer<arg_t> defn, int strict, params string[] args)
        {
            Pointer<Pointer<byte>> f_argv;
            int f_argc = args.Length;
            if (f_argc % 2 != 0)
            {
                err.E_ERROR("Number of arguments must be even!\n");
                return PointerHelpers.NULL<cmd_ln_t>();
            }
            
            // C# variable arguments simplify this function immensely
            f_argv = ckd_alloc.ckd_calloc<Pointer<byte>>(f_argc);
            for (int c = 0; c < f_argc; c++)
            {
                f_argv[c] = cstring.ToCString(args[c]);
            }

            return parse_options(inout_cmdln, defn, f_argc, f_argv, strict);
        }

        public static Pointer<cmd_ln_t> cmd_ln_parse_file_r(Pointer<cmd_ln_t> inout_cmdln, Pointer<arg_t> defn, Pointer<byte> filename, int strict)
        {
            FILE file;
            int argc;
            int argv_size;
            Pointer<byte> str;
            int arg_max_length = 512;
            int len = 0;
            int quoting, ch;
            Pointer<Pointer<byte>> f_argv;
            int rv = 0;
            Pointer<byte>separator = cstring.ToCString(" \t\r\n");

            if ((file = FILE.fopen(filename, "r")) == null)
            {
                err.E_ERROR(string.Format("Cannot open configuration file {0} for reading\n", cstring.FromCString(filename)));
                return PointerHelpers.NULL<cmd_ln_t>();
            }

            ch = file.fgetc();
            /* Skip to the next interesting character */
            for (; ch != FILE.EOF && cstring.strchr(separator, checked((byte)ch)).IsNonNull; ch = file.fgetc()) ;

            if (ch == FILE.EOF)
            {
                file.fclose();
                return PointerHelpers.NULL<cmd_ln_t>();
            }

            /*
             * Initialize default argv, argc, and argv_size.
             */
            argv_size = 30;
            argc = 0;
            f_argv = ckd_alloc.ckd_calloc<Pointer<byte>>(argv_size);
            /* Silently make room for \0 */
            str = ckd_alloc.ckd_calloc<byte>(arg_max_length + 1);
            quoting = 0;

            do
            {
                /* Handle arguments that are commented out */
                if (len == 0 && argc % 2 == 0)
                {
                    while (ch == '#')
                    {
                        /* Skip everything until newline */
                        for (ch = file.fgetc(); ch != FILE.EOF && ch != '\n'; ch = file.fgetc()) ;
                        /* Skip to the next interesting character */
                        for (ch = file.fgetc(); ch != FILE.EOF && cstring.strchr(separator, checked((byte)ch)).IsNonNull; ch = file.fgetc()) ;
                    }

                    /* Check if we are at the last line (without anything interesting in it) */
                    if (ch == FILE.EOF)
                        break;
                }

                /* Handle quoted arguments */
                if (ch == '"' || ch == '\'')
                {
                    if (quoting == ch) /* End a quoted section with the same type */
                        quoting = 0;
                    else if (quoting != 0)
                    {
                        err.E_ERROR("Nesting quotations is not supported!\n");
                        rv = 1;
                        break;
                    }
                    else
                        quoting = ch; /* Start a quoted section */
                }
                else if (ch == FILE.EOF || (quoting == 0 && cstring.strchr(separator, checked((byte)ch)).IsNonNull))
                {
                    /* Reallocate argv so it is big enough to contain all the arguments */
                    if (argc >= argv_size)
                    {
                        Pointer<Pointer<byte>> tmp_argv;
                        if ((tmp_argv = ckd_alloc.ckd_realloc<Pointer<byte>>(f_argv, argv_size * 2)).IsNull)
                        {
                            rv = 1;
                            break;
                        }
                        f_argv = tmp_argv;
                        argv_size *= 2;
                    }

                    /* Add the string to the list of arguments */
                    f_argv[argc] = ckd_alloc.ckd_salloc(str);
                    len = 0;
                    str[0] = (byte)'\0';
                    argc++;

                    if (quoting != 0)
                        err.E_WARN("Unclosed quotation, having FILE.EOF close it...\n");

                    /* Skip to the next interesting character */
                    for (; ch != FILE.EOF && cstring.strchr(separator, checked((byte)ch)).IsNonNull; ch = file.fgetc()) ;

                    if (ch == FILE.EOF)
                        break;

                    /* We already have the next character */
                    continue;
                }
                else
                {
                    if (len >= arg_max_length)
                    {
                        /* Make room for more chars (including the \0 !) */
                        Pointer<byte> tmp_str = str;
                        if ((tmp_str = ckd_alloc.ckd_realloc(str, (1 + arg_max_length * 2))).IsNull)
                        {
                            rv = 1;
                            break;
                        }
                        str = tmp_str;
                        arg_max_length *= 2;
                    }
                    /* Add the char to the argument string */
                    str[len++] = checked((byte)ch);
                    /* Always null terminate */
                    str[len] = (byte)'\0';
                }

                ch = file.fgetc();
            } while (true);

            file.fclose();

            ckd_alloc.ckd_free(str);

            if (rv != 0)
            {
                for (ch = 0; ch < argc; ++ch)
                    ckd_alloc.ckd_free(f_argv[ch]);
                ckd_alloc.ckd_free(f_argv);
                return PointerHelpers.NULL<cmd_ln_t>();
            }

            return parse_options(inout_cmdln, defn, argc, f_argv, strict);
        }

        public static void cmd_ln_print_help_r(Pointer<cmd_ln_t> cmdln, Pointer<arg_t> defn)
        {
            if (defn.IsNull)
                return;
            Console.WriteLine("Arguments list definition:\n(print statement not ported yet)\n");
            //arg_dump_r(cmdln, fp, defn, TRUE);
        }

        public static void cmd_ln_print_values_r(Pointer<cmd_ln_t> cmdln, Pointer<arg_t> defn)
        {
            if (defn.IsNull)
                return;
            Console.WriteLine("Current configuration:\n(print statement not ported yet)\n");
            //arg_dump_r(cmdln, fp, defn, FALSE);
        }

        public static int cmd_ln_exists_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name)
        {
            BoxedValue<object> val = new BoxedValue<object>();
            if (cmdln.IsNull)
                return 0;
            return (hash_table.hash_table_lookup(cmdln.Deref.ht, name, val) == 0) ? 1 : 0;
        }

        public static Pointer<cmd_ln_val_t> cmd_ln_access_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name)
        {
            BoxedValue<object> val = new BoxedValue<object>();
            if (hash_table.hash_table_lookup(cmdln.Deref.ht, name, val) < 0)
            {
                err.E_ERROR(string.Format("Unknown argument: {0}\n", cstring.FromCString(name)));
                return PointerHelpers.NULL<cmd_ln_val_t>();
            }
            return (Pointer < cmd_ln_val_t > )val.Val;
        }

        public static Pointer<byte> cmd_ln_str_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name)
        {
            Pointer<cmd_ln_val_t> val;
            val = cmd_ln_access_r(cmdln, name);
            if (val.IsNull || val.Deref.val == null)
                return PointerHelpers.NULL<byte>();
            return (Pointer<byte>)val.Deref.val;
        }

        public static byte cmd_ln_boolean_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name)
        {
            return cmd_ln_int_r(cmdln, name) != 0 ? (byte)1 : (byte)0;
        }

        public static long cmd_ln_int_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name)
        {
            Pointer<cmd_ln_val_t> val;
            val = cmd_ln_access_r(cmdln, name);
            if (val.IsNull)
                return 0L;
            if (val.Deref.val is int)
            {
                return (long)(int)val.Deref.val;
            }
            else
            {
                return (long)val.Deref.val;
            }
        }

        public static double cmd_ln_float_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name)
        {
            Pointer<cmd_ln_val_t> val;
            val = cmd_ln_access_r(cmdln, name);
            if (val.IsNull)
                return 0.0;
            return (double)val.Deref.val;
        }

        public static void cmd_ln_set_str_extra_r(Pointer<cmd_ln_t> cmdln, Pointer<byte> name, Pointer<byte> str)
        {
            BoxedValue<object> boxed_val = new BoxedValue<object>();
            Pointer<cmd_ln_val_t> val;
            if (hash_table.hash_table_lookup(cmdln.Deref.ht, name, boxed_val) < 0)
            {
                val = cmd_ln_val_init(ARG_STRING, name, str);
                hash_table.hash_table_enter(cmdln.Deref.ht, val.Deref.name, val);
            }
            else
            {
                val = (Pointer<cmd_ln_val_t>)boxed_val.Val;
                ckd_alloc.ckd_free((Pointer<byte>)val.Deref.val);
                val.Deref.val = ckd_alloc.ckd_salloc(str);
            }
        }

        public static Pointer<cmd_ln_t> cmd_ln_retain(Pointer<cmd_ln_t> cmdln)
        {
            ++cmdln.Deref.refcount;
            return cmdln;
        }

        public static int cmd_ln_free_r(Pointer<cmd_ln_t> cmdln)
        {
            if (cmdln.IsNull)
                return 0;
            if (--cmdln.Deref.refcount > 0)
                return cmdln.Deref.refcount;

            if (cmdln.Deref.ht.IsNonNull)
            {
                Pointer<gnode_t> entries;
                Pointer<gnode_t> gn;
                BoxedValue<int> n = new BoxedValue<int>();
                entries = hash_table.hash_table_tolist(cmdln.Deref.ht, n);
                for (gn = entries; gn.IsNonNull; gn = glist.gnode_next(gn))
                {
                    Pointer<hash_entry_t> e = (Pointer<hash_entry_t>)glist.gnode_ptr(gn);
                    cmd_ln_val_free((Pointer<cmd_ln_val_t>)e.Deref.val);
                }
                glist.glist_free(entries);
                hash_table.hash_table_free(cmdln.Deref.ht);
                cmdln.Deref.ht = PointerHelpers.NULL<hash_table_t>();
            }

            if (cmdln.Deref.f_argv.IsNonNull)
            {
                int i;
                for (i = 0; i < cmdln.Deref.f_argc; ++i)
                {
                    ckd_alloc.ckd_free(cmdln.Deref.f_argv[i]);
                }
                ckd_alloc.ckd_free(cmdln.Deref.f_argv);
                cmdln.Deref.f_argv = PointerHelpers.NULL<Pointer<byte>>();
                cmdln.Deref.f_argc = 0;
            }
            ckd_alloc.ckd_free(cmdln);
            return 0;
        }
    }
}
