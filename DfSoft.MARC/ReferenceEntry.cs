using DfSoft.MARC.Utils;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DfSoft.MARC
{
    public class ReferenceEntry : DirectoryEntry
    {
        protected readonly List<byte> raw = new List<byte>();

        public override int Length => raw.Count;

        public ReferenceEntry(byte[] tagArray, int lenOfImplDefined = 0) : base(tagArray, lenOfImplDefined)
        {

        }

        public ReferenceEntry(string tagString, int lenOfImplDefined = 0) : this(Encoding.UTF8.GetBytes(tagString), lenOfImplDefined)
        {

        }

        public static ReferenceEntry Parse(byte[] tagArray, int lenOfImplDefined, byte[] bytes)
        {
            ReferenceEntry ret = new ReferenceEntry(tagArray, lenOfImplDefined);
            ret.SetValue(bytes);
            return ret;
        }

        public override byte[] Serialization()
        {
            return raw.ToArray();
        }

        public override string ToString()
        {
            // 转换为字符串时需去掉最后 1 字节的字段结束符。
            return $"{Tag.GetString()} {MarcRecord.Encoding.GetString(raw.Take(raw.Count - 1).ToArray())}";
        }

        public override void SetValue(byte[] bytes)
        {
            raw.Clear();
            if (bytes != null)
            {
                raw.AddRange(bytes);
            }
            // 最后 1 字节一定是字段结束符。
            if (raw.Count == 0 || raw.Last() != MarcRecord.FIELD_TERMINATOR)
            {
                raw.Add(MarcRecord.FIELD_TERMINATOR);
            }
        }
    }
}
