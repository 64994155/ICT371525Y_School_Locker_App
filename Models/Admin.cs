using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public int SchoolId { get; set; }

    public string AdminName { get; set; } = null!;

    public string? AdminEmail { get; set; }

    public long? AdminIdNumber { get; set; }

    public virtual School School { get; set; } = null!;
}
