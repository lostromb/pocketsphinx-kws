
namespace SphinxPortManaged.CPlusPlus
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text;
    
    /// <summary>
    /// This simulates a C++ style pointer as far as can be implemented in C#. It represents a handle
    /// to an array of objects, along with a base offset that represents the address.
    /// When you are programming in debug mode, this class also enforces memory boundaries,
    /// tracks uninitialized values, and also records all statistics of accesses to its base array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Pointer<T>
    {
        #region Properties

        private IMemoryBlockAccess<T> _memory;
        private int _elementsOffset; // Number of elements to offset in the base array
        private int _elementSize; // Size in bytes of each array element (set to 1 for non-primitive types)

        #endregion

        #region Constructors
        
        public Pointer(int capacity)
        {
            _memory = new BasicMemoryBlockAccess<T>(new MemoryBlock<T>(capacity));
            _elementsOffset = 0;
            _elementSize = PointerHelpers.GetElementSize(typeof(T));
        }

        public Pointer(T[] buffer)
        {
            _memory = new BasicMemoryBlockAccess<T>(new MemoryBlock<T>(buffer));
            _elementsOffset = 0;
            _elementSize = PointerHelpers.GetElementSize(typeof(T));
        }

        public Pointer(T[] buffer, int absoluteOffsetElements)
        {
            _memory = new BasicMemoryBlockAccess<T>(new MemoryBlock<T>(buffer));
            _elementsOffset = absoluteOffsetElements;
            _elementSize = PointerHelpers.GetElementSize(typeof(T));
        }

        public Pointer(T[] buffer, uint absoluteOffsetElements) : this(buffer, (int)absoluteOffsetElements)
        {
        }

        public Pointer(IMemoryBlockAccess<T> memoryAccess, int absoluteOffsetElements)
        {
            _memory = memoryAccess;
            _elementsOffset = absoluteOffsetElements;
            _elementSize = PointerHelpers.GetElementSize(typeof(T));
        }

        #endregion

        #region Accessors

        public T this[int index]
        {
            get
            {
                return _memory[index + _elementsOffset];
            }

            set
            {
                _memory[index + _elementsOffset] = value;
            }
        }

        public T this[uint index]
        {
            get
            {
                return _memory[(int)index + _elementsOffset];
            }

            set
            {
                _memory[(int)index + _elementsOffset] = value;
            }
        }

        public void Set(int index, T newVal)
        {
            _memory[index + _elementsOffset] = newVal;
        }

        public T Deref
        {
            get
            {
                return _memory[_elementsOffset];
            }
            set
            {
                _memory[_elementsOffset] = value;
            }
        }
        
        /// <summary>
        /// Used when testing the pointer object as a boolean, e.g. "if (ptr) { }".
        /// Indicates whether this pointer refers to valid data or not
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool IsNonNull => _memory != null;
        public bool IsNull => _memory == null;

        /// <summary>
        /// Overload operator + will dereference the pointer (I would have used * but that can't be overridden as a unary operator)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T operator +(Pointer<T> t)
        {
            return t[0];
        }

        #endregion

        #region Iterators

        public static Pointer<T> operator ++(Pointer<T> t)
        {
            return t.Point(1);
        }

        public static Pointer<T> operator --(Pointer<T> t)
        {
            return t.Point(-1);
        }

        /// <summary>
        /// Returns the value currently under the pointer, and returns a new pointer with +1 offset.
        /// This method is not very efficient because it creates new pointers; this is because we must preserve
        /// the pass-by-value nature of C++ pointers when they are used as arguments to functions
        /// </summary>
        /// <returns></returns>
        public Pointer<T> Iterate(out T returnVal)
        {
            returnVal = this[0];
            return Point(1);
        }
        
        public Pointer<T> Point(int relativeOffset)
        {
            if (relativeOffset == 0) return this;
            return new Pointer<T>(_memory, _elementsOffset + relativeOffset);
        }

        public Pointer<T> Point(uint relativeOffset)
        {
            if (relativeOffset == 0) return this;
            return new Pointer<T>(_memory, _elementsOffset + (int)relativeOffset);
        }

        /// <summary>
        /// Simulates pointer zooming: newPtr = &amp;ptr[offset].
        /// Returns a pointer that is offset from this one within the same buffer by N elements
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer<T> operator +(Pointer<T> arg, int offset)
        {
            return new Pointer<T>(arg._memory, arg._elementsOffset + offset);
        }

        public static Pointer<T> operator +(Pointer<T> arg, uint offset)
        {
            return new Pointer<T>(arg._memory, (int)(arg._elementsOffset + offset));
        }

        /// <summary>
        /// Simulates pointer zooming: newPtr = &amp;ptr[-offset].
        /// Returns a pointer that is offset from this one within the same buffer by N elements
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer<T> operator -(Pointer<T> arg, int offset)
        {
            return new Pointer<T>(arg._memory, arg._elementsOffset - offset);
        }

        /// <summary>
        /// Returns the difference between two pointers in units of ELEMENTS
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static int operator -(Pointer<T> one, Pointer<T> two)
        {
            return (one._elementsOffset - two._elementsOffset)/* * one._elementSize*/;
        }

        /// <summary>
        /// Determines if two pointers refer to the same location in the same block of memory.
        /// WARNING!!! Not very reliable since it depends on reference equality!
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator ==(Pointer<T> one, Pointer<T> two)
        {
            return one._elementsOffset == two._elementsOffset &&
                one._memory == two._memory;
        }

        public static bool operator !=(Pointer<T> one, Pointer<T> two)
        {
            return one._elementsOffset != two._elementsOffset ||
                one._memory != two._memory;
        }

        #endregion

        #region Copies and utilities

        /// <summary>
        /// Copies the contents of this pointer, starting at its current address, into the space of another pointer.
        /// !!! IMPORTANT !!! REMEMBER THAT C++ memcpy is (DEST, SOURCE, LENGTH) !!!!
        /// IN C# IT IS (SOURCE, DEST, LENGTH). DON'T GET SCOOPED LIKE I DID
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="lengthInElements"></param>
        public void MemCopyTo(Pointer<T> destination, int lengthInElements)
        {
            Assert(lengthInElements >= 0, "Cannot memcopy() with a negative length!");
            for (int c = 0; c < lengthInElements; c++)
            {
                destination[c] = _memory[c + _elementsOffset];
            }
        }

        /// <summary>
        /// Copies the contents of this pointer, starting at its current address, into an array.
        /// !!! IMPORTANT !!! REMEMBER THAT C++ memcpy is (DEST, SOURCE, LENGTH) !!!!
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="length"></param>
        public void MemCopyTo(T[] destination, int destOffset, int lengthInElements)
        {
            Assert(lengthInElements >= 0, "Cannot memcopy() with a negative length!");
            for (int c = 0; c < lengthInElements; c++)
            {
                destination[c + destOffset] = _memory[c + _elementsOffset];
            }
        }

        /// <summary>
        /// Loads N elements from a source array into this pointer's space
        /// </summary>
        /// <param name="length"></param>
        public void MemCopyFrom(T[] source, int sourceOffset, int lengthInElements)
        {
            Assert(lengthInElements >= 0, "Cannot memcopy() with a negative length!");
            for (int c = 0; c < lengthInElements; c++)
            {
                _memory[c + _elementsOffset] = source[c + sourceOffset];
            }
        }

        /// <summary>
        /// Assigns a certain value to a range of spaces in this array
        /// </summary>
        /// <param name="value">The value to set</param>
        /// <param name="length">The number of elements to write</param>
        public void MemSet(T value, int lengthInElements)
        {
            Assert(lengthInElements >= 0, "Cannot memset() with a negative length!");
            MemSet(value, (uint)lengthInElements);
        }

        /// <summary>
        /// Assigns a certain value to a range of spaces in this array
        /// </summary>
        /// <param name="value">The value to set</param>
        /// <param name="lengthInElements">The number of elements to write</param>
        public void MemSet(T value, uint lengthInElements)
        {
            for (int c = _elementsOffset; c < _elementsOffset + lengthInElements; c++)
            {
                _memory[c] = value;
            }
        }

        public void MemMoveTo(Pointer<T> other, int lengthInElements)
        {
            if (_memory == other._memory)
            {
                // Pointers refer to the same array, perform a move
                //if (debug)
                //    PrintMemCopy(_array, _offset, length);
                MemMove(other._elementsOffset - _elementsOffset, lengthInElements);
            }
            else
            {
                // Pointers refer to different arrays (if you end up here you probably wanted to just to MemCopy())
                // Debug.WriteLine("Unnecessary memmove detected");
                MemCopyTo(other, lengthInElements);
            }
        }

        /// <summary>
        /// Moves regions of memory within the bounds of this pointer's array.
        /// Extra checks are done to ensure that the data is not corrupted if the copy
        /// regions overlap
        /// </summary>
        /// <param name="move_dist">The offset to send this pointer's data to</param>
        /// <param name="lengthInElements">The number of values to copy</param>
        public void MemMove(int move_dist, int lengthInElements)
        {
            Assert(lengthInElements >= 0, "Cannot memmove() with a negative length!");
            if (move_dist == 0 || lengthInElements == 0)
                return;

            // Do regions overlap?
            if ((move_dist > 0 && move_dist < lengthInElements) || (move_dist < 0 && 0 - move_dist > lengthInElements))
            {
                // Take extra precautions
                if (move_dist < 0)
                {
                    // Copy forwards
                    for (int c = 0; c < lengthInElements; c++)
                    {
                        _memory[c + _elementsOffset + move_dist] = _memory[c + _elementsOffset];
                    }
                }
                else
                {
                    // Copy backwards
                    for (int c = lengthInElements - 1; c >= 0; c--)
                    {
                        _memory[c + _elementsOffset + move_dist] = _memory[c + _elementsOffset];
                    }
                }
            }
            else
            {
                for (int c = 0; c < lengthInElements; c++)
                {
                    _memory[c + _elementsOffset + move_dist] = _memory[c + _elementsOffset];
                }
            }
        }

        #endregion
        
        public Pointer<E> ReinterpretCast<E>() where E : struct
        {
            Type currentType = typeof(T);
            Type targetType = typeof(E);

            if (!PointerHelpers.IsPrimitiveIntegerType(currentType))
            {
                throw new InvalidOperationException("Cannot cast a pointer from a non-primitive type");
            }
            if (!PointerHelpers.IsPrimitiveIntegerType(targetType))
            {
                throw new InvalidOperationException("Cannot cast a pointer to a non-primitive type");
            }

            if (currentType == targetType)
            {
                return (Pointer<E>)(object)this;
            }

            if (!(this._memory is BasicMemoryBlockAccess<T>))
            {
                throw new InvalidOperationException("Cannot cast a pointer that has already been cast from another type");
            }
            
            // Upcasting - accessing a byte array as a sequence of wide integers
            if (currentType == typeof(byte) &&
                    (targetType == typeof(sbyte) ||
                    targetType == typeof(short) ||
                    targetType == typeof(ushort) ||
                    targetType == typeof(int) ||
                    targetType == typeof(uint) ||
                    targetType == typeof(long) ||
                    targetType == typeof(ulong) ||
                    targetType == typeof(float) ||
                    targetType == typeof(double)))
            {
                MemoryBlock<byte> block = (this._memory as BasicMemoryBlockAccess<byte>).Block;
                int targetElemSize = PointerHelpers.GetElementSize(targetType);
                return new Pointer<E>(new UpcastingMemoryBlockAccess<E>(block, _elementsOffset % targetElemSize), _elementsOffset / targetElemSize);
            }

            // Downcasting - accessing an integer array as a sequence of bytes
            if (targetType == typeof(byte) &&
                    (currentType == typeof(sbyte) ||
                    currentType == typeof(short) ||
                    currentType == typeof(ushort) ||
                    currentType == typeof(int) ||
                    currentType == typeof(uint) ||
                    currentType == typeof(long) ||
                    currentType == typeof(ulong) ||
                    currentType == typeof(float) ||
                    currentType == typeof(double)))
            {
                throw new NotImplementedException("Cannot cast a pointer in this way");
                //MemoryBlock<T> block = (this._memory as BasicMemoryBlockAccess<T>).Block;
                //int sourceElemSize = PointerHelpers.GetElementSize(currentType);
                //return (Pointer<E>)(object)(new Pointer<byte>(new DowncastingMemoryBlockAccess<T>(block), _elementsOffset * sourceElemSize));
            }

            throw new InvalidOperationException("Cannot cast a pointer from " + currentType.ToString() + " to " + targetType.ToString());
        }

        /// <summary>
        /// Indicates that this entire pointer's memory space has been freed, and this reference cannot be used again
        /// </summary>
        public void Free()
        {
            if (_memory != null)
            {
                _memory.Free();
                _memory = null;
            }

            _elementsOffset = -1;
        }

        public Pointer<T> Realloc(int new_size_in_elements)
        {
            if (new_size_in_elements == 0)
            {
                Free();
                return PointerHelpers.NULL<T>();
            }
            else if (_memory == null)
            {
                return new Pointer<T>(new_size_in_elements);
            }
            else
            {
                _memory.Realloc(new_size_in_elements);
                return this;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion error:" + message);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Pointer<T> other = (Pointer<T>)obj;
            return other._elementsOffset == _elementsOffset &&
                other._memory == _memory;
        }
        
        public override int GetHashCode()
        {
            return _memory.GetHashCode() + _elementsOffset.GetHashCode();
        }
    }
}