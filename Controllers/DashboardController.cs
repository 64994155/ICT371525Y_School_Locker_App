//using ICT371525Y_School_Locker_App.Data;
//using ICT371525Y_School_Locker_App.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace ICT371525Y_School_Locker_App.Controllers
//{
//    [Route("Dashboard")]
//    public class DashboardController : Controller
//    {
//        private readonly LockerAdminDbContext _context;

//        public DashboardController(LockerAdminDbContext context)
//        {
//            _context = context;
//        }

//        [HttpGet("index")]
//        public async Task<IActionResult> Index()
//        {
//            // Locker usage by grade
//            var lockerByGrade = await _context.Lockers
//                .Where(l => l.IsAssigned == true)
//                .Join(_context.Students,
//                      locker => locker.StudentId,
//                      student => student.StudentId,
//                      (locker, student) => new { locker, student })
//                .Join(_context.Grades,
//                      ls => ls.student.GradesId,
//                      grade => grade.GradesId,
//                      (ls, grade) => new { grade.GradeName })
//                .GroupBy(x => x.GradeName)
//                .Select(g => new GradeCount
//                {
//                    Grade = g.Key,
//                    Count = g.Count()
//                })
//                .ToListAsync();

//            // Locker usage for grades 8 and 11
//            var lockerUsageGrade8and11 = await _context.Parents
//                .Join(_context.Students,
//                      parent => parent.ParentId,
//                      student => student.ParentId,
//                      (parent, student) => new { parent, student })
//                .Join(_context.Grades,
//                      ps => ps.student.GradesId,
//                      grade => grade.GradesId,
//                      (ps, grade) => new { ps.parent, ps.student, grade })
//                .Join(_context.Lockers,
//                      psg => psg.student.StudentId,
//                      locker => locker.StudentId,
//                      (psg, locker) => new LockerUsageDetail
//                      {
//                          ParentEmail = psg.parent.ParentEmail,
//                          StudentSchoolNumber = psg.student.StudentSchoolNumber,
//                          GradeNumber = psg.grade.GradeNumber.ToString(),
//                          LockerNumber = locker.LockerNumber,
//                          IsAssigned = locker.IsAssigned == true,
//                          IsAdminApproved = locker.IsAssigned == true
//                      })
//                .Where(x => x.GradeNumber == "8" || x.GradeNumber == "11")
//                .Take(100)
//                .ToListAsync();

//            // Lockers booked between Jan and November 2025
//            var lockersBookedJanToJun = await _context.Lockers
//                .Where(l => l.IsAssigned == true &&
//                            l.AssignedDate >= new DateTime(2025, 1, 1) &&
//                            l.AssignedDate <= new DateTime(2025, 11, 30))
//                .CountAsync();

//            // Final ViewModel
//            var model = new LockerDashboardViewModel
//            {
//                LockerByGrade = lockerByGrade,
//                LockerUsageGrade8and11 = lockerUsageGrade8and11,
//                LockersBookedJanToJun = lockersBookedJanToJun
//            };

//            return View(model);
//        }
//    }
//}


using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Controllers
{
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly LockerAdminDbContext _context;

        public DashboardController(LockerAdminDbContext context)
        {
            _context = context;
        }

        [HttpGet("index")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? gradeId, bool? isAdminApproved)
        {
            // Default date range = current year
            var from = startDate ?? new DateTime(DateTime.Now.Year, 1, 1);
            var to = endDate ?? new DateTime(DateTime.Now.Year, 12, 31);

            // --- Locker usage query ---
            var lockerUsageQuery = _context.Parents
                .Join(_context.Students,
                      parent => parent.ParentId,
                      student => student.ParentId,
                      (parent, student) => new { parent, student })
                .Join(_context.Grades,
                      ps => ps.student.GradesId,
                      grade => grade.GradesId,
                      (ps, grade) => new { ps.parent, ps.student, grade })
                .Join(_context.Lockers,
                      psg => psg.student.StudentId,
                      locker => locker.StudentId,
                      (psg, locker) => new LockerUsageDetail
                      {
                          ParentEmail = psg.parent.ParentEmail,
                          StudentSchoolNumber = psg.student.StudentSchoolNumber,
                          GradeNumber = psg.grade.GradeNumber.ToString(),
                          LockerNumber = locker.LockerNumber,
                          IsAssigned = locker.IsAssigned == true,
                          IsAdminApproved = locker.IsAdminApproved == true,
                          AssignedDate = locker.AssignedDate
                      })
                .Where(x => x.IsAssigned) // ✅ always only assigned lockers
                .Where(x => x.AssignedDate >= from && x.AssignedDate <= to);

            // Grade filter
            if (gradeId.HasValue)
            {
                lockerUsageQuery = lockerUsageQuery.Where(x => x.GradeNumber == gradeId.Value.ToString());
            }

            // Admin approval filter
            if (isAdminApproved.HasValue)
            {
                lockerUsageQuery = lockerUsageQuery.Where(x => x.IsAdminApproved == isAdminApproved.Value);
            }

            var lockerUsageResults = await lockerUsageQuery.Take(200).ToListAsync();

            // Group by grade for summary
            var lockerByGrade = lockerUsageResults
                .GroupBy(x => x.GradeNumber)
                .Select(g => new GradeCount
                {
                    Grade = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Final ViewModel
            var model = new LockerDashboardViewModel
            {
                LockerUsageGrade8and11 = lockerUsageResults,
                LockerByGrade = lockerByGrade,
                LockersBookedJanToJun = lockerUsageResults.Count
            };

            return View(model);
        }
    }
}