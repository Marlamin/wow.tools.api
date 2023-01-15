using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static class BinaryReaderExtensions
    {
        public static int ReadInt32(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToInt32(reader.ReadInvertedBytes(4), 0);
            }

            return reader.ReadInt32();
        }

        public static uint ReadUInt32(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToUInt32(reader.ReadInvertedBytes(4), 0);
            }

            return reader.ReadUInt32();
        }
        private static byte[] ReadInvertedBytes(this BinaryReader reader, int byteCount)
        {
            byte[] byteArray = reader.ReadBytes(byteCount);
            Array.Reverse(byteArray);

            return byteArray;
        }
        public static byte[] ToByteArray(this string str)
        {
            str = str.Replace(" ", string.Empty);
            return Convert.FromHexString(str);
        }
    }
}
    