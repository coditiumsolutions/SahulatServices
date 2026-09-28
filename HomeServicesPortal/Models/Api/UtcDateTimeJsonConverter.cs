using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeServicesPortal.Models.Api;

/// <summary>
/// Ensures DateTime values serialize with an explicit UTC "Z" suffix. EF Core returns SQL Server
/// datetime columns as Kind=Unspecified, so the default serializer omits the offset — clients
/// (the Flutter app) then have no reliable way to know these values are UTC. Registered globally
/// in Program.cs since every outbound DateTime in this API is a UTC instant.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture));

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
