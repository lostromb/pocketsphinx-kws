
namespace SphinxPortManaged.CPlusPlus
{
    public class BoxedValueInt
    {
        public int Val;

        public BoxedValueInt(int v = 0)
        {
            Val = v;
        }

        public override string ToString()
        {
            return Val.ToString();
        }
    }

    public class BoxedValueShort
    {
        public short Val;

        public BoxedValueShort(short v = 0)
        {
            Val = v;
        }

        public override string ToString()
        {
            return Val.ToString();
        }
    }

    public class BoxedValueSbyte
    {
        public sbyte Val;

        public BoxedValueSbyte(sbyte v = 0)
        {
            Val = v;
        }

        public override string ToString()
        {
            return Val.ToString();
        }
    }

    /// <summary>
    /// For performance reasons, do not use this generic class if possible
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BoxedValue<T>
    {
        public T Val;

        public BoxedValue(T v = default(T))
        {
            Val = v;
        }

        public override string ToString()
        {
            return Val == null ? "null" : Val.ToString();
        }
    }
}
