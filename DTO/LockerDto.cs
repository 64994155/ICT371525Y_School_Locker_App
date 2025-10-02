namespace ICT371525Y_School_Locker_App.DTO
{
    public class LockerDto
    {
        public int LockerId { get; set; }
        public string? LockerNumber { get; set; }
        public string? Location { get; set; }
        public string? SchoolName { get; set; }
        public int StudentID { get; set; }
        public string? YearType { get; set; }
        public int? SchoolId { get; internal set; }
        public int? GradeId { get; internal set; }
        public bool? CurrentBookingYear { get; set; }
        public bool? FollowingBookingYear { get; set; }
    }
}
