using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Helper;
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

        [HttpGet("Index")]
        public async Task<IActionResult> Index(int adminId)
        {
            var admin = await _context.Admins
                .Include(a => a.School)
                .FirstOrDefaultAsync(a => a.AdminId == adminId);

            if (admin == null)
                return BadRequest("Admin not found.");

            var model = new AdminViewModel
            {
                SchoolId = admin.SchoolId,
                AdminName = admin.AdminName,
                AdminIdNumber = admin.AdminIdNumber.ToString(),
                ShowParentSection = false
            };

            model.Grades = await (from sg in _context.SchoolGrades
                                  join g in _context.Grades on sg.GradeId equals g.GradesId
                                  where sg.SchoolId == model.SchoolId
                                  orderby g.GradeNumber
                                  select new SelectListItem
                                  {
                                      Value = g.GradesId.ToString(),
                                      Text = g.GradeName
                                  }).ToListAsync();

            ViewBag.Grades = model.Grades;

            return View(model);
        }

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
                AllocatedStudents = allocatedStudents,
                ShowParentSection = true
            };

            vm.Grades = await (from sg in _context.SchoolGrades
                               join g in _context.Grades on sg.GradeId equals g.GradesId
                               where sg.SchoolId == vm.SchoolId
                               orderby g.GradeNumber
                               select new SelectListItem
                               {
                                   Value = g.GradesId.ToString(),
                                   Text = g.GradeName
                               }).ToListAsync();

            return View("Index", vm);
        }

        [HttpPost("AddStudent")]
        public async Task<IActionResult> AddStudent(AdminViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.StudentName) || !model.SelectedGradeId.HasValue)
            {
                ModelState.AddModelError("", "All fields are required.");
                return await SearchParent(model);
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

            return await SearchParent(model);
        }

        [HttpPost("ApproveLocker")]
        public async Task<IActionResult> ApproveLocker([FromBody] LockerDto dto)
        {
            if (dto.LockerId <= 0)
                return BadRequest("Invalid locker ID.");

            var locker = await _context.Lockers.FirstOrDefaultAsync(l => l.LockerId == dto.LockerId);

            if (locker == null)
                return NotFound("Locker not found.");

            locker.IsAdminApproved = true;
            await _context.SaveChangesAsync();

            var student = await _context.Students
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == locker.StudentId);

            if (student?.Parent?.ParentEmail != null)
            {
                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Locker Approval Confirmation",
                    $"Dear Parent,\n\nLocker {locker.LockerNumber} for {student.StudentName} has been approved by the school admin."
                );
            }

            return Ok("Locker approved successfully.");
        }

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
                SchoolId = schoolId,
                ShowParentSection = true
            };

            return await SearchParent(model);
        }

        [HttpPost("SearchGrade")]
        public async Task<IActionResult> SearchGrade(AdminViewModel model)
        {
            if (!model.SelectedGradeId.HasValue)
            {
                ModelState.AddModelError("", "Grade selection is required.");
                return View("Index", model);
            }

            // --- Precompute sets for flags ---
            var currentYearLockers = await _context.Lockers
                .Where(l => (l.CurrentBookingYear ?? false) == true)
                .Select(l => l.StudentId)
                .Distinct()
                .ToListAsync();

            var followingYearLockers = await _context.Lockers
                .Where(l => (l.FollowingBookingYear ?? false) == true)
                .Select(l => l.StudentId)
                .Distinct()
                .ToListAsync();

            var waitingList = await _context.LockerWaitingLists
                .Where(wl => wl.Status == true)
                .Select(wl => wl.StudentId)
                .Distinct()
                .ToListAsync();

            // --- Query students in this school/grade ---
            var students = await _context.Students
                .Where(s => s.SchoolId == model.SchoolId && s.GradesId == model.SelectedGradeId.Value)
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    StudentSchoolNumber = s.StudentSchoolNumber,
                    GradesId = s.GradesId,
                    SchoolId = s.SchoolId,

                    // Use precomputed lists for fast lookups
                    HasCurrentYearLocker = currentYearLockers.Contains(s.StudentId),
                    HasFollowingYearLocker = followingYearLockers.Contains(s.StudentId),
                    IsOnWaitingList = waitingList.Contains(s.StudentId)
                })
                .ToListAsync();

            // --- Apply filter logic ---
            switch (model.GradeFilter)
            {
                case "Assigned":
                    students = students.Where(s => s.HasCurrentYearLocker || s.HasFollowingYearLocker).ToList();
                    break;
                case "Unassigned":
                    students = students.Where(s =>
                        !s.HasCurrentYearLocker || !s.HasFollowingYearLocker
                    ).ToList();
                    break;
                case "Waiting":
                    students = students.Where(s => s.IsOnWaitingList).ToList();
                    break;
                default: // "All"
                    students = students
                        .OrderByDescending(s => s.IsOnWaitingList)
                        .ThenByDescending(s => s.HasCurrentYearLocker || s.HasFollowingYearLocker)
                        .ToList();
                    break;
            }

            var vm = new AdminViewModel
            {
                AdminName = model.AdminName,
                SchoolId = model.SchoolId,
                SelectedGradeId = model.SelectedGradeId,
                GradeFilter = model.GradeFilter,
                GradeStudents = students,
                ShowGradeSection = true
            };

            vm.Grades = await (from sg in _context.SchoolGrades
                               join g in _context.Grades on sg.GradeId equals g.GradesId
                               where sg.SchoolId == vm.SchoolId
                               orderby g.GradeNumber
                               select new SelectListItem
                               {
                                   Value = g.GradesId.ToString(),
                                   Text = g.GradeName
                               }).ToListAsync();

            return View("Index", vm);
        }

        [HttpPost("SearchStudentNumber")]
        public async Task<IActionResult> SearchStudentNumber(AdminViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.StudentSchoolNumber))
            {
                ModelState.AddModelError("", "Student School Number is required.");
                model.ShowStudentSection = true;
                return View("Index", model);
            }

            var student = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.School)
                .Where(s => s.StudentSchoolNumber == model.StudentSchoolNumber && s.SchoolId == model.SchoolId)
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    StudentSchoolNumber = s.StudentSchoolNumber,
                    GradesId = s.GradesId,
                    SchoolId = s.SchoolId
                })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                ModelState.AddModelError("", "Student not found.");
                model.ShowStudentSection = true;
                return View("Index", model);
            }

            var vm = new AdminViewModel
            {
                AdminName = model.AdminName,
                SchoolId = model.SchoolId,
                StudentSchoolNumber = model.StudentSchoolNumber,
                FoundStudent = student,  
                ShowStudentSection = true,
                GradeFilter = "All"
            };

            vm.Grades = await (from sg in _context.SchoolGrades
                               join g in _context.Grades on sg.GradeId equals g.GradesId
                               where sg.SchoolId == vm.SchoolId
                               orderby g.GradeNumber
                               select new SelectListItem
                               {
                                   Value = g.GradesId.ToString(),
                                   Text = g.GradeName
                               }).ToListAsync();

            return View("Index", vm);
        }

    }
}

