
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
            //var from = startDate ?? new DateTime(DateTime.Now.Year, 1, 1);
            //var to = endDate ?? new DateTime(DateTime.Now.Year, 12, 31);

            //var lockerUsageQuery = _context.Parents
            //    .Join(_context.Students,
            //          parent => parent.ParentId,
            //          student => student.ParentId,
            //          (parent, student) => new { parent, student })
            //    .Join(_context.Grades,
            //          ps => ps.student.GradesId,
            //          grade => grade.GradesId,
            //          (ps, grade) => new { ps.parent, ps.student, grade })
            //    .Join(_context.Lockers,
            //          psg => psg.student.StudentId,
            //          locker => locker.StudentId,
            //          (psg, locker) => new LockerUsageDetail
            //          {
            //              ParentEmail = psg.parent.ParentEmail,
            //              StudentSchoolNumber = psg.student.StudentSchoolNumber,
            //              GradeNumber = psg.grade.GradeNumber.ToString(),
            //              LockerNumber = locker.LockerNumber,
            //              IsAssigned = locker.IsAssigned == true,
            //              IsAdminApproved = locker.IsAdminApproved == true,
            //              AssignedDate = locker.AssignedDate
            //          })
            //    .Where(x => x.IsAssigned) 
            //    .Where(x => x.AssignedDate >= from && x.AssignedDate <= to);

            //if (gradeId.HasValue)
            //{
            //    lockerUsageQuery = lockerUsageQuery.Where(x => x.GradeNumber == gradeId.Value.ToString());
            //}

            //if (isAdminApproved.HasValue)
            //{
            //    lockerUsageQuery = lockerUsageQuery.Where(x => x.IsAdminApproved == isAdminApproved.Value);
            //}

            //var lockerUsageResults = await lockerUsageQuery.Take(200).ToListAsync();

            //var lockerByGrade = lockerUsageResults
            //    .GroupBy(x => x.GradeNumber)
            //    .Select(g => new GradeCount
            //    {
            //        Grade = g.Key,
            //        Count = g.Count()
            //    })
            //    .ToList();

            //var model = new LockerDashboardViewModel
            //{
            //    LockerUsageGrade8and11 = lockerUsageResults,
            //    LockerByGrade = lockerByGrade,
            //    LockersBookedJanToJun = lockerUsageResults.Count
            //};

            //return View(model);

            return View(new LockerDashboardViewModel());
        }
    }
}