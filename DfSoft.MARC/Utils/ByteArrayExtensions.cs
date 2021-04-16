using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DfSoft.MARC.Utils
{
    internal static class ByteArrayExtensions
    {
        public static byte[] GetSubArray(this byte[] bytes, int index, int count = -1)
        {
            return bytes.Where((v, i) => i >= index && (count < 0 || i < index + count)).ToArray();
        }

        public static string Join(this byte[] bytes)
        {
            return new string(bytes.Select(v => (char)v).ToArray());
        }

        public static string Join(this byte[] bytes, int index, int count)
        {
            return Join(GetSubArray(bytes, index, count));
        }

        public static int IndexOf(this byte[] bytes, byte value, int index = 0)
        {
            for (int i = index; i < bytes.Length; i++)
            {
                if (bytes[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
