using System;
using System.Collections.Generic;
using Xunit;
using LeptJSON;

namespace LeptJSON.UnitTest
{
    public class TestBase
    {
        protected Lept parser = new Lept();
        protected void TestError(string errorJSON, LeptParseResult errorResult)
        {
            parser.Boolean = false;
            Assert.Equal(errorResult, parser.Parse(errorJSON));
            Assert.Equal(LeptType.Null, parser.Type);
        }
    }

    public class TestOtherErrors : TestBase
    {
        [Theory]
        [InlineData("null x"), InlineData("0123"), InlineData("0x0"), InlineData("0x123")]
        [InlineData("123 123"), InlineData("-"), InlineData("1E")]
        void TestRootNotSingular(string json) => TestError(json, LeptParseResult.RootNotSingular);

        [Theory]
        [InlineData("nul"), InlineData("?")]
        void TestInvalidValue(string json) => TestError(json, LeptParseResult.InvalidValue);

        [Theory]
        [InlineData(""), InlineData(" ")]
        void TestExpectValue(string json) => TestError(json, LeptParseResult.ExpectValue);
    }

    public class TestLiteral : TestBase
    {
        [Fact]
        void TestNull()
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
        void TestTrue() => TestBoolean("true", LeptType.True);
        [Fact]
        void TestFalse() => TestBoolean("false", LeptType.False);

        void TestAccessBoolean(bool boolean)
        {
            parser.String = "a";
            parser.Boolean = boolean;
            Assert.Equal(boolean ? LeptType.True : LeptType.False, parser.Type);
        }

        [Fact]
        void TestAccessTrue() => TestAccessBoolean(true);
        [Fact]
        void TestAccessFalse() => TestAccessBoolean(false);
    }

    public class TestNumbers : TestBase
    {
        [Theory]
        [MemberData(nameof(TestNumberSource))]
        void TestNumber(double expected, string json)
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse(json));
            Assert.Equal(LeptType.Number, parser.Type);
            Assert.Equal(expected, parser.Number);
        }

        public static IEnumerable<object[]> TestNumberSource() // MemberData must reference a public member
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
        void TestInvalidNumber(string invalidNumber) => TestError(invalidNumber, LeptParseResult.InvalidValue);

        [Theory]
        [InlineData("1e309"), InlineData("-1e309")]
        void TestNumberTooBig(string bigNumber) => TestError(bigNumber, LeptParseResult.NumberTooBig);
    }

    public class TestStrings : TestBase
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
        void TestAccessString(string json, int length)
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
        [InlineData("\"\\u00/0\""), InlineData("\"\\u00G0\""), InlineData("\"\\u000/\""), InlineData("\"\\u000G\"")]
        [InlineData("\"\\u 0024\"")]
        void TestInvalidUnicodeHex(string json) => TestError(json, LeptParseResult.InvalidUnicodeHex);

        [Theory]
        [InlineData("\"\\uD800\""), InlineData("\"\\uDBFF\""), InlineData("\"\\uD800\\\\\"")]
        [InlineData("\"\\uD800\\uDBFF\""), InlineData("\"\\uD800\\uE000\"")]
        void TestInvalidUnicodeSurrogate(string json) => TestError(json, LeptParseResult.InvalidUnicodeSurrogate);
    }

    public class TestArrays : TestBase
    {
        [Fact]
        void TestArray1()
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse("[ null , false , true , 123 , \"abc\" ]"));
            Assert.Equal(LeptType.Array, parser.Type);
            Assert.Equal(5, parser.Array.Length);
            Assert.Equal(LeptType.Null, parser.Array[0].Type);
            Assert.Equal(LeptType.False, parser.Array[1].Type);
            Assert.Equal(LeptType.True, parser.Array[2].Type);
            Assert.Equal(LeptType.Number, parser.Array[3].Type);
            Assert.Equal(LeptType.String, parser.Array[4].Type);
            Assert.Equal(123.0, parser.Array[3].Number);
            Assert.Equal("abc", parser.Array[4].String);
            Assert.Equal(3, parser.Array[4].String.Length);
        }

        [Fact]
        void TestArray2()
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse("[ [ ] , [ 0 ] , [ 0 , 1 ] , [ 0 , 1 , 2 ] ]"));
            Assert.Equal(LeptType.Array, parser.Type);
            Assert.Equal(4, parser.Array.Length);
            for (int i = 0; i < 4; i++)
            {
                Lept element1 = parser.Array[i];
                Assert.Equal(LeptType.Array, element1.Type);
                Assert.Equal(i, element1.Array.Length);
                for (int j = 0; j < i; j++)
                {
                    Lept element2 = element1.Array[j];
                    Assert.Equal(LeptType.Number, element2.Type);
                    Assert.Equal((double)j, element2.Number);
                }
            }
        }

        [Theory]
        [InlineData("[1"), InlineData("[1}"), InlineData("[1 2"), InlineData("[[]")]
        void TestMissCommaOrSquareBracket(string json) => TestError(json, LeptParseResult.MissCommaOrSquareBracket);
    }

    public class TestObjects : TestBase
    {
        [Fact]
        void TestObject1()
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse(" { } "));
            Assert.Equal(LeptType.Object, parser.Type);
            Assert.Empty(parser.Object);
        }

        [Fact]
        void TestObject2()
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse(
                " { " +
                "\"n\" : null , " +
                "\"f\" : false , " +
                "\"t\" : true , " +
                "\"i\" : 123 , " +
                "\"s\" : \"abc\", " +
                "\"a\" : [ 1, 2, 3 ]," +
                "\"o\" : { \"1\" : 1, \"2\" : 2, \"3\" : 3 }" +
                " } "
            ));
            Assert.Equal(LeptType.Object, parser.Type);
            Assert.Equal(7, parser.Object.Length);
            Assert.Equal("n", parser.Object[0].Key);
            Assert.Equal(LeptType.Null, parser.Object[0].Value.Type);
            Assert.Equal("f", parser.Object[1].Key);
            Assert.Equal(LeptType.False, parser.Object[1].Value.Type);
            Assert.Equal("t", parser.Object[2].Key);
            Assert.Equal(LeptType.True, parser.Object[2].Value.Type);
            Assert.Equal("i", parser.Object[3].Key);
            Assert.Equal(LeptType.Number, parser.Object[3].Value.Type);
            Assert.Equal(123.0, parser.Object[3].Value.Number);
            Assert.Equal("s", parser.Object[4].Key);
            Assert.Equal(LeptType.String, parser.Object[4].Value.Type);
            Assert.Equal("abc", parser.Object[4].Value.String);
            Assert.Equal("a", parser.Object[5].Key);
            Assert.Equal(LeptType.Array, parser.Object[5].Value.Type);
            Assert.Equal(3, parser.Object[5].Value.Array.Length);
            for (int i = 0; i < 3; i++)
            {
                Lept element = parser.Object[5].Value.Array[i];
                Assert.Equal(LeptType.Number, element.Type);
                Assert.Equal(i + 1.0, element.Number);
            }
            Assert.Equal("o", parser.Object[6].Key);
            {
                Lept element1 = parser.Object[6].Value;
                Assert.Equal(LeptType.Object, element1.Type);
                for (int i = 0; i < 3; i++)
                {
                    Lept element2 = element1.Object[i].Value;
                    Assert.Equal('1' + i, element1.Object[i].Key[0]);
                    Assert.Equal(1, element1.Object[i].Key.Length);
                    Assert.Equal(LeptType.Number, element2.Type);
                    Assert.Equal(i + 1.0, element2.Number);
                }
            }
        }

        [Theory]
        [InlineData("{:1,"), InlineData("{1:1,"), InlineData("{true:1,"), InlineData("{false:1,")]
        [InlineData("{null:1,"), InlineData("{[]:1,"), InlineData("{{}:1,"), InlineData("{\"a\":1,")]
        void TestMissKey(string json) => TestError(json, LeptParseResult.MissKey);

        [Theory]
        [InlineData("{\"a\"}"), InlineData("{\"a\",\"b\"}")]
        void TestMissColon(string json) => TestError(json, LeptParseResult.MissColon);

        [Theory]
        [InlineData("{\"a\":1"), InlineData("{\"a\":1]"), InlineData("{\"a\":1 \"b\""), InlineData("{\"a\":{}")]
        void TestMissCommaOrCurlyBracket(string json) => TestError(json, LeptParseResult.MissCommaOrCurlyBracket);

        [Fact]
        void TestKeyStringError()
        {
            TestError("{\":1}", LeptParseResult.MissQuotationMark);
            TestError("{\"\\v\":1}", LeptParseResult.InvalidStringEscape);
            TestError("{\"\x01\":1}", LeptParseResult.InvalidStringChar);
        }

        public class TestStringify : TestBase
        {
            void TestRoundTrip(string json)
            {
                Assert.Equal(LeptParseResult.OK, parser.Parse(json));
                Assert.Equal(json, parser.Stringify());
            }

            [Theory]
            [InlineData("null"), InlineData("true"), InlineData("false")]
            void TestStringifyLiteral(string json) => TestRoundTrip(json);

            [Theory]
            [InlineData("0"), InlineData("1"), InlineData("-1"), InlineData("1.5"), InlineData("-1.5"),]
            [InlineData("3.25"), InlineData("1e+20"), InlineData("1.234e+20"), InlineData("1.234e-20")]
            [InlineData("1.0000000000000002"), InlineData("4.9406564584124654e-324"), InlineData("-4.9406564584124654e-324")]
            [InlineData("2.2250738585072009e-308"), InlineData("-2.2250738585072009e-308"), InlineData("2.2250738585072014e-308")]
            [InlineData("-2.2250738585072014e-308"), InlineData("1.7976931348623157e+308"), InlineData("-1.7976931348623157e+308")]
            // [InlineData("-0")]
            void TestStringifyNumber(string json) => TestRoundTrip(json);

            [Theory]
            [InlineData("\"\""), InlineData("\"Hello\""), InlineData("\"Hello\\nWorld\"")]
            [InlineData("\"\\\" \\\\ / \\b \\f \\n \\r \\t\""), InlineData("\"Hello\\u0000World\"")]
            void TestStringifyString(string json) => TestRoundTrip(json);

            [Theory]
            [InlineData("[]"), InlineData("[null,false,true,123,\"abc\",[1,2,3]]")]
            void TestStringifyArray(string json) => TestRoundTrip(json);

            [Theory]
            [InlineData("{}")]
            [InlineData("{\"n\":null,\"f\":false,\"t\":true,\"i\":123,\"s\":\"abc\",\"a\":[1,2,3],\"o\":{\"1\":1,\"2\":2,\"3\":3}}")]
            void TestStringifyObject(string json) => TestRoundTrip(json);

        }
    }
}
