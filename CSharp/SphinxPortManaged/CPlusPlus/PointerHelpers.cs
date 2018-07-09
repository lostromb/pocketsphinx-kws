using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    /// <summary>
    /// This is a helper class which contains static methods that involve pointers
    /// </summary>
    public static class PointerHelpers
    {
        /// <summary>
        /// Allocates a new array and returns a pointer to it
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static Pointer<E> Malloc<E>(int capacity)
        {
            //this returns a pointer inside of a random field, to make sure offset indexing works properly
            //E[] field = new E[capacity * 2];
            //return new Pointer<E>(field, new Random().Next(0, capacity - 1));
            return new Pointer<E>(capacity);
        }

        /// <summary>
        /// Allocates a new array and returns a pointer to it
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static Pointer<E> Malloc<E>(uint capacity)
        {
            return Malloc<E>((int)capacity);
        }

        /// <summary>
        /// Creates a pointer to an existing array
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="memory"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer<E> GetPointer<E>(this E[] memory, int offset = 0)
        {
            if (memory == null)
                return default(Pointer<E>);
            //if (Debugger.IsAttached && offset == memory.Length / 2)
            //{
            //    // This may be a partitioned array. Signal the debugger
            //    Debugger.Break();
            //}
            return new Pointer<E>(memory, offset);
        }

        /// <summary>
        /// "Zeroes-out" an array of structs by reinitializing the object, effectively returning all fields to default value.
        /// Note that since structs in this codebase are classes instead, this method is constrained to class types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="length"></param>
        public static void ZeroOutStruct<T>(this Pointer<T> ptr, int length) where T : class, new()
        {
            for (int c = 0; c < length; c++)
            {
                ptr[c] = new T();
            }
        }

        public static Pointer<E> NULL<E>()
        {
            return default(Pointer<E>);
        }

        public static bool IsPrimitiveIntegerType(Type type)
        {
            return (type == typeof(byte) || 
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double));
        }

        public static int GetElementSize(Type type)
        {
            if (type == typeof(byte) || type == typeof(sbyte))
            {
                return 1;
            }
            else if (type == typeof(short) || type == typeof(ushort))
            {
                return 2;
            }
            else if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
            {
                return 4;
            }
            else if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
            {
                return 8;
            }
            else
            {
                return 1;
            }
        }
    }
}
