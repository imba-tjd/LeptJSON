namespace Tutorial01.Leptjson
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
        RootNotSingular
    }

    public class Lept
    {
        class LeptContext
        {
            public string json;
            public void ParseWhiteSpace()
            {
                json = json.TrimStart(' ', '\t', '\n', '\r');
            }
        }
        public LeptType Type { get; private set; }
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
                    result = LeptParseResult.RootNotSingular;
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
                default: return LeptParseResult.InvalidValue;
            }
        }
        [System.Obsolete]
        LeptParseResult ParseNull(LeptContext context)
        {
            if (!context.json.StartsWith("null"))
                return LeptParseResult.InvalidValue;

            context.json = context.json.Substring(4);
            Type = LeptType.Null;

            return LeptParseResult.OK;
        }
        LeptParseResult ParseLiteral(LeptContext context, string literal, LeptType expectType)
        {
            if (!context.json.StartsWith(literal))
                return LeptParseResult.InvalidValue;

            context.json = context.json.Substring(literal.Length);
            Type = expectType;

            return LeptParseResult.OK;
        }
    }
}
