using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Sovereign.Core
{
    /// <summary>
    /// Writes a state object graph as JSON by reflection - every instance field,
    /// public or private, so a save captures the whole run without a hand-written
    /// mapping that goes stale the moment someone adds a field. Floats are written in
    /// round-trip form: a loaded game must continue bit-for-bit as if it had never
    /// been saved, and the save test holds it to that.
    /// </summary>
    public static class StateWriter
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string ToJson(object root)
        {
            StringBuilder b = new StringBuilder(1 << 16);
            Write(b, root);
            return b.ToString();
        }

        static void Write(StringBuilder b, object value)
        {
            if (value == null) { b.Append("null"); return; }
            Type t = value.GetType();

            if (t == typeof(float)) { WriteFloat(b, (float)value); return; }
            if (t == typeof(double)) { b.Append(((double)value).ToString("R", Inv)); return; }
            if (t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(byte))
            { b.Append(Convert.ToString(value, Inv)); return; }
            if (t == typeof(bool)) { b.Append((bool)value ? "true" : "false"); return; }
            if (t == typeof(string)) { WriteString(b, (string)value); return; }
            if (t.IsEnum) { b.Append(Convert.ToInt64(value, Inv).ToString(Inv)); return; }

            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                b.Append('{');
                bool first = true;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (!first) b.Append(',');
                    first = false;
                    WriteString(b, Convert.ToString(entry.Key, Inv));
                    b.Append(':');
                    Write(b, entry.Value);
                }
                b.Append('}');
                return;
            }

            IEnumerable sequence = value as IEnumerable;
            if (sequence != null)
            {
                b.Append('[');
                bool first = true;
                foreach (object item in sequence)
                {
                    if (!first) b.Append(',');
                    first = false;
                    Write(b, item);
                }
                b.Append(']');
                return;
            }

            b.Append('{');
            bool firstField = true;
            for (Type type = t; type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(Fields | BindingFlags.DeclaredOnly))
                {
                    if (!Saved(field)) continue;
                    if (!firstField) b.Append(',');
                    firstField = false;
                    WriteString(b, field.Name);
                    b.Append(':');
                    Write(b, field.GetValue(value));
                }
            }
            b.Append('}');
        }

        /// <summary>Delegates and anything marked [NonSerialized] are not state.</summary>
        public static bool Saved(FieldInfo field)
        {
            if (field.IsNotSerialized) return false;
            return !typeof(Delegate).IsAssignableFrom(field.FieldType);
        }

        static void WriteFloat(StringBuilder b, float f)
        {
            if (float.IsNaN(f)) b.Append("\"NaN\"");
            else if (float.IsPositiveInfinity(f)) b.Append("\"Infinity\"");
            else if (float.IsNegativeInfinity(f)) b.Append("\"-Infinity\"");
            else b.Append(f.ToString("R", Inv));
        }

        static void WriteString(StringBuilder b, string s)
        {
            b.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (c < ' ') b.Append("\\u").Append(((int)c).ToString("x4", Inv));
                        else b.Append(c);
                        break;
                }
            }
            b.Append('"');
        }
    }
}
