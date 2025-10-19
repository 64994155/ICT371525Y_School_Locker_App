using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class Locker
{
    public int LockerId { get; set; }

    public int? SchoolId { get; set; }

    public int? GradeId { get; set; }

    public int? StudentIdCurrentBookingYear { get; set; }

    public string? LockerNumber { get; set; }

    public bool? IsAssigned { get; set; }

    public bool? IsAdminApprovedCurrentBookingYear { get; set; }

    public DateTime? AssignedDate { get; set; }

    public bool? CurrentBookingYear { get; set; }

    public bool? FollowingBookingYear { get; set; }

    public int? StudentIdFollowingBookingYear { get; set; }

    public bool IsAdminApprovedFollowingBookingYear { get; set; }

    public DateTime? CurrentAssignedDate { get; set; }

    public DateTime? FollowingAssignedDate { get; set; }

    public virtual Grade? Grade { get; set; }

    public virtual School? School { get; set; }

    public virtual Student? StudentIdCurrentBookingYearNavigation { get; set; }
}
