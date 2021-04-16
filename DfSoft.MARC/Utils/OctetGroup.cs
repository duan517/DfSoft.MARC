using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DfSoft.MARC.Utils
{
    public class OctetGroup : IComparable<OctetGroup>
    {
        public byte[] Raw { get; }
        public int Length => Raw.Length;
        public byte this[int index] { get => Raw[index]; set => Raw[index] = value; }

        public OctetGroup(int capacity)
        {
            Raw = new byte[capacity];
        }

        public OctetGroup(byte[] bytes)
        {
            Raw = bytes ?? throw new ArgumentNullException();
        }

        public void SetValue(byte[] bytes)
        {
            if (bytes == null || bytes.Length > Raw.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            Array.Copy(bytes, Raw, bytes.Length);
        }

        public int GetInt(int index = 0, int count = 0)
        {
            return int.Parse(GetString(index, count));
        }

        public string GetString(int index = 0, int count = 0)
        {
            return Raw.Join(index, count != 0 ? count : Raw.Length - index);
        }

        public int CompareTo(OctetGroup other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            for (int i = 0; i < Raw.Length; i++)
            {
                if (i > other.Raw.Length)
                {
                    return 1;
                }
                else if (Raw[i] != other.Raw[i])
                {
                    return Raw[i].CompareTo(other.Raw[i]);
                }
            }

            return Raw.Length == other.Raw.Length ? 0 : -1;
        }
    }
}
