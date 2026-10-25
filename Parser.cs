using System;
using System.Collections.Generic;
using System.Numerics;

namespace MyExcelMAUIApp
{
    public abstract class Node { }

    public class NumberNode : Node
    {
        public BigInteger Value { get; }
        public NumberNode(BigInteger value) { Value = value; }
    }

    public class BooleanNode : Node
    {
        public bool Value { get; }
        public BooleanNode(bool value) { Value = value; }
    }

    public class CellNode : Node
    {
        public string Address { get; }
        public CellNode(string address) { Address = address; }
    }

    public class UnaryNode : Node
    {
        public TokenType Op { get; }
        public Node Operand { get; }
        public UnaryNode(TokenType op, Node operand) { Op = op; Operand = operand; }
    }

    public class BinaryNode : Node
    {
        public TokenType Op { get; }
        public Node Left { get; }
        public Node Right { get; }
        public BinaryNode(TokenType op, Node left, Node right)
        { Op = op; Left = left; Right = right; }
    }

    public class Parser
    {
        private List<Token> tokens = new List<Token>();
        private int pos;

        private Token Current => tokens[pos];

        public Node Parse(List<Token> tokenList)
        {
            tokens = tokenList;
            pos = 0;

            Node result = ParseComparison();

            if (Current.Type != TokenType.End)
                throw new ExpressionException($"Неочікуваний символ: '{Current.Text}'");

            return result;
        }

        private Node ParseComparison()
        {
            Node left = ParseAdditive();

            if (IsComparison(Current.Type))
            {
                TokenType op = Current.Type;
                pos++;
                Node right = ParseAdditive();
                left = new BinaryNode(op, left, right);
            }

            return left;
        }

        private Node ParseAdditive()
        {
            Node left = ParseMultiplicative();

            while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
            {
                TokenType op = Current.Type;
                pos++;
                Node right = ParseMultiplicative();
                left = new BinaryNode(op, left, right);
            }

            return left;
        }

        private Node ParseMultiplicative()
        {
            Node left = ParseUnary();

            while (Current.Type == TokenType.Mul || Current.Type == TokenType.Div ||
                   Current.Type == TokenType.Mod || Current.Type == TokenType.IntDiv)
            {
                TokenType op = Current.Type;
                pos++;
                Node right = ParseUnary();
                left = new BinaryNode(op, left, right);
            }

            return left;
        }

        private Node ParseUnary()
        {
            if (Current.Type == TokenType.Minus || Current.Type == TokenType.Plus)
            {
                TokenType op = Current.Type;
                pos++;
                Node operand = ParseUnary();
                return new UnaryNode(op, operand);
            }

            return ParsePower();
        }

        private Node ParsePower()
        {
            Node left = ParsePrimary();

            if (Current.Type == TokenType.Pow)
            {
                pos++;
                Node right = ParseUnary();
                return new BinaryNode(TokenType.Pow, left, right);
            }

            return left;
        }

        private Node ParsePrimary()
        {
            Token t = Current;

            if (t.Type == TokenType.Number)
            {
                pos++;

                if (!BigInteger.TryParse(t.Text, out BigInteger number))
                    throw new ExpressionException($"Некоректне число: {t.Text}");

                return new NumberNode(number);
            }

            if (t.Type == TokenType.Boolean)
            {
                pos++;
                return new BooleanNode(t.Text.Equals("true", StringComparison.OrdinalIgnoreCase));
            }

            if (t.Type == TokenType.CellRef)
            {
                pos++;
                return new CellNode(t.Text);
            }

            if (t.Type == TokenType.LParen)
            {
                pos++;
                Node inner = ParseComparison();
                if (Current.Type != TokenType.RParen)
                    throw new ExpressionException("Очікується закриваюча дужка ')'");
                pos++;
                return inner;
            }

            if (t.Type == TokenType.End)
                throw new ExpressionException("Вираз закінчився передчасно");

            throw new ExpressionException($"Неочікуваний символ: '{t.Text}'");
        }

        private bool IsComparison(TokenType t) =>
            t == TokenType.Eq || t == TokenType.Lt || t == TokenType.Gt ||
            t == TokenType.Neq || t == TokenType.Le || t == TokenType.Ge;
    }
}