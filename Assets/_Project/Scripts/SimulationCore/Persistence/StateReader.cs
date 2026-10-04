using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Sovereign.Core
{
    /// <summary>
    /// Fills an existing object graph back in from JSON written by StateWriter.
    /// Objects and collections already present are filled IN PLACE rather than
    /// replaced, so readonly fields - which is most of the state - keep the very
    /// instances everything else already points at.
    /// </summary>
    public static class StateReader
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Populate(object target, string json)
        {
            Dictionary<string, object> node = JsonParser.Parse(json) as Dictionary<string, object>;
            if (node == null) throw new FormatException("A save must be a JSON object.");
            PopulateObject(target, node);
        }

        static void PopulateObject(object target, Dictionary<string, object> node)
        {
            for (Type type = target.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(Fields | BindingFlags.DeclaredOnly))
                {
                    object value;
                    if (!StateWriter.Saved(field) || !node.TryGetValue(field.Name, out value)) continue;
                    object existing = field.GetValue(target);
                    field.SetValue(target, Convert(value, field.FieldType, existing));
                }
            }
        }

        static object Convert(object node, Type type, object existing)
        {
            if (node == null) return type.IsValueType ? Activator.CreateInstance(type) : null;

            if (type == typeof(float)) return ParseFloat(node);
            if (type == typeof(double)) return double.Parse(Text(node), Inv);
            if (type == typeof(int)) return int.Parse(Text(node), Inv);
            if (type == typeof(uint)) return uint.Parse(Text(node), Inv);
            if (type == typeof(long)) return long.Parse(Text(node), Inv);
            if (type == typeof(bool)) return (bool)node;
            if (type == typeof(string)) return (string)node;
            if (type.IsEnum) return Enum.ToObject(type, long.Parse(Text(node), Inv));

            if (type.IsArray)
            {
                List<object> items = (List<object>)node;
                Type element = type.GetElementType();
                Array array = Array.CreateInstance(element, items.Count);
                for (int i = 0; i < items.Count; i++) array.SetValue(Convert(items[i], element, null), i);
                return array;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                Type[] args = type.GetGenericArguments();

                if (definition == typeof(Dictionary<,>))
                {
                    IDictionary dictionary = (existing as IDictionary) ?? (IDictionary)Activator.CreateInstance(type);
                    dictionary.Clear();
                    foreach (KeyValuePair<string, object> entry in (Dictionary<string, object>)node)
                        dictionary[Key(entry.Key, args[0])] = Convert(entry.Value, args[1], null);
                    return dictionary;
                }
                if (definition == typeof(List<>))
                {
                    IList list = (existing as IList) ?? (IList)Activator.CreateInstance(type);
                    list.Clear();
                    foreach (object item in (List<object>)node) list.Add(Convert(item, args[0], null));
                    return list;
                }
                if (definition == typeof(HashSet<>) || definition == typeof(Queue<>))
                {
                    object collection = existing ?? Activator.CreateInstance(type);
                    type.GetMethod("Clear").Invoke(collection, null);
                    MethodInfo add = type.GetMethod(definition == typeof(Queue<>) ? "Enqueue" : "Add");
                    foreach (object item in (List<object>)node) add.Invoke(collection, new[] { Convert(item, args[0], null) });
                    return collection;
                }
            }

            // A struct or class: fill the one already there, or make one without
            // running a constructor - some state types only have parameterised ones.
            object instance = existing ?? (type.IsValueType ? Activator.CreateInstance(type) : FormatterServices.GetUninitializedObject(type));
            PopulateObject(instance, (Dictionary<string, object>)node);
            return instance;
        }

        static object Key(string text, Type keyType)
        {
            if (keyType == typeof(string)) return text;
            if (keyType == typeof(int)) return int.Parse(text, Inv);
            if (keyType.IsEnum) return Enum.Parse(keyType, text);
            throw new NotSupportedException("Dictionary keys of type " + keyType.Name + " are not supported.");
        }

        static float ParseFloat(object node)
        {
            string s = node as string;
            if (s == "NaN") return float.NaN;
            if (s == "Infinity") return float.PositiveInfinity;
            if (s == "-Infinity") return float.NegativeInfinity;
            return float.Parse(Text(node), NumberStyles.Float, Inv);
        }

        static string Text(object node)
        {
            JsonNumber number = node as JsonNumber;
            return number != null ? number.text : System.Convert.ToString(node, Inv);
        }
    }
}
