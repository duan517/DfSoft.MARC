using DfSoft.MARC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DfSoft.MARC
{
    public class DataEntry : DirectoryEntry
    {
        protected readonly List<byte> data = new List<byte>();
        protected readonly List<Subfield> subfields = new List<Subfield>();

        public override int Length
        {
            get
            {
                if (LenOfIdentifier == 0)
                {
                    return Indicator.Length + data.Count;
                }
                // 指示符长度 + 子字段长度 + 字段结束符。
                return Indicator.Length + subfields.Aggregate(0, (s, i) => s += i.Length) + 1;
            }
        }

        public DataEntry(byte[] tagArray, int lenOfImplDefined = 0, int lenOfIndicator = 0, int lenOfIdentifier = 0) : base(tagArray, lenOfImplDefined, lenOfIndicator, lenOfIdentifier)
        {

        }

        public DataEntry(string tagString, int lenOfImplDefined = 0, int lenOfIndicator = 0, int lenOfIdentifier = 0) : this(Encoding.UTF8.GetBytes(tagString), lenOfImplDefined, lenOfIndicator, lenOfIdentifier)
        {

        }

        public static DataEntry Parse(byte[] tagArray, int lenOfImplDefined, int lenOfIndicator, int lenOfIdentifier, byte[] bytes)
        {
            if (tagArray == null || bytes == null)
            {
                throw new ArgumentNullException();
            }

            DataEntry ret = new DataEntry(tagArray, lenOfImplDefined, lenOfIndicator, lenOfIdentifier);

            if (lenOfIndicator != 0)
            {
                ret.Indicator.SetValue(bytes.GetSubArray(0, lenOfIndicator));
            }

            if (lenOfIdentifier == 0)
            {
                // 当标识符长度为 0 时，直接把指示符后面的所有数据作为字段内容保存。
                ret.SetValue(bytes.GetSubArray(lenOfIndicator));
            }
            else
            {
                // 从指示符后面开始分析所有数据。
                for (int i = lenOfIndicator; i < bytes.Length; i++)
                {
                    // 确保子字段总是以子字段界定符开始。
                    if (bytes[i] != MarcRecord.SUBFIELD_DELIMITER)
                    {
                        continue;
                    }

                    // 查找下一个子字段的起始位置。
                    int next = bytes.IndexOf(MarcRecord.SUBFIELD_DELIMITER, i + 1);
                    // 计算当前子字段的长度。
                    int length = (next != -1 ? next : bytes.Length) - lenOfIdentifier - i;
                    // 如果子字段数据最后一个字节是字段分隔符，则不要将该字节算到子字段长度内。
                    if (bytes[i + lenOfIdentifier + length - 1] == MarcRecord.FIELD_TERMINATOR)
                    {
                        length--;
                    }

                    ret.subfields.Add(Subfield.Parse(bytes.GetSubArray(i, lenOfIdentifier), bytes.GetSubArray(i + lenOfIdentifier, length)));
                    // 跳过当前已识别为子字段内容的字符，减少扫描次数。
                    i += length > 0 ? length - 1 : 0;
                }
            }

            return ret;
        }

        public override byte[] Serialization()
        {
            if (LenOfIdentifier == 0)
            {
                // 标识符长度为 0 时，表示不分子字段，直接返回所有数据（该数据已经包含字段结束符）。
                return data.ToArray();
            }
            // 包含子字段时需依次返回子字段的内容。
            List<byte> ret = new List<byte>();
            subfields.ForEach(item => ret.AddRange(item.Serialization()));
            // 添加字段结束符。
            ret.Add(MarcRecord.FIELD_TERMINATOR);
            return ret.ToArray();
        }

        public override string ToString()
        {
            string fieldString = $"{Tag.GetString()} {Indicator.GetString()}";
            if (LenOfIdentifier == 0)
            {
                // 标识符长度为 0 时表示不分子字段，直接将数据转换为字符串返回即可。
                return fieldString + MarcRecord.Encoding.GetString(data.ToArray());
            }
            // 有标识符表示需区分子字段，需拼接各子字段的内容。
            StringBuilder ret = new StringBuilder(fieldString);
            subfields.ForEach(item => ret.Append(item));
            return ret.ToString();
        }

        public override void SetValue(byte[] bytes)
        {
            if (LenOfIdentifier != 0)
            {
                throw new MarcException("标识符长度不为 0 时需通过子字段访问。", new InvalidOperationException());
            }

            data.Clear();
            if (bytes != null)
            {
                data.AddRange(bytes);
            }
            // 不分子字段时，确保最后 1 字节一定是字段结束符。
            if (data.Count == 0 || data.Last() != MarcRecord.FIELD_TERMINATOR)
            {
                data.Add(MarcRecord.FIELD_TERMINATOR);
            }
        }

        public Subfield[] GetSubfields()
        {
            if (LenOfIdentifier == 0)
            {
                throw new MarcException("标识符长度为 0 时不能访问子字段。", new InvalidOperationException());
            }
            return subfields.ToArray();
        }

        public Subfield[] GetSubfields(string idString)
        {
            if (idString == null || idString.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (LenOfIdentifier == 0)
            {
                throw new MarcException("标识符长度为 0 时不能访问子字段。", new InvalidOperationException());
            }
            else if (LenOfIdentifier == 1)
            {
                // 子字段标识符总是以子字段界定符开始，因此当标识符长度等于 1 时，只包含界定符，无法进行查找。
                return new Subfield[0];
            }

            List<Subfield> ret = new List<Subfield>();
            foreach (var item in subfields)
            {
                if (Regex.IsMatch(item.Identifier.GetString(1), idString))
                {
                    ret.Add(item);
                }
            }
            return ret.ToArray();
        }

        public Subfield GetSubfieldAt(int index)
        {
            if (LenOfIdentifier == 0)
            {
                throw new MarcException("标识符长度为 0 时不能访问子字段。", new InvalidOperationException());
            }
            return subfields[index];
        }

        public void AddSubfield(Subfield subfield)
        {
            if (LenOfIdentifier == 0)
            {
                throw new MarcException("标识符长度为 0 时不能添加子字段。", new InvalidOperationException());
            }
            if (subfield.Identifier.Length != LenOfIdentifier)
            {
                throw new MarcException("子字段标识符长度与期望的长度不一致。", new InvalidOperationException());
            }
            subfields.Add(subfield);
        }

        public int RemoveSubfields(string idString)
        {
            if (idString == null || idString.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (LenOfIdentifier == 0)
            {
                throw new MarcException("标识符长度为 0 时不能访问子字段。", new InvalidOperationException());
            }
            else if (LenOfIdentifier == 1)
            {
                // 子字段标识符总是以子字段界定符开始，因此当标识符长度等于 1 时，只包含界定符，无法按子字段标识符删除。
                return 0;
            }

            int count = 0;
            for (var i = subfields.Count - 1; i >= 0; i--)
            {
                if (Regex.IsMatch(subfields[i].Identifier.GetString(1), idString))
                {
                    subfields.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }

        public void RemoveSubfieldAt(int index)
        {
            if (LenOfIdentifier == 0)
            {
                throw new MarcException("标识符长度为 0 时不能访问子字段。", new InvalidOperationException());
            }
            subfields.RemoveAt(index);
        }
    }
}
