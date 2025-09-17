using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class ParentStudentStaging
{
    public string ParentTitle { get; set; } = null!;

    public string ParentIdnumber { get; set; } = null!;

    public string ParentName { get; set; } = null!;

    public string ParentSurname { get; set; } = null!;

    public string ParentEmailAddress { get; set; } = null!;

    public string ParentHomeAddress { get; set; } = null!;

    public string ParentPhoneNumber { get; set; } = null!;

    public string StudentSchoolNumber { get; set; } = null!;

    public string StudentName { get; set; } = null!;

    public string StudentSurname { get; set; } = null!;

    public string StudentGrade { get; set; } = null!;
}
