using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Helper;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LockerWaitingListController : ControllerBase
    {
        private readonly LockerAdminDbContext _context;

        public LockerWaitingListController(LockerAdminDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LockerWaitingList>>> GetWaitingList()
        {
            return await _context.LockerWaitingLists.ToListAsync();
        }

        [HttpGet("IsUserOnWaitingList/{studentId}/{yearType}")]
        public async Task<IActionResult> IsUserOnWaitingList(int studentId, string yearType)
        {
            bool checkCurrent = yearType.Equals("current", StringComparison.OrdinalIgnoreCase);
            bool checkFollowing = yearType.Equals("following", StringComparison.OrdinalIgnoreCase);

            var items = await _context.LockerWaitingLists
                .Where(wl => wl.StudentId == studentId &&
                             ((checkCurrent && wl.CurrentYear == true) ||
                              (checkFollowing && wl.FollowingYear == true) &&
                              (wl.Status == true))
                      )
                .Include(wl => wl.School)
                .Include(wl => wl.Grade)
                .Select(wl => new LockerWaitingListDto
                {
                    AppliedDate = wl.AppliedDate,
                    SchoolName = wl.School.SchoolName,
                    SchoolId = wl.SchoolId,
                    GradeId = wl.GradeId,
                    GradeName = wl.Grade.GradeName,
                    CurrentYear = wl.CurrentYear ?? false,
                    FollowingYear = wl.FollowingYear ?? false
                })
                .ToListAsync();

            if (!items.Any())
                return Ok(false);

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LockerWaitingList>> GetWaitingListItem(int id) //TODO this endpoint to be made into a function in services or private
        {
            var item = await _context.LockerWaitingLists.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPut("Assign")]
        public async Task<IActionResult> AssignWaitingList([FromBody] LockerWaitingListDto dto)
        {
            if (dto == null || dto.StudentId <= 0)
                return BadRequest("Invalid request.");

            if (dto.CurrentYear == false)
            {
                dto.GradeId = await (
                    from g in _context.Grades
                    join sg in _context.SchoolGrades on g.GradesId equals sg.GradeId
                    where sg.SchoolId == dto.SchoolId &&
                    g.GradeNumber > _context.Grades
              .Where(g2 => g2.GradesId == dto.GradeId)
              .Select(g2 => g2.GradeNumber)
              .FirstOrDefault()
                    orderby g.GradeNumber
                    select g.GradesId
                    ).FirstOrDefaultAsync();
            }

            LockerWaitingList existingItem = new LockerWaitingList();

            existingItem.SchoolId = dto.SchoolId;
            existingItem.GradeId = dto.GradeId;
            existingItem.AppliedDate = DateTime.Now;
            existingItem.StudentId = dto.StudentId;
            existingItem.Status = true;
            existingItem.CurrentYear = dto.CurrentYear;
            existingItem.FollowingYear = dto.FollowingYear;

            _context.LockerWaitingLists.Update(existingItem);
            await _context.SaveChangesAsync();

            //TODO: Add email confirming place on waiting list.

            return Ok(existingItem);

        }

        [HttpPost("Unassign")]
        public async Task<IActionResult> UnassignWaitingList([FromBody] LockerWaitingListDto dto)
        {
            if (dto == null || dto.StudentId <= 0 || dto.SchoolId <= 0 || dto.GradeId <= 0)
                return BadRequest("Invalid request parameters.");

            if (dto.YearType?.ToLower() == "current")
            {
                dto.CurrentYear = true;
                dto.FollowingYear = false;
            }
            else if (dto.YearType?.ToLower() == "following")
            {
                dto.FollowingYear = true;
                dto.CurrentYear = false;
            }

            var existingItem = await _context.LockerWaitingLists
                .FirstOrDefaultAsync(l =>
                    l.StudentId == dto.StudentId &&
                    l.SchoolId == dto.SchoolId &&
                    l.GradeId == dto.GradeId &&
                    l.CurrentYear == dto.CurrentYear &&
                    l.FollowingYear == dto.FollowingYear
                );

            if (existingItem == null)
                return NotFound("Waiting list item not found.");

            _context.LockerWaitingLists.Remove(existingItem);
            await _context.SaveChangesAsync();

            var student = await _context.Students.Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentId);
            if (student?.Parent?.ParentEmail != null)
            {
                await EmailHelper.SendEmailAsync(
                    student.Parent.ParentEmail,
                    "Waiting List Cancellation",
                    $"Dear Parent,\n\nThe waiting list request for {student.StudentName} has been canceled."
                );
            }

            return Ok("Waiting list request canceled successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWaitingListItem(int id)
        {
            var item = await _context.LockerWaitingLists.FindAsync(id);
            if (item == null) return NotFound();

            _context.LockerWaitingLists.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<LockerWaitingListDto>> PostWaitingListItem(LockerWaitingListDto item)
        {
            var existingItem = await _context.LockerWaitingLists
                .FirstOrDefaultAsync(w => w.StudentId == item.StudentId);

            if (existingItem != null)
            {
                existingItem.SchoolId = item.SchoolId;
                existingItem.GradeId = item.GradeId;
                existingItem.AppliedDate = DateTime.Now;
                existingItem.Status = true;

                _context.LockerWaitingLists.Update(existingItem);
                await _context.SaveChangesAsync();

                return Ok(existingItem);
            }

            var newItem = new LockerWaitingList
            {
                StudentId = item.StudentId,
                SchoolId = item.SchoolId,
                GradeId = item.GradeId,
                AppliedDate = DateTime.Now
            };

            _context.LockerWaitingLists.Add(newItem);
            await _context.SaveChangesAsync();

            item.WaitingListId = newItem.WaitingListId;

            return CreatedAtAction(nameof(GetWaitingListItem), new { id = newItem.WaitingListId }, item);
        }

        private bool WaitingListItemExists(int id) =>
            _context.LockerWaitingLists.Any(e => e.StudentId == id);
    }
}
