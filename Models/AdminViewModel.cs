using ICT371525Y_School_Locker_App.Models;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.DTO
{
    public class AdminViewModel
    {
        // School / admin info
        public int SchoolId { get; set; }
        public string? AdminName { get; set; }
        public long? AdminIdNumber { get; set; }

        public int AdminId { get; set; }


        // Grades for dropdown
        public List<Grade> Grades { get; set; } = new List<Grade>();

        // Students for display
        public List<Student> Students { get; set; } = new List<Student>();

        // Add student inputs
        public int? ParentId { get; set; }

        public string? ParentIdNumber { get; set; }
        public string? ParentName { get; set; }

        public string? StudentName { get; set; }
        public int? SelectedGradeId { get; set; }
        // List of allocated students
        public List<StudentDto> AllocatedStudents { get; set; } = new List<StudentDto>();

        public bool ShowParentSection { get; set; } = false;
    }
}