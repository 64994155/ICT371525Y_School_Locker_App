namespace ICT371525Y_School_Locker_App.DTO
{
    public class StudentDto
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentSchoolNumber { get; set; }
        public string? SchoolName { get; set; }
        public int? GradesId { get; set; }
        public int? SchoolId { get; set; }
        public string? GradeName { get; set; }
        public bool HasCurrentYearLocker { get; set; }
        public bool HasFollowingYearLocker { get; set; }
        public bool IsOnWaitingList { get; set; }
        public bool PaidCurrentYear { get; set; }
        public bool PaidFollowingYear { get; set; }
    }
}