using System;
using System.Text;
using System.Collections.Generic;

namespace LeptJSON
{
    public partial class Lept
    {
        class LeptContext
        {
            internal string JSON { get; private set; }
            internal char this[int index] => JSON[index];
            internal void ParseWhiteSpace() => JSON = JSON.TrimStart();

            internal LeptContext(string json) => JSON = json + "\0";

            internal bool ParseLiteral(string literal)
            {
                if (!JSON.StartsWith(literal))
                    return false;

                JSON = JSON.Substring(literal.Length);
                return true;
            }

            #region Parsing Number

            int validNumberEnd = 0;
            bool PositionIsDigit(int position) => JSON[position] >= '0' && JSON[position] <= '9';
            internal int GetValidNumberEnd(bool useRegex = false) => validNumberEnd = useRegex == true ? GetValidNumberEndRegex() : GetValidNumberEndInternal();
            int GetValidNumberEndRegex()
            {
                int end = System.Text.RegularExpressions.Regex.Match(JSON, @"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?").Length;
                return JSON[end] == '.' ? 0 : end; // 1. is invalid
            }

            int GetValidNumberEndInternal()
            {
                int position = 0;

                if (JSON[position] == '-') // jump across optional negative sign
                    position++;

                if (JSON[position] >= '1' && JSON[position] <= '9')
                {
                    while (PositionIsDigit(position)) // integer part
                    {
                        position++;
                    }
                }
                else if (JSON[position] == '0') // if integer part starts with 0, it should be a single 0
                    position++;
                else return 0; // invalid value or none or **have negative sign but no digit**, so it must be 0, not position

                if (JSON[position] == '.')
                {
                    position++; // jump across decimal point
                    if (!PositionIsDigit(position)) // have decimal point but no digit
                        return 0; // invalid value
                    else
                        while (PositionIsDigit(position)) // decimal part
                            position++;
                }

                if (JSON[position] == 'e' || JSON[position] == 'E')
                {
                    position++; // jump across natural constant symbol

                    if (JSON[position] == '+' || JSON[position] == '-') // jump across optional positive and negative sign
                        position++;

                    if (!PositionIsDigit(position)) // invalid caracter or have natural logarithmic symbol symbol but no digit
                    {
                        if (JSON[position - 1] == '+' || JSON[position - 1] == '-') // look back
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

                JSON = JSON.Substring(validNumberEnd);
                validNumberEnd = 0;
            }
            internal string GetValidNumberString()
            {
                if (validNumberEnd == 0)
                    GetValidNumberEndInternal();
                if (validNumberEnd == 0)
                    return string.Empty;

                string num = JSON.Substring(0, validNumberEnd);
                // validNumberEnd = 0;
                return num;
            }

            #endregion

            #region Parsing String

            internal LeptParseResult ParseString(out string result)
            {
                StringBuilder sb = new StringBuilder();

                int position = 0;
                if (JSON[position++] != '\"')
                    throw new Exception();

                result = null;
                while (true)
                {
                    switch (JSON[position])
                    {
                        case '\"':
                            {
                                result = sb.ToString();
                                JSON = JSON.Substring(position + 1);
                                return LeptParseResult.OK;
                            }
                        case '\0': return LeptParseResult.MissQuotationMark;
                        case '\\':
                            {
                                switch (JSON[++position])
                                {
                                    case '\"': sb.Append('\"'); break;
                                    case '\\': sb.Append('\\'); break;
                                    case '/': sb.Append('/'); break;
                                    case 'b': sb.Append('\b'); break;
                                    case 'f': sb.Append('\f'); break;
                                    case 'n': sb.Append('\n'); break;
                                    case 'r': sb.Append('\r'); break;
                                    case 't': sb.Append('\t'); break;
                                    case 'u':
                                        {
                                            LeptParseResult parseResult;
                                            if ((parseResult = ParseCodePoint(ref position, out uint codePoint)) != LeptParseResult.OK)
                                                return parseResult;
                                            List<char> UTF8chars = EncodeUTF8(ref codePoint);
                                            if (UTF8chars == null)
                                                return LeptParseResult.InvalidUnicodeSurrogate;
                                            else
                                                UTF8chars.ForEach(x => sb.Append(x));
                                            break;
                                        }
                                    default: return LeptParseResult.InvalidStringEscape;
                                }
                                position++;
                                continue;
                            }
                        default:
                            {
                                if (JSON[position] < 0x20)
                                    return LeptParseResult.InvalidStringChar;

                                sb.Append(JSON[position++]);
                                continue;
                            }
                    }
                }
            }

            LeptParseResult ParseCodePoint(ref int position, out uint codePoint)
            {
                codePoint = 0;
                if ((JSON[position++] != 'u'))
                    throw new Exception();

                uint? H;
                if ((H = ParseHex(ref position)) == null) // not a hex number
                    return LeptParseResult.InvalidUnicodeHex;


                if (!(H >= 0xD800 && H <= 0xDBFF)) // no need low surrogate
                    codePoint = H.Value;
                else
                {
                    if (JSON[position] != '\\' || JSON[position + 1] != 'u')
                        return LeptParseResult.InvalidUnicodeSurrogate; // no low surrogate
                    position += 2;

                    uint? L;
                    if ((L = ParseHex(ref position)) == null)
                        return LeptParseResult.InvalidUnicodeHex;
                    else if (!(L >= 0xDC00 && L <= 0xDFFF))
                        return LeptParseResult.InvalidUnicodeSurrogate; // low surrogate out of range
                    else // if commented out, it would report "L doesn't exist in the context", because L may not be declared if the first condition is true
                        codePoint = 0x10000 + (H.Value - 0xD800) * 0x400 + (L.Value - 0xDC00);
                }

                position--; // correspond with other situation
                return LeptParseResult.OK;
            }

            uint? ParseHex(ref int position)
            {
                uint hex = 0;
                for (int i = 0; i < 4; i++)
                {
                    char ch = JSON[position + i];
                    hex <<= 4;
                    if (ch >= '0' && ch <= '9') hex |= ch - (uint)'0';
                    else if (ch >= 'A' && ch <= 'F') hex |= ch - (uint)'A' + 10;
                    else if (ch >= 'a' && ch <= 'f') hex |= ch - (uint)'a' + 10;
                    else return null; // invalid hex number including spaces which ConvertTo.uint and uint.Parse don't fail
                }
                position += 4;
                return hex;
            }

            List<char> EncodeUTF8(ref uint codePoint)
            {
                List<char> UTF8chars = new List<char>();

                if (codePoint <= 0x007F)
                    UTF8chars.Add((char)(codePoint));
                else if (codePoint <= 0x07FF)
                {
                    UTF8chars.Add((char)(0xC0 | ((codePoint >> 6) & 0xFF)));
                    UTF8chars.Add((char)(0x80 | (codePoint & 0x3F)));
                }
                else if (codePoint <= 0xFFFF)
                {
                    UTF8chars.Add((char)(0xE0 | ((codePoint >> 12) & 0xFF)));
                    UTF8chars.Add((char)(0x80 | ((codePoint >> 6) & 0x3F)));
                    UTF8chars.Add((char)(0x80 | (codePoint & 0x3F)));
                }
                else if (codePoint <= 0x10FFFF)
                {
                    UTF8chars.Add((char)(0xF0 | ((codePoint >> 18) & 0xFF)));
                    UTF8chars.Add((char)(0x80 | ((codePoint >> 12) & 0x3F)));
                    UTF8chars.Add((char)(0x80 | ((codePoint >> 6) & 0x3F)));
                    UTF8chars.Add((char)(0x80 | (codePoint & 0x3F)));
                }
                else
                    return null;

                return UTF8chars;
            }

            #endregion

            #region Parsing Array

            internal LeptParseResult ParseArray(out Lept[] array)
            {
                if (JSON[0] != '[')
                    throw new Exception();

                array = null;
                List<Lept> buffer = new List<Lept>();
                // SkipWhiteSpace(ref position);
                JSON = JSON.Substring(1);
                ParseWhiteSpace();
                if (JSON[0] == ']')
                {
                    JSON = JSON.Substring(1);
                    array = new Lept[0];
                    return LeptParseResult.OK;
                }

                while (true)
                {
                    Lept value = new Lept(this);
                    LeptParseResult parseResult;
                    if ((parseResult = value.ParseValue()) != LeptParseResult.OK)
                        return parseResult;
                    buffer.Add(value);

                    ParseWhiteSpace();
                    switch (JSON[0])
                    {
                        case ',': JSON = JSON.Substring(1); ParseWhiteSpace(); continue;
                        case ']':
                            {
                                JSON = JSON.Substring(1);
                                array = buffer.ToArray();
                                return LeptParseResult.OK;
                            }
                        default: return LeptParseResult.MissCommaOrSquareBracket;
                    }
                }
            }

            #endregion
        }
    }
}
