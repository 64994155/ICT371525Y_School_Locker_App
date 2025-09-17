using System;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.Models;

public partial class Parent
{
    public int ParentId { get; set; }

    public long? ParentIdnumber { get; set; }

    public string? ParentTitle { get; set; }

    public string? ParentName { get; set; }

    public string? ParentSurname { get; set; }

    public string? ParentEmail { get; set; }

    public string? ParentHomeAddress { get; set; }

    public long? ParentPhoneNumber { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
