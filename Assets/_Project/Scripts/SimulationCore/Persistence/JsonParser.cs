using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sovereign.Core
{
    /// <summary>A number kept as its original text, so a float parses back to exactly
    /// the float that was written rather than through a double.</summary>
    public sealed class JsonNumber
    {
        public readonly string text;
        public JsonNumber(string text) { this.text = text; }
    }

    /// <summary>Minimal JSON: objects become Dictionary, arrays List, numbers
    /// JsonNumber, and strings, booleans and null as themselves.</summary>
    public sealed class JsonParser
    {
        readonly string _s;
        int _i;

        JsonParser(string s) { _s = s; }

        public static object Parse(string json)
        {
            JsonParser p = new JsonParser(json);
            object value = p.Value();
            p.Skip();
            if (p._i != p._s.Length) throw new FormatException("Trailing characters at " + p._i);
            return value;
        }

        object Value()
        {
            Skip();
            if (_i >= _s.Length) throw new FormatException("Unexpected end of JSON");
            char c = _s[_i];
            switch (c)
            {
                case '{': return Object();
                case '[': return Array();
                case '"': return String();
                case 't': Expect("true"); return true;
                case 'f': Expect("false"); return false;
                case 'n': Expect("null"); return null;
                default: return Number();
            }
        }

        Dictionary<string, object> Object()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            _i++;
            Skip();
            if (_s[_i] == '}') { _i++; return result; }
            while (true)
            {
                Skip();
                string key = String();
                Skip();
                if (_s[_i++] != ':') throw new FormatException("Expected ':' at " + (_i - 1));
                result[key] = Value();
                Skip();
                char next = _s[_i++];
                if (next == '}') return result;
                if (next != ',') throw new FormatException("Expected ',' or '}' at " + (_i - 1));
            }
        }

        List<object> Array()
        {
            List<object> result = new List<object>();
            _i++;
            Skip();
            if (_s[_i] == ']') { _i++; return result; }
            while (true)
            {
                result.Add(Value());
                Skip();
                char next = _s[_i++];
                if (next == ']') return result;
                if (next != ',') throw new FormatException("Expected ',' or ']' at " + (_i - 1));
            }
        }

        string String()
        {
            if (_s[_i] != '"') throw new FormatException("Expected string at " + _i);
            _i++;
            StringBuilder b = new StringBuilder();
            while (true)
            {
                char c = _s[_i++];
                if (c == '"') return b.ToString();
                if (c != '\\') { b.Append(c); continue; }
                char e = _s[_i++];
                switch (e)
                {
                    case 'n': b.Append('\n'); break;
                    case 'r': b.Append('\r'); break;
                    case 't': b.Append('\t'); break;
                    case 'u': b.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); _i += 4; break;
                    default: b.Append(e); break;
                }
            }
        }

        JsonNumber Number()
        {
            int start = _i;
            while (_i < _s.Length && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
            if (_i == start) throw new FormatException("Unexpected character '" + _s[_i] + "' at " + _i);
            return new JsonNumber(_s.Substring(start, _i - start));
        }

        void Expect(string word)
        {
            if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw new FormatException("Expected " + word + " at " + _i);
            _i += word.Length;
        }

        void Skip()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
        }
    }
}
