using System;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UIElements;
using static LuaInterpreter.Lexer.UserDefinedFunction;

public class LuaInterpreter
{
    public class Environment
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public Environment Parent { get; }

        // Default constructor for the global environment.
        public Environment()
        {
            Parent = null;
        }

        // Constructor that takes a parent environment.
        public Environment(Environment parent)
        {
            Parent = parent;
        }

        public void Define(string name, object value)
        {
            values[name] = value;
        }

        public object Get(string name)
        {
            if (values.TryGetValue(name, out var value))
                return value;
            if (Parent != null)
                return Parent.Get(name);
            // Instead of throwing an error, return Lua nil (which we represent as null).
            return null;
        }

        public void Set(string name, object value)
        {
            if (values.ContainsKey(name))
            {
                values[name] = value;
            }
            else if (Parent != null)
            {
                Parent.Set(name, value);
            }
            else
            {
                throw new Exception($"Undefined variable '{name}'.");
            }
        }
    }

    public static class BuiltIns
    {
        public static void Register(Environment env)
        {
            // Example: Register a callback to print a message.
            env.Define("print", new Func<List<object>, object>((args) => {
                foreach (var arg in args)
                {
                    if (arg == null)
                        Debug.Log("nil");
                    else
                        Debug.Log(arg + " ");
                }
                return null;
            }));

            // Register a math function, like 'sqrt'
            env.Define("sqrt", new Func<List<object>, object>((args) => {
                if (args.Count != 1)
                {
                    throw new Exception("sqrt expects 1 argument.");
                }
                return Math.Sqrt(Convert.ToDouble(args[0]));
            }));
        }
    }

    public enum TokenType
    {
        Local,
        Identifier,
        Number,
        String,
        Plus,       // +
        Minus,      // -
        Star,       // *
        Slash,      // /
        Equal,      // used for assignment =
        EqualEqual, // ==
        NotEqual,   // ~=
        Less,       // <
        LessEqual,  // <=
        Greater,    // >
        GreaterEqual, // >=
        Comma,
        LeftParen,
        RightParen,
        Concat,
        Function,
        Return,
        End,
        If,
        Then,
        Else,
        // Boolean and nil literals:
        True,
        False,
        Nil,
        And,
        Or,
        Not,
        EOF
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public object Literal { get; }
        public Token(TokenType type, string lexeme, object literal)
        {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
        }
    }

    // A simple Lexer for one line of input.
    public class Lexer
    {
        private readonly string source;
        private int start = 0;
        private int current = 0;

        public Lexer(string source)
        {
            this.source = source;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (!IsAtEnd())
            {
                start = current;
                Token token = NextToken();
                if (token != null)
                {
                    tokens.Add(token);
                }
            }
            tokens.Add(new Token(TokenType.EOF, "", null));
            return tokens;
        }

        private Token NextToken()
        {
            char c = Advance();
            switch (c)
            {
                case '"':
                    return ParseString();
                case '(':
                    return NewToken(TokenType.LeftParen);
                case ')':
                    return NewToken(TokenType.RightParen);
                case '+':
                    return NewToken(TokenType.Plus);
                case '-':
                    return NewToken(TokenType.Minus);
                case '*':
                    return NewToken(TokenType.Star);
                case '/':
                    return NewToken(TokenType.Slash);
                case '=':
                    if (!IsAtEnd() && Peek() == '=')
                    {
                        Advance();
                        return NewToken(TokenType.EqualEqual);
                    }
                    return NewToken(TokenType.Equal);
                case '~':
                    if (!IsAtEnd() && Peek() == '=')
                    {
                        Advance();
                        return NewToken(TokenType.NotEqual);
                    }
                    throw new Exception($"Unexpected character: {c}");
                case '<':
                    if (!IsAtEnd() && Peek() == '=')
                    {
                        Advance();
                        return NewToken(TokenType.LessEqual);
                    }
                    return NewToken(TokenType.Less);
                case '>':
                    if (!IsAtEnd() && Peek() == '=')
                    {
                        Advance();
                        return NewToken(TokenType.GreaterEqual);
                    }
                    return NewToken(TokenType.Greater);
                case ',':
                    return NewToken(TokenType.Comma);
                case '.':
                    if (!IsAtEnd() && Peek() == '.')
                    {
                        Advance();
                        return NewToken(TokenType.Concat);
                    }
                    throw new Exception($"Unexpected character: {c}");
                case ' ':
                case '\t':
                    return null;
                default:
                    if (char.IsDigit(c))
                        return Number();
                    if (char.IsLetter(c) || c == '_')
                        return Identifier();
                    throw new Exception($"Unexpected character: {c}");
            }
        }

        private Token ParseString()
        {
            // We assume the opening double quote has been consumed.
            while (!IsAtEnd() && Peek() != '"')
            {
                if (Peek() == '\n')
                {
                    // Optionally update line number.
                }
                Advance();
            }

            if (IsAtEnd())
                throw new Exception("Unterminated string literal.");

            // Consume the closing double quote.
            Advance();

            // The string content is between the quotes.
            int length = current - start - 2; // skip both quotes.
            string value = source.Substring(start + 1, length);

            return NewToken(TokenType.String, value);
        }

        private Token Number()
        {
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                Advance();
            }
            string numStr = source.Substring(start, current - start);
            return NewToken(TokenType.Number, double.Parse(numStr));
        }

        private Token Identifier()
        {
            while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
            {
                Advance();
            }
            string text = source.Substring(start, current - start);
            TokenType type = text switch
            {
                "local" => TokenType.Local,
                "function" => TokenType.Function,
                "return" => TokenType.Return,
                "end" => TokenType.End,
                "if" => TokenType.If,
                "then" => TokenType.Then,
                "else" => TokenType.Else,
                "true" => TokenType.True,
                "false" => TokenType.False,
                "nil" => TokenType.Nil,
                "and" => TokenType.And,
                "or" => TokenType.Or,
                "not" => TokenType.Not,
                _ => TokenType.Identifier
            };
            return new Token(type, text, null);
        }

        private bool IsAtEnd() => current >= source.Length;

        private char Advance() => source[current++];

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return source[current];
        }

        private Token NewToken(TokenType type, object literal = null)
        {
            string text = source.Substring(start, current - start);
            return new Token(type, text, literal);
        }

        public abstract class Statement { }

        public class IfStatement : Statement
        {
            public Expression Condition { get; }
            public List<Statement> ThenBranch { get; }
            public List<Statement> ElseBranch { get; } // May be null if no else.

            public IfStatement(Expression condition, List<Statement> thenBranch, List<Statement> elseBranch)
            {
                Condition = condition;
                ThenBranch = thenBranch;
                ElseBranch = elseBranch;
            }
        }

        public class ExpressionStatement : Statement
        {
            public Expression Expr { get; }
            public ExpressionStatement(Expression expr) { Expr = expr; }
        }

        // Now support multiple assignment variables:
        public class AssignmentStatement : Statement
        {
            // List of one or more variable names.
            public List<string> Identifiers { get; }
            public Expression Expr { get; }
            public AssignmentStatement(List<string> identifiers, Expression expr)
            {
                Identifiers = identifiers;
                Expr = expr;
            }
        }

        // Change ReturnStatement to hold multiple return expressions.
        public class ReturnStatement : Statement
        {
            public List<Expression> Values { get; }
            public ReturnStatement(List<Expression> values)
            {
                Values = values;
            }
        }

        public class FunctionDeclaration : Statement
        {
            public string Name { get; }
            public List<string> Parameters { get; }
            public List<Statement> Body { get; }
            public FunctionDeclaration(string name, List<string> parameters, List<Statement> body)
            {
                Name = name;
                Parameters = parameters;
                Body = body;
            }
        }

        public abstract class Expression { }

        public class LiteralExpression : Expression
        {
            public object Value { get; }
            public LiteralExpression(object value) { Value = value; }
        }

        public class VariableExpression : Expression
        {
            public string Name { get; }
            public VariableExpression(string name) { Name = name; }
        }

        public class BinaryExpression : Expression
        {
            public Expression Left { get; }
            public Token Operator { get; }
            public Expression Right { get; }
            public BinaryExpression(Expression left, Token op, Expression right)
            {
                Left = left;
                Operator = op;
                Right = right;
            }
        }

        public class LogicalExpression : Expression
        {
            public Expression Left { get; }
            public Token Operator { get; }
            public Expression Right { get; }
            public LogicalExpression(Expression left, Token op, Expression right)
            {
                Left = left;
                Operator = op;
                Right = right;
            }
        }

        public class CallExpression : Expression
        {
            public Expression Callee { get; }
            public List<Expression> Arguments { get; }
            public CallExpression(Expression callee, List<Expression> args)
            {
                Callee = callee;
                Arguments = args;
            }
        }

        public class Parser
        {
            private readonly List<Token> tokens;
            private int current = 0;

            public Parser(List<Token> tokens) { this.tokens = tokens; }

            // Helper: peek at token after the current without advancing.
            private Token PeekNext()
            {
                if (current + 1 < tokens.Count)
                    return tokens[current + 1];
                return new Token(TokenType.EOF, "", null);
            }

            public Statement ParseDeclaration()
            {
                if (Match(TokenType.Local))
                {
                    if (Match(TokenType.Function))
                    {
                        return ParseFunctionDeclaration();
                    }
                    // Other local declarations … 
                }
                // Check for an if statement.
                if (Match(TokenType.If))
                {
                    return ParseIfStatement();
                }
                return ParseStatement();
            }

            private Statement ParseIfStatement()
            {
                // We assume the "if" token was already consumed.
                Expression condition = ParseExpression();   // parse the condition
                Consume(TokenType.Then, "Expected 'then' after condition.");

                // Parse then-branch statements:
                var thenBranch = new List<Statement>();
                while (!Check(TokenType.Else) && !Check(TokenType.End) && !IsAtEnd())
                {
                    thenBranch.Add(ParseDeclaration());
                }

                // Parse else-branch if present.
                List<Statement> elseBranch = null;
                if (Match(TokenType.Else))
                {
                    elseBranch = new List<Statement>();
                    while (!Check(TokenType.End) && !IsAtEnd())
                    {
                        elseBranch.Add(ParseDeclaration());
                    }
                }

                Consume(TokenType.End, "Expected 'end' after if statement.");
                return new IfStatement(condition, thenBranch, elseBranch);
            }

            private FunctionDeclaration ParseFunctionDeclaration()
            {
                // Expect the function’s name.
                Token nameToken = Consume(TokenType.Identifier, "Expected function name.");
                string functionName = nameToken.Lexeme;

                // Parse parameters: expect '(' then a list of identifiers and then ')'.
                Consume(TokenType.LeftParen, "Expected '(' after function name.");
                List<string> parameters = new List<string>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        Token param = Consume(TokenType.Identifier, "Expected parameter name.");
                        parameters.Add(param.Lexeme);
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters.");

                // Now parse the function body. For simplicity assume each line (or block) is a statement
                List<Statement> body = new List<Statement>();

                // Keep parsing until you hit the 'end' token.
                while (!Check(TokenType.End) && !IsAtEnd())
                {
                    // Choose the proper method based on your grammar.
                    body.Add(ParseDeclaration());
                }
                Consume(TokenType.End, "Expected 'end' after function body.");

                return new FunctionDeclaration(functionName, parameters, body);
            }

            public Statement ParseStatement()
            {
                // For return statements, now parse multiple return expressions.
                if (Match(TokenType.Return))
                {
                    var expressions = new List<Expression>();
                    expressions.Add(ParseExpression());
                    while (Match(TokenType.Comma))
                    {
                        expressions.Add(ParseExpression());
                    }
                    return new ReturnStatement(expressions);
                }
                // Check if we have an assignment – lookahead for ',' or '='.
                if (Check(TokenType.Identifier) &&
                   (PeekNext().Type == TokenType.Equal || PeekNext().Type == TokenType.Comma))
                {
                    return ParseAssignment();
                }
                return new ExpressionStatement(ParseExpression());
            }

            private Statement ParseAssignment()
            {
                // Parse one identifier.
                List<string> identifiers = new List<string>();
                Token idToken = Consume(TokenType.Identifier, "Expected variable name.");
                identifiers.Add(idToken.Lexeme);
                // If there is a comma, keep consuming identifiers.
                while (Match(TokenType.Comma))
                {
                    Token nextId = Consume(TokenType.Identifier, "Expected variable name.");
                    identifiers.Add(nextId.Lexeme);
                }
                // Now expect the equal token.
                Consume(TokenType.Equal, "Expected '=' after variable name(s).");
                Expression expr = ParseExpression();
                return new AssignmentStatement(identifiers, expr);
            }
            public Expression ParseExpression()
            {
                return ParseOr();
            }

            private Expression ParseOr()
            {
                Expression expr = ParseAnd();
                while (Match(TokenType.Or))
                {
                    Token op = Previous();
                    Expression right = ParseAnd();
                    expr = new LogicalExpression(expr, op, right);
                }
                return expr;
            }

            private Expression ParseAnd()
            {
                Expression expr = ParseComparison();
                while (Match(TokenType.And))
                {
                    Token op = Previous();
                    Expression right = ParseComparison();
                    expr = new LogicalExpression(expr, op, right);
                }
                return expr;
            }

            private Expression ParseComparison()
            {
                Expression expr = ParseConcat();  // or whichever is the next lower precedence
                while (Match(TokenType.EqualEqual,
                              TokenType.NotEqual,
                              TokenType.Less,
                              TokenType.LessEqual,
                              TokenType.Greater,
                              TokenType.GreaterEqual))
                {
                    Token op = Previous();
                    Expression right = ParseConcat();
                    expr = new BinaryExpression(expr, op, right);
                }
                return expr;
            }

            private Expression ParseConcat()
            {
                Expression expr = ParseTerm();  // parse the next lower precedence expressions

                while (Match(TokenType.Concat))
                {
                    Token op = Previous();
                    // For simplicity, we treat it as left-associative.
                    Expression right = ParseTerm();
                    expr = new BinaryExpression(expr, op, right);
                }
                return expr;
            }

            private Expression ParseTerm()
            {
                Expression expr = ParseFactor();
                while (Match(TokenType.Plus, TokenType.Minus))
                {
                    Token op = Previous();
                    Expression right = ParseFactor();
                    expr = new BinaryExpression(expr, op, right);
                }
                return expr;
            }

            private Expression ParseFactor()
            {
                Expression expr = ParsePrimary();
                while (Match(TokenType.Star, TokenType.Slash))
                {
                    Token op = Previous();
                    Expression right = ParsePrimary();
                    expr = new BinaryExpression(expr, op, right);
                }
                return expr;
            }

            private Expression ParsePrimary()
            {
                // Handle grouping expressions with parentheses.
                if (Match(TokenType.LeftParen))
                {
                    Expression expr = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after expression.");
                    return expr;
                }

                if (Match(TokenType.Number))
                {
                    return new LiteralExpression(Previous().Literal);
                }
                if (Match(TokenType.String))
                {
                    return new LiteralExpression(Previous().Literal);
                }
                if (Match(TokenType.True))
                {
                    return new LiteralExpression(true);
                }
                if (Match(TokenType.False))
                {
                    return new LiteralExpression(false);
                }
                if (Match(TokenType.Nil))
                {
                    return new LiteralExpression(null);
                }
                if (Match(TokenType.Identifier))
                {
                    Expression expr = new VariableExpression(Previous().Lexeme);
                    if (Match(TokenType.LeftParen))
                    {
                        List<Expression> args = new List<Expression>();
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                args.Add(ParseExpression());
                            } while (Match(TokenType.Comma));
                        }
                        Consume(TokenType.RightParen, "Expected ')' after function arguments.");
                        expr = new CallExpression(expr, args);
                    }
                    return expr;
                }
                throw new Exception("Unexpected token in primary expression");
            }

            private bool Match(params TokenType[] types)
            {
                foreach (var type in types)
                {
                    if (Check(type))
                    {
                        Advance();
                        return true;
                    }
                }
                return false;
            }

            private bool Check(TokenType type)
            {
                if (IsAtEnd()) return false;
                return Peek().Type == type;
            }

            private Token Advance()
            {
                if (!IsAtEnd()) current++;
                return Previous();
            }

            private bool IsAtEnd() => Peek().Type == TokenType.EOF;
            private Token Peek() => tokens[current];
            private Token Previous() => tokens[current - 1];

            private Token Consume(TokenType type, string message)
            {
                if (Check(type)) return Advance();
                throw new Exception(message);
            }
        }

        public class Evaluator
        {
            public Environment env;
            public Evaluator(Environment environment)
            {
                env = environment;
            }

            // Dispatcher which selects the proper execution based on Statement type.
            public void ExecuteStatement(Statement stmt, Environment localEnv = null)
            {
                Environment currentEnv = localEnv ?? env;
                switch (stmt)
                {
                    case ExpressionStatement exprStmt:
                        Evaluate(exprStmt.Expr, currentEnv);
                        break;
                    case AssignmentStatement assignStmt:
                        object value = Evaluate(assignStmt.Expr, currentEnv);
                        // If the right‐hand expression returned multiple values, then value should be a List<object>.
                        List<object> values;
                        if (value is List<object> list)
                        {
                            values = list;
                        }
                        else
                        {
                            values = new List<object> { value };
                        }
                        // Assign one value per variable. If there are fewer values than identifiers, assign null to the extra ones.
                        for (int i = 0; i < assignStmt.Identifiers.Count; i++)
                        {
                            object valToAssign = i < values.Count ? values[i] : null;
                            currentEnv.Define(assignStmt.Identifiers[i], valToAssign);
                        }
                        break;
                    case FunctionDeclaration funcDecl:
                        var func = new UserDefinedFunction(funcDecl.Parameters, funcDecl.Body, currentEnv);
                        currentEnv.Define(funcDecl.Name, func);
                        break;
                    case ReturnStatement retStmt:
                        var retValues = new List<object>();
                        foreach (Expression expr in retStmt.Values)
                        {
                            retValues.Add(Evaluate(expr, currentEnv));
                        }
                        throw new UserDefinedFunction.ReturnException(retValues);
                    case IfStatement ifStmt:
                        object condVal = Evaluate(ifStmt.Condition, currentEnv);
                        // In Lua all values except false and nil are true.
                        bool conditionIsTrue = true;
                        if (condVal is bool b)
                            conditionIsTrue = b;
                        else if (condVal == null)
                            conditionIsTrue = false;
                        // Execute the proper branch:
                        if (conditionIsTrue)
                        {
                            foreach (Statement s in ifStmt.ThenBranch)
                            {
                                ExecuteStatement(s, currentEnv);
                            }
                        }
                        else if (ifStmt.ElseBranch != null)
                        {
                            foreach (Statement s in ifStmt.ElseBranch)
                            {
                                ExecuteStatement(s, currentEnv);
                            }
                        }
                        break;
                    default:
                        throw new Exception("Unknown statement type");
                }
            }

            public object Evaluate(Expression expr, Environment currentEnv)
            {
                switch (expr)
                {
                    case LiteralExpression literal:
                        return literal.Value;
                    case VariableExpression variable:
                        return currentEnv.Get(variable.Name);
                    case BinaryExpression binary:
                        object left = Evaluate(binary.Left, currentEnv);
                        object right = Evaluate(binary.Right, currentEnv);
                        return EvaluateBinary(binary.Operator, left, right);
                    case CallExpression call:
                        object callee = Evaluate(call.Callee, currentEnv);
                        List<object> args = new List<object>();
                        foreach (Expression arg in call.Arguments)
                        {
                            args.Add(Evaluate(arg, currentEnv));
                        }
                        object result;
                        if (callee is UserDefinedFunction udf)
                        {
                            result = udf.Call(args);
                        }
                        else if (callee is Func<List<object>, object> builtin)
                        {
                            result = builtin(args);
                        }
                        else
                        {
                            throw new Exception("Called object is not a function");
                        }
                        // If result is a List<object> with a single value, unwrap it.
                        if (result is List<object> singleValueList && singleValueList.Count == 1)
                        {
                            return singleValueList[0];
                        }
                        return result;
                    case LogicalExpression logical:
                        {
                            object leftt = Evaluate(logical.Left, currentEnv);
                            // Lua considers false and nil as falsey.
                            bool leftIsFalse = leftt is bool b ? !b : leftt == null;
                            if (logical.Operator.Type == TokenType.And)
                            {
                                if (leftIsFalse)
                                    return leftt;
                                else
                                    return Evaluate(logical.Right, currentEnv);
                            }
                            else if (logical.Operator.Type == TokenType.Or)
                            {
                                if (!leftIsFalse)
                                    return leftt;
                                else
                                    return Evaluate(logical.Right, currentEnv);
                            }
                            break;
                        }
                    default:
                        throw new Exception("Unknown expression type");
                }
                return null;
            }

            private object EvaluateBinary(Token op, object left, object right)
            {
                switch (op.Type)
                {
                    case TokenType.Plus:
                        return Convert.ToDouble(left) + Convert.ToDouble(right);
                    case TokenType.Minus:
                        return Convert.ToDouble(left) - Convert.ToDouble(right);
                    case TokenType.Star:
                        return Convert.ToDouble(left) * Convert.ToDouble(right);
                    case TokenType.Slash:
                        return Convert.ToDouble(left) / Convert.ToDouble(right);
                    case TokenType.Concat:
                        return left.ToString() + right.ToString();
                    case TokenType.EqualEqual:
                        return Equals(left, right);
                    case TokenType.NotEqual:
                        return !Equals(left, right);
                    case TokenType.Less:
                        return Convert.ToDouble(left) < Convert.ToDouble(right);
                    case TokenType.LessEqual:
                        return Convert.ToDouble(left) <= Convert.ToDouble(right);
                    case TokenType.Greater:
                        return Convert.ToDouble(left) > Convert.ToDouble(right);
                    case TokenType.GreaterEqual:
                        return Convert.ToDouble(left) >= Convert.ToDouble(right);
                    default:
                        throw new Exception($"Unknown binary operator: {op.Lexeme}");
                }
            }
        }

        public class UserDefinedFunction
        {
            public List<string> Parameters { get; }
            public List<Statement> Body { get; }
            public Environment Closure { get; }

            public UserDefinedFunction(List<string> parameters, List<Statement> body, Environment closure)
            {
                Parameters = parameters;
                Body = body;
                Closure = closure;
            }

            public object Call(List<object> arguments)
            {
                Environment localEnv = new Environment(Closure);
                if (arguments.Count != Parameters.Count)
                {
                    throw new Exception("Argument count mismatch");
                }
                for (int i = 0; i < Parameters.Count; i++)
                {
                    localEnv.Define(Parameters[i], arguments[i]);
                }
                try
                {
                    foreach (Statement stmt in Body)
                    {
                        var evaluator = new Evaluator(localEnv);
                        evaluator.ExecuteStatement(stmt, localEnv);
                    }
                }
                catch (ReturnException returnEx)
                {
                    return returnEx.Values;
                }
                // If no return was executed, return a single nil value (represented here by null).
                return new List<object> { null };
            }

            public class ReturnException : Exception
            {
                public List<object> Values { get; }
                public ReturnException(List<object> values)
                {
                    Values = values;
                }
            }
        }
    }
}

