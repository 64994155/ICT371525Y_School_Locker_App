using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Helper;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
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
                        SchoolId = s.School.SchoolId,
                        SchoolName = s.School.SchoolName
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


        [HttpPost("unassignLocker")]
        public async Task<IActionResult> UnassignLocker([FromBody] LockerDto dto)
        {
            if (dto.LockerId <= 0 || dto.StudentID <= 0)
                return BadRequest("Invalid locker or student ID.");

            var locker = await _context.Lockers
                .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId && l.StudentId == dto.StudentID && l.IsAssigned == true);

            if (locker == null)
                return NotFound("Locker not assigned to this student or does not exist.");

            var student = await _context.Students
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentID);

            if (student == null)
                return NotFound("Student not found.");

            if (string.IsNullOrEmpty(student.Parent?.ParentEmail))
                return BadRequest("Parent email not available.");

            locker.IsAssigned = false;
            locker.StudentId = null;
            locker.AssignedDate = null;
            locker.IsAdminApproved = null;

            if (dto.YearType?.ToLower() == "current")
            {
                locker.CurrentBookingYear = false;
            }
            else if (dto.YearType?.ToLower() == "following")
            {
                locker.FollowingBookingYear = false;
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
                .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId && l.IsAssigned == false);

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

            locker.IsAssigned = true;
            locker.StudentId = dto.StudentID;
            locker.AssignedDate = DateTime.UtcNow;

            locker.CurrentBookingYear = dto.YearType?.ToLower() == "current";
            locker.FollowingBookingYear = dto.YearType?.ToLower() == "following";

            try
            {
                await _context.SaveChangesAsync();

                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Locker Assignment Confirmation",
                    $"Dear Parent,\n\nLocker {locker.LockerNumber} has been assigned to {student.StudentName} for the {dto.YearType} booking year."
                );

                return Ok("Locker assigned successfully with the correct booking year.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, "Concurrency error occurred while assigning locker.");
            }
        }

        [HttpPost("adminAssignLocker")]
        public async Task<IActionResult> AdminAssignLocker([FromBody] LockerDto dto)
        {
            if (dto.LockerId <= 0 || dto.StudentID <= 0)
                return BadRequest("Invalid locker or student ID.");

            var locker = await _context.Lockers
                .FirstOrDefaultAsync(l => l.LockerId == dto.LockerId && l.IsAssigned == false);

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

            locker.IsAssigned = true;
            locker.StudentId = dto.StudentID;
            locker.AssignedDate = DateTime.UtcNow;
            locker.IsAdminApproved = true;

            locker.CurrentBookingYear = isCurrentYear;
            locker.FollowingBookingYear = isFollowingYear;

            try
            {
                await _context.SaveChangesAsync();

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
                        .Where(l => l.StudentId == studentId &&
                ((l.CurrentBookingYear ?? false) || (l.FollowingBookingYear ?? false)))
            .Select(l => new
                    {
                    l.LockerId,
                    l.LockerNumber,
                    GradeId = l.Grade.GradesId,
                    GradeName = l.Grade.GradeName,
                    GradeNumber = l.Grade.GradeNumber,
                    l.IsAdminApproved,
                    l.CurrentBookingYear,
                    l.FollowingBookingYear
                })
                .ToListAsync();

                var assignedLockers = lockers.SelectMany(l => new[]
                {
            (bool)l.CurrentBookingYear ? new {
                l.LockerId,
                l.LockerNumber,
                l.GradeId,
                l.GradeName,
                l.GradeNumber,
                l.IsAdminApproved,
                Year = currentYear,
                YearType = "current"
            } : null,

            (bool)l.FollowingBookingYear ? new {
                l.LockerId,
                l.LockerNumber,
                l.GradeId,
                l.GradeName,
                l.GradeNumber,
                l.IsAdminApproved,
                Year = nextYear,
                YearType = "following"
            } : null
        }.Where(x => x != null))
                .ToList();

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

        private bool LockerExists(int id) =>
            _context.Lockers.Any(e => e.LockerId == id);
    }
}
