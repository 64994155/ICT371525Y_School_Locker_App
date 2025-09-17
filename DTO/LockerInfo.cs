namespace ICT371525Y_School_Locker_App.DTO
{
    public class LockerInfo
    {
        public int LockerId { get; set; }          // Primary key of the locker
        public string LockerNumber { get; set; }   // Locker number (e.g., "A12")
        public int GradeId { get; set; }           // Grade the locker is assigned to
        public bool IsAssigned { get; set; }       // Whether the locker is already assigned
    }
}