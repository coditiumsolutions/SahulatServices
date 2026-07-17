using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeServicesPortal.Models.Api;

/// <summary>
/// Treats null, empty, or whitespace JSON values as null for optional DateOnly fields.
/// </summary>
public sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
            {
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                if (DateOnly.TryParse(raw, out var date))
                {
                    return date;
                }

                throw new JsonException($"Unable to convert \"{raw}\" to DateOnly. Expected format {Format}.");
            }
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing DateOnly?.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(Format));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
