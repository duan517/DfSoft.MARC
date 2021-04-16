using DfSoft.MARC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DfSoft.MARC
{
    public class Subfield : IComparable<Subfield>
    {
        protected readonly List<byte> raw = new List<byte>();

        public static char SubfieldDelimiterChar { get; set; } = '$';

        public OctetGroup Identifier { get; }
        public int Length => Identifier.Length + raw.Count;

        public Subfield(byte[] idArray)
        {
            if (idArray == null)
            {
                Identifier = new OctetGroup(0);
            }
            else
            {
                // 根据 ISO 2709:2008 规定，子字段标识符必须以子字段界定符开头。S
                if (idArray.First() == MarcRecord.SUBFIELD_DELIMITER)
                {
                    Identifier = new OctetGroup(idArray);
                }
                else
                {
                    Identifier = new OctetGroup(idArray.Prepend(MarcRecord.SUBFIELD_DELIMITER).ToArray());
                }
            }
        }

        public Subfield(string idString) : this(Encoding.UTF8.GetBytes(idString))
        {

        }

        public static Subfield Parse(byte[] idArray, byte[] bytes)
        {
            Subfield ret = new Subfield(idArray);
            ret.SetValue(bytes);
            return ret;
        }

        public int CompareTo(Subfield other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }
            return Identifier.CompareTo(other.Identifier);
        }

        public override string ToString()
        {
            return $"{SubfieldDelimiterChar}{(Identifier.Length > 1 ? Identifier.GetString(1) : "")}{MarcRecord.Encoding.GetString(raw.ToArray())}";
        }

        public void SetValue(byte[] bytes)
        {
            raw.Clear();
            if (bytes != null)
            {
                raw.AddRange(bytes);
            }
        }

        public void SetValue(string value)
        {
            SetValue(MarcRecord.Encoding.GetBytes(value));
        }

        public byte[] Serialization()
        {
            return Identifier.Raw.Concat(raw.ToArray()).ToArray();
        }
    }
}
