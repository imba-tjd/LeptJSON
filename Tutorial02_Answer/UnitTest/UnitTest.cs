using System;
using System.Collections.Generic;
using Xunit;
using Tutorial02.Leptjson;

namespace Tutorial01.UnitTest
{
    public class UnitTest
    {
        Lept parser = new Lept();

        #region TestError

        void TestError(string errorJson, LeptParseResult errorResult)
        {
            parser.GetType().GetProperty("Type").SetValue(parser, LeptType.False);
            Assert.Equal(errorResult, parser.Parse(errorJson));
            Assert.Equal(LeptType.Null, parser.Type);
        }

        [Theory]
        [InlineData("null x"), InlineData("0123"), InlineData("0x0"), InlineData("0x123")]
        public void TestParseRootNotSingular(string json) => TestError(json, LeptParseResult.RootNotSingular);

        [Theory]
        [InlineData("nul"), InlineData("?")]
        [InlineData("1E"), InlineData("1~")] // This test is misled.
        public void TestParseInvalidValue(string json) => TestError(json, LeptParseResult.InvalidValue);

        [Theory]
        [InlineData(""), InlineData(" ")]
        public void TestExpectValue(string json) => TestError(json, LeptParseResult.ExpectValue);

        [Theory]
        [InlineData("+0"), InlineData("+1"), InlineData(".123")]
        [InlineData("1."), InlineData("INF"), InlineData("inf"), InlineData("NAN"), InlineData("nan")]
        public void TestInvalidNumber(string invalidNumber) => TestError(invalidNumber, LeptParseResult.InvalidValue);

        [Theory]
        [InlineData("1e309"), InlineData("-1e309")]
        public void TestNumberTooBig(string bigNumber) => TestError(bigNumber, LeptParseResult.NumberTooBig);

        #endregion

        #region TestLiteral

        void TestLiteral(string json, LeptType expectedType)
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse(json));
            Assert.Equal(expectedType, parser.Type);
        }

        [Fact]
        public void TestNull() => TestLiteral("null", LeptType.Null); // Testing null's expected type will always success because Type was set to Null even when parsing failed.
        // }
        [Fact]
        public void TestTrue() => TestLiteral("true", LeptType.True);
        [Fact]
        public void TestFalse() => TestLiteral("false", LeptType.False);

        #endregion

        [Theory]
        [MemberData(nameof(TestNumberSource))]
        public void TestNumber(double expected, string actual)
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse(actual));
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
    }
}
