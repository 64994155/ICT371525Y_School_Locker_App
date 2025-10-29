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
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    StudentSchoolNumber = s.StudentSchoolNumber,
                    GradesId = s.GradesId,
                    SchoolId = s.SchoolId,
                    PaidCurrentYear = s.PaidCurrentYear ?? false,
                    PaidFollowingYear = s.PaidFollowingYear ?? false
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

        [HttpPost("UpdatePaymentStatus")]
        public async Task<IActionResult> UpdatePaymentStatus(int studentId, string yearType, bool paid)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound("Student not found.");

            if (yearType.Equals("current", StringComparison.OrdinalIgnoreCase))
                student.PaidCurrentYear = paid;
            else if (yearType.Equals("following", StringComparison.OrdinalIgnoreCase))
                student.PaidFollowingYear = paid;
            else
                return BadRequest("Invalid year type.");

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment status updated successfully." });
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

            var student = await _context.Students
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentID);

            if (student == null)
                return NotFound("Student not found.");

            bool isApproved = false;
            string message = string.Empty;

            if (dto.YearType.Equals("current", StringComparison.OrdinalIgnoreCase))
            {
                if (student.PaidCurrentYear == false || student.PaidCurrentYear == null)
                {
                    message = "The parent payment setting for the current year has not been set. Please confirm payment before approving.";
                    return Ok(new { showPopup = true, message });
                }
                else
                {
                    locker.IsAdminApprovedCurrentBookingYear = true;
                    isApproved = true;
                }
            }
            else if (dto.YearType.Equals("following", StringComparison.OrdinalIgnoreCase))
            {
                if (student.PaidFollowingYear == false || student.PaidFollowingYear == null)
                {
                    message = "The parent payment setting for the following year has not been set. Please confirm payment before approving.";
                    return Ok(new { showPopup = true, message });
                }
                else
                {
                    locker.IsAdminApprovedFollowingBookingYear = true;
                    isApproved = true;
                }
            }

            await _context.SaveChangesAsync();

            if (isApproved && student?.Parent?.ParentEmail != null)
            {
                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Locker Assignment Confirmation",
                    $@"
            <p>Dear Parent,</p>

            <p>Thank you for sending confirmation of payment.</p>

            <p>We are pleased to confirm that <strong>Locker {locker.LockerNumber}</strong> 
            has been successfully assigned to <strong>{student.StudentName}</strong> 
            for the <strong>{dto.YearType}</strong> booking year.</p>
       
            <p>Kind regards,<br/>
            <strong>School Locker Administration</strong></p>"
                );
            }

            return Ok(new { showPopup = false, message = "Locker approved successfully." });
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

            var currentYearLockers = await _context.Lockers
                .Where(l => (l.CurrentBookingYear ?? false) == true)
                .Select(l => l.StudentIdCurrentBookingYear)
                .Distinct()
                .ToListAsync();

            var followingYearLockers = await _context.Lockers
                .Where(l => (l.FollowingBookingYear ?? false) == true)
                .Select(l => l.StudentIdFollowingBookingYear)
                .Distinct()
                .ToListAsync();

            var waitingList = await _context.LockerWaitingLists
                .Where(wl => wl.Status == true)
                .Select(wl => wl.StudentId)
                .Distinct()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => s.SchoolId == model.SchoolId && s.GradesId == model.SelectedGradeId.Value)
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    StudentSchoolNumber = s.StudentSchoolNumber,
                    GradesId = s.GradesId,
                    SchoolId = s.SchoolId,
                    HasCurrentYearLocker = currentYearLockers.Contains(s.StudentId),
                    HasFollowingYearLocker = followingYearLockers.Contains(s.StudentId),
                    IsOnWaitingList = waitingList.Contains(s.StudentId)
                })
                .ToListAsync();

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

        [HttpGet("All/{studentId}")]
        public async Task<IActionResult> GetAll(int studentId)
        {
            var student = await _context.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => new
                {
                    s.StudentId,
                    s.StudentName,
                    s.StudentSchoolNumber,
                    s.GradesId,
                    s.SchoolId
                })
                .FirstOrDefaultAsync();

            if (student == null)
                return NotFound("Student not found.");

            bool isFinalGrade = student.GradesId == 24;

            var lockers = await _context.Lockers
                .Where(l =>
                    (l.StudentIdCurrentBookingYear == studentId && l.CurrentBookingYear == true) ||
                    (l.StudentIdFollowingBookingYear == studentId && l.FollowingBookingYear == true)
                )
                .ToListAsync();

            var assigned = new List<object>();

            foreach (var l in lockers)
            {
                if (l.CurrentBookingYear == true && l.StudentIdCurrentBookingYear == studentId)
                {
                    assigned.Add(new
                    {
                        l.LockerId,
                        l.LockerNumber,
                        YearType = "current",
                        IsAdminApproved = l.IsAdminApprovedCurrentBookingYear
                    });
                }

                if (!isFinalGrade && l.FollowingBookingYear == true && l.StudentIdFollowingBookingYear == studentId)
                {
                    assigned.Add(new
                    {
                        l.LockerId,
                        l.LockerNumber,
                        YearType = "following",
                        IsAdminApproved = l.IsAdminApprovedFollowingBookingYear
                    });
                }
            }

            var waitingQuery = _context.LockerWaitingLists
                .Include(w => w.Grade)
                .Include(w => w.School)
                .Where(w => w.StudentId == studentId && w.Status == true);

            if (isFinalGrade)
            {
                waitingQuery = waitingQuery.Where(w => w.CurrentYear == true);
            }

            var waiting = await waitingQuery
                .OrderBy(w => w.AppliedDate)
                .Select(w => new
                {
                    w.SchoolId,
                    SchoolName = w.School.SchoolName,
                    w.GradeId,
                    GradeName = w.Grade.GradeName,
                    w.AppliedDate,
                    YearType = w.CurrentYear == true ? "current" :
                               (w.FollowingYear == true ? "following" : "unknown")
                })
                .ToListAsync();

            bool hasCurrentAssigned = assigned.Any(a =>
                a.GetType().GetProperty("YearType")?.GetValue(a)?.ToString() == "current");

            bool hasFollowingAssigned = !isFinalGrade && assigned.Any(a =>
                a.GetType().GetProperty("YearType")?.GetValue(a)?.ToString() == "following");

            bool onCurrentWaiting = waiting.Any(w => w.YearType == "current");
            bool onFollowingWaiting = !isFinalGrade && waiting.Any(w => w.YearType == "following");

            var allCurrentAvailable = await _context.Lockers
                .Where(l =>
                    l.SchoolId == student.SchoolId &&
                    l.GradeId == student.GradesId &&
                    (l.CurrentBookingYear == null || l.CurrentBookingYear == false))
                .Select(l => new { l.LockerId, l.LockerNumber })
                .ToListAsync<object>();

            var allFollowingAvailable = new List<object>();
            if (!isFinalGrade)
            {
                var nextGradeId = await _context.Grades
                    .Where(g => g.GradeNumber >
                        _context.Grades
                            .Where(gr => gr.GradesId == student.GradesId)
                            .Select(gr => gr.GradeNumber)
                            .FirstOrDefault())
                    .OrderBy(g => g.GradeNumber)
                    .Select(g => g.GradesId)
                    .FirstOrDefaultAsync();

                if (nextGradeId != 0)
                {
                    allFollowingAvailable = await _context.Lockers
                        .Where(l =>
                            l.SchoolId == student.SchoolId &&
                            l.GradeId == nextGradeId &&
                            (l.FollowingBookingYear == null || l.FollowingBookingYear == false))
                        .Select(l => new { l.LockerId, l.LockerNumber })
                        .ToListAsync<object>();
                }
            }

            var currentAvailable = (!hasCurrentAssigned && !onCurrentWaiting)
                ? allCurrentAvailable
                : new List<object>();

            var followingAvailable = (!isFinalGrade && !hasFollowingAssigned && !onFollowingWaiting)
                ? allFollowingAvailable
                : new List<object>();

            var gradeId = student.GradesId;

            return Ok(new
            {
                student.StudentId,
                student.StudentName,
                student.StudentSchoolNumber,
                gradeId,
                student.SchoolId,
                assigned,
                waiting,
                unassigned = new
                {
                    current = currentAvailable,
                    following = followingAvailable,
                    allCurrent = allCurrentAvailable,
                    allFollowing = allFollowingAvailable
                }
            });
        }

        [HttpGet("AllByGrade/{schoolId}/{gradeId}")]
        public async Task<IActionResult> GetAllByGrade(int schoolId, int gradeId)
        {
            var students = await _context.Students
                .Include(s => s.Parent)
                .Where(s => s.SchoolId == schoolId && s.GradesId == gradeId)
                .Select(s => new
                {
                    s.StudentId,
                    s.StudentName,
                    s.StudentSchoolNumber,
                    s.SchoolId,
                    s.GradesId,
                    s.Parent.ParentIdnumber
                })
                .ToListAsync();

            var result = new List<object>();

            foreach (var student in students)
            {
                int studentId = student.StudentId;
                bool isFinalGrade = student.GradesId == 24;

                var lockers = await _context.Lockers
                    .Where(l =>
                        (l.StudentIdCurrentBookingYear == studentId && l.CurrentBookingYear == true) ||
                        (l.StudentIdFollowingBookingYear == studentId && l.FollowingBookingYear == true)
                    )
                    .ToListAsync();

                var assigned = new List<object>();

                foreach (var l in lockers)
                {
                    if (l.CurrentBookingYear == true && l.StudentIdCurrentBookingYear == studentId)
                    {
                        assigned.Add(new
                        {
                            l.LockerId,
                            l.LockerNumber,
                            YearType = "current",
                            IsAdminApproved = l.IsAdminApprovedCurrentBookingYear
                        });
                    }

                    if (!isFinalGrade && l.FollowingBookingYear == true && l.StudentIdFollowingBookingYear == studentId)
                    {
                        assigned.Add(new
                        {
                            l.LockerId,
                            l.LockerNumber,
                            YearType = "following",
                            IsAdminApproved = l.IsAdminApprovedFollowingBookingYear
                        });
                    }
                }

                var waitingQuery = _context.LockerWaitingLists
                    .Include(w => w.Grade)
                    .Include(w => w.School)
                    .Where(w => w.StudentId == studentId && w.Status == true);

                if (isFinalGrade)
                {
                    waitingQuery = waitingQuery.Where(w => w.CurrentYear == true);
                }

                var waiting = await waitingQuery
                    .OrderBy(w => w.AppliedDate)
                    .Select(w => new
                    {
                        w.SchoolId,
                        SchoolName = w.School.SchoolName,
                        w.GradeId,
                        GradeName = w.Grade.GradeName,
                        w.AppliedDate,
                        YearType = w.CurrentYear == true ? "current" :
                                   (w.FollowingYear == true ? "following" : "unknown")
                    })
                    .ToListAsync();

                bool hasCurrentAssigned = assigned.Any(a =>
                    a.GetType().GetProperty("YearType")?.GetValue(a)?.ToString() == "current");

                bool hasFollowingAssigned = !isFinalGrade && assigned.Any(a =>
                    a.GetType().GetProperty("YearType")?.GetValue(a)?.ToString() == "following");

                bool onCurrentWaiting = waiting.Any(w => w.YearType == "current");
                bool onFollowingWaiting = !isFinalGrade && waiting.Any(w => w.YearType == "following");

                var currentAvailable = new List<object>();
                var followingAvailable = new List<object>();
                var allCurrentAvailable = new List<object>();
                var allFollowingAvailable = new List<object>();

                allCurrentAvailable = await _context.Lockers
                    .Where(l =>
                        l.SchoolId == student.SchoolId &&
                        l.GradeId == student.GradesId &&
                        (l.CurrentBookingYear == null || l.CurrentBookingYear == false))
                    .Select(l => new { l.LockerId, l.LockerNumber })
                    .ToListAsync<object>();

                if (!isFinalGrade)
                {
                    var nextGradeId = await _context.Grades
                        .Where(g => g.GradeNumber >
                            _context.Grades
                                .Where(gr => gr.GradesId == student.GradesId)
                                .Select(gr => gr.GradeNumber)
                                .FirstOrDefault())
                        .OrderBy(g => g.GradeNumber)
                        .Select(g => g.GradesId)
                        .FirstOrDefaultAsync();

                    if (nextGradeId != 0)
                    {
                        allFollowingAvailable = await _context.Lockers
                            .Where(l =>
                                l.SchoolId == student.SchoolId &&
                                l.GradeId == nextGradeId &&
                                (l.FollowingBookingYear == null || l.FollowingBookingYear == false))
                            .Select(l => new { l.LockerId, l.LockerNumber })
                            .ToListAsync<object>();
                    }
                }

                if (!hasCurrentAssigned && !onCurrentWaiting)
                    currentAvailable = allCurrentAvailable;

                if (!isFinalGrade && !hasFollowingAssigned && !onFollowingWaiting)
                    followingAvailable = allFollowingAvailable;

                DateTime? earliestAppliedDate = waiting.Any()
                    ? waiting.Min(w => w.AppliedDate)
                    : (DateTime?)null;

                result.Add(new
                {
                    student.StudentId,
                    student.StudentName,
                    student.StudentSchoolNumber,
                    student.ParentIdnumber,
                    gradeId,
                    schoolId,
                    assigned,
                    waiting,
                    unassigned = new
                    {
                        current = currentAvailable,
                        following = followingAvailable,
                        allCurrent = allCurrentAvailable,
                        allFollowing = allFollowingAvailable
                    },
                    EarliestAppliedDate = earliestAppliedDate
                });
            }

            var orderedResult = result
                .OrderBy(r => ((DateTime?)r.GetType().GetProperty("EarliestAppliedDate").GetValue(r)) ?? DateTime.MaxValue)
                .ToDictionary(
                    r => (int)r.GetType().GetProperty("StudentId").GetValue(r),
                    r => new
                    {
                        StudentId = r.GetType().GetProperty("StudentId").GetValue(r),
                        StudentName = r.GetType().GetProperty("StudentName").GetValue(r),
                        StudentSchoolNumber = r.GetType().GetProperty("StudentSchoolNumber").GetValue(r),
                        ParentIdNumber = r.GetType().GetProperty("ParentIdnumber").GetValue(r),
                        gradeId = r.GetType().GetProperty("gradeId").GetValue(r),
                        schoolId = r.GetType().GetProperty("schoolId").GetValue(r),
                        assigned = r.GetType().GetProperty("assigned").GetValue(r),
                        waiting = r.GetType().GetProperty("waiting").GetValue(r),
                        unassigned = r.GetType().GetProperty("unassigned").GetValue(r)
                    });

            return Ok(orderedResult);
        }
    }
}


