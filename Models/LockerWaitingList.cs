using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class LockerWaitingList
{
    public int WaitingListId { get; set; }

    public int SchoolId { get; set; }

    public int GradeId { get; set; }

    public int? StudentId { get; set; }

    public DateTime AppliedDate { get; set; }

    public bool Status { get; set; }

    public bool? CurrentYear { get; set; }

    public bool? FollowingYear { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual School School { get; set; } = null!;

    public virtual Student? Student { get; set; }
}
