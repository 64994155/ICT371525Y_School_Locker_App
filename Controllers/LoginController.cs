using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Controllers
{
    [Route("Login")]
    public class LoginController : Controller
    {
        private readonly LockerAdminDbContext _context;

        public LoginController(LockerAdminDbContext context)
        {
            _context = context;
        }

        [HttpGet("index")]
        public IActionResult Index()
        {
            return View();
        }

       [HttpPost("login")]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
        return View("Index", model);

    if (long.TryParse(model.IdNumber?.Trim(), out var idNumber))
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminIdNumber.HasValue && a.AdminIdNumber.Value == idNumber);

                if (admin != null)
                {
                    return Redirect($"/Admin/Index?adminId={admin.AdminId}");
                }

                var parent = await _context.Parents
            .FirstOrDefaultAsync(p => p.ParentIdnumber == idNumber);

        if (parent != null)
        {
            return Redirect($"/Locker/index?parentId={parent.ParentId}");
        }
    }

    ViewBag.ErrorMessage = "ID Number not found. Please register first.";
    return View("Index", model);
}
    }
}
