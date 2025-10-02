namespace ICT371525Y_School_Locker_App.DTO
{
    public class AssignedLockerDto
    {
        public int LockerId { get; set; }
        public string LockerNumber { get; set; }
        public int GradeId { get; set; }
        public string GradeName { get; set; }
        public int? GradeNumber { get; set; }
        public int Year { get; set; }
        public string YearType { get; set; }
        public bool? IsAdminApproved { get; set; }
    }
}
