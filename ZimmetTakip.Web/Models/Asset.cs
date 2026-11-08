using System;
using System.Collections.Generic;

namespace ZimmetTakip.Web.Models;

public partial class Asset
{
    public int AssetId { get; set; }

    public int CategoryId { get; set; }

    public string AssetName { get; set; } = null!;

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string SerialNo { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateOnly? PurchaseDate { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual Category Category { get; set; } = null!;
}
