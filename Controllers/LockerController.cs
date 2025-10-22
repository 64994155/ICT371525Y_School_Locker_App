using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Helper;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Controllers
{
    [Route("Locker")]
    public class LockerController : Controller
    {
        private readonly LockerAdminDbContext _context;
        
        public LockerController(LockerAdminDbContext context)
        {
            _context = context;
        }

        [HttpGet("index")]
        public async Task<IActionResult> Index(int? parentId)
        {
            var model = new LockerDashboardViewModel();

            if (parentId != null)
            {
                model.Students = await _context.Students
                    .Include(s => s.Grades)
                    .Include(s => s.School)
                    .Where(s => s.ParentId == parentId)
                    .Select(s => new StudentInfo
                    {
                        StudentId = s.StudentId,
                        FullName = s.StudentName,
                        GradeId = s.Grades.GradesId,
                        GradeName = s.Grades.GradeName,
                        GradeNumber = s.Grades.GradeNumber,
                        SchoolId = s.School.SchoolId,
                        SchoolName = s.School.SchoolName,
                        SchoolStudentNumber = s.StudentSchoolNumber
                    })
                    .ToListAsync();
            }

            model.Schools = await _context.Schools
                .Select(s => new LockerSchoolViewModel
                {
                    SchoolID = s.SchoolId,
                    SchoolName = s.SchoolName,
                    Grades = (
                        from sg in _context.SchoolGrades
                        join g in _context.Grades on sg.GradeId equals g.GradesId
                        where sg.SchoolId == s.SchoolId
                        select new Grades
                        {
                            GradeId = g.GradesId,
                            Grade = g.GradeNumber
                        }
                    ).Distinct().ToList()
                })
                .ToListAsync();

            model.ParentId = parentId ?? 0;

            return View(model);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableLockers(int schoolId, int gradeId, string yearType)
        {
            //---Normal runtime behaviour ---
            var today = DateTime.Now;

            // --- TESTING MODE 
            //var today = new DateTime(2025, 11, 29);  // Before cutoff (should allow allocation)
            //var today = new DateTime(2025, 12, 5);   // During cutoff (should block allocation)
            //var today = new DateTime(2026, 1, 2);    // After cutoff (should allow allocation again)

            // Define cutoff range: from 30 November (inclusive) to 1 January (exclusive)
            var cutoffStart = new DateTime(today.Year, 11, 30, 0, 0, 0); // 30 Nov
            var cutoffEnd = new DateTime(today.Year + 1, 1, 1, 0, 0, 0); // 1 Jan next year

            // If today's date falls between 30 Nov and 1 Jan, allocation is closed
            if (today >= cutoffStart && today < cutoffEnd)
            {
                return Ok(new
                {
                    cutoffReached = true,
                    message = "Locker allocations are closed between November 30 and January 1."
                });
            }

            bool selectCurrentYear = yearType.Equals("current", StringComparison.OrdinalIgnoreCase);

            if (!selectCurrentYear)
            {
                gradeId = await (
                    from g in _context.Grades
                    join sg in _context.SchoolGrades on g.GradesId equals sg.GradeId
                    where sg.SchoolId == schoolId &&
                          g.GradeNumber > _context.Grades
                            .Where(g2 => g2.GradesId == gradeId)
                            .Select(g2 => g2.GradeNumber)
                            .FirstOrDefault()
                    orderby g.GradeNumber
                    select g.GradesId
                ).FirstOrDefaultAsync();
            }

            var lockers = await _context.Lockers
                .Include(l => l.School)
                .Where(l => l.SchoolId == schoolId
                            && l.GradeId == gradeId
                            && l.IsAssigned == false
                            && (selectCurrentYear ? l.CurrentBookingYear == false
                                                  : l.FollowingBookingYear == false))
                .Select(l => new LockerDto
                {
                    LockerId = l.LockerId,
                    LockerNumber = l.LockerNumber,
                    Location = l.School!.SchoolName
                })
                .ToListAsync();

            return Ok(lockers);
        }

        [HttpGet("adminAvailable")]
        public async Task<IActionResult> GetAdminAvailableLockers(int schoolId, int gradeId, string yearType)
        {
            //Normal behaviour
            var today = DateTime.Now;

            //Possible Exam Testing: Simulate Nov Cut Off 
            //var today = new DateTime(2025, 12, 5);   // Simulate after cutoff

            var cutoffDate = new DateTime(today.Year, 11, 30, 23, 59, 59);

            if (today > cutoffDate)
            {
                return Ok(new
                {
                    cutoffReached = true,
                    message = "Locker allocations are closed for this year (after November 30)."
                });
            }

            var lockers = await _context.Lockers
                .Include(l => l.School)
                .Where(l => l.SchoolId == schoolId
                            && l.GradeId == gradeId
                            && l.IsAssigned == false
                                                  )
                .Select(l => new LockerDto
                {
                    LockerId = l.LockerId,
                    LockerNumber = l.LockerNumber,
                    Location = l.School!.SchoolName
                })
                .ToListAsync();

            //Exam Test: Uncomment to test waiting list
            //return Ok(new List<LockerDto>());

            return Ok(lockers);
        }


        [HttpPost("UnassignLocker")]
        public async Task<IActionResult> UnassignLocker([FromBody] LockerDto dto)
        {
            if (dto.LockerId <= 0 || dto.StudentID <= 0)
                return BadRequest("Invalid locker or student ID.");


            Locker locker;
            if (dto.YearType == "current")
            {
                locker = await _context.Lockers
                    .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId &&
                                              (l.CurrentBookingYear == true));
            }
            else if (dto.YearType == "following")
            {
                locker = await _context.Lockers
                    .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId &&
                                              (l.FollowingBookingYear == true));
            }
            else
            {
                return NotFound("Invalid year type specified.");
            }

            var student = await _context.Students
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentID);

            if (student == null)
                return NotFound("Student not found.");

            if (string.IsNullOrEmpty(student.Parent?.ParentEmail))
                return BadRequest("Parent email not available.");

            if (dto.YearType?.ToLower() == "current")
            {
                locker.CurrentBookingYear = false;
                locker.IsAdminApprovedCurrentBookingYear = false;
                locker.StudentIdCurrentBookingYear = null;
                locker.AssignedDate = null;
            
            }
            else if (dto.YearType?.ToLower() == "following")
            {
                locker.FollowingBookingYear = false;
                locker.IsAdminApprovedFollowingBookingYear = false;
                locker.StudentIdFollowingBookingYear = null;
                locker.AssignedDate = null;
            }

            try
            {
                await _context.SaveChangesAsync();

                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Locker Assignment Cancellation",
                    $"Dear Parent,\n\nThe locker assignment for {student.StudentName} (Locker {locker.LockerNumber}) " +
                    $"for the {dto.YearType} booking year has been canceled as per request.\n\nRegards,\nSchool Admin"
                );

                return Ok("Locker unassigned successfully and cancellation email sent.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, "Concurrency error occurred while unassigning locker.");
            }
        }

        [HttpPost("assignLocker")]
        public async Task<IActionResult> AssignLocker([FromBody] LockerDto dto)
        {
            if (dto.LockerId <= 0 || dto.StudentID <= 0)
                return BadRequest("Invalid locker or student ID.");

            var locker = await _context.Lockers
                .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId);

            if (locker == null)
                return NotFound("Locker not found or already assigned.");

            var student = await _context.Students
                .Include(s => s.Parent)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentID);

            if (student == null)
                return NotFound("Student not found.");

            if (string.IsNullOrEmpty(student.Parent?.ParentEmail))
                return BadRequest("Parent email not available.");

            // --- Assign locker ---
            DateTime assignedDate = DateTime.Now;
            string formattedDate = string.Empty;
            string bookingYearLabel = dto.YearType?.ToUpper() ?? "CURRENT";

            if (dto.YearType?.ToLower() == "current")
            {
                locker.CurrentBookingYear = true;
                locker.IsAdminApprovedCurrentBookingYear = false;
                locker.StudentIdCurrentBookingYear = dto.StudentID;
                locker.CurrentAssignedDate = assignedDate;

                formattedDate = assignedDate.ToString("dd MMM yyyy");
            }
            else if (dto.YearType?.ToLower() == "following")
            {
                locker.FollowingBookingYear = true;
                locker.IsAdminApprovedFollowingBookingYear = false;
                locker.StudentIdFollowingBookingYear = dto.StudentID;
                locker.FollowingAssignedDate = assignedDate;

                formattedDate = assignedDate.AddYears(1).ToString("dd MMM yyyy");
            }
            else
            {
                return BadRequest("Invalid year type specified.");
            }

            try
            {
                await _context.SaveChangesAsync();

                // --- Build email message ---
                string emailBody = $@"
                    Dear Parent,
                    
                    Locker {locker.LockerNumber} has been assigned to {student.StudentName}
                    for the {bookingYearLabel} booking year ({formattedDate}).
                    
                    Please reply with proof of payment for the R100 usage fee.

                    Kindly note, Failure to pay fee within 30 days of this application 
                    will result in locker deallocation.
                    
                    Kind regards,
                    School Locker Administration
                    ";

                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Locker Assignment Confirmation",
                    emailBody
                );

                return Ok("Locker assigned successfully and confirmation email sent.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, "Concurrency error occurred while assigning locker.");
            }
        }

        [HttpPost("AdminAssignLocker")]
        public async Task<IActionResult> AdminAssignLocker([FromBody] LockerDto dto)
        {
            if (dto.LockerId <= 0 || dto.StudentID <= 0)
                return BadRequest("Invalid locker or student ID.");

            Locker locker;

            if (dto.YearType == "current")
            {
                locker = await _context.Lockers
                    .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId);
            }
            else if (dto.YearType == "following")
            {
                locker = await _context.Lockers
                    .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId);
            }
            else
            {
                return NotFound("Invalid year type specified.");
            }

            if (locker == null)
                return NotFound("Locker already assigned or does not exist.");

            var student = await _context.Students
                .Include(s => s.Parent)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentID);

            if (student == null)
                return NotFound("Student not found.");

            if (string.IsNullOrEmpty(student.Parent?.ParentEmail))
                return BadRequest("Parent email not available.");

            bool isCurrentYear = dto.YearType?.ToLower() == "current";
            bool isFollowingYear = dto.YearType?.ToLower() == "following";

            // 🔎 check waiting list
            var waitingListItem = await _context.LockerWaitingLists.FirstOrDefaultAsync(wl =>
                wl.StudentId == dto.StudentID &&
                wl.SchoolId == locker.SchoolId &&
                wl.GradeId == locker.GradeId &&
                ((isCurrentYear && wl.CurrentYear == true) ||
                 (isFollowingYear && wl.FollowingYear == true))
            );

            bool wasOnWaitingList = false;

            if (waitingListItem != null)
            {
                wasOnWaitingList = true;
                _context.LockerWaitingLists.Remove(waitingListItem);
            }

            // 🔧 assign locker based on year type
            if (isCurrentYear)
            {
                locker.CurrentBookingYear = true;
                locker.IsAdminApprovedCurrentBookingYear = true;
                locker.StudentIdCurrentBookingYear = dto.StudentID;
                locker.CurrentAssignedDate = DateTime.Now;
            }
            else if (isFollowingYear)
            {
                locker.FollowingBookingYear = true;
                locker.IsAdminApprovedFollowingBookingYear = true;
                locker.StudentIdFollowingBookingYear = dto.StudentID;
                locker.FollowingAssignedDate = DateTime.Now;
            }

            try
            {
                await _context.SaveChangesAsync();

                // 🔔 send emails
                if (wasOnWaitingList)
                {
                    await EmailHelper.SendEmailAsync(
                        student.Parent.ParentEmail,
                        "Waiting List Update",
                        $"Dear Parent,\n\n{student.StudentName} has been removed from the waiting list for " +
                        $"{(isCurrentYear ? "the current year" : "the following year")} because a locker has been assigned."
                    );
                }

                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Locker Assignment Confirmation",
                    $"Dear Parent,\n\nLocker {locker.LockerNumber} has been successfully assigned to {student.StudentName} " +
                    $"for the {dto.YearType} booking year."
                );

                return Ok("Locker assigned successfully. Emails sent for waiting list removal and locker assignment.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, "Concurrency error occurred while assigning locker.");
            }
        }


        [HttpGet("AssignedLocker/{studentId}")]
        public async Task<IActionResult> GetAssignedLocker(int studentId)
        {
            var currentYear = DateTime.Now.Year;
            var nextYear = currentYear + 1;

            var lockers = await _context.Lockers
                .Where(l =>
                    (l.StudentIdCurrentBookingYear == studentId && (l.CurrentBookingYear ?? false)) ||
                    (l.StudentIdFollowingBookingYear == studentId && (l.FollowingBookingYear ?? false))
                )
                .Select(l => new
                {
                    l.LockerId,
                    l.LockerNumber,
                    l.Grade.GradesId,
                    l.Grade.GradeName,
                    l.Grade.GradeNumber,
                    l.IsAdminApprovedCurrentBookingYear,
                    l.IsAdminApprovedFollowingBookingYear,
                    l.CurrentBookingYear,
                    l.StudentIdCurrentBookingYear,
                    l.StudentIdFollowingBookingYear,
                    l.FollowingBookingYear
                })
                .ToListAsync();

            var assignedLockers = lockers.SelectMany(l =>
            {
                var list = new List<AssignedLockerDto>();

                if (l.CurrentBookingYear == true && l.StudentIdCurrentBookingYear == studentId)
                {
                    list.Add(new AssignedLockerDto
                    {
                        LockerId = l.LockerId,
                        LockerNumber = l.LockerNumber,
                        GradeId = l.GradesId,
                        GradeName = l.GradeName,
                        GradeNumber = l.GradeNumber,
                        Year = currentYear,
                        YearType = "current",
                        IsAdminApproved = l.IsAdminApprovedCurrentBookingYear
                    });
                }

                if (l.FollowingBookingYear == true && l.StudentIdFollowingBookingYear == studentId)
                {
                    list.Add(new AssignedLockerDto
                    {
                        LockerId = l.LockerId,
                        LockerNumber = l.LockerNumber,
                        GradeId = l.GradesId,
                        GradeName = l.GradeName,
                        GradeNumber = l.GradeNumber,
                        Year = nextYear,
                        YearType = "following",
                        IsAdminApproved = l.IsAdminApprovedFollowingBookingYear
                    });
                }

                return list;
            }).ToList();

            return Ok(assignedLockers);
        }

        [HttpPost("AddStudent")]
        public async Task<IActionResult> AddStudent(
            int parentId,
            int selectedSchoolId,
            string studentName,
            int SelectedGradeId
        )
        {
            var parent = await _context.Parents.FindAsync(parentId);
            if (parent == null)
            {
                return BadRequest("Parent not found.");
            }

            var gradeEntity = await _context.Grades
                .FirstOrDefaultAsync(g => g.GradesId == SelectedGradeId);
            if (gradeEntity == null)
            {
                return BadRequest("Invalid grade.");
            }

            string newStudentId;
            var rng = new Random();
            do
            {
                newStudentId = rng.Next(100000, 999999).ToString();
            } while (await _context.Students.AnyAsync(s => s.StudentSchoolNumber == newStudentId));

            var student = new Student
            {
                SchoolId = selectedSchoolId,
                StudentSchoolNumber = newStudentId,
                StudentName = studentName,
                ParentId = parentId,
                GradesId = gradeEntity.GradesId
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { parentId = parentId });

        }

        [HttpPost("SearchStudent")]
        public async Task<IActionResult> SearchStudent(AdminViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.StudentSchoolNumber))
            {
                ModelState.AddModelError("", "Student School Number is required.");
                return View("Index", model);
            }

            var student = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.School)
                .FirstOrDefaultAsync(s => s.StudentSchoolNumber == model.StudentSchoolNumber);

            if (student == null)
            {
                ModelState.AddModelError("", "Student not found.");
                model.ShowStudentSection = true;
                return View("Index", model);
            }

            model.FoundStudent = new StudentDto
            {
                StudentId = student.StudentId,
                StudentName = student.StudentName,
                StudentSchoolNumber = student.StudentSchoolNumber,
                GradesId = student.GradesId,
                SchoolId = student.SchoolId
            };

            model.ShowStudentSection = true;

            model.Grades = await (from sg in _context.SchoolGrades
                                  join g in _context.Grades on sg.GradeId equals g.GradesId
                                  where sg.SchoolId == model.SchoolId
                                  orderby g.GradeNumber
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                                  {
                                      Value = g.GradesId.ToString(),
                                      Text = g.GradeName
                                  }).ToListAsync();

            return View("Index", model);
        }

        private bool LockerExists(int id) =>
            _context.Lockers.Any(e => e.LockerId == id);
    }
}
