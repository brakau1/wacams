using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Infrastructure.Services;

/// <summary>
/// Template engine for rendering notification content with placeholders
/// </summary>
public class TemplateEngine
{
    private readonly ILogger<TemplateEngine> _logger;

    public TemplateEngine(ILogger<TemplateEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Render a template by replacing placeholders with values
    /// </summary>
    /// <param name="template">Template string with {{placeholder}} syntax</param>
    /// <param name="parameters">Dictionary of placeholder values</param>
    /// <returns>Rendered template</returns>
    public string Render(string template, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        if (parameters == null || parameters.Count == 0)
            return template;

        var result = template;

        foreach (var kvp in parameters)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            result = result.Replace(placeholder, kvp.Value ?? string.Empty);
        }

        // Log any unreplaced placeholders
        var unreplaced = Regex.Matches(result, @"\{\{([^}]+)\}\}")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        if (unreplaced.Any())
        {
            _logger.LogWarning("Template contains unreplaced placeholders: {Placeholders}",
                string.Join(", ", unreplaced));
        }

        return result;
    }
}
