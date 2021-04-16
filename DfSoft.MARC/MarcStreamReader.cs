using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DfSoft.MARC.Utils;

namespace DfSoft.MARC
{
    public class MarcStreamReader
    {
        public Stream InputStream { get; set; }
        public int[] GapChars { get; set; } = new int[] { 0x0d, 0x0a };

        public MarcStreamReader(Stream inputStream)
        {
            InputStream = inputStream;
        }

        public MarcRecord NextRecord()
        {
            if (InputStream == null)
            {
                throw new NullReferenceException("InputStream 属性为空。");
            }

            // 至少需要 26 字节才能构成一条完成的 MARC 数据。
            // 按照 ISO 2709:2008 的规定，这 26 个字节分别是：24 字节的记录标头、1 字节目录区结尾的字段分隔符、1 字节的字段分隔符。
            // 实践中，有的文件会在每条记录结尾后追加回车换行符，因此当剩余数据不够时直接返回空值，表示没有下一条记录，而不引发异常。
            if (InputStream.Length - InputStream.Position < 26)
            {
                return null;
            }

            byte[] lenOfRecord = new byte[5];
            for (int value = InputStream.ReadByte(); value != -1; value = InputStream.ReadByte())
            {
                // 实践中，有的文件会在每条记录结尾后追加回车换行符。
                // 当上一条记录读完后，读写位置正好位于回车或换行符上，需要跳过这些字符，从真正的记录起始处开始分析。
                if (GapChars != null && GapChars.Contains(value))
                {
                    continue;
                }

                lenOfRecord[0] = (byte)value;
                if (InputStream.Read(lenOfRecord, 1, 4) != 4)
                {
                    throw new EndOfStreamException();
                }

                byte[] buffer = new byte[int.Parse(lenOfRecord.Join()) - 5];    // 排除之前已经读出的 5 个字节的记录长度。
                if (InputStream.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new EndOfStreamException();
                }

                // 按照 ISO 2709:2008 的规定，记录标头的字段长度应该包含字段结束符，但在实践中发现有的文件并未遵守此规定。
                // 在此种情况下，按照标头指示的长度读出数据后，读写位置实际上位于下一条记录之前若干字节处。
                // 若出现这种情况，直接向后推进读写位置，直到到达字段结束符为止，以免影响读取下一条记录。
                for (value = buffer.Last(); value != MarcRecord.RECORD_TERMINATOR; value = InputStream.ReadByte()) ;

                return MarcRecord.Parse(lenOfRecord.Concat(buffer).ToArray());
            }

            throw new EndOfStreamException();
        }
    }
}
