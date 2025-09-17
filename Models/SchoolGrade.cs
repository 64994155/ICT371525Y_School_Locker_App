using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class SchoolGrade
{
    public int SchoolGrade1 { get; set; }

    public int? SchoolId { get; set; }

    public int? GradeId { get; set; }

    public int? LockerCount { get; set; }

    public virtual Grade? Grade { get; set; }

    public virtual School? School { get; set; }
}
