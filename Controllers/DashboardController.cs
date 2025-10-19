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
        public async Task<IActionResult> Index(int adminId, int? year, int? gradeId, bool? isAdminApproved)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            // store adminId for back button
            ViewBag.AdminId = adminId;

            // Join Parents → Students → Grades → Lockers
            var lockerUsageQuery =
                from parent in _context.Parents
                join student in _context.Students on parent.ParentId equals student.ParentId
                join grade in _context.Grades on student.GradesId equals grade.GradesId
                join locker in _context.Lockers on student.StudentId equals locker.StudentIdCurrentBookingYear into lockerCurrent
                from lc in lockerCurrent.DefaultIfEmpty()
                join locker2 in _context.Lockers on student.StudentId equals locker2.StudentIdFollowingBookingYear into lockerFollowing
                from lf in lockerFollowing.DefaultIfEmpty()
                select new
                {
                    parent.ParentEmail,
                    student.StudentSchoolNumber,
                    GradeNumber = grade.GradeNumber,
                    CurrentLocker = lc,
                    FollowingLocker = lf
                };

            var usageDetails = lockerUsageQuery
                .AsEnumerable()
                .SelectMany(x =>
                {
                    var list = new List<LockerUsageDetail>();

                    if (x.CurrentLocker != null && x.CurrentLocker.CurrentBookingYear == true)
                    {
                        list.Add(new LockerUsageDetail
                        {
                            ParentEmail = x.ParentEmail,
                            StudentSchoolNumber = x.StudentSchoolNumber,
                            GradeNumber = x.GradeNumber.ToString(),
                            LockerNumber = x.CurrentLocker.LockerNumber,
                            IsAssigned = true,
                            IsAdminApproved = x.CurrentLocker.IsAdminApprovedCurrentBookingYear ?? false,
                            AssignedDate = x.CurrentLocker.CurrentAssignedDate,
                            BookingYear = 2025
                        });
                    }

                    if (x.FollowingLocker != null && x.FollowingLocker.FollowingBookingYear == true)
                    {
                        list.Add(new LockerUsageDetail
                        {
                            ParentEmail = x.ParentEmail,
                            StudentSchoolNumber = x.StudentSchoolNumber,
                            GradeNumber = x.GradeNumber.ToString(),
                            LockerNumber = x.FollowingLocker.LockerNumber,
                            IsAssigned = true,
                            IsAdminApproved = x.FollowingLocker.IsAdminApprovedFollowingBookingYear,
                            AssignedDate = x.FollowingLocker.FollowingAssignedDate,
                            BookingYear = 2026
                        });
                    }

                    return list;
                })
                .Where(x => x.BookingYear == selectedYear);

            if (gradeId.HasValue && gradeId.Value > 0)
                usageDetails = usageDetails.Where(x => x.GradeNumber == gradeId.Value.ToString());

            if (isAdminApproved.HasValue)
                usageDetails = usageDetails.Where(x => x.IsAdminApproved == isAdminApproved.Value);

            var lockerUsageResults = usageDetails.ToList();

            var lockerByGrade = lockerUsageResults
                .GroupBy(x => x.GradeNumber)
                .Select(g => new GradeCount
                {
                    Grade = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var model = new LockerDashboardViewModel
            {
                LockerUsageGrade8and11 = lockerUsageResults,
                LockerByGrade = lockerByGrade,
                LockersBookedJanToJun = lockerUsageResults.Count
            };

            ViewBag.SelectedYear = selectedYear;

            return View(model);
        }
    }
}
