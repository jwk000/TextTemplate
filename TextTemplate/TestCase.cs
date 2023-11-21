using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextTemplate
{
    class Table
    {
        public List<Row> rowList;
        public Row[] RowList => rowList.ToArray();
    }
    class Row
    {
        public List<Multiply> mulList;
        public Multiply[] MulList => mulList.ToArray();
    }
    class Multiply
    {
        public int a { get; set; }
        public int b { get; set; }
        public int c { get; set; }
        public Multiply(int _a, int _b, int _c)
        {
            a = _a; b = _b; c = _c;
        }
    }

    static class TestCase
    {
        public static void TestDict()
        {
            //生成九九乘法表
            Dictionary<string, object> metaDict = new Dictionary<string, object>();
            Table t = new Table();
            metaDict.Add("Table", t);
            t.rowList = new List<Row>();
            for (int i = 1; i <= 9; i++)
            {
                Row row = new Row();
                row.mulList = new List<Multiply>();
                t.rowList.Add(row);
                for (int j = 1; j <= i; j++)
                {
                    row.mulList.Add(new Multiply(i, j, i * j));
                }
            }
            CodeDump.GenerateCode("test_dict/template.txt", "test_dict/out.txt", metaDict);
        }

        public static void TestIdl()
        {
            CodeDump.GenerateCode("test_idl/template.h", "test_idl/skill_container.h", "test_idl/skill_container.json.idl");
            CodeDump.GenerateCode("test_idl/template.cpp", "test_idl/skill_container.cpp", "test_idl/skill_container.json.idl");
        }

    }
}
