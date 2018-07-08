using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public class PointerStatistics
    {
        public PointerStatistics(int baseOffset)
        {
            this.baseOffset = baseOffset;
        }

        public int baseOffset;
        public int minReadIndex = int.MaxValue;
        public int maxReadIndex = int.MinValue;
        public int minWriteIndex = int.MaxValue;
        public int maxWriteIndex = int.MinValue;

        public Tuple<int, int> ReadRange
        {
            get
            {
                if (minReadIndex == int.MaxValue || maxReadIndex == int.MinValue)
                    return null;
                return new Tuple<int, int>(minReadIndex - baseOffset, maxReadIndex - baseOffset);
            }
        }

        public Tuple<int, int> WriteRange
        {
            get
            {
                if (minWriteIndex == int.MaxValue || maxWriteIndex == int.MinValue)
                    return null;
                return new Tuple<int, int>(minWriteIndex - baseOffset, maxWriteIndex - baseOffset);
            }
        }
    }
}
