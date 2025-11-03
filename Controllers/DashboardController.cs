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
            ViewBag.AdminId = adminId;

            // --- Base query joining related tables
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

            // --- Build full list of usage details
            var allUsageDetails = lockerUsageQuery
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
                .ToList();

            int totalLockersForYear = allUsageDetails.Count(x => x.BookingYear == selectedYear);

            // --- Apply filters (grade/admin approval) only to display list
            var filteredUsageDetails = allUsageDetails
                .Where(x => x.BookingYear == selectedYear);

            if (gradeId.HasValue && gradeId.Value > 0)
                filteredUsageDetails = filteredUsageDetails.Where(x => x.GradeNumber == gradeId.Value.ToString());

            if (isAdminApproved.HasValue)
                filteredUsageDetails = filteredUsageDetails.Where(x => x.IsAdminApproved == isAdminApproved.Value);

            var lockerUsageResults = filteredUsageDetails.ToList();

            // --- Group summaries for filtered display
            var lockerByGrade = lockerUsageResults
                .GroupBy(x => x.GradeNumber)
                .Select(g => new GradeCount
                {
                    Grade = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var groupedLockers = lockerUsageResults
                .GroupBy(x => x.GradeNumber)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            // --- Build model
            var model = new LockerDashboardViewModel
            {
                LockerUsageGrade8and11 = lockerUsageResults,
                LockerByGrade = lockerByGrade,
                LockersBooked = totalLockersForYear 
            };

            ViewBag.SelectedYear = selectedYear;
            ViewBag.GroupedLockers = groupedLockers;

            return View(model);
        }
    }
}