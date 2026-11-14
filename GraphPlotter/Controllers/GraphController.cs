using GraphPlotter.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphPlotter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GraphController : ControllerBase
{
    [HttpGet("points")]
    public IActionResult GetPoints(
        [FromQuery] string formula,
        [FromQuery] double xMin = -10,
        [FromQuery] double xMax = 10,
        [FromQuery] int points = 500)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return BadRequest("Формула не указана");

        formula = formula.Trim();

        if (!ExpressionParser.IsValidExpression(formula))
            return BadRequest($"Некорректная формула: '{formula}'");

        try
        {
            var parser = new ExpressionParser(formula);
            var xPoints = new List<double>(points);
            var yPoints = new List<double>(points);

            double step = (xMax - xMin) / (points - 1);

            for (int i = 0; i < points; i++)
            {
                double x = xMin + i * step;
                try
                {
                    double y = parser.EvaluateWithX(x);
                    xPoints.Add(x);
                    yPoints.Add(y);
                }
                catch (DivideByZeroException)
                {
                    // Skip points where division by zero occurs
                    xPoints.Add(double.NaN);
                    yPoints.Add(double.NaN);
                }
                catch (OverflowException)
                {
                    xPoints.Add(double.NaN);
                    yPoints.Add(double.NaN);
                }
            }

            return Ok(new
            {
                formula,
                xMin,
                xMax,
                points = xPoints.Count,
                xValues = xPoints,
                yValues = yPoints
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Ошибка вычисления: {ex.Message}");
        }
    }

    [HttpPost("validate")]
    public IActionResult ValidateFormula([FromBody] ValidateFormulaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Formula))
            return BadRequest("Формула не указана");

        bool isValid = ExpressionParser.IsValidExpression(request.Formula);
        return Ok(new { isValid, formula = request.Formula });
    }
}

public record ValidateFormulaRequest(string Formula);
