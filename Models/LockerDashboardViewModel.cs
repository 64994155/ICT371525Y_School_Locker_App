namespace ICT371525Y_School_Locker_App.Models
{
    public class LockerDashboardViewModel
    {
        public int ParentId { get; set; }   
        public List<StudentInfo> Students { get; set; } = new();
        public List<LockerSchoolViewModel> Schools { get; set; } = new();
        public List<GradeCount> LockerByGrade { get; set; } = new();
        public List<LockerUsageDetail> LockerUsageGrade8and11 { get; set; } = new();
        public int LockersBooked { get; set; } = 0;
    }

    public class LockerSchoolViewModel
    {
        public int SchoolID { get; set; }
        public string SchoolName { get; set; }
        public List<Grades> Grades { get; set; }
    }

    public class StudentInfo
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public int SchoolId { get; set; }
        public string SchoolName { get; set; }
        public int GradeId { get; set; }
        public string GradeName { get; set; }
        public int? GradeNumber { get; set; }
        public string? SchoolStudentNumber { get; internal set; }
    }

    public class Grades
    {
        public int GradeId { get; set; }
        public int? Grade { get; set; }
    }

    public class GradeCount
    {
        public string Grade { get; set; }
        public int Count { get; set; }
    }


    public class LockerUsageDetail
    {
        public string ParentEmail { get; set; }
        public string StudentSchoolNumber { get; set; }
        public string GradeNumber { get; set; }
        public string LockerNumber { get; set; }
        public bool IsAssigned { get; set; }
        public bool IsAdminApproved { get; set; }
        public bool IsAdminApprovedFollowingBookingYear { get; set; }
        public bool? IsAdminApprovedCurrentBookingYear { get; set; }
        public DateTime? AssignedDate { get; set; }
        public int BookingYear { get; internal set; }
    }
}
