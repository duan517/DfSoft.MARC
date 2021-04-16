using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DfSoft.MARC;

namespace Host
{
    class Program
    {
        static void Main(string[] args)
        {
            using (FileStream fs = new FileStream(@"d:\desktop\marc\123.iso", FileMode.Open))
            {
                MarcStreamReader reader = new MarcStreamReader(fs);

                using (FileStream file = new FileStream(@"d:\desktop\out.bin", FileMode.Create))
                {
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

                        byte[] buffer = record.Serialization();
                        file.Write(buffer, 0, buffer.Length);
                        //break;
                    }

                    //MarcRecord record = new MarcRecord(2, 2);

                    //IdentifierEntry identifier = new IdentifierEntry();
                    //identifier.SetValue("1234567890");
                    //record.AddEntry(identifier);

                    //ReferenceEntry reference = new ReferenceEntry("005");
                    //reference.SetValue("20200520170000.0");
                    //record.AddEntry(reference);

                    //DataEntry data = new DataEntry("100", 0, 2, 2);
                    //Subfield subfield = new Subfield("f");
                    //subfield.SetValue("中文测试");
                    //data.AddSubfield(subfield);
                    //record.AddEntry(data);

                    //byte[] buffer = record.Serialization();
                    //file.Write(buffer, 0, buffer.Length);
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
