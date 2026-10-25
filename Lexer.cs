using System;
using System.Collections.Generic;

namespace MyExcelMAUIApp
{
    public class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message) { }
    }

    public enum TokenType
    {
        Number,
        CellRef,
        Boolean,
        Plus,
        Minus,
        Mul,
        Div,
        Mod,
        IntDiv,
        Pow,
        Eq,
        Lt,
        Gt,
        Neq,
        Le,
        Ge,
        LParen,
        RParen,
        End
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Text { get; }

        public Token(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }

        public override string ToString() => $"{Type}({Text})";
    }

    public class Lexer
    {
        public List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    int start = i;
                    while (i < text.Length && text[i] >= '0' && text[i] <= '9') i++;
                    tokens.Add(new Token(TokenType.Number, text.Substring(start, i - start)));
                    continue;
                }

                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < text.Length &&
                           (char.IsLetter(text[i]) || (text[i] >= '0' && text[i] <= '9')))
                        i++;
                    string word = text.Substring(start, i - start);
                    string lower = word.ToLower();

                    if (lower == "true" || lower == "false") tokens.Add(new Token(TokenType.Boolean, lower));
                    else if (lower == "mod") tokens.Add(new Token(TokenType.Mod, word));
                    else if (lower == "div") tokens.Add(new Token(TokenType.IntDiv, word));
                    else if (IsCellName(word)) tokens.Add(new Token(TokenType.CellRef, word.ToUpper()));
                    else throw new ExpressionException($"Невідомий символ або слово: '{word}'");
                    continue;
                }

                switch (c)
                {
                    case '+': tokens.Add(new Token(TokenType.Plus, "+")); i++; break;
                    case '-': tokens.Add(new Token(TokenType.Minus, "-")); i++; break;
                    case '*': tokens.Add(new Token(TokenType.Mul, "*")); i++; break;
                    case '/': tokens.Add(new Token(TokenType.Div, "/")); i++; break;
                    case '^': tokens.Add(new Token(TokenType.Pow, "^")); i++; break;
                    case '(': tokens.Add(new Token(TokenType.LParen, "(")); i++; break;
                    case ')': tokens.Add(new Token(TokenType.RParen, ")")); i++; break;
                    case '=': tokens.Add(new Token(TokenType.Eq, "=")); i++; break;

                    case '<':
                        if (i + 1 < text.Length && text[i + 1] == '=')
                        { tokens.Add(new Token(TokenType.Le, "<=")); i += 2; }
                        else if (i + 1 < text.Length && text[i + 1] == '>')
                        { tokens.Add(new Token(TokenType.Neq, "<>")); i += 2; }
                        else
                        { tokens.Add(new Token(TokenType.Lt, "<")); i++; }
                        break;

                    case '>':
                        if (i + 1 < text.Length && text[i + 1] == '=')
                        { tokens.Add(new Token(TokenType.Ge, ">=")); i += 2; }
                        else
                        { tokens.Add(new Token(TokenType.Gt, ">")); i++; }
                        break;

                    case '#':
                        throw new ExpressionException("Посилання на видалену клітинку");
                    default:
                        throw new ExpressionException($"Недопустимий символ: '{c}'");
                }
            }

            tokens.Add(new Token(TokenType.End, ""));
            return tokens;
        }

        private bool IsCellName(string word)
        {
            int i = 0;

            while (i < word.Length &&
                   ((word[i] >= 'A' && word[i] <= 'Z') ||
                    (word[i] >= 'a' && word[i] <= 'z')))
            {
                i++;
            }

            if (i == 0 || i == word.Length)
                return false;

            if (word[i] == '0')
                return false;

            while (i < word.Length)
            {
                if (word[i] < '0' || word[i] > '9')
                    return false;

                i++;
            }

            return true;
        }
    }
}