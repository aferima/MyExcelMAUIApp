using System;
using System.Numerics;

namespace MyExcelMAUIApp
{
    public class ExpressionEvaluator
    {
        private readonly Lexer lexer = new();
        private readonly Parser parser = new();

        public object EvaluateValue(string expression, Spreadsheet spreadsheet)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ExpressionException("Порожній вираз");
            var tokens = lexer.Tokenize(expression);
            Node tree = parser.Parse(tokens);
            return Eval(tree, spreadsheet);
        }
        public bool Evaluate(string expression, Spreadsheet spreadsheet)
        {
            object result = EvaluateValue(expression, spreadsheet);
            return result is bool value ? value : throw new ExpressionException("Результат не є логічним");
        }
        public BigInteger EvaluateNumber(string expression, Spreadsheet spreadsheet)
        {
            object result = EvaluateValue(expression, spreadsheet);
            return result is BigInteger value ? value : throw new ExpressionException("Результат не є числом");
        }

        private object Eval(Node node, Spreadsheet spreadsheet)
        {
            switch (node)
            {
                case NumberNode n: return n.Value;
                case BooleanNode b: return b.Value;
                case CellNode c: return spreadsheet.GetReferencedResult(c.Address);
                case UnaryNode u:
                    BigInteger x = RequireNumber(Eval(u.Operand, spreadsheet));
                    return u.Op == TokenType.Minus ? -x : x;
                case BinaryNode b:
                    object left = Eval(b.Left, spreadsheet);
                    object right = Eval(b.Right, spreadsheet);
                    return ApplyOperator(left, right, b.Op);
                default: throw new ExpressionException("Невідомий вузол дерева виразу");
            }
        }

        private static BigInteger RequireNumber(object obj)
        {
            if (obj is BigInteger n) return n;
            throw new ExpressionException("Очікувалося число, а отримано true/false");
        }
        private static object ApplyOperator(object left, object right, TokenType op)
        {
            if (op == TokenType.Eq || op == TokenType.Neq)
            {
                if (left is bool lb && right is bool rb)
                    return op == TokenType.Eq ? lb == rb : lb != rb;
                if (left is BigInteger ln && right is BigInteger rn)
                    return op == TokenType.Eq ? ln == rn : ln != rn;
                throw new ExpressionException("Не можна порівнювати число з true/false");
            }
            if ((op == TokenType.Lt || op == TokenType.Gt || op == TokenType.Le || op == TokenType.Ge) &&
                (left is bool || right is bool))
                throw new ExpressionException("Оператори <, >, <=, >= дозволені лише для чисел");

            BigInteger a = RequireNumber(left);
            BigInteger b = RequireNumber(right);
            switch (op)
            {
                case TokenType.Plus: return a + b;
                case TokenType.Minus: return a - b;
                case TokenType.Mul: return a * b;
                case TokenType.Div:
                    if (b.IsZero) throw new ExpressionException("Ділення на нуль");
                    return BigInteger.Divide(a, b);
                case TokenType.IntDiv:
                    if (b.IsZero) throw new ExpressionException("Ділення на нуль (div)");
                    return FloorDiv(a, b);
                case TokenType.Mod:
                    if (b.IsZero) throw new ExpressionException("Ділення на нуль (mod)");
                    return FloorMod(a, b);
                case TokenType.Pow:
                    if (b.Sign < 0) throw new ExpressionException("Від'ємний степінь не підтримується");
                    if (b > 10000) throw new ExpressionException("Занадто великий степінь");
                    return BigInteger.Pow(a, (int)b);
                case TokenType.Lt: return a < b;
                case TokenType.Gt: return a > b;
                case TokenType.Le: return a <= b;
                case TokenType.Ge: return a >= b;
                default: throw new ExpressionException("Невідома операція");
            }
        }
        private static BigInteger FloorDiv(BigInteger a, BigInteger b)
        {
            BigInteger q = BigInteger.DivRem(a, b, out BigInteger r);
            if (!r.IsZero && (r.Sign < 0) != (b.Sign < 0)) q--;
            return q;
        }
        private static BigInteger FloorMod(BigInteger a, BigInteger b)
        {
            BigInteger r = BigInteger.Remainder(a, b);
            if (!r.IsZero && (r.Sign < 0) != (b.Sign < 0)) r += b;
            return r;
        }
    }
}
