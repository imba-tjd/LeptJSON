using System;
using Xunit;
using LeptJSON;

namespace LeptJSON.UnitTest
{
    public class UnitTest
    {
        Lept parser = new Lept();

        [Fact]
        public void TestParseRootNotSingular()
        {
            parser.GetType().GetProperty("Type").SetValue(parser, LeptType.False);
            Assert.Equal(LeptParseResult.RootNotSingular, parser.Parse("null x"));
            Assert.Equal(LeptType.Null, parser.Type);
        }
        [Fact]
        public void TestParseInvalidValue()
        {
            parser.GetType().GetProperty("Type").SetValue(parser, LeptType.False);
            Assert.Equal(LeptParseResult.InvalidValue, parser.Parse("nul"));
            Assert.Equal(LeptType.Null, parser.Type);
        }
        [Fact]
        public void TestExpectValue()
        {
            parser.GetType().GetProperty("Type").SetValue(parser, LeptType.False);
            Assert.Equal(LeptParseResult.ExpectValue, parser.Parse(""));
            Assert.Equal(LeptType.Null, parser.Type);

            parser.GetType().GetProperty("Type").SetValue(parser, LeptType.False);
            Assert.Equal(LeptParseResult.ExpectValue, parser.Parse(" "));
            Assert.Equal(LeptType.Null, parser.Type);
        }
        [Fact]
        public void TestNull()
        {
            parser.GetType().GetProperty("Type").SetValue(parser, LeptType.False);
            Assert.Equal(LeptParseResult.OK, parser.Parse("null"));
            Assert.Equal(LeptType.Null, parser.Type);
        }
        [Fact]
        public void TestTrue()
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse("true"));
            Assert.Equal(LeptType.True, parser.Type);
        }
        [Fact]
        public void TestFalse()
        {
            Assert.Equal(LeptParseResult.OK, parser.Parse("false"));
            Assert.Equal(LeptType.False, parser.Type);
        }
    }
}
