using System;
using System.Text;
using System.Collections.Generic;

namespace LeptJSON
{
    public enum LeptType
    {
        Null,
        False,
        True,
        Number,
        String,
        Array,
        Object
    }
    public enum LeptParseResult
    {
        OK,
        ExpectValue,
        InvalidValue,
        RootNotSingular,
        NumberTooBig,
        MissQuotationMark,
        InvalidStringEscape,
        InvalidStringChar,
        InvalidUnicodeHex,
        InvalidUnicodeSurrogate,
        MissCommaOrSquareBracket,
        MissKey,
        MissColon,
        MissCommaOrCurlyBracket
    }

    public partial class Lept
    {
        class LeptValue
        {
            internal LeptType type;
            internal double number;
            internal string _string;
            internal bool boolean;
            internal Lept[] array;
            internal KeyValuePair<string, Lept>[] _object;
        }

        LeptContext context;
        LeptValue _value = new LeptValue();
        Lept(LeptContext context) => this.context = context;
        public Lept() { }

        #region Value

        public LeptType Type { get => _value.type; set => _value.type = value; }
        public double Number
        {
            get
            {
                if (_value.type != LeptType.Number)
                    throw new Exception("LeptType isn't Number.");
                return _value.number;
            }
            set
            {
                _value = new LeptValue();
                _value.type = LeptType.Number;
                _value.number = value;
            }
        }
        public string String
        {
            get
            {
                if (_value.type != LeptType.String)
                    throw new Exception("LeptType isn't String.");
                return _value._string;
            }
            set
            {
                _value = new LeptValue();
                _value.type = LeptType.String;
                _value._string = value;
            }
        }
        public bool Boolean
        {
            get
            {
                if (_value.type != LeptType.True || Type != LeptType.False)
                    throw new Exception("LeptType isn't Boolean.");
                return _value.boolean;
            }
            set
            {
                _value = new LeptValue();
                _value.type = value ? LeptType.True : LeptType.False;
                _value.boolean = value;
            }
        }
        public Lept[] Array
        {
            get
            {
                if (_value.type != LeptType.Array)
                    throw new Exception("LeptType isn't String.");
                return _value.array;
            }
            set
            {
                _value = new LeptValue();
                _value.type = LeptType.Array;
                _value.array = value;
            }
        }
        public KeyValuePair<string, Lept>[] Object
        {
            get
            {
                if (_value.type != LeptType.Object)
                    throw new Exception("LeptType isn't String.");
                return _value._object;
            }
            set
            {
                _value = new LeptValue();
                _value.type = LeptType.Object;
                _value._object = value;
            }
        }

        #endregion

        #region Parsing

        public System.Threading.Tasks.Task<LeptParseResult> ParseAsync(string json) =>
            System.Threading.Tasks.Task.Run(() => Parse(json));
        public LeptParseResult Parse(string json)
        {
            context = new LeptContext(json);
            Type = LeptType.Null; // Type is set to Null when parsing fails

            context.ParseWhiteSpace();
            LeptParseResult result = ParseValue();
            if (result == LeptParseResult.OK)
            {
                context.ParseWhiteSpace();
                if (context[0] != '\0')
                {
                    result = LeptParseResult.RootNotSingular;
                    Type = LeptType.Null;
                }
            }

            return result;
        }

        internal LeptParseResult ParseValue()
        {
            switch (context[0])
            {
                case 'n': return ParseLiteral("null", LeptType.Null);
                case 't': return ParseLiteral("true", LeptType.True);
                case 'f': return ParseLiteral("false", LeptType.False);
                case '\"': return ParseString();
                case '[': return ParseArray();
                case '{': return ParseObject();
                case '\0': return LeptParseResult.ExpectValue;
                default: return ParseNumber(); // include invalidValue
            }
        }

        LeptParseResult ParseLiteral(string literal, LeptType expectedType)
        {
            if (!context.ParseLiteral(literal))
                return LeptParseResult.InvalidValue;

            Type = expectedType;
            return LeptParseResult.OK;
        }

        LeptParseResult ParseNumber()
        {
            int validNumberEnd = context.GetValidNumberEnd(); // parse till invalid value
            if (validNumberEnd == 0)
                return context[0] == '-' ? LeptParseResult.RootNotSingular : LeptParseResult.InvalidValue;

            try { Number = double.Parse(context.GetValidNumberString()); }
            catch (OverflowException) { return LeptParseResult.NumberTooBig; }

            // Type = LeptType.Number;
            context.JumpToValidNumberEnd();

            return LeptParseResult.OK; // can still be root not singular in the end
        }

        LeptParseResult ParseString()
        {
            LeptParseResult parseResult = context.ParseString(out string result);
            if (parseResult == LeptParseResult.OK)
                String = result;

            return parseResult;
        }

        LeptParseResult ParseArray()
        {
            LeptParseResult parseResult = context.ParseArray(out Lept[] array);

            if (parseResult == LeptParseResult.OK)
                Array = array;

            // Type = LeptType.Array;
            return parseResult;
        }

        LeptParseResult ParseObject()
        {
            LeptParseResult parseResult = context.ParseObject(out KeyValuePair<string, Lept>[] objects);

            if (parseResult == LeptParseResult.OK)
                Object = objects;

            // Type = LeptType.Array;
            return parseResult;
        }

        #endregion

        #region Stringify

        public string Stringify()
        {
            StringBuilder sb = new StringBuilder();
            StringifyValue(sb);
            return sb.ToString();
        }

        void StringifyValue(StringBuilder sb)
        {
            switch (Type)
            {
                case LeptType.Null: sb.Append("null"); break;
                case LeptType.False: sb.Append("false"); break;
                case LeptType.True: sb.Append("true"); break;
                case LeptType.Number: sb.Append(Number.ToString("g17")); break;
                case LeptType.String: StringifyString(sb); break;
                case LeptType.Array:
                    {
                        sb.Append('[');
                        if (Array.Length > 0)
                            Array[0].StringifyValue(sb);
                        for (int i = 1; i < Array.Length; i++)
                        {
                            sb.Append(',');
                            Array[i].StringifyValue(sb);
                        }
                        sb.Append(']');
                        break;
                    }
                case LeptType.Object:
                    {
                        sb.Append('{');
                        if (Object.Length > 0)
                        {
                            sb.Append('\"' + Object[0].Key + '\"');
                            sb.Append(':');
                            Object[0].Value.StringifyValue(sb);
                        }
                        for (int i = 1; i < Object.Length; i++)
                        {
                            sb.Append(',');
                            sb.Append('\"' + Object[i].Key + '\"');
                            sb.Append(':');
                            Object[i].Value.StringifyValue(sb);
                        }
                        sb.Append('}');
                        break;
                    }
            }
        }

        void StringifyString(StringBuilder sb)
        {
            sb.Append('\"');
            for (int i = 0; i < String.Length; i++)
            {
                char ch = String[i];
                switch (ch)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    // case '/': sb.Append("\\/"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        {
                            if (ch < 0x20)
                                sb.Append("\\u" + ((int)ch).ToString("x4"));
                            else
                                sb.Append(ch);
                            break;
                        }
                }
            }
            sb.Append('\"');
        }

        #endregion
    }
}
