using System;

namespace HobbyDS9.FoundationRepositoryHost
{
    // Newtonsoft accepts comments, single quotes, trailing commas, undefined and
    // constructors. Validate RFC 8259 syntax first so none reach the DTO loader.
    internal sealed class JsonGrammar
    {
        internal sealed class DepthLimitException : Exception { }

        private readonly string text;
        private int position;
        internal JsonGrammar(string text) { this.text = text; }
        internal void Validate() { Value(0); Space(); if (position != text.Length) Fail(); }
        private void Space()
        { while (position < text.Length && (text[position] == ' ' || text[position] == '\t' || text[position] == '\r' || text[position] == '\n')) position++; }
        private bool Take(char c)
        { Space(); if (position == text.Length || text[position] != c) return false; position++; return true; }
        private void Need(char c) { if (!Take(c)) Fail(); }
        private void Value(int depth)
        {
            if (depth > 128) throw new DepthLimitException();
            Space(); if (position == text.Length) Fail();
            var c = text[position];
            if (c == '{')
            {
                position++; if (Take('}')) return;
                do { Space(); String(); Need(':'); Value(depth + 1); } while (Take(','));
                Need('}');
            }
            else if (c == '[')
            {
                position++; if (Take(']')) return;
                do { Value(depth + 1); } while (Take(','));
                Need(']');
            }
            else if (c == '"') String();
            else if (c == 't') Literal("true");
            else if (c == 'f') Literal("false");
            else if (c == 'n') Literal("null");
            else Number();
        }
        private void Literal(string value)
        {
            foreach (var c in value) { if (position == text.Length || text[position++] != c) Fail(); }
        }
        private void String()
        {
            if (position == text.Length || text[position++] != '"') Fail();
            while (position < text.Length)
            {
                var c = text[position++];
                if (c == '"') return;
                if (c < 0x20) Fail();
                if (c != '\\') continue;
                if (position == text.Length) Fail();
                c = text[position++];
                if (c == 'u')
                {
                    var unit = HexUnit();
                    if (unit >= 0xd800 && unit <= 0xdbff)
                    {
                        if (position + 1 >= text.Length || text[position++] != '\\' || text[position++] != 'u') Fail();
                        var low = HexUnit();
                        if (low < 0xdc00 || low > 0xdfff) Fail();
                    }
                    else if (unit >= 0xdc00 && unit <= 0xdfff) Fail();
                }
                else if (c != '"' && c != '\\' && c != '/' && c != 'b' && c != 'f' && c != 'n' && c != 'r' && c != 't') Fail();
            }
            Fail();
        }
        private int HexUnit()
        {
            var value = 0;
            for (var i = 0; i < 4; i++)
            {
                if (position == text.Length) Fail();
                var c = text[position++];
                var digit = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
                if (digit < 0) Fail();
                value = value * 16 + digit;
            }
            return value;
        }
        private bool Digit() => position < text.Length && text[position] >= '0' && text[position] <= '9';
        private void Digits() { if (!Digit()) Fail(); while (Digit()) position++; }
        private void Number()
        {
            if (position < text.Length && text[position] == '-') position++;
            if (position < text.Length && text[position] == '0') position++; else Digits();
            if (position < text.Length && text[position] == '.') { position++; Digits(); }
            if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
            {
                position++;
                if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;
                Digits();
            }
        }
        private static void Fail() => throw new ArgumentException("Invalid strict JSON syntax.");
    }
}
