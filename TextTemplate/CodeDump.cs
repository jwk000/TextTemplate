using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDLEngine;
using TTEngine;

namespace TextTemplate
{
    static class CodeDump
    {
        //使用字典生成代码
        public static void GenerateCode(string templateFilePath, string codeFilePath, Dictionary<string, object> metaDict)
        {
            string[] lines = File.ReadAllLines(templateFilePath);
            //解析规则
            var rules = TemplateParser.Parse(lines.ToList());
            //展开规则
            List<string> code = new List<string>();
            TemplateData data = new TemplateData();
            data.SetGlobalVariant("Meta", metaDict);
            //meta变量注入
            foreach (var kv in metaDict)
            {
                data.SetGlobalVariant(kv.Key, kv.Value);
            }
            //代码生成
            foreach (var rule in rules)
            {
                var code_line = rule.Unfold(data);
                code.AddRange(code_line);
            }
            //删除旧文件
            if (File.Exists(codeFilePath))
            {
                File.SetAttributes(codeFilePath, FileAttributes.Normal);
                File.Delete(codeFilePath);
            }
            //写入文件
            using (FileStream fs = new FileStream(codeFilePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (var line in code)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }

        //使用idl文件生成代码
        public static void GenerateCode(string templateFilePath, string codeFilePath, string idlFilePath)
        {
            IDLMeta metaData = IDLParser.Parse(idlFilePath);
            metaData.code_file_name = Path.GetFileNameWithoutExtension(codeFilePath);
            string[] lines = File.ReadAllLines(templateFilePath);
            //解析规则
            var rules = TemplateParser.Parse(lines.ToList());
            //展开规则
            List<string> code = new List<string>();
            TemplateData data = new TemplateData();
            data.SetGlobalVariant("Meta", metaData);
            //meta变量注入
            foreach (var kv in metaData.meta_variant)
            {
                data.SetGlobalVariant(kv.Key, kv.Value);
            }
            //代码生成
            foreach (var rule in rules)
            {
                var code_line = rule.Unfold(data);
                code.AddRange(code_line);
            }
            //删除旧文件
            if (File.Exists(codeFilePath))
            {
                File.SetAttributes(codeFilePath, FileAttributes.Normal);
                File.Delete(codeFilePath);
            }
            //写入文件
            using (FileStream fs = new FileStream(codeFilePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (var line in code)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }

    }
}
