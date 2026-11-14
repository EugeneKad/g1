using System.Globalization;

namespace GraphPlotter.Services;

/// <summary>
/// Парсер и вычислитель математических выражений.
/// Поддерживает: +, -, *, /, ^, sin, cos, tan, sqrt, abs, log, ln, exp, pi, e, переменную x.
/// </summary>
public class ExpressionParser
{
    private readonly string _expression;
    private int _pos;

    public ExpressionParser(string expression)
    {
        _expression = expression.Replace(" ", "").ToLowerInvariant();
        _pos = 0;
    }

    public double Evaluate(double x)
    {
        double result = ParseExpression();
        if (_pos < _expression.Length)
            throw new FormatException($"Неожиданный символ '{_expression[_pos]}' в позиции {_pos}");
        return result;
    }

    public static bool IsValidExpression(string expression)
    {
        try
        {
            var parser = new ExpressionParser(expression);
            parser.ParseExpression();
            return parser._pos == parser._expression.Length;
        }
        catch
        {
            return false;
        }
    }

    private double ParseExpression()
    {
        double result = ParseTerm();

        while (_pos < _expression.Length)
        {
            char op = _expression[_pos];
            if (op == '+')
            {
                _pos++;
                result += ParseTerm();
            }
            else if (op == '-')
            {
                _pos++;
                result -= ParseTerm();
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private double ParseTerm()
    {
        double result = ParsePower();

        while (_pos < _expression.Length)
        {
            char op = _expression[_pos];
            if (op == '*' || op == '×')
            {
                _pos++;
                result *= ParsePower();
            }
            else if (op == '/' || op == '÷')
            {
                _pos++;
                double divisor = ParsePower();
                if (divisor == 0)
                    throw new DivideByZeroException("Деление на ноль");
                result /= divisor;
            }
            else if (op == '%')
            {
                _pos++;
                result %= ParsePower();
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private double ParsePower()
    {
        double result = ParseUnary();

        if (_pos < _expression.Length && _expression[_pos] == '^')
        {
            _pos++;
            double exponent = ParsePower(); // right-associative
            result = Math.Pow(result, exponent);
        }

        return result;
    }

    private double ParseUnary()
    {
        if (_pos < _expression.Length)
        {
            if (_expression[_pos] == '-')
            {
                _pos++;
                return -ParseUnary();
            }
            if (_expression[_pos] == '+')
            {
                _pos++;
                return ParseUnary();
            }
        }

        return ParsePrimary();
    }

    private double ParsePrimary()
    {
        // Parentheses
        if (_pos < _expression.Length && _expression[_pos] == '(')
        {
            _pos++; // skip '('
            double result = ParseExpression();
            if (_pos >= _expression.Length || _expression[_pos] != ')')
                throw new FormatException("Ожидается ')'");
            _pos++; // skip ')'
            return result;
        }

        // Number
        if (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.')
        {
            return ParseNumber();
        }

        // Variable or function
        return ParseIdentifier();
    }

    private double ParseNumber()
    {
        int start = _pos;
        bool hasDot = false;

        while (_pos < _expression.Length)
        {
            if (char.IsDigit(_expression[_pos]))
            {
                _pos++;
            }
            else if (_expression[_pos] == '.' && !hasDot)
            {
                hasDot = true;
                _pos++;
            }
            else
            {
                break;
            }
        }

        return double.Parse(_expression[start.._pos], CultureInfo.InvariantCulture);
    }

    private double ParseIdentifier()
    {
        int start = _pos;
        while (_pos < _expression.Length && (char.IsLetterOrDigit(_expression[_pos]) || _expression[_pos] == '_'))
        {
            _pos++;
        }

        string name = _expression[start.._pos];

        // Constants
        if (name == "pi") return Math.PI;
        if (name == "e") return Math.E;
        if (name == "x") return 0; // will be set during evaluation
        if (name == "y") return 0; // will be set during evaluation
        if (name == "true") return 1;
        if (name == "false") return 0;

        // Functions
        var functions = new Dictionary<string, Func<double, double>>
        {
            { "sin", Math.Sin },
            { "cos", Math.Cos },
            { "tan", Math.Tan },
            { "asin", Math.Asin },
            { "acos", Math.Acos },
            { "atan", Math.Atan },
            { "sqrt", Math.Sqrt },
            { "abs", Math.Abs },
            { "log", Math.Log10 },
            { "ln", Math.Log },
            { "exp", Math.Exp },
            { "ceil", Math.Ceiling },
            { "floor", Math.Floor },
            { "round", Math.Round },
            { "sign", v => Math.Sign(v) }
        };

        if (functions.TryGetValue(name, out var func))
        {
            // Expect '('
            if (_pos >= _expression.Length || _expression[_pos] != '(')
                throw new FormatException($"Ожидается '(' после '{name}'");
            _pos++; // skip '('
            double arg = ParseExpression();
            if (_pos >= _expression.Length || _expression[_pos] != ')')
                throw new FormatException($"Ожидается ')' после аргумента функции '{name}'");
            _pos++; // skip ')'
            return func(arg);
        }

        // Unknown identifier
        throw new FormatException($"Неизвестный идентификатор '{name}'");
    }

    public double EvaluateWithX(double x)
    {
        return EvaluateWithVariable(x, 0);
    }

    public double EvaluateWithXY(double x, double y)
    {
        return EvaluateWithVariable(x, y);
    }

    private double EvaluateWithVariable(double xValue, double yValue)
    {
        _pos = 0;
        return ParseExpressionWithXY(xValue, yValue);
    }

    private double ParseExpressionWithXY(double xValue, double yValue)
    {
        double result = ParseTermWithXY(xValue, yValue);

        while (_pos < _expression.Length)
        {
            char op = _expression[_pos];
            if (op == '+')
            {
                _pos++;
                result += ParseTermWithXY(xValue, yValue);
            }
            else if (op == '-')
            {
                _pos++;
                result -= ParseTermWithXY(xValue, yValue);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private double ParseTermWithXY(double xValue, double yValue)
    {
        double result = ParsePowerWithXY(xValue, yValue);

        while (_pos < _expression.Length)
        {
            char op = _expression[_pos];
            if (op == '*' || op == '×')
            {
                _pos++;
                result *= ParsePowerWithXY(xValue, yValue);
            }
            else if (op == '/' || op == '÷')
            {
                _pos++;
                double divisor = ParsePowerWithXY(xValue, yValue);
                if (divisor == 0)
                    throw new DivideByZeroException("Деление на ноль");
                result /= divisor;
            }
            else if (op == '%')
            {
                _pos++;
                result %= ParsePowerWithXY(xValue, yValue);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private double ParsePowerWithXY(double xValue, double yValue)
    {
        double result = ParseUnaryWithXY(xValue, yValue);

        if (_pos < _expression.Length && _expression[_pos] == '^')
        {
            _pos++;
            double exponent = ParsePowerWithXY(xValue, yValue);
            result = Math.Pow(result, exponent);
        }

        return result;
    }

    private double ParseUnaryWithXY(double xValue, double yValue)
    {
        if (_pos < _expression.Length)
        {
            if (_expression[_pos] == '-')
            {
                _pos++;
                return -ParseUnaryWithXY(xValue, yValue);
            }
            if (_expression[_pos] == '+')
            {
                _pos++;
                return ParseUnaryWithXY(xValue, yValue);
            }
        }

        return ParsePrimaryWithXY(xValue, yValue);
    }

    private double ParsePrimaryWithXY(double xValue, double yValue)
    {
        // Parentheses
        if (_pos < _expression.Length && _expression[_pos] == '(')
        {
            _pos++;
            double result = ParseExpressionWithXY(xValue, yValue);
            if (_pos >= _expression.Length || _expression[_pos] != ')')
                throw new FormatException("Ожидается ')'");
            _pos++;
            return result;
        }

        // Number
        if (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.')
        {
            return ParseNumber();
        }

        // Variable or function
        return ParseIdentifierWithXY(xValue, yValue);
    }

    private double ParseIdentifierWithXY(double xValue, double yValue)
    {
        int start = _pos;
        while (_pos < _expression.Length && (char.IsLetterOrDigit(_expression[_pos]) || _expression[_pos] == '_'))
        {
            _pos++;
        }

        string name = _expression[start.._pos];

        // Constants
        if (name == "pi") return Math.PI;
        if (name == "e") return Math.E;
        if (name == "x") return xValue;
        if (name == "y") return yValue;
        if (name == "true") return 1;
        if (name == "false") return 0;

        // Functions
        var functions = new Dictionary<string, Func<double, double>>
        {
            { "sin", Math.Sin },
            { "cos", Math.Cos },
            { "tan", Math.Tan },
            { "asin", Math.Asin },
            { "acos", Math.Acos },
            { "atan", Math.Atan },
            { "sqrt", Math.Sqrt },
            { "abs", Math.Abs },
            { "log", Math.Log10 },
            { "ln", Math.Log },
            { "exp", Math.Exp },
            { "ceil", Math.Ceiling },
            { "floor", Math.Floor },
            { "round", Math.Round },
            { "sign", v => Math.Sign(v) }
        };

        if (functions.TryGetValue(name, out var func))
        {
            if (_pos >= _expression.Length || _expression[_pos] != '(')
                throw new FormatException($"Ожидается '(' после '{name}'");
            _pos++;
            double arg = ParseExpressionWithXY(xValue, yValue);
            if (_pos >= _expression.Length || _expression[_pos] != ')')
                throw new FormatException($"Ожидается ')' после аргумента функции '{name}'");
            _pos++;
            return func(arg);
        }

        throw new FormatException($"Неизвестный идентификатор '{name}'");
    }
}
