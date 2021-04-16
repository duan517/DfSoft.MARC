using DfSoft.MARC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DfSoft.MARC
{
    public abstract class DirectoryEntry : IComparable<DirectoryEntry>
    {
        public OctetGroup Tag { get; } = new OctetGroup(3);
        public OctetGroup Indicator { get; }
        public int LenOfIdentifier { get; }
        public OctetGroup ImplDefined { get; }
        public bool IsFragment { get; set; } = false;
        public abstract int Length { get; }

        public abstract byte[] Serialization();
        public abstract void SetValue(byte[] value);

        public DirectoryEntry(byte[] tagArray, int lenOfImplDefined = 0, int lenOfIndicator = 0, int lenOfIdentifier = 0)
        {
            Tag.SetValue(tagArray);
            Indicator = new OctetGroup(lenOfIndicator);
            LenOfIdentifier = lenOfIdentifier;
            ImplDefined = new OctetGroup(lenOfImplDefined);
        }

        public int CompareTo(DirectoryEntry other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }
            return Tag.CompareTo(other.Tag);
        }

        public void SetValue(string value)
        {
            SetValue(MarcRecord.Encoding.GetBytes(value));
        }
    }
}
