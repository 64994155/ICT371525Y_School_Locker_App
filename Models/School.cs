using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class School
{
    public int SchoolId { get; set; }

    public string? SchoolName { get; set; }

    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

    public virtual ICollection<LockerWaitingList> LockerWaitingLists { get; set; } = new List<LockerWaitingList>();

    public virtual ICollection<Locker> Lockers { get; set; } = new List<Locker>();

    public virtual ICollection<SchoolGrade> SchoolGrades { get; set; } = new List<SchoolGrade>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
