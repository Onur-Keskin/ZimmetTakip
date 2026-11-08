using System;
using System.Collections.Generic;

namespace ZimmetTakip.Web.Models;

public partial class Assignment
{
    public int AssignmentId { get; set; }

    public int AssetId { get; set; }

    public int UserId { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public string? AssignmentNote { get; set; }

    public string? ReturnNote { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
