using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class ProviderDocument
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentNo { get; set; }

    public string? FilePath { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public virtual ServiceProvider ProviderU { get; set; } = null!;
}
