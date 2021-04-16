# 读写 Marc 格式（ISO 2709:2008）文件

## 读文件

```csharp
using (FileStream fs = new FileStream(@"c:\marcfile.iso", FileMode.Open))
{
    MarcStreamReader reader = new MarcStreamReader(fs);
    for (MarcRecord record = reader.NextRecord(); record != null; record = reader.NextRecord())
    {
        Console.WriteLine("========================");
        Console.WriteLine(record.ToString());
        Console.WriteLine("------------------------");
        foreach (var item in record.GetEntries())
        {
            Console.WriteLine(item);
        }
        Console.WriteLine("");
    }
}

```

## 写文件

```csharp
using (FileStream file = new FileStream(@"c:\marcfile.iso", FileMode.Create))
{
    MarcRecord record = new MarcRecord(2, 2);
    
    // Add Identifier Field.
    IdentifierEntry identifier = new IdentifierEntry();
    identifier.SetValue("1234567890");
    record.AddEntry(identifier);
    
    // Add Reference Field.
    ReferenceEntry reference = new ReferenceEntry("005");
    reference.SetValue("20200520170000.0");
    record.AddEntry(reference);
    
    // Add Data Field.
    DataEntry data = new DataEntry("100", 0, 2, 2);
    Subfield subfield = new Subfield("f");		// "f" = Subfield name.
    subfield.SetValue("Subfield Value.");
    data.AddSubfield(subfield);
    record.AddEntry(data);
    
    byte[] buffer = record.Serialization();
    file.Write(buffer, 0, buffer.Length);
}

```

## 获取或设置编码器

按照 ISO2709:2008 的规定，记录标头区、目录区、指示符、子字段标识符、字段分隔符和记录分隔符的所有数据必须以 UTF8 编码。
其他区域用此处设置的编码器对字符串进行编码。

```csharp
MarcRecord.Encoding = Encoding.UTF8;

```

## 获取或设置子字段界定符代替字符

此处设置作为字符串输出字段内容时，用于代替子字段界定符（0x1F）的字符。

```csharp
Subfield.SubfieldDelimiterChar = '$';

```


