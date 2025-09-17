using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ICT371525Y_School_Locker_App.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly LockerAdminDbContext _context;

        public AdminController(LockerAdminDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Index
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int adminId)
        {
            var admin = await _context.Admins
                .Include(a => a.School)
                .FirstOrDefaultAsync(a => a.AdminId == adminId);

            if (admin == null)
                return BadRequest("Admin not found.");

            // Load grades for school
            var grades = await (from g in _context.Grades
                                join sg in _context.SchoolGrades on g.GradesId equals sg.GradeId
                                where sg.SchoolId == admin.SchoolId
                                orderby g.GradeNumber
                                select g).ToListAsync();

            var model = new AdminViewModel
            {
                SchoolId = admin.SchoolId,
                AdminName = admin.AdminName,
                AdminIdNumber = admin.AdminIdNumber,
                Grades = grades
            };

            return View(model);
        }

        // POST: Admin/SearchParent
        [HttpPost("SearchParent")]
        public async Task<IActionResult> SearchParent(AdminViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ParentIdNumber))
            {
                ModelState.AddModelError("", "Parent ID Number is required.");
                return View("Index", model);
            }

            if (!long.TryParse(model.ParentIdNumber.Trim(), out var parentIdNumber))
            {
                ModelState.AddModelError("", "Invalid Parent ID Number.");
                return View("Index", model);
            }

            var parent = await _context.Parents
                .FirstOrDefaultAsync(p => p.ParentIdnumber == parentIdNumber);

            if (parent == null)
            {
                ModelState.AddModelError("", "Parent not found.");
                return View("Index", model);
            }

            // Load allocated students
            var allocatedStudents = await _context.Students
                .Where(s => s.ParentId == parent.ParentId)
                .Select(s => new StudentDto
                {
                    StudentSchoolNumber = s.StudentSchoolNumber,
                    StudentName = s.StudentName,
                    StudentId = s.StudentId,
                    GradesId = s.GradesId,
                    SchoolId = s.SchoolId
                })
                .ToListAsync();

            var vm = new AdminViewModel
            {
                AdminName = model.AdminName,
                SchoolId = model.SchoolId,
                ParentId = parent.ParentId,
                ParentIdNumber = parent.ParentIdnumber.ToString(),
                ParentName = parent.ParentName,
                AllocatedStudents = allocatedStudents
            };

            // Load grades for dropdown
            ViewBag.Grades = await (from sg in _context.SchoolGrades
                                    join g in _context.Grades on sg.GradeId equals g.GradesId
                                    where sg.SchoolId == model.SchoolId
                                    select new SelectListItem
                                    {
                                        Value = g.GradesId.ToString(),
                                        Text = g.GradeName
                                    }).ToListAsync();

            return View("Index", vm);
        }

        // POST: Admin/AddStudent
        [HttpPost("AddStudent")]
        public async Task<IActionResult> AddStudent(AdminViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.StudentName) || !model.SelectedGradeId.HasValue)
            {
                ModelState.AddModelError("", "All fields are required.");
                return await SearchParent(model); // reload parent + students
            }

            var student = new Student
            {
                StudentName = model.StudentName,
                ParentId = model.ParentId.Value,
                GradesId = model.SelectedGradeId.Value,
                SchoolId = model.SchoolId,
                StudentSchoolNumber = Guid.NewGuid().ToString().Substring(0, 6)
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return await SearchParent(model); // refresh view
        }

        // POST: Admin/RemoveStudent
        [HttpPost("RemoveStudent")]
        public async Task<IActionResult> RemoveStudent(int studentId, string parentIdNumber, int schoolId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }

            var model = new AdminViewModel
            {
                ParentIdNumber = parentIdNumber,
                SchoolId = schoolId
            };

            return await SearchParent(model); // refresh parent + student list
        }
    }
}
