using DfSoft.MARC.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace DfSoft.MARC
{
    public class IdentifierEntry : DirectoryEntry
    {
        protected readonly List<byte> raw = new List<byte>();

        public override int Length => raw.Count;

        public IdentifierEntry(int lenOfImplDefined = 0) : base(new byte[] { 48, 48, 49 }, lenOfImplDefined)
        {
            // 标识字段的 Tag 值固定为字符“001”。
        }

        public static IdentifierEntry Parse(int lenOfImplDefined, byte[] bytes)
        {
            IdentifierEntry ret = new IdentifierEntry(lenOfImplDefined);
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
