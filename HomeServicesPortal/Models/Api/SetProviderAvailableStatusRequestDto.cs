using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class SetProviderAvailableStatusRequestDto : IValidatableObject
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProviderUid { get; set; }

    public bool IsOnline { get; set; }

    public string? AvailableFrom { get; set; }

    public string? AvailableTo { get; set; }

    /// <summary>Alternative single-field timing, e.g. "09:00 - 18:00".</summary>
    [StringLength(100)]
    public string? AvailableTiming { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsOnline)
        {
            yield break;
        }

        if (!TryResolveTiming(out var from, out var to, out var error))
        {
            yield return new ValidationResult(
                error ?? "AvailableFrom and AvailableTo are required when provider is online.",
                [nameof(AvailableFrom), nameof(AvailableTo), nameof(AvailableTiming)]);
            yield break;
        }

        if (TimeOnly.Parse(from!) >= TimeOnly.Parse(to!))
        {
            yield return new ValidationResult(
                "AvailableFrom must be earlier than AvailableTo.",
                [nameof(AvailableFrom), nameof(AvailableTo), nameof(AvailableTiming)]);
        }
    }

    public bool TryResolveTiming(out string? availableFrom, out string? availableTo, out string? error)
    {
        availableFrom = null;
        availableTo = null;
        error = null;

        var hasFrom = !string.IsNullOrWhiteSpace(AvailableFrom);
        var hasTo = !string.IsNullOrWhiteSpace(AvailableTo);

        if (hasFrom || hasTo)
        {
            if (hasFrom != hasTo)
            {
                error = "Both AvailableFrom and AvailableTo must be provided together.";
                return false;
            }

            if (!TryNormalizeTime(AvailableFrom!, out availableFrom) || !TryNormalizeTime(AvailableTo!, out availableTo))
            {
                error = "Invalid time format. Use HH:mm (example: 09:00).";
                return false;
            }

            return true;
        }

        if (!string.IsNullOrWhiteSpace(AvailableTiming))
        {
            var parts = AvailableTiming.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "AvailableTiming must be in 'HH:mm - HH:mm' format.";
                return false;
            }

            if (!TryNormalizeTime(parts[0], out availableFrom) || !TryNormalizeTime(parts[1], out availableTo))
            {
                error = "Invalid time format in AvailableTiming.";
                return false;
            }

            return true;
        }

        error = "AvailableFrom and AvailableTo are required when provider is online.";
        return false;
    }

    private static bool TryNormalizeTime(string input, out string? normalized)
    {
        normalized = null;
        if (!TimeOnly.TryParse(input.Trim(), out var time))
        {
            return false;
        }

        normalized = time.ToString("HH:mm");
        return true;
    }
}
