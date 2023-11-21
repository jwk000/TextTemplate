using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace TTEngine
{
    //模板规则
    enum eTemplateRule
    {
        METATEXT,//元文本
        IF, //if语句
        SWITCH,//switch语句
        FOREACH,//foreach语句
        ONELINE,//多行视为单行
    }

    //内置元数据
    class GlobalMeta
    {
        public string DATETIME
        {
            get { return DateTime.Now.ToString(); }
        }
    }
    //模板数据
    class TemplateData
    {
        //全局变量
        public Dictionary<string, object> globalVariantDict = new Dictionary<string, object>();
        //局部变量
        public Dictionary<string, object> localVariantDict = new Dictionary<string, object>();

        public TemplateData()
        {
            globalVariantDict.Add("G", new GlobalMeta());
        }

        public void SetGlobalVariant(string name, object obj)
        {
            globalVariantDict[name] = obj;
        }

        public void SetLocalVariant(string name, object obj)
        {
            localVariantDict[name] = obj;
        }

        //反射提取对象字段值
        public object GetVariantObject(string objname, string fieldname)
        {
            object obj = null;
            if (objname == "G")//直接映射表
            {
                if (localVariantDict.TryGetValue(fieldname, out obj))
                {
                    return obj;
                }
                if (globalVariantDict.TryGetValue(fieldname, out obj))
                {
                    return obj;
                }
            }

            if (localVariantDict.TryGetValue(objname, out obj))
            {
                PropertyInfo info = obj.GetType().GetRuntimeProperty(fieldname);
                return info.GetValue(obj);
            }
            if (globalVariantDict.TryGetValue(objname, out obj))
            {
                PropertyInfo info = obj.GetType().GetRuntimeProperty(fieldname);
                return info.GetValue(obj);
            }
            return null;
        }

        public object GetVariantObject(string varname)
        {
            object obj;
            if (localVariantDict.TryGetValue(varname, out obj))
            {
                return obj;
            }
            if (globalVariantDict.TryGetValue(varname, out obj))
            {
                return obj;
            }
            return null;
        }

        //提取对象字段值
        public object ExtraMetaData(string extra)
        {
            Regex reg = new Regex(@"\${(\w+).(\w+)}");
            var m = reg.Match(extra);
            if (m.Success)
            {
                string objname = m.Groups[1].Value;
                string fieldname = m.Groups[2].Value;
                return GetVariantObject(objname, fieldname);
            }

            reg = new Regex(@"\${(\w+)}");
            m = reg.Match(extra);
            if (m.Success)
            {
                string varname = m.Groups[1].Value;
                return GetVariantObject(varname);
            }

            return null;
        }

        //数据替换
        public string ReplaceMetaData(string line)
        {
            Regex reg = new Regex(@"\${(\w+)\.(\w+)}");
            if (reg.IsMatch(line))
            {
                return reg.Replace(line, m =>
                {
                    string objname = m.Groups[1].Value;
                    string fieldname = m.Groups[2].Value;
                    string s = GetVariantObject(objname, fieldname)?.ToString();
                    return s;
                });
            }

            reg = new Regex(@"\${(\w+)}");
            if (reg.IsMatch(line))
            {
                return reg.Replace(line, m =>
                {
                    string objname = m.Groups[1].Value;
                    string s = GetVariantObject(objname).ToString();
                    return s;
                });
            }
            return line;
        }
    }

    //规则匹配器
    class RuleMatcher
    {
        public eTemplateRule rule_type = eTemplateRule.METATEXT;
        public string match_begin;
        public string match_end;
        public int match_deepth;

    }

    //模板规则接口
    interface ITemplateRule
    {
        RuleMatcher matcher { get; set; }
        List<string> rule_lines { get; set; }
        string rule_param_line { get; set; }
        //领域展开
        List<string> Unfold(TemplateData data);
    }

    //文本规则
    class RuleMetaText : ITemplateRule
    {
        public RuleMatcher matcher { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }
        public List<string> Unfold(TemplateData data)
        {
            List<string> result = new List<string>();
            foreach (var line in rule_lines)
            {
                string s = data.ReplaceMetaData(line);//逐行替换
                result.Add(s);
            }
            return result;
        }
    }

    //if语句
    class RuleIf : ITemplateRule
    {
        public RuleMatcher matcher { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }

        public virtual string ElseMatchText => "@{ELSE}";

        public bool CheckIfCondition(string cond, TemplateData data)
        {
            string replacedCond = data.ReplaceMetaData(cond);//逐行替换
            string[] ss = replacedCond.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

            if (ss.Length > 1)
            {
                bool ret = false;
                foreach (string s in ss)
                {
                    ret = ret || CheckIfCondition(s, data);
                    if (ret)
                    {
                        return true;
                    }
                }
                return false;
            }

            ss = replacedCond.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length > 1)
            {
                bool ret = true;
                foreach (string s in ss)
                {
                    ret = ret && CheckIfCondition(s, data);
                    if (!ret)
                    {
                        return false;
                    }
                }
                return true;

            }

            ss = replacedCond.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 2)
            {
                return ss[0].ToLower() == ss[1].ToLower();
            }

            ss = replacedCond.Split(new string[] { "!=" }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 2)
            {
                return ss[0].ToLower() != ss[1].ToLower();
            }

            if (replacedCond.ToLower() == "true") return true;
            if (replacedCond.ToLower() == "false") return false;

            return false;
        }
        public bool CheckIfLine(string line, TemplateData data)
        {
            Regex reg = new Regex(@"@{\w+\((.+)\)}");
            Match m = reg.Match(line);
            if (!m.Success)
            {
                Console.WriteLine("{0}格式错误 {1}", matcher.rule_type, rule_param_line);
                return false;
            }

            string condition = m.Groups[1].Value;
            return CheckIfCondition(condition, data);
        }
        public List<string> Unfold(TemplateData data)
        {
            bool condition_is_true = CheckIfLine(rule_param_line, data);
            List<string> extend_lines = new List<string>();

            bool find_else = false; //寻找else
            foreach (string s in rule_lines)
            {
                if (s.Trim() == ElseMatchText)
                {
                    find_else = true;
                    continue;
                }
                //寻找其他if
                if (Regex.IsMatch(s, @"@{ELSEIF\(.+\)}"))
                {
                    if (condition_is_true)
                    {
                        find_else = true;
                    }
                    else
                    {
                        condition_is_true = CheckIfLine(s, data);
                    }
                    continue;
                }

                if (condition_is_true)
                {
                    if (find_else) break;
                    extend_lines.Add(s);
                }
                else
                {
                    if (find_else)
                    {
                        extend_lines.Add(s);
                    }
                }
            }

            //继续展开
            List<string> result = new List<string>();
            List<ITemplateRule> extend_rules = TemplateParser.Parse(extend_lines);
            foreach (var rule in extend_rules)
            {
                var ss = rule.Unfold(data);
                result.AddRange(ss);
            }

            return result;
        }
    }

    //switch语句
    class RuleSwitch : ITemplateRule
    {
        public RuleMatcher matcher { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }

        public List<string> Unfold(TemplateData data)
        {
            Regex reg = new Regex(@"@{SWITCH\((.+)\)}");
            Match m = reg.Match(rule_param_line);
            if (!m.Success)
            {
                Console.WriteLine("{0}格式错误 {1}", matcher.rule_type, rule_param_line);
                return null;
            }

            string condition = m.Groups[1].Value;
            condition = data.ExtraMetaData(condition) as string;
            Regex regcase = new Regex(@"@{CASE\((.+)\)}");
            bool find_case = false; //寻找else

            List<string> extend_lines = new List<string>();
            foreach (string s in rule_lines)
            {
                if (regcase.IsMatch(s))
                {
                    if (find_case)
                        break;

                    string casee = regcase.Match(s).Groups[1].Value;
                    string[] ss = casee.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Contains(condition))
                    {
                        find_case = true;
                        continue;
                    }
                }
                if (find_case)
                {
                    extend_lines.Add(s);
                }
            }

            //继续展开
            List<string> result = new List<string>();
            List<ITemplateRule> extend_rules = TemplateParser.Parse(extend_lines);
            foreach (var rule in extend_rules)
            {
                var ss = rule.Unfold(data);
                result.AddRange(ss);
            }

            return result;
        }
    }

    //foreach语句
    class RuleForeach : ITemplateRule
    {
        public RuleMatcher matcher { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }
        public List<string> Unfold(TemplateData data)
        {
            Regex reg = new Regex(@"@{FOREACH\((\w+)\s+IN\s+(.+)\)}");
            Match m = reg.Match(rule_param_line);
            if (!m.Success)
            {
                Console.WriteLine("{0}格式错误 {1}", matcher.rule_type, rule_param_line);
                return null;
            }

            string var_name = m.Groups[1].Value;
            string extra = m.Groups[2].Value;
            object[] var_array = data.ExtraMetaData(extra) as object[];

            List<string> result = new List<string>();
            int index = 0;
            foreach (object v in var_array)
            {
                data.SetLocalVariant(var_name, v);
                data.SetLocalVariant("ForeachIndex", index.ToString());
                data.SetLocalVariant("ForeachLast", index == var_array.Length-1);
                index++;
                List<string> res = new List<string>();
                List<ITemplateRule> extend_rules = TemplateParser.Parse(rule_lines);
                foreach (var rule in extend_rules)
                {
                    var ss = rule.Unfold(data);
                    res.AddRange(ss);
                }
                result.AddRange(res);
            }
            return result;
        }
    }

    //oneline 语句
    class RuleOneline : ITemplateRule
    {
        public RuleMatcher matcher { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }
        public List<string> Unfold(TemplateData data)
        {
            List<string> result = new List<string>();
            List<string> res = new List<string>();
            List<ITemplateRule> extend_rules = TemplateParser.Parse(rule_lines);
            foreach (var rule in extend_rules)
            {
                var ss = rule.Unfold(data);
                res.AddRange(ss);
            }
            result.Add(string.Concat(res));
            return result;
        }

    }
    //规则解析器
    static class TemplateParser
    {
        static RuleMatcher matchtext = new RuleMatcher { rule_type = eTemplateRule.METATEXT };
        static List<RuleMatcher> matchlist = new List<RuleMatcher>();
        static TemplateParser()
        {
            matchlist.Add(new RuleMatcher { match_begin = @"@{IF\(.+\)}", match_end = "@{END_IF}", rule_type = eTemplateRule.IF });
            matchlist.Add(new RuleMatcher { match_begin = @"@{SWITCH\(.+\)}", match_end = "@{END_SWITCH}", rule_type = eTemplateRule.SWITCH });
            matchlist.Add(new RuleMatcher { match_begin = @"@{FOREACH\(.+\)}", match_end = "@{END_FOREACH}", rule_type = eTemplateRule.FOREACH });
            matchlist.Add(new RuleMatcher { match_begin = @"@{ONELINE}", match_end = "@{END_ONELINE}", rule_type = eTemplateRule.ONELINE });
        }
        public static ITemplateRule CreateRule(RuleMatcher info)
        {
            ITemplateRule rule = null;

            if (info.rule_type == eTemplateRule.METATEXT) rule = new RuleMetaText();
            else if (info.rule_type == eTemplateRule.IF) rule = new RuleIf();
            else if (info.rule_type == eTemplateRule.SWITCH) rule = new RuleSwitch();
            else if (info.rule_type == eTemplateRule.FOREACH) rule = new RuleForeach();
            else if (info.rule_type == eTemplateRule.ONELINE) rule = new RuleOneline();
            if (rule != null)
            {
                rule.matcher = info;
                rule.rule_lines = new List<string>();
            }
            return rule;
        }

        public static RuleMatcher MatchBegin(string line)
        {
            foreach (var match in matchlist)
            {
                Regex reg = new Regex(match.match_begin);
                if (reg.IsMatch(line.Trim()))
                {
                    match.match_deepth++;
                    return match;
                }
            }
            return matchtext;
        }
        public static bool MatchEnd(string line, RuleMatcher matchtxt)
        {
            //寻找end的时候要检查深度
            Regex reg = new Regex(matchtxt.match_begin);
            if (reg.IsMatch(line.Trim()))
            {
                matchtxt.match_deepth++;
                return false;
            }

            if (matchtxt.match_end == line.Trim())
            {
                matchtxt.match_deepth--;
                if (matchtxt.match_deepth == 0)
                {
                    return true;
                }
            }
            return false;
        }
        public static List<ITemplateRule> Parse(List<string> rule_lines)
        {
            List<ITemplateRule> extend_rules = new List<ITemplateRule>();

            RuleMatcher info = null;
            ITemplateRule rule = null;
            for (int i = 0; i < rule_lines.Count; i++)
            {
                if (info == null || info.rule_type == eTemplateRule.METATEXT)
                {
                    var match_info = MatchBegin(rule_lines[i]);
                    if (match_info != info)
                    {
                        if (rule != null && rule.rule_lines.Count > 0)
                        {
                            extend_rules.Add(rule);
                        }
                        info = match_info;
                        rule = CreateRule(info);
                        if (info.rule_type != eTemplateRule.METATEXT)
                        {
                            //匹配行含有参数
                            rule.rule_param_line = rule_lines[i];
                            //跳过匹配行
                            continue;
                        }
                    }

                    rule.rule_lines.Add(rule_lines[i]);
                    continue;
                }

                if (MatchEnd(rule_lines[i], info))
                {
                    if (rule.rule_lines.Count > 0)
                    {
                        extend_rules.Add(rule);
                    }
                    info = null;
                    rule = null;
                    continue;
                }

                rule.rule_lines.Add(rule_lines[i]);

            }
            if (rule != null && rule.rule_lines.Count > 0)
            {
                extend_rules.Add(rule);
            }
            return extend_rules;
        }

        public static string Render(string templateText, Dictionary<string, object> dict)
        {
            TemplateData data = new TemplateData();
            foreach (var kv in dict)
            {
                data.SetGlobalVariant(kv.Key, kv.Value);
            }
            string[] ss = templateText.Split('\n');
            List<ITemplateRule> rules = Parse(ss.ToList());
            List<string> codelines = new List<string>();
            foreach (var rule in rules)
            {
                var lines = rule.Unfold(data);
                codelines.AddRange(lines);
            }
            return string.Join('\n', codelines);
        }
    }

}