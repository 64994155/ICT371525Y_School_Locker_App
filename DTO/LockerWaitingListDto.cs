using ICT371525Y_School_Locker_App.Models;

namespace ICT371525Y_School_Locker_App.DTO
{
    public class LockerWaitingListDto
    {

        public int WaitingListId { get; set; }

        public int SchoolId { get; set; }

        public string? SchoolName { get; set; }

        public int GradeId { get; set; }

        public int StudentId { get; set; }

        public DateTime AppliedDate { get; set; }

        public bool Status { get; set; }
        public string? GradeName { get; internal set; }

        public bool? CurrentYear { get; set; }

        public bool? FollowingYear { get; set; }

        public int Year { get; set; }

        public string YearType { get; set; }


    }
}
