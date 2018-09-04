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
            internal char this[int index] => json[index];
            internal void ParseWhiteSpace() => json = json.TrimStart();

            internal LeptContext(string json) => this.json = json + '\0';

            internal bool ParseLiteral(string literal)
            {
                if (!json.StartsWith(literal))
                    return false;

                json = json.Substring(literal.Length);
                return true;
            }


            #region Checking Number

            int validNumberEnd = 0;
            bool PositionIsDigit(int position) => json[position] >= '0' && json[position] <= '9';
            internal int GetValidNumberEnd(bool useRegex = false) => validNumberEnd = useRegex == true ? GetValidNumberEndRegex() : GetValidNumberEndInternal();
            int GetValidNumberEndRegex()
            {
                int end = System.Text.RegularExpressions.Regex.Match(json, @"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?").Length;
                return json[end] == '.' ? 0 : end; // 1. is invalid
            }

            int GetValidNumberEndInternal()
            {
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
                else return 0; // invalid value or none or **have negative sign but no digit**, so it must be 0, not position

                if (json[position] == '.')
                {
                    position++; // jump across decimal point
                    if (!PositionIsDigit(position)) // have decimal point but no digit
                        return 0; // invalid value
                    else
                        while (PositionIsDigit(position)) // decimal part
                            position++;
                }

                if (json[position] == 'e' || json[position] == 'E')
                {
                    position++; // jump across natural constant symbol

                    if (json[position] == '+' || json[position] == '-') // jump across optional positive and negative sign
                        position++;

                    if (!PositionIsDigit(position)) // invalid caracter or have natural logarithmic symbol symbol but no digit
                    {
                        if (json[position - 1] == '+' || json[position - 1] == '-') // look back
                            return position - 2; // root not singular.
                        else
                            return position - 1;
                    }

                    // else
                    while (PositionIsDigit(position)) // exponent part
                        position++;
                }

                return position;
            }

            internal void JumpToValidNumberEnd()
            {
                if (validNumberEnd == 0)
                    GetValidNumberEndInternal();
                if (validNumberEnd == 0)
                    return;

                json = json.Substring(validNumberEnd);
                validNumberEnd = 0;
            }
            internal string GetValidNumberString()
            {
                if (validNumberEnd == 0)
                    GetValidNumberEndInternal();
                if (validNumberEnd == 0)
                    return string.Empty;

                string num = json.Substring(0, validNumberEnd);
                // validNumberEnd = 0;
                return num;
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
            LeptContext context = new LeptContext(json);
            Type = LeptType.Null; // Type is set to Null when parsing fails

            context.ParseWhiteSpace();
            LeptParseResult result = ParseValue(context);
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
        LeptParseResult ParseValue(LeptContext context)
        {
            switch (context[0])
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
            if (!context.ParseLiteral(literal))
                return LeptParseResult.InvalidValue;

            Type = expectedType;
            return LeptParseResult.OK;
        }
        LeptParseResult ParseNumber(LeptContext context)
        {
            int validNumberEnd = context.GetValidNumberEnd(); // parse till invalid value
            if (validNumberEnd == 0)
                return context[0] == '-' ? LeptParseResult.RootNotSingular : LeptParseResult.InvalidValue;

            try { number = double.Parse(context.GetValidNumberString()); }
            catch (OverflowException) { return LeptParseResult.NumberTooBig; }

            Type = LeptType.Number;
            context.JumpToValidNumberEnd();

            return LeptParseResult.OK; // can still be root not singular in the end
        }
    }
}
