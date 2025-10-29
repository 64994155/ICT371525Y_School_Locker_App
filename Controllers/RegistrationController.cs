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
        public async Task<IActionResult> Submit(RegistrationViewModel model)
        {
            if (await _context.Parents.AnyAsync(p => p.ParentIdnumber == long.Parse(model.IDNumber)))
            {
                ModelState.AddModelError("IDNumber", "A parent with this ID number already exists.");
                return View("Index", model);
            }

            try
            {
                var parent = new Parent
                {
                    ParentIdnumber = long.Parse(model.IDNumber),
                    ParentTitle = model.Title,
                    ParentName = model.Name,
                    ParentSurname = model.Surname,
                    ParentEmail = model.Email,
                    ParentHomeAddress = model.HomeAddress,
                    ParentPhoneNumber = long.Parse(model.PhoneNumber)
                };

                _context.Parents.Add(parent);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Parent registration completed successfully!";
                TempData.Keep("Success");
                return RedirectToAction(nameof(Index), "Registration");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View("Index", model);
            }
        }
    }
}