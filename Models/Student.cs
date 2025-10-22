using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int? SchoolId { get; set; }

    public string? StudentSchoolNumber { get; set; }

    public string? StudentName { get; set; }

    public int? ParentId { get; set; }

    public int? GradesId { get; set; }

    public bool? PaidCurrentYear { get; set; }

    public bool? PaidFollowingYear { get; set; }

    public virtual Grade? Grades { get; set; }

    public virtual ICollection<LockerWaitingList> LockerWaitingLists { get; set; } = new List<LockerWaitingList>();

    public virtual ICollection<Locker> Lockers { get; set; } = new List<Locker>();

    public virtual Parent? Parent { get; set; }

    public virtual School? School { get; set; }
}
