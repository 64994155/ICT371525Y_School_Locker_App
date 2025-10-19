using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ICT371525Y_School_Locker_App.Models
{
    public class RegistrationViewModel
    {
        [Required(ErrorMessage = "ID Number is required.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "ID Number must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID Number must contain only numbers.")]
        public string IDNumber { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(10, ErrorMessage = "Title must be 10 characters or fewer.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, ErrorMessage = "Name must be 50 characters or fewer.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Name can only contain letters and spaces.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(50, ErrorMessage = "Surname must be 50 characters or fewer.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Surname can only contain letters and spaces.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Home address is required.")]
        [StringLength(100, ErrorMessage = "Home address must be 100 characters or fewer.")]
        public string HomeAddress { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must start with 0 and be 10 digits long.")]

        public string PhoneNumber { get; set; }

        public string StudentName { get; set; }

        public int? SelectedSchoolId { get; set; }

        public string Grade { get; set; }

        public List<SchoolViewModel> Schools { get; set; } = new();
    }

    public class SchoolViewModel
    {
        public int SchoolID { get; set; }
        public string SchoolName { get; set; }
    }
}
