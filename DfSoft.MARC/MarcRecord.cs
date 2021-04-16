using DfSoft.MARC.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DfSoft.MARC
{
    public class MarcRecord
    {
        protected List<DirectoryEntry> entries = new List<DirectoryEntry>();

        public static Encoding Encoding { get; set; } = Encoding.Default;
        // 记录分隔符。
        public const byte RECORD_TERMINATOR = 0x1d;
        // 字段分隔符。
        public const byte FIELD_TERMINATOR = 0x1e;
        // 子字段界定符。
        public const byte SUBFIELD_DELIMITER = 0x1f;

        public byte Status { get; set; } = 0x6e;
        public int LenOfIndicator { get; private set; }
        public int LenOfIdentifier { get; private set; }
        public OctetGroup ImplCode { get; } = new OctetGroup(new byte[] { 0x61, 0x6d, 0x30, 0x20 });
        public OctetGroup UserDefined { get; } = new OctetGroup(new byte[] { 0x20, 0x20, 0x20 });
        public OctetGroup DirectoryMap { get; } = new OctetGroup(new byte[] { 0x34, 0x35, 0x30, 0x20 });
        public int Length => BaseAddress + entries.Aggregate(0, (s, i) => s += i.Length) + 1;
        public int BaseAddress => 24 + (3 + DirectoryMap.GetInt(0, 1) + DirectoryMap.GetInt(1, 1) + DirectoryMap.GetInt(2, 1)) * entries.Count + 1;
        public int NumberOfEntries => entries.Count;

        public MarcRecord(int lenOfIndicator = 0, int lenOfIdentifier = 0)
        {
            LenOfIndicator = lenOfIndicator;
            LenOfIdentifier = lenOfIdentifier;
        }

        public static MarcRecord Parse(byte[] bytes)
        {
            // 有效数据至少需要 25 字节（记录标头 24 字节 + 目录区结尾的字段分隔符 1 字节）。
            // 按照 ISO 2709:2008 的规定，标头的记录长度应该把记录结束符计算在内，但在实践中发现少数 CNMARC 文件并未完全遵守此规定。
            // 因此此处不检查最后的记录结束符，防止按照标头指示的长度读取数据时，没有读到记录结束符。
            if (bytes == null || bytes.Length < 25)
            {
                throw new MarcException("数据为空或长度不足 25 字节。", new InvalidDataException());
            }

            MarcRecord ret = new MarcRecord()
            {
                Status = bytes[5],
                // 以字符串方式而不要直接通过 ASCII 码换算为数值，以便在字符不是有效的数字字符时引发异常。
                LenOfIndicator = int.Parse(bytes.Join(10, 1)),
                LenOfIdentifier = int.Parse(bytes.Join(11, 1))
            };
            ret.ImplCode.SetValue(bytes.GetSubArray(6, 4));
            ret.UserDefined.SetValue(bytes.GetSubArray(17, 3));
            ret.DirectoryMap.SetValue(bytes.GetSubArray(20, 4));

            int lenOfFieldLength = ret.DirectoryMap.GetInt(0, 1);
            int lenOfStartIndex = ret.DirectoryMap.GetInt(1, 1);
            int lenOfImplDefined = ret.DirectoryMap.GetInt(2, 1);

            int baseAddress = int.Parse(bytes.Join(12, 5));
            // 计算目录条目长度：3 字节的 Tag，后面依次是表示字段长度的字符数、表示起始位置的字符数、表示用户定义部分的字符数。
            int entrySize = 3 + lenOfFieldLength + lenOfStartIndex + lenOfImplDefined;

            if ((baseAddress - 1 - 24) % entrySize != 0 || bytes[baseAddress - 1] != MarcRecord.FIELD_TERMINATOR)
            {
                throw new MarcException("目录区长度不正确或未以字段分隔符结尾。", new InvalidDataException());
            }

            for (int i = 24; i < baseAddress - entrySize; i += entrySize)
            {
                int length = int.Parse(bytes.Join(i + 3, lenOfFieldLength));
                int offset = baseAddress + int.Parse(bytes.Join(i + 3 + lenOfFieldLength, lenOfStartIndex));
                bool fragment = false;

                if (length == 0)
                {
                    // 按照 ISO 2709:2008 的规定，长度为 0 表示该字段被拆分为了多个部分，每个部分都有对应的目录条目。
                    // 除最后一部分外，其余部分的长度取目录区能表示的最大值。
                    length = (int)Math.Pow(10, lenOfFieldLength) - 1;
                    fragment = true;
                }

                DirectoryEntry entry;
                byte[] tag = bytes.GetSubArray(i, 3);
                if (tag[0] == 48 && tag[1] == 48 && tag[2] == 49)
                {
                    // "001" 为标识符字段。
                    entry = IdentifierEntry.Parse(lenOfImplDefined, bytes.GetSubArray(offset, length));
                }
                else if (tag[0] == 48 && tag[1] == 48 && (tag[2] >= 50 && tag[2] <= 57 || tag[2] >= 65 && tag[2] <= 90 || tag[2] >= 97 && tag[2] <= 122))
                {
                    // "002" 到 "009" 或 "00A" 到 "00Z" 或 "00a" 到 "00z" 为引用字段。
                    entry = ReferenceEntry.Parse(tag, lenOfImplDefined, bytes.GetSubArray(offset, length));
                }
                else
                {
                    // 其他值为数据字段。
                    entry = DataEntry.Parse(tag, lenOfImplDefined, ret.LenOfIndicator, ret.LenOfIdentifier, bytes.GetSubArray(offset, length));
                }

                if (lenOfImplDefined > 0)
                {
                    entry.ImplDefined.SetValue(bytes.GetSubArray(i + 3 + lenOfFieldLength + lenOfStartIndex, lenOfImplDefined));
                }
                entry.IsFragment = fragment;

                ret.entries.Add(entry);
            }

            return ret;
        }

        public override string ToString()
        {
            return Length.ToString("00000") + (char)Status + ImplCode.Raw.Join() + LenOfIndicator + LenOfIdentifier + BaseAddress.ToString("00000") + UserDefined.Raw.Join() + DirectoryMap.Raw.Join();
        }

        protected string Repeat(string element, int count)
        {
            return Enumerable.Repeat(element, count).Aggregate("", (s, i) => s += i);
        }

        public DirectoryEntry[] GetEntries()
        {
            return entries.ToArray();
        }

        public DirectoryEntry[] GetEntries(string tagString)
        {
            if (tagString == null || tagString.Length == 0)
            {
                throw new ArgumentNullException();
            }

            List<DirectoryEntry> ret = new List<DirectoryEntry>();
            foreach (var item in entries)
            {
                if (Regex.IsMatch(item.Tag.GetString(), tagString))
                {
                    ret.Add(item);
                }
            }
            return ret.ToArray();
        }

        public DirectoryEntry GetEntryAt(int index)
        {
            return entries[index];
        }

        public void AddEntry(DirectoryEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException();
            }

            // 检查目录区的用户定义部分是否与记录要求的一致。
            if (DirectoryMap.GetInt(2, 1) != entry.ImplDefined.Length)
            {
                throw new MarcException("记录要求的目录区结构与正在添加的条目不符。", new InvalidOperationException());
            }
            // 检查记录长度是否超过了目录条目能存储的范围。
            if (entry.Length > (int)Math.Pow(10, DirectoryMap.GetInt(0, 1)))
            {
                throw new MarcException("数据太大，超出了单个字段能存储的容量。", new OverflowException());
            }
            // 对于数据字段，要检查指示符和标识符长度是否与记录要求的一致。
            if (entry.GetType() == typeof(DataEntry) && (LenOfIndicator != entry.Indicator.Length || LenOfIdentifier != entry.LenOfIdentifier))
            {
                throw new MarcException("记录要求的指示符长度和标识符长度与正在添加的条目不符。", new InvalidOperationException());
            }

            entries.Add(entry);
        }

        public int RemoveEntries(string tagString)
        {
            if (tagString == null || tagString.Length == 0)
            {
                throw new ArgumentNullException();
            }

            int count = 0;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (Regex.IsMatch(entries[i].Tag.GetString(), tagString))
                {
                    entries.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }

        public void RemoveEntryAt(int index)
        {
            entries.RemoveAt(index);
        }

        public void SortDirectory()
        {
            entries.Sort();
        }

        public byte[] Serialization()
        {
            List<byte> buffer = new List<byte>(Encoding.UTF8.GetBytes(ToString()));

            // 写入目录区。
            int offset = 0;
            foreach (DirectoryEntry directory in entries)
            {
                // 目录 Tag。
                buffer.AddRange(directory.Tag.Raw);
                // 记录长度。
                buffer.AddRange(Encoding.UTF8.GetBytes((directory.IsFragment ? 0 : directory.Length).ToString(Repeat("0", DirectoryMap.GetInt(0, 1)))));
                // 记录起始位置。
                buffer.AddRange(Encoding.UTF8.GetBytes(offset.ToString(Repeat("0", DirectoryMap.GetInt(1, 1)))));

                offset += directory.Length;
            }
            // 目录区以字段分隔符结束。
            buffer.Add(FIELD_TERMINATOR);

            // 第二次遍历时写入数据区。
            foreach (DirectoryEntry field in entries)
            {
                if (field.Indicator.Length > 0)
                {
                    buffer.AddRange(field.Indicator.Raw);
                }
                buffer.AddRange(field.Serialization());
            }
            // 字段结束符。
            buffer.Add(RECORD_TERMINATOR);

            return buffer.ToArray();
        }
    }
}
