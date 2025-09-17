using Microsoft.AspNetCore.Mvc.Rendering;

namespace ICT371525Y_School_Locker_App.Models
{
    public class RegistrationViewModel
    {
        // Registration Fields
        public string IDNumber { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string HomeAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string StudentName { get; set; }
        public int? SelectedSchoolId { get; set; }
        public string Grade { get; set; }

        // School dropdown
        public List<SchoolViewModel> Schools { get; set; } = new();
    }

    public class SchoolViewModel
    {
        public int SchoolID { get; set; }
        public string SchoolName { get; set; }
    }
}
