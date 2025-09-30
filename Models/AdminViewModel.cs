using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ICT371525Y_School_Locker_App.DTO
{
    public class AdminViewModel
    {
        public int SchoolId { get; set; }
        public string AdminName { get; set; }
        public string AdminIdNumber { get; set; }

        public string ParentIdNumber { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; }

        public string StudentName { get; set; }
        public int? SelectedGradeId { get; set; }

        public List<StudentDto> AllocatedStudents { get; set; }
        public List<StudentDto> GradeStudents { get; set; }

        // FIX: Change to SelectListItem for dropdown binding
        public IEnumerable<SelectListItem> Grades { get; set; }

        public bool ShowParentSection { get; set; }
        public bool ShowGradeSection { get; set; }
    }
}