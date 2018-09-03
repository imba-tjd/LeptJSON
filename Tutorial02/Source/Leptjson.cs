using System;

namespace Tutorial02.Leptjson
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
            internal int FindFisrtWhiteSpace()
            {
                return json.IndexOfAny(new char[] { ' ', '\t', '\n', '\r', '\0' });
            }

            #region Checking Number

            internal bool PositionIsDigit(int position) => json[position] >= '0' && json[position] <= '9';
            internal bool CheckNumberRegex() => System.Text.RegularExpressions.Regex.IsMatch(json, @"^-?(0[xX])?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?[\s\0]");
            bool AssertRemaningIsWhiteSpace(int position)
            {
                while (json[++position] != '\0')
                    if (!char.IsWhiteSpace(json[position]))
                        return false;
                return true;
            }
            internal bool CheckNumber(bool useRegex = false)
            {
                if (useRegex == true)
                    return CheckNumberRegex();

                int position = 0;
                if (json[position] == '-') // jump across optional negative sign
                    position++;

                if (json[position] == '0' && (json[position + 1] == 'x' || json[position + 1] == 'X')) // jump across 0x and 0X
                    position += 2;

                if (json[position] >= '1' || json[position] <= 9)
                {
                    while (json[position] != '\0') // integer part
                        if (PositionIsDigit(position))
                            position++;
                        else if (json[position] == '.' || json[position] == 'e' || json[position] == 'E')
                            break;
                        else if (char.IsWhiteSpace(json[position]))
                            return AssertRemaningIsWhiteSpace(position);
                        else
                            return false;
                }
                else if (json[position] == '0') // if integer part starts with 0, it should be a single 0
                    position++;
                else
                    return false;

                if (json[position] == '.') // determine which part is next
                {
                    position++; // jump across decimal point
                    if (!PositionIsDigit(position))
                        return false; // have decimal point but no decimal

                    while (json[position] != '\0') // decimal part
                        if (PositionIsDigit(position))
                            position++;
                        else if (json[position] == 'e' || json[position] == 'E')
                            break;
                        else if (char.IsWhiteSpace(json[position]))
                            return AssertRemaningIsWhiteSpace(position);
                        else
                            return false;
                }

                if (json[position] == 'e' || json[position] == 'E') // jump across natural constant symbol, must happens
                {
                    position++;
                    if (json[position] == '+' || json[position] == '-') // jump across optional positive and negative sign
                        position++;
                    if (json[position] == '\0') // have natural constant symbol but no exponent
                        return false;
                }

                while (json[position] != '\0') // exponent part
                    if (PositionIsDigit(position))
                        position++;
                    else if (char.IsWhiteSpace(json[position]))
                        return AssertRemaningIsWhiteSpace(position);
                    else
                        return false;

                return true;
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
                // case 'n': return ParseNull(context);
                case 'n': return ParseLiteral(context, "null", LeptType.Null);
                case 't': return ParseLiteral(context, "true", LeptType.True);
                case 'f': return ParseLiteral(context, "false", LeptType.False);
                case '\0': return LeptParseResult.ExpectValue;
                // default: return LeptParseResult.InvalidValue;
                default: return ParseNumber(context);
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
            if (context.CheckNumber(false) == false)
                return LeptParseResult.InvalidValue;

            int zeroPosition = context.json[0] == '-' ? 1 : 0;
            if (context.json[zeroPosition] == '0' && (context.PositionIsDigit(zeroPosition + 1) // 0123 is invalid
                || context.json[zeroPosition + 1] == 'x' || context.json[zeroPosition + 1] == 'X'
                )
            )
                // return LeptParseResult.InvalidValue;
                return LeptParseResult.RootNotSingular;

            try
            {
                number = double.Parse(context.json);
            }
            catch (OverflowException)
            {
                return LeptParseResult.NumberTooBig;
            }
            Type = LeptType.Number;

            context.json = context.json.Substring(context.FindFisrtWhiteSpace());

            return LeptParseResult.OK;
        }
    }
}
