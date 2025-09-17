using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Controllers
{
    [Route("Registration")]
    public class RegistrationController : Controller
    {
        private readonly LockerAdminDbContext _context;

        public RegistrationController(LockerAdminDbContext context)
        {
            _context = context;
        }

        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            var schoolList = await _context.Schools
                .Select(s => new SchoolViewModel
                {
                    SchoolID = s.SchoolId,
                    SchoolName = s.SchoolName
                })
                .ToListAsync();

            var model = new RegistrationViewModel
            {
                Schools = schoolList
            };

            return View(model);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            string IDNumber,
            string Title,
            string Name,
            string Surname,
            string Email,
            string HomeAddress,
            string PhoneNumber
        )
        {
            if (await _context.Parents.AnyAsync(p => p.ParentIdnumber == long.Parse(IDNumber)))
            {
                ModelState.AddModelError("", "Parent already exists.");
                return RedirectToAction("Index");
            }

            try
            {
                var parent = new Parent
                {
                    ParentIdnumber = long.Parse(IDNumber),
                    ParentTitle = Title,
                    ParentName = Name,
                    ParentSurname = Surname,
                    ParentEmail = Email,
                    ParentHomeAddress = HomeAddress,
                    ParentPhoneNumber = long.Parse(PhoneNumber)
                };

                _context.Parents.Add(parent);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Parent registration completed successfully.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return RedirectToAction("Index");
            }
        }
    }
}
