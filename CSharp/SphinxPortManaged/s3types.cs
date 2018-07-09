using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class s3types
    {
        public const short BAD_S3CIPID = unchecked((short)0xFFFFU);
        public const short MAX_S3CIPID = 32767;

        public static int NOT_S3CIPID(short p)
        {
            return p < 0 ? 1 : 0;
        }

        public static int IS_S3CIPID(short p)
        {
            return p >= 0 ? 1 : 0;
        }

        public const ushort BAD_S3SSID = (ushort)0xFFFFU;
        public const ushort MAX_S3SSID = (ushort)0xFFFEU;

        public static int NOT_S3PID(int p)
        {
            return p < 0 ? 1 : 0;
        }

        public static int IS_S3PID(int p)
        {
            return p >= 0 ? 1 : 0;
        }

        public const int BAD_S3TMATID = unchecked((int)0xFFFFFFFFU);
        public const int MAX_S3TMATID = 0x7FFFFFFE;

        public static int NOT_S3SSID(int p)
        {
            return p == BAD_S3SSID ? 1 : 0;
        }

        public static int IS_S3SSID(int p)
        {
            return p != BAD_S3SSID ? 1 : 0;
        }

        public const int BAD_S3WID = unchecked((int)0xFFFFFFFFU);
        public const int MAX_S3WID = 0x7FFFFFFE;

        public static int NOT_S3WID(int p)
        {
            return p < 0 ? 1 : 0;
        }

        public static int IS_S3WID(int p)
        {
            return p >= 0 ? 1 : 0;
        }
    }
}
