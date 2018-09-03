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
        NumberTooBig
    }

    public class Lept
    {
        class LeptContext
        {
            internal string json;

            internal void ParseWhiteSpace()
            {
                json = json.TrimStart();
            }

            #region Checking Number

            bool PositionIsDigit(int position) => json[position] >= '0' && json[position] <= '9';
            int JumpToValidNumberEndRegex() => System.Text.RegularExpressions.Regex.Match(json, @"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?").Length; // match 50/51
            internal int JumpToValidNumberEnd(bool useRegex = false)
            {
                if (useRegex == true)
                    return JumpToValidNumberEndRegex();

                int position = 0;

                if (json[position] == '-') // jump across optional negative sign
                    position++;

                if (json[position] >= '1' && json[position] <= '9')
                {
                    while (PositionIsDigit(position)) // integer part
                    {
                        position++;
                    }
                }
                else if (json[position] == '0') // if integer part starts with 0, it should be a single 0
                    position++;
                else return -1;

                if (json[position] == '.')
                {
                    position++; // jump across decimal point
                    if (!PositionIsDigit(position)) // have decimal point but no digit
                        return -1;
                    else
                        while (PositionIsDigit(position)) // decimal part
                            position++;
                }

                if (json[position] == 'e' || json[position] == 'E')
                {
                    position++; // jump across natural constant symbol
                    if (json[position] == '+' || json[position] == '-') // jump across optional positive and negative sign
                        position++;

                    if (!PositionIsDigit(position)) // have natural logarithmic symbol symbol but no digit
                        return -1;
                    else
                        while (PositionIsDigit(position)) // exponent part
                            position++;
                }

                return position;
            }

            #endregion
        }

        public LeptType Type { get; private set; }
        double number;
        public double Number
        {
            get
            {
                if (LeptType.Number != Type)
                    throw new Exception("LeptType isn't Number.");
                return number;
            }
        }

        public LeptParseResult Parse(string json)
        {
            LeptContext context = new LeptContext();
            context.json = json + '\0'; // implement ExpectValue Feature
            Type = LeptType.Null; // Type is set to Null when parsing fails

            context.ParseWhiteSpace();
            LeptParseResult result = ParseValue(context);
            if (result == LeptParseResult.OK)
            {
                context.ParseWhiteSpace();
                if (context.json[0] != '\0')
                {
                    result = LeptParseResult.RootNotSingular;
                    Type = LeptType.Null;
                }
            }

            return result;
        }
        LeptParseResult ParseValue(LeptContext context)
        {
            switch (context.json[0])
            {
                case 'n': return ParseLiteral(context, "null", LeptType.Null);
                case 't': return ParseLiteral(context, "true", LeptType.True);
                case 'f': return ParseLiteral(context, "false", LeptType.False);
                case '\0': return LeptParseResult.ExpectValue;
                default: return ParseNumber(context); // include invalidValue
            }
        }
        LeptParseResult ParseLiteral(LeptContext context, string literal, LeptType expectedType)
        {
            if (!context.json.StartsWith(literal))
                return LeptParseResult.InvalidValue;

            context.json = context.json.Substring(literal.Length);
            Type = expectedType;

            return LeptParseResult.OK;
        }
        LeptParseResult ParseNumber(LeptContext context)
        {
            int validNumberEnd = context.JumpToValidNumberEnd(false);
            if (validNumberEnd == 0 || validNumberEnd == -1)
                return LeptParseResult.InvalidValue;

            try { number = double.Parse(context.json.Substring(0, validNumberEnd)); }
            catch (OverflowException) { return LeptParseResult.NumberTooBig; }

            Type = LeptType.Number;
            context.json = context.json.Substring(validNumberEnd);

            return LeptParseResult.OK;
        }
    }
}
