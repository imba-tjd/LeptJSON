using System;

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
        MissCommaOrSquareBracket
    }

    public partial class Lept
    {
        class LeptValue
        {
            internal LeptType type = LeptType.Null;
            internal double number;
            internal string _string;
            internal bool boolean;
            internal Lept[] array = new Lept[0];
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

        #endregion

        #region Parsing

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

        #endregion
    }
}
