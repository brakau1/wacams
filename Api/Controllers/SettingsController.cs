using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using Wacam.Api.Data;
using Wacam.Api.Middleware;
using Wacam.Api.Services;

namespace Wacam.Api.Controllers;

/// <summary>
/// System settings endpoints — get/update configuration (grace period, thresholds, shift configs).
/// Admin/SuperAdmin only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(WacamDbContext context, AuditService auditService, ILogger<SettingsController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/settings — Get all system settings as key-value pairs.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(List<SettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var settings = await _context.SystemSettings.ToListAsync();
        var result = settings.Select(s => new SettingDto
        {
            Id = s.Id,
            Key = s.SettingKey,
            Value = s.SettingValue,
            Description = s.Description,
            DataType = s.DataType,
            UpdatedAt = s.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/settings/{key} — Get a single setting by its key.
    /// Any authenticated user can read settings.
    /// </summary>
    [HttpGet("{key}")]
    [Authorize]
    [ProducesResponseType(typeof(SettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        if (setting is null)
            return NotFound(new { error = "NotFound", message = $"Setting with key '{key}' not found." });

        return Ok(new SettingDto
        {
            Id = setting.Id,
            Key = setting.SettingKey,
            Value = setting.SettingValue,
            Description = setting.Description,
            DataType = setting.DataType,
            UpdatedAt = setting.UpdatedAt
        });
    }

    /// <summary>
    /// PUT /api/settings/{key} — Update a single system setting value.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(SettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        if (setting is null)
            return NotFound(new { error = "NotFound", message = $"Setting with key '{key}' not found." });

        // Validate data type
        if (!ValidateDataType(request.Value, setting.DataType))
            return BadRequest(new { error = "InvalidValue", message = $"Value must be of type '{setting.DataType}'." });

        var oldValue = setting.SettingValue;
        setting.SettingValue = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: User.FindFirst("employeeId")?.Value,
            action: "UpdateSetting",
            entityType: "SystemSettings",
            entityId: key,
            httpEndpoint: $"PUT /api/settings/{key}",
            changesJson: $"{{\"OldValue\":\"{oldValue}\",\"NewValue\":\"{request.Value}\"}}");

        _logger.LogInformation("Setting updated: Key={Key}, Old={Old}, New={New}", key, oldValue, request.Value);

        return Ok(new SettingDto
        {
            Id = setting.Id,
            Key = setting.SettingKey,
            Value = setting.SettingValue,
            Description = setting.Description,
            DataType = setting.DataType,
            UpdatedAt = setting.UpdatedAt
        });
    }

    /// <summary>
    /// POST /api/settings/bulk-update — Bulk update multiple settings at once.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpPost("bulk-update")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateSettingsRequest request)
    {
        var updatedCount = 0;

        foreach (var item in request.Settings)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == item.Key);
            if (setting is null)
                continue;

            if (!ValidateDataType(item.Value, setting.DataType))
                continue; // Skip invalid updates

            setting.SettingValue = item.Value;
            setting.UpdatedAt = DateTime.UtcNow;
            updatedCount++;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: User.FindFirst("employeeId")?.Value,
            action: "BulkUpdateSettings",
            entityType: "SystemSettings",
            entityId: "bulk",
            httpEndpoint: "POST /api/settings/bulk-update",
            changesJson: $"{{\"Updated\":{updatedCount}}}");

        _logger.LogInformation("Bulk settings update: {Count} settings updated.", updatedCount);

        return Ok(new { updatedCount });
    }

    private static bool ValidateDataType(string value, string dataType)
    {
        return dataType.ToLowerInvariant() switch
        {
            "int" => int.TryParse(value, out _),
            "decimal" => decimal.TryParse(value, out _),
            "boolean" => bool.TryParse(value, out _),
            "string" or "json" => true, // Always valid
            _ => true
        };
    }
}

public record SettingDto
{
    public int Id { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public required string DataType { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record UpdateSettingRequest(string Value);

public record BulkUpdateSettingsRequest(List<SettingUpdateItem> Settings);

public record SettingUpdateItem(string Key, string Value);
