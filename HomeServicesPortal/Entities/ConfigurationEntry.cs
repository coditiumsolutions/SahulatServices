namespace HomeServicesPortal.Entities;

/// <summary>Key/value app configuration (ConfigValue may be comma-separated).</summary>
public class ConfigurationEntry
{
    public int Uid { get; set; }

    public string ConfigKey { get; set; } = string.Empty;

    public string ConfigValue { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }
}
