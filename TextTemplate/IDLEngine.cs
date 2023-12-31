﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IDLEngine
{
    //支持的语言
    enum CodeLanguage
    {
        CPP,
        CS,
        LUA,
    }

    //支持的类型
    enum eIDLType
    {
        INVALID,
        BOOL,
        INT,
        FLOAT,
        STRING, //字符串
        ENUM, //枚举
        CLASS, //类结构
        LIST, //数组
        DICT, //字典
    }

    //类型描述
    class IDLType
    {
        public string type_name;
        public eIDLType type = eIDLType.CLASS;//基本类型或容器类型
        public IDLType[] inner_type;//容器内部类型
    }

    //自定义特性
    class IDLAttr
    {
        public string attr_name;
        public string attr_param;//所有参数不拆分
        public string[] attr_params;//参数按逗号拆分

        public static IDLAttr ParseAttr(string line)
        {
            line = line.Substring(line.IndexOf('[') + 1, line.IndexOf(']') - line.IndexOf('[') - 1);
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }
            IDLAttr idlattr = new IDLAttr();
            //处理带参数的属性
            string[] ss = line.Split('(');
            idlattr.attr_name = ss[0];

            if (ss.Length > 1)
            {
                idlattr.attr_param = ss[1].Split(')')[0];
                idlattr.attr_params = idlattr.attr_param.Split(',');
            }
            return idlattr;
        }
    }
    
    //特性包装
    class IDLAttrWrapper
    {
        public List<IDLAttr> attrs = new List<IDLAttr>();

        public bool HasAttr(string name)
        {
            if (attrs.Count == 0)
            {
                return false;
            }
            IDLAttr a = attrs.Find(attr => attr.attr_name == name);
            if (a != null)
            {
                return true;
            }
            return false;
        }

        public string GetAttrParam(string name)
        {
            if (attrs.Count == 0)
            {
                return null;
            }

            IDLAttr a = attrs.Find(attr => attr.attr_name == name);
            if (a != null)
            {
                return a.attr_param;
            }
            return null;
        }
        public string[] GetAttrParams(string name)
        {
            if (attrs.Count == 0)
            {
                return null;
            }

            IDLAttr a = attrs.Find(attr => attr.attr_name == name);
            if (a != null)
            {
                return a.attr_params;
            }
            return null;
        }
        public string AttrName
        {
            get
            {
                if (attrs.Count == 0) return null;
                return attrs.First().attr_name;
            }
        }
        public string AttrParam
        {
            get
            {
                if (attrs.Count == 0) return null;
                return attrs.First().attr_param;
            }
        }

    }

    //枚举成员
    class IDLEnumField
    {
        public string item_name;
        public string item_value;
        public string comment;

        public IDLEnum Enum { get; set; }
        public string Name { get { return item_name; } }
        public string Value { get { return item_value; } }
        public string Comment { get { return comment; } }

    }

    //枚举
    class IDLEnum
    {
        public string enum_name;
        public string comment;
        public Dictionary<string, IDLEnumField> enum_fields = new Dictionary<string, IDLEnumField>();

        public IDLMeta Meta { get; set; }
        public string Comment { get { return comment; } }
        public string Name { get { return enum_name; } }
        public object[] FieldList { get { return enum_fields.Values.ToArray(); } }
    }

    //类成员
    class IDLClassField : IDLAttrWrapper
    {
        public IDLType field_type = new IDLType();
        public string type_name;
        public string field_name;
        public string comment;
        public string default_value;

        public IDLMeta Meta { get { return Class.Meta; } }
        public IDLClass Class { get; set; }
        public string Comment { get { return comment; } }
        public string Name { get { return field_name; } }
        public string Index
        {
            get
            {
                int idx = Class.fieldList.IndexOf(this) + 1;
                return idx.ToString();
            }
        }
        public string MetaType
        {
            get
            {
                switch (field_type.type)
                {
                    case eIDLType.BOOL:
                        return "bool";
                    case eIDLType.STRING:
                        return "string";
                    case eIDLType.INT:
                        return "int";
                    case eIDLType.FLOAT:
                        return "float";
                    case eIDLType.ENUM:
                        return "enum";
                    case eIDLType.CLASS:
                        return "class";
                    case eIDLType.DICT:
                        {
                            if (HasAttr("string")) return "dict_string";
                            if (field_type.inner_type[1].type == eIDLType.CLASS) return "dict_class";
                            if (field_type.inner_type[1].type < eIDLType.CLASS) return "dict_basic";
                        }
                        return "error";
                    case eIDLType.LIST:
                        {
                            if (HasAttr("string")) return "list_string";
                            if (field_type.inner_type[0].type == eIDLType.CLASS) return "list_class";
                            if (field_type.inner_type[0].type < eIDLType.CLASS) return "list_basic";
                        }
                        return "error";
                }
                return GetFieldTypeName(field_type, Meta.lang);
            }
        }
        public string Type
        {
            get
            {
                return GetFieldTypeName(field_type, Meta.lang);
            }
        }
        public string DictKeyType
        {
            get
            {
                if (field_type.type != eIDLType.DICT) return null;
                return GetFieldTypeName(field_type.inner_type[0], Meta.lang);
            }
        }
        public string DictKeyName
        {
            get
            {
                if (field_type.type != eIDLType.DICT) return null;
                if (!HasAttr("key")) return null;
                return GetAttrParam("key");
            }
        }
        public string DictValueType
        {
            get
            {
                if (field_type.type != eIDLType.DICT) return null;
                return GetFieldTypeName(field_type.inner_type[1], Meta.lang);
            }
        }
        public string DictValueTag
        {
            get
            {
                if (field_type.type != eIDLType.DICT) return null;
                IDLType type = field_type.inner_type[1];
                IDLClass cls = IDLParser.FindUsingClass(Class.Meta, type.type_name);
                if (cls != null)
                {
                    return cls.Tag;
                }
                return GetFieldTypeName(field_type.inner_type[1], Meta.lang);
            }
        }
        public string ListValueType
        {
            get
            {
                if (field_type.type != eIDLType.LIST) return null;
                return GetFieldTypeName(field_type.inner_type[0], Meta.lang);
            }
        }
        public string ListValueTag
        {
            get
            {
                if (field_type.type != eIDLType.LIST) return null;
                IDLType type = field_type.inner_type[0];
                IDLClass cls = IDLParser.FindUsingClass(Class.Meta, type.type_name);
                if (cls != null)
                {
                    return cls.Tag;
                }
                return GetFieldTypeName(field_type.inner_type[0], Meta.lang);
            }
        }
        public string DefaultValue
        {
            get
            {
                if (!string.IsNullOrEmpty(default_value))
                {
                    return default_value;
                }
                return GetFieldDefaultValue(field_type.type, Meta.lang);
            }
        }

        public string GetFieldDefaultValue(eIDLType t, CodeLanguage l)
        {
            switch (t)
            {
                case eIDLType.INT:
                case eIDLType.FLOAT:
                    return "0";
                case eIDLType.STRING:
                    return "\"\"";
                case eIDLType.BOOL when l == CodeLanguage.CPP || l == CodeLanguage.CS:
                    return "false";
                case eIDLType.BOOL when l == CodeLanguage.LUA:
                    return "0";
                case eIDLType.LIST when l == CodeLanguage.LUA:
                case eIDLType.DICT when l == CodeLanguage.LUA:
                case eIDLType.CLASS when l == CodeLanguage.LUA:
                    return "{}";

            }
            return null;
        }
        public string GetFieldTypeName(IDLType t, CodeLanguage lang)
        {
            switch (t.type)
            {
                case eIDLType.INT:
                    return "int";
                case eIDLType.FLOAT:
                    return "float";
                case eIDLType.BOOL:
                    return "bool";
                case eIDLType.ENUM:
                    return "int";
                case eIDLType.STRING when lang == CodeLanguage.CPP:
                    return "std::string";
                case eIDLType.LIST when lang == CodeLanguage.CPP:
                    return "std::vector<" + GetFieldTypeName(t.inner_type[0], lang) + ">";
                case eIDLType.DICT when lang == CodeLanguage.CPP:
                    return "std::map<" + GetFieldTypeName(t.inner_type[0], lang) + "," + GetFieldTypeName(t.inner_type[1], lang) + ">";
                case eIDLType.STRING when lang == CodeLanguage.CS:
                    return "string";
                case eIDLType.LIST when lang == CodeLanguage.CS:
                    return "List<" + GetFieldTypeName(t.inner_type[0], lang) + ">";
                case eIDLType.DICT when lang == CodeLanguage.CS:
                    return "H3DDictionary<" + GetFieldTypeName(t.inner_type[0], lang) + "," + GetFieldTypeName(t.inner_type[1], lang) + ">";
                case eIDLType.STRING when lang == CodeLanguage.LUA:
                    return "string";
                case eIDLType.LIST when lang == CodeLanguage.LUA:
                    return "list<" + GetFieldTypeName(t.inner_type[0], lang) + ">";
                case eIDLType.DICT when lang == CodeLanguage.LUA:
                    return "map<" + GetFieldTypeName(t.inner_type[0], lang) + "," + GetFieldTypeName(t.inner_type[1], lang) + ">";
                case eIDLType.CLASS when lang == CodeLanguage.LUA:
                    {
                        if (t.type_name == "TPersistID") return "int64";
                        if (t.type_name == "time_t") return "int64";
                        return t.type_name;
                    }
                case eIDLType.CLASS when lang == CodeLanguage.CS:
                    {
                        if (t.type_name == "TPersistID") return "long";
                        if (t.type_name == "time_t") return "long";
                        return t.type_name;
                    }
                case eIDLType.CLASS when lang== CodeLanguage.CPP:
                    return t.type_name;
            }
            return null; ;
        }

        public string Tag
        {
            get
            {
                if (HasAttr("tag"))
                {
                    return GetAttrParam("tag");
                }
                return field_name;
            }
        }
        public string IsOptional
        {
            get
            {
                if (HasAttr("Optional"))
                {
                    return "true";
                }
                return "false";
            }
        }

    }
    
    //类
    class IDLClass : IDLAttrWrapper
    {
        public string class_name;
        public string comment;
        public List<IDLClassField> fieldList = new List<IDLClassField>();

        public IDLMeta Meta { get; set; }
        public string Comment { get { return comment; } }
        public string Name { get { return class_name; } }
        public string Tag
        {
            get
            {
                if (HasAttr("tag"))
                {
                    return GetAttrParam("tag");
                }
                return class_name;
            }
        }
        public string Base
        {
            get
            {
                if (HasAttr("base"))
                {
                    return GetAttrParam("base");
                }
                return "Base Class Not Found";
            }
        }
        public object[] FieldList { get { return fieldList.ToArray(); } }
    }

    //引用其他IDL文件
    class IDLUsing
    {
        public string comment;
        public string using_name;


        public IDLMeta Meta { get; set; }
        public string Name { get { return using_name + ".h"; } }
        public string Comment { get { return comment; } }
    }

    //数据格式描述
    class IDLMeta
    {
        public CodeLanguage lang;
        public string meta_name;
        public string code_file_name;
        public string meta_file_path;
        public string root_class_name;
        
        public List<IDLUsing> meta_using = new List<IDLUsing>();
        public Dictionary<string, IDLClass> meta_class = new Dictionary<string, IDLClass>();
        public Dictionary<string, IDLEnum> meta_enum = new Dictionary<string, IDLEnum>();
        public Dictionary<string, string> meta_variant = new Dictionary<string, string>();

        int _auto_id = 0;
        public string AutoIncID { get { return (++_auto_id).ToString(); } }
        public string Name { get { return meta_name; } }
        public string FilePath { get { return meta_file_path; } }
        public string FileName { get { return code_file_name; } }
        public string HasRoot { get { return string.IsNullOrEmpty(root_class_name) ? "false" : "true"; } }
        public string RootClassName { get { return root_class_name; } }
        public object[] UsingList { get { return meta_using.ToArray(); } }
        public object[] ClassList { get { return meta_class.Values.ToArray(); } }
        public object[] EnumList { get { return meta_enum.Values.ToArray(); } }
        public string GetVar(string key) { return meta_variant.TryGetValue(key, out string val) ? val : null; }

    }

    //IDL文件解析器
    static class IDLParser
    {
        public static Dictionary<string, IDLMeta> all_idl_meta = new Dictionary<string, IDLMeta>();

        enum ParseState
        {
            End,
            BeginClass,
            BeginEnum
        }

        public static string parseComment(ref string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '/' && line[i + 1] == '/')
                {
                    string s = line.Substring(i);//带//
                    line = line.Substring(0, i);
                    return s;
                }
            }
            return null;
        }

        public static string parseDefaultValue(ref string line)
        {
            var ss = line.Split('=');
            if (ss.Length == 1)
            {
                return null;
            }
            line = ss[0] + ";";
            ss = ss[1].Split(';');
            return ss[0].Trim();
        }

        public static void parseGenericType(ref string line)
        {
            Regex reg = new Regex(@"<.+>");
            Match m = reg.Match(line);
            if (m.Success)
            {
                string r = m.Value.Replace(" ", "");
                line = line.Replace(m.Value, r);
            }
        }

        public static IDLEnum FindMetaEnum(string meta_name, string enum_name)
        {
            if (all_idl_meta.TryGetValue(meta_name, out IDLMeta meta))
            {
                if (meta.meta_enum.TryGetValue(enum_name, out IDLEnum e))
                {
                    return e;
                }
            }

            return null;
        }
        public static IDLClass FindMetaClass(string meta_name, string class_name)
        {
            if (all_idl_meta.TryGetValue(meta_name, out IDLMeta meta))
            {
                if (meta.meta_class.TryGetValue(class_name, out IDLClass cls))
                {
                    return cls;
                }
            }

            return null;
        }

        public static IDLEnum FindUsingEnum(IDLMeta meta, string enum_name)
        {
            if (meta.meta_enum.TryGetValue(enum_name, out IDLEnum enumtype))
            {
                return enumtype;
            }

            foreach (var u in meta.meta_using)
            {
                var c = FindMetaEnum(u.using_name, enum_name);
                if (c != null)
                {
                    return c;
                }
            }
            return null;

        }
        public static IDLClass FindUsingClass(IDLMeta meta, string class_name)
        {
            if (class_name == null) return null;
            if (meta.meta_class.TryGetValue(class_name, out IDLClass classtype))
            {
                return classtype;
            }

            foreach (var u in meta.meta_using)
            {
                var c = FindMetaClass(u.using_name, class_name);
                if (c != null)
                {
                    return c;
                }
            }
            return null;
        }
        public static bool ParseFieldType(IDLMeta meta, string typename, IDLType fieldtype, bool undefinedasclass = false)
        {
            fieldtype.type_name = typename;
            switch (typename)
            {
                case "int": { fieldtype.type = eIDLType.INT; return true; }
                case "float": { fieldtype.type = eIDLType.FLOAT; return true; }
                case "string": { fieldtype.type = eIDLType.STRING; return true; }
                case "bool": { fieldtype.type = eIDLType.BOOL; return true; }
                default:
                    break;
            }
            Match m = Regex.Match(typename, @"List<(\w+)>");
            if (m.Success)
            {
                fieldtype.type = eIDLType.LIST;
                fieldtype.inner_type = new IDLType[1] { new IDLType() };
                string inner_name = m.Groups[1].Value;
                if (!ParseFieldType(meta, inner_name, fieldtype.inner_type[0], undefinedasclass))
                {
                    return false;
                }
                return true;
            }

            m = Regex.Match(typename, @"Dict<(\w+),(\w+)>");
            if (m.Success)
            {
                fieldtype.type = eIDLType.DICT;
                fieldtype.inner_type = new IDLType[2] { new IDLType(), new IDLType() };
                string key_name = m.Groups[1].Value;
                if (!ParseFieldType(meta, key_name, fieldtype.inner_type[0]))
                {
                    return false;
                }
                string value_name = m.Groups[2].Value;
                if (!ParseFieldType(meta, value_name, fieldtype.inner_type[1], undefinedasclass))
                {
                    return false;
                }
                return true;
            }

            if (FindUsingClass(meta, typename) != null)
            {
                fieldtype.type = eIDLType.CLASS;
                return true;
            }

            if (FindUsingEnum(meta, typename) != null)
            {
                fieldtype.type = eIDLType.ENUM;
                return true;
            }

            if (undefinedasclass)
            {
                fieldtype.type = eIDLType.CLASS;
                return true;
            }
            Console.WriteLine("解析类型名称错误，未能识别{0}", typename);
            return false;
        }

        public static IDLMeta Parse(string idlFilePath)
        {
            IDLMeta meta = new IDLMeta();
            IDLClass m_class = null;
            IDLEnum m_enum = null;
            string[] lines = File.ReadAllLines(idlFilePath);
            string metaname = new string(Path.GetFileNameWithoutExtension(idlFilePath).TakeWhile(c => c != '.').ToArray());
            meta.meta_file_path = Path.Combine(Path.GetDirectoryName(idlFilePath), metaname).Replace('\\', '/');
            meta.meta_name = metaname;
            string comment = null;
            IDLAttr attr = null;
            ParseState m_parseState = ParseState.End;

            for (int i = 0; i < lines.Count(); i++)
            {
                //注释
                string cc = parseComment(ref lines[i]);
                if (!string.IsNullOrEmpty(cc))
                {
                    comment = cc;
                }
                //空行
                if (Regex.IsMatch(lines[i], @"^\s*$"))
                {
                    continue;
                }
                //特性
                if (Regex.IsMatch(lines[i], @"^\s*\[.*\]\s*$"))
                {
                    attr = IDLAttr.ParseAttr(lines[i]);
                    continue;
                }
                //变量
                Match match = Regex.Match(lines[i], @"^\s*set\s+(\w+)\s*=\s*(\w+)\s*;\s*$");
                if (match.Success)
                {
                    string key = match.Groups[1].Value;
                    string val = match.Groups[2].Value;
                    meta.meta_variant.Add(key, val);
                    continue;
                }
                //引用
                match = Regex.Match(lines[i], @"^\s*using\s+(\w+)\s*;\s*$");
                if (match.Success)
                {
                    IDLUsing u = new IDLUsing();
                    u.comment = comment;
                    u.using_name = match.Groups[1].Value;
                    u.Meta = meta;
                    meta.meta_using.Add(u);
                    comment = null;
                    continue;
                }

                //结束
                if (Regex.IsMatch(lines[i], @"^\s*};?\s*$"))
                {
                    if (m_parseState == ParseState.End)
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }
                    if (m_parseState == ParseState.BeginClass)
                    {
                        meta.meta_class.Add(m_class.class_name, m_class);
                        m_class = null;
                    }
                    else if (m_parseState == ParseState.BeginEnum)
                    {
                        meta.meta_enum.Add(m_enum.enum_name, m_enum);
                        m_enum = null;
                    }
                    m_parseState = ParseState.End;

                    continue;
                }
                //开始
                if (Regex.IsMatch(lines[i], @"^\s*{\s*$"))
                {
                    if (m_parseState != ParseState.BeginClass && m_parseState != ParseState.BeginEnum)
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }
                    continue;
                }
                //枚举开始
                match = Regex.Match(lines[i], @"^\s*enum\s*(\w+)\s*{?\s*$");
                if (match.Success)
                {
                    if (m_parseState != ParseState.End)
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }
                    m_parseState = ParseState.BeginEnum;
                    m_enum = new IDLEnum();
                    m_enum.comment = comment;
                    m_enum.enum_name = match.Groups[1].Value;
                    m_enum.Meta = meta;
                    attr = null;//用完清空
                    comment = null;
                    continue;
                }

                //结构体开始
                match = Regex.Match(lines[i], @"^\s*(struct|class)\s*(\w+)\s*{?\s*$");
                if (match.Success)
                {
                    if (m_parseState != ParseState.End)
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }
                    m_parseState = ParseState.BeginClass;
                    m_class = new IDLClass();
                    m_class.comment = comment;
                    m_class.class_name = match.Groups[2].Value;
                    m_class.Meta = meta;
                    if (attr != null)
                    {
                        m_class.attrs.Add(attr);
                    }
                    if (attr != null && attr.attr_name=="root")
                    {
                        meta.root_class_name = m_class.class_name;
                    }
                    attr = null;//用完清空
                    comment = null;
                    continue;
                }

                //类
                if (m_parseState == ParseState.BeginClass)
                {
                    parseGenericType(ref lines[i]);
                    string def = parseDefaultValue(ref lines[i]);
                    Match m = Regex.Match(lines[i], @"\s*(\S+)\s+(\w+)\s*;\s*$");
                    if (m.Success == false)
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }

                    IDLClassField field = new IDLClassField();
                    field.comment = comment;
                    field.type_name = m.Groups[1].Value;
                    field.field_name = m.Groups[2].Value;
                    field.default_value = def;
                    if (attr != null)
                    {
                        field.attrs.Add(attr);
                    }
                    field.Class = m_class;
                    if (!ParseFieldType(meta, field.type_name, field.field_type, true))
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }
                    m_class.fieldList.Add(field);
                    attr = null;//用完清空
                    comment = null;
                    continue;
                }

                //枚举
                if (m_parseState == ParseState.BeginEnum)
                {
                    Match m = Regex.Match(lines[i], @"\s*(\w+)\s*=\s*(\w+)\s*,\s*$");
                    if (m.Success == false)
                    {
                        throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
                    }

                    IDLEnumField item = new IDLEnumField();
                    item.comment = comment;
                    item.item_name = m.Groups[1].Value;
                    item.item_value = m.Groups[2].Value;
                    item.Enum = m_enum;
                    m_enum.enum_fields.Add(item.item_name, item);
                    attr = null;//用完清空
                    comment = null;
                    continue;
                }

                throw new Exception($"idl文件错误：第{i + 1}行,{lines[i]}");
            }
            return meta;


        }
        
        
        public static IDLMeta ParseIDL(string idlFilePath)
        {
            IDLMeta meta = null;
            string meta_name = Path.GetFileNameWithoutExtension(idlFilePath).Split('.')[0];

            if (!all_idl_meta.TryGetValue(meta_name, out meta))
            {
                try
                {
                    meta = Parse(idlFilePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("解析IDL文件{0}失败,{1}", idlFilePath, e);
                    return null;
                }
                all_idl_meta.Add(meta_name, meta);
            }
            return meta;
        }


    }
}
