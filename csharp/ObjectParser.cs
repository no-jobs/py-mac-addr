using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MacAddress;

public interface IObjectWrapper
{
    public object UnWrap();
}

public class RedundantObject
{
}

internal static class _ObjectParserUtil
{
    public static string GetMemberName(MemberInfo member)
    {
        if (member.IsDefined(typeof(DataMemberAttribute), true))
        {
            DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)Attribute.GetCustomAttribute(member, typeof(DataMemberAttribute), true);
            if (!string.IsNullOrEmpty(dataMemberAttribute.Name))
                return dataMemberAttribute.Name;
        }

        return member.Name;
    }

}
public class ObjectParser
{
    bool ForceASCII;
    public ObjectParser(bool ForceASCII = false)
    {
        this.ForceASCII = ForceASCII;
    }
    public static string ToPrintable(bool ShowDetail, object x, string title = null)
    {
        ObjectParser op = new ObjectParser(false);
        if (x is IObjectWrapper)
        {
            x = ((IObjectWrapper)x).UnWrap();
        }
        string s = "";
        if (title != null) s = title + ": ";
        if (x is null) return s + "null";
        if (x is string)
        {
            if (!ShowDetail) return s + (string)x;
            return s + "`" + (string)x + "`";
        }
        string output = null;
        try
        {
            output = op.Stringify(x, true);
        }
        catch (Exception)
        {
            output = x.ToString();
        }
        if (!ShowDetail) return s + output;
        return s + $"<{FullName(x)}> {output}";
    }
    public static string FullName(dynamic x)
    {
        if (x is null) return "null";
        string fullName = ((object)x).GetType().FullName;
        return fullName.Split('`')[0];
    }
    public object Parse(object x, bool NumberAsDecimal = false)
    {
        if (x is IObjectWrapper)
        {
            x = ((IObjectWrapper)x).UnWrap();
        }

        if (x == null)
        {
            return null;
        }

        if (x is IObjectWrapper)
        {
            x = ((IObjectWrapper)x).UnWrap();
        }

        Type type = x.GetType();
        if (type == typeof(string) || type == typeof(char))
        {
            return x.ToString();
        }
        else if (type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint)
            || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal))
        {
            if (NumberAsDecimal)
                return Convert.ToDecimal(x);
            return Convert.ToDouble(x);
        }
        else if (type == typeof(bool))
        {
            return x;
        }
        else if (type == typeof(DateTime))
        {
            return ((DateTime)x).ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
        }
        else if (type == typeof(TimeSpan))
        {
            return x.ToString();
        }
        else if (type == typeof(Guid))
        {
            return x.ToString();
        }
        else if (type.IsEnum)
        {
            return x.ToString();
        }
        else if (x is ExpandoObject)
        {
            var dic = x as IDictionary<string, object>;
            var result = new Dictionary<string, object>();
            foreach (var key in dic.Keys)
            {
                result[key] = dic[key];
            }
            return result;
        }
        else if (x is IList)
        {
            IList list = x as IList;
            var result = new List<object>();
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(list[i]);
            }
            return result;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type keyType = type.GetGenericArguments()[0];
            var result = new Dictionary<string, object>();
            //Refuse to output dictionary keys that aren't of type string
            if (keyType != typeof(string))
            {
                return result;
            }
            IDictionary dict = x as IDictionary;
            foreach (object key in dict.Keys)
            {
                result[(string)key] = dict[key];
            }
            return result;
        }
        else if (x is IEnumerable)
        {
            IEnumerable enumerable = (IEnumerable)x;
            var result = new List<object>();
            IEnumerator e = enumerable.GetEnumerator();
            while (e.MoveNext())
            {
                object o = e.Current;
                result.Add(Parse(o, NumberAsDecimal));
            }
            return result;
        }
        else
        {
            var result = new Dictionary<string, object>();
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                if (fieldInfos[i].IsDefined(typeof(IgnoreDataMemberAttribute), true))
                    continue;
                object value = fieldInfos[i].GetValue(x);
                result[_ObjectParserUtil.GetMemberName(fieldInfos[i])] = Parse(value);
            }
            PropertyInfo[] propertyInfo = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < propertyInfo.Length; i++)
            {
                if (!propertyInfo[i].CanRead || propertyInfo[i].IsDefined(typeof(IgnoreDataMemberAttribute), true))
                    continue;
                object value = propertyInfo[i].GetValue(x, null);
                result[_ObjectParserUtil.GetMemberName(propertyInfo[i])] = Parse(value);
            }
            return result;
        }
    }
    public string Stringify(object x, bool indent)
    {
        StringBuilder sb = new StringBuilder();
        new _JsonStringBuilder(this.ForceASCII).WriteToSB(sb, x, indent, 0);
        string json = sb.ToString();
        return json;
    }
}

internal class _JsonStringBuilder
{
    bool ForceASCII = false;
    public _JsonStringBuilder(bool ForceASCII)
    {
        this.ForceASCII = ForceASCII;
    }

    void Indent(StringBuilder sb, bool indent, int level)
    {
        if (indent)
        {
            for (int i = 0; i < level; i++)
            {
                sb.Append("  ");
            }
        }
    }

    public static Type GetGenericIDictionaryType(Type type)
    {
        if (type == null) return null;
        var ifs = type.GetInterfaces();
        foreach (var i in ifs)
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>))
            {
                return i;
            }
        }
        return null;
    }
    public void WriteProcessGenericIDictionaryToSB<T>(StringBuilder sb, System.Collections.Generic.IDictionary<string, T> dict, bool indent, int level)
    {
        sb.Append("{");
        int count = 0;
        foreach (string key in dict.Keys)
        {
            if (count == 0 && indent) sb.Append('\n');
            if (count > 0)
            {
                sb.Append(",");
                if (indent) sb.Append('\n');
            }
            WriteToSB(sb, (string)key, indent, level + 1);
            sb.Append(": ");
            WriteToSB(sb, dict[key], indent, level + 1, true);
            count++;
        }
        if (count > 0 && indent)
        {
            sb.Append('\n');
            Indent(sb, indent, level);
        }
        sb.Append("}");
    }
    public void WriteToSB(StringBuilder sb, object x, bool indent, int level, bool cancelIndent = false)
    {
        if (!cancelIndent) Indent(sb, indent, level);

        if (x is IObjectWrapper)
        {
            x = ((IObjectWrapper)x).UnWrap();
        }

        if (x == null)
        {
            sb.Append("null");
            return;
        }

        Type type = x.GetType();
        if (type == typeof(string) || type == typeof(char))
        {
            string str = x.ToString();
            sb.Append('"');
            sb.Append(Escape(str));
            sb.Append('"');
            return;
        }
        if (type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint)
            || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal))
        {
            sb.Append(x.ToString());
            return;
        }
        else if (type == typeof(bool))
        {
            sb.Append(x.ToString().ToLower());
            return;
        }
        else if (type == typeof(DateTime))
        {
            WriteToSB(sb, ((DateTime)x).ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"), indent, level);
            return;
        }
        else if (type == typeof(TimeSpan))
        {
            WriteToSB(sb, x.ToString(), indent, level);
            return;
        }
        else if (type == typeof(Guid))
        {
            WriteToSB(sb, x.ToString(), indent, level);
            return;
        }
        else if (type.IsEnum)
        {
            WriteToSB(sb, x.ToString(), indent, level, cancelIndent);
            return;
        }
        else if (x is ExpandoObject)
        {
            var dic = x as IDictionary<string, object>;
            var result = new Dictionary<string, object>();
            foreach (var key in dic.Keys)
            {
                result[key] = dic[key];
            }
            WriteToSB(sb, result, indent, level, cancelIndent);
            return;
        }
        else if (x is IList)
        {
            IList list = x as IList;
            if (list.Count == 0)
            {
                sb.Append("[]");
                return;
            }
            sb.Append('[');
            if (indent) sb.Append('\n');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                    if (indent) sb.Append('\n');
                }
                WriteToSB(sb, list[i], indent, level + 1);
            }
            if (indent) sb.Append('\n');
            Indent(sb, indent, level);
            sb.Append(']');
            return;
        }
        else if (GetGenericIDictionaryType(type) != null)
        {
            type = GetGenericIDictionaryType(type);
            Type keyType = type.GetGenericArguments()[0];
            //Refuse to output dictionary keys that aren't of type string
            if (keyType != typeof(string))
            {
                sb.Append("{}");
                return;
            }
            WriteProcessGenericIDictionaryToSB(sb, (dynamic)x, indent, level);

            return;
        }
        else if (x is IEnumerable)
        {
            IEnumerable enumerable = (IEnumerable)x;
            var result = new List<object>();
            IEnumerator e = enumerable.GetEnumerator();
            while (e.MoveNext())
            {
                object o = e.Current;
                result.Add(o);
            }
            WriteToSB(sb, result, indent, level, cancelIndent);
        }
        else
        {
            int count = 0;
            sb.Append('{');
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                if (fieldInfos[i].IsDefined(typeof(IgnoreDataMemberAttribute), true))
                    continue;
                object value = fieldInfos[i].GetValue(x);
                if (x is RedundantObject)
                {
                    if (value == null) continue;
                }
                if (count == 0 && indent) sb.Append('\n');
                if (count > 0)
                {
                    sb.Append(",");
                    if (indent) sb.Append('\n');
                }
                WriteToSB(sb, _ObjectParserUtil.GetMemberName(fieldInfos[i]), indent, level + 1);
                sb.Append(": ");
                WriteToSB(sb, value, indent, level + 1, true);
                count++;
            }
            PropertyInfo[] propertyInfo = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < propertyInfo.Length; i++)
            {
                if (!propertyInfo[i].CanRead || propertyInfo[i].IsDefined(typeof(IgnoreDataMemberAttribute), true))
                    continue;
                object value = propertyInfo[i].GetValue(x, null);
                if (x is RedundantObject)
                {
                    if (value == null) continue;
                }
                if (count == 0 && indent) sb.Append('\n');
                if (count > 0)
                {
                    sb.Append(",");
                    if (indent) sb.Append('\n');
                }
                WriteToSB(sb, _ObjectParserUtil.GetMemberName(propertyInfo[i]), indent, level + 1);
                sb.Append(": ");
                WriteToSB(sb, value, indent, level + 1, true);
                count++;
            }
            if (count > 0 && indent)
            {
                sb.Append('\n');
                Indent(sb, indent, level);
            }
            sb.Append('}');
            return;
        }
    }
    string Escape(string aText /*, bool ForceASCII*/)
    {
        var sb = new StringBuilder();
        sb.Length = 0;
        if (sb.Capacity < aText.Length + aText.Length / 10)
            sb.Capacity = aText.Length + aText.Length / 10;
        foreach (char c in aText)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                default:
                    if (c < ' ' || (ForceASCII && c > 127))
                    {
                        ushort val = c;
                        sb.Append("\\u").Append(val.ToString("X4"));
                    }
                    else
                        sb.Append(c);
                    break;
            }
        }
        string result = sb.ToString();
        sb.Length = 0;
        return result;
    }
}