using System;
using System.Collections.Generic;
using Xunit;
using LeptJSON;

namespace LeptJSON.UnitTest
{
    public class UnitTest
    {
        static Lept parser = new Lept();

        static void TestError(string errorJSON, LeptParseResult errorResult)
        {
            parser.Boolean = false;
            Assert.Equal(errorResult, parser.Parse(errorJSON));
            Assert.Equal(LeptType.Null, parser.Type);
        }

        public class TestOtherErrors
        {
            [Theory]
            [InlineData("null x"), InlineData("0123"), InlineData("0x0"), InlineData("0x123")]
            [InlineData("123 123"), InlineData("-"), InlineData("1E")]
            public void TestRootNotSingular(string json) => TestError(json, LeptParseResult.RootNotSingular);

            [Theory]
            [InlineData("nul"), InlineData("?")]
            public void TestInvalidValue(string json) => TestError(json, LeptParseResult.InvalidValue);

            [Theory]
            [InlineData(""), InlineData(" ")]
            public void TestExpectValue(string json) => TestError(json, LeptParseResult.ExpectValue);
        }

        public class TestLiteral
        {
            [Fact]
            public void TestNull()
            {
                parser.Boolean = false;
                Assert.Equal(LeptParseResult.OK, parser.Parse("null"));
                Assert.Equal(LeptType.Null, parser.Type);
            }
            [Fact]
            void TestAccessNull()
            {
                parser.String = "a";
                parser.Type = LeptType.Null;
                Assert.Equal(LeptType.Null, parser.Type);
            }

            void TestBoolean(string json, LeptType expectedType)
            {
                Assert.Equal(LeptParseResult.OK, parser.Parse(json));
                Assert.Equal(expectedType, parser.Type);
            }
            [Fact]
            public void TestTrue() => TestBoolean("true", LeptType.True);
            [Fact]
            public void TestFalse() => TestBoolean("false", LeptType.False);

            void TestAccessBoolean(bool boolean)
            {
                parser.String = "a";
                parser.Boolean = boolean;
                Assert.Equal(boolean ? LeptType.True : LeptType.False, parser.Type);
            }
            [Fact]
            public void TestAccessTrue() => TestAccessBoolean(true);
            [Fact]
            public void TestAccessFalse() => TestAccessBoolean(false);
        }

        public class TestNumbers
        {
            [Theory]
            [MemberData(nameof(TestNumberSource))]
            public void TestNumber(double expected, string json)
            {
                Assert.Equal(LeptParseResult.OK, parser.Parse(json));
                Assert.Equal(LeptType.Number, parser.Type);
                Assert.Equal(expected, parser.Number);
            }
            public static IEnumerable<object[]> TestNumberSource()
            {
                yield return new object[] { 0.0, "0" };
                yield return new object[] { 0.0, "-0" };
                yield return new object[] { 0.0, "-0.0" };
                yield return new object[] { 1.0, "1" };
                yield return new object[] { -1.0, "-1" };
                yield return new object[] { 1.5, "1.5" };
                yield return new object[] { -1.5, "-1.5" };
                yield return new object[] { 3.1416, "3.1416" };
                yield return new object[] { 1E10, "1E10" };
                yield return new object[] { 1e10, "1e10" };
                yield return new object[] { 1E+10, "1E+10" };
                yield return new object[] { 1E-10, "1E-10" };
                yield return new object[] { -1E10, "-1E10" };
                yield return new object[] { -1e10, "-1e10" };
                yield return new object[] { -1E+10, "-1E+10" };
                yield return new object[] { -1E-10, "-1E-10" };
                yield return new object[] { 1.234E+10, "1.234E+10" };
                yield return new object[] { 1.234E-10, "1.234E-10" };
                yield return new object[] { 0.0, "1e-10000" }; // must underflow

                yield return new object[] { 1.0000000000000002, "1.0000000000000002" }; // the smallest number > 1
                yield return new object[] { 4.9406564584124654e-324, "4.9406564584124654e-324" }; // minimum denormal
                yield return new object[] { -4.9406564584124654e-324, "-4.9406564584124654e-324" };
                yield return new object[] { 2.2250738585072009e-308, "2.2250738585072009e-308" };  // Max subnormal double
                yield return new object[] { -2.2250738585072009e-308, "-2.2250738585072009e-308" };
                yield return new object[] { 2.2250738585072014e-308, "2.2250738585072014e-308" };  // Min normal positive double
                yield return new object[] { -2.2250738585072014e-308, "-2.2250738585072014e-308" };
                yield return new object[] { 1.7976931348623157e+308, "1.7976931348623157e+308" };  // Max double
                yield return new object[] { -1.7976931348623157e+308, "-1.7976931348623157e+308" };

                yield return new object[] { 1, "1 " };
            }

            [Theory]
            [InlineData("+0"), InlineData("+1"), InlineData(".123")]
            [InlineData("1."), InlineData("INF"), InlineData("inf"), InlineData("NAN"), InlineData("nan")]
            public void TestInvalidNumber(string invalidNumber) => TestError(invalidNumber, LeptParseResult.InvalidValue);

            [Theory]
            [InlineData("1e309"), InlineData("-1e309")]
            public void TestNumberTooBig(string bigNumber) => TestError(bigNumber, LeptParseResult.NumberTooBig);
        }

        public class TestStrings
        {
            [Theory]
            [MemberData(nameof(TestStringSource))]
            void TestString(string expected, string json)
            {
                Assert.Equal(LeptParseResult.OK, parser.Parse(json));
                Assert.Equal(LeptType.String, parser.Type);
                Assert.Equal(expected, parser.String);
            }

            public static IEnumerable<object[]> TestStringSource()
            {
                yield return new object[] { "", "\"\"" };
                yield return new object[] { "Hello", "\"Hello\"" };
                yield return new object[] { "Hello\nWorld", "\"Hello\\nWorld\"" };
                yield return new object[] { "\" \\ / \b \f \n \r \t", "\"\\\" \\\\ \\/ \\b \\f \\n \\r \\t\"" };
                yield return new object[] { "Hello\0World", "\"Hello\\u0000World\"" };
                yield return new object[] { "\x24", "\"\\u0024\"" }; // Dollar sign U+0024
                yield return new object[] { "\xC2\xA2", "\"\\u00A2\"" }; // Cents sign U+00A2
                yield return new object[] { "\xE2\x82\xAC", "\"\\u20AC\"" }; // Euro sign U+20AC
                yield return new object[] { "\xF0\x9D\x84\x9E", "\"\\uD834\\uDD1E\"" }; // G clef sign U+1D11E
                yield return new object[] { "\xF0\x9D\x84\x9E", "\"\\ud834\\udd1e\"" }; // G clef sign U+1D11E
            }

            [Theory]
            [InlineData("", 0), InlineData("Hello", 5)]
            public void TestAccessString(string json, int length)
            {
                parser.String = json;
                Assert.Equal(json, parser.String);
                Assert.Equal(LeptType.String, parser.Type);
                Assert.Equal(length, parser.String.Length);
            }
            [Theory]
            [InlineData("\""), InlineData("\"abc")]
            void TestMissingQuotationMark(string json) => TestError(json, LeptParseResult.MissQuotationMark);
            [Theory]
            [InlineData("\"\\v\""), InlineData("\"\\'\""), InlineData("\"\\0\""), InlineData("\"\\x12\"")]
            void TestInvalidStringEscape(string json) => TestError(json, LeptParseResult.InvalidStringEscape);
            [Theory]
            [InlineData("\"\x01\""), InlineData("\"\x1F\"")]
            void TestInvalidStringChar(string json) => TestError(json, LeptParseResult.InvalidStringChar);

            [Theory]
            [InlineData("\"\\u\""), InlineData("\"\\u0\""), InlineData("\"\\u01\""), InlineData("\"\\u012\"")]
            [InlineData("\"\\u/000\""), InlineData("\"\\uG000\""), InlineData("\"\\u0/00\""), InlineData("\"\\u0G00\"")]
            [InlineData("\"\\u0/00\""), InlineData("\"\\u00G0\""), InlineData("\"\\u000/\""), InlineData("\"\\u000G\"")]
            void TestInvalidUnicodeHex(string json) => TestError(json, LeptParseResult.InvalidUnicodeHex);

            [Theory]
            [InlineData("\"\\uD800\""), InlineData("\"\\uDBFF\""), InlineData("\"\\uD800\\\\\"")]
            [InlineData("\"\\uD800\\uDBFF\""), InlineData("\"\\uD800\\uE000\"")]
            void TestInvalidUnicodeSurrogate(string json) => TestError(json, LeptParseResult.InvalidUnicodeSurrogate);
        }
    }
}
