using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ckd_alloc
    {
        public static Pointer<T> ckd_malloc<T>(uint num_elements) where T : struct
        {
            return PointerHelpers.Malloc<T>(num_elements);
        }

        public static Pointer<T> ckd_malloc<T>(int num_elements) where T : struct
        {
            return PointerHelpers.Malloc<T>(num_elements);
        }

        public static void ckd_free<T>(Pointer<T> ptr)
        {
            ptr.Free();
        }

        public static void ckd_free_2d<T>(Pointer<Pointer<T>> ptr)
        {
            if (ptr.IsNonNull)
                ckd_free(ptr[0]);
            ckd_free(ptr);
        }

        public static void ckd_free_3d<T>(Pointer<Pointer<Pointer<T>>> ptr)
        {
            if (ptr.IsNonNull && ptr[0].IsNonNull)
                ckd_free(ptr[0][0]);
            if (ptr.IsNonNull)
                ckd_free(ptr[0]);
            ckd_free(ptr);
        }

        public static Pointer<T> ckd_calloc<T>(int num_elements) where T : struct
        {
            // The semantics of this function rely on zero-initialization, which fortunately the runtime does for us
            return new Pointer<T>(new T[num_elements]);
        }

        public static Pointer<T> ckd_calloc<T>(uint num_elements) where T : struct
        {
            // The semantics of this function rely on zero-initialization, which fortunately the runtime does for us
            return new Pointer<T>(new T[num_elements]);
        }

        public static Pointer<T> ckd_calloc_struct<T>(int num_elements) where T : new()
        {
            T[] array = new T[num_elements];
            for (int c = 0; c < num_elements; c++)
            {
                array[c] = new T();
            }

            return new Pointer<T>(array);
        }

        public static Pointer<T> ckd_calloc_struct<T>(uint num_elements) where T : new()
        {
            T[] array = new T[num_elements];
            for (int c = 0; c < num_elements; c++)
            {
                array[c] = new T();
            }

            return new Pointer<T>(array);
        }

        public static Pointer<byte> ckd_salloc(Pointer<byte> orig)
        {
            uint len;
            Pointer<byte> buf;

            if (orig.IsNull)
                return PointerHelpers.NULL<byte>();

            len = cstring.strlen(orig) + 1;
            buf = ckd_malloc<byte>(len);

            cstring.strcpy(buf, orig);
            return (buf);
        }

        public static Pointer<T> ckd_realloc<T>(Pointer<T> ptr, int new_size_in_elements)
        {
            return ptr.Realloc(new_size_in_elements);
        }

        public static Pointer<T> ckd_realloc<T>(Pointer<T> ptr, uint new_size_in_elements)
        {
            return ptr.Realloc((int)new_size_in_elements);
        }

        /// <summary>
        /// Allocates a 2d array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="d1">Dimension 1, in ELEMENTS</param>
        /// <param name="d2">Dimension 2, in ELEMENTS</param>
        /// <returns></returns>
        public static Pointer<Pointer<T>> ckd_calloc_2d<T>(uint d1, uint d2) where T : struct
        {
            Pointer<Pointer<T>> _ref;
            Pointer<T> mem;
            uint i, offset;

            mem = ckd_calloc<T>(d1* d2);
            _ref = ckd_malloc<Pointer<T>>(d1);

            for (i = 0, offset = 0; i<d1; i++, offset += d2)
                _ref[i] = mem + offset;

            return _ref;
        }

        public static Pointer<Pointer<T>> ckd_calloc_struct_2d<T>(uint d1, uint d2) where T : new()
        {
            Pointer<Pointer<T>> _ref;
            Pointer<T> mem;
            uint i, offset;

            mem = ckd_calloc_struct<T>(d1 * d2);
            _ref = ckd_malloc<Pointer<T>>(d1);

            for (i = 0, offset = 0; i < d1; i++, offset += d2)
                _ref[i] = mem + offset;

            return _ref;
        }

        public static Pointer<Pointer<Pointer<T>>> ckd_calloc_3d<T>(uint d1, uint d2, uint d3) where T : struct
        {
            Pointer<Pointer<Pointer<T>>> ref1;
            Pointer<Pointer<T>> ref2;
            Pointer<T> mem;
            uint i, j, offset;

            mem = ckd_calloc<T>(d1* d2 * d3);
            ref1 = ckd_malloc<Pointer<Pointer<T>>>(d1);
            ref2 = ckd_malloc<Pointer<T>>(d1* d2);

            for (i = 0, offset = 0; i<d1; i++, offset += d2)
                ref1[i] = ref2 + offset;

            offset = 0;
            for (i = 0; i<d1; i++) {
                for (j = 0; j<d2; j++) {
                    ref1[i].Set((int)j, mem + offset);
                    offset += d3;
                }
            }

            return ref1;
        }

        public static Pointer<Pointer<Pointer<T>>> ckd_calloc_struct_3d<T>(uint d1, uint d2, uint d3) where T : new()
        {
            Pointer<Pointer<Pointer<T>>> ref1;
            Pointer<Pointer<T>> ref2;
            Pointer<T> mem;
            uint i, j, offset;

            mem = ckd_calloc_struct<T>(d1 * d2 * d3);
            ref1 = ckd_malloc<Pointer<Pointer<T>>>(d1);
            ref2 = ckd_malloc<Pointer<T>>(d1 * d2);

            for (i = 0, offset = 0; i < d1; i++, offset += d2)
                ref1[i] = ref2 + offset;

            offset = 0;
            for (i = 0; i < d1; i++)
            {
                for (j = 0; j < d2; j++)
                {
                    ref1[i].Set((int)j, mem + offset);
                    offset += d3;
                }
            }

            return ref1;
        }

        /// <summary>
        /// Creates a 3-d array of access pointers over an existing linearized storage space.
        ///  All sizes (d1, d2, d3) are represented in terms of elements, not bytes!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <param name="d3"></param>
        /// <param name="store">The data to wrap around</param>
        /// <returns></returns>
        public static Pointer<Pointer<Pointer<T>>> ckd_alloc_3d_ptr<T>(
            uint d1,
           uint d2,
           uint d3,
           Pointer<T> store) where T: struct
        {
            Pointer<Pointer<T>> tmp1;
            Pointer<Pointer<Pointer<T>>> _out;
            uint i, j;

            tmp1 = ckd_calloc<Pointer<T>>(d1 * d2);

            _out  = ckd_calloc<Pointer<Pointer<T>>>(d1);

            for (i = 0, j = 0; i < d1 * d2; i++, j += d3)
            {
                tmp1[i] = store.Point(j);
            }

            for (i = 0, j = 0; i < d1; i++, j += d2)
            {
	            _out[i] = tmp1.Point(j);
            }

            return _out;
        }
    }
}
