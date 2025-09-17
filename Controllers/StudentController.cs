using ICT371525Y_School_Locker_App.Data;
using ICT371525Y_School_Locker_App.DTO;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : Controller
    {
        private readonly LockerAdminDbContext _context;

        public StudentController(LockerAdminDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ✅ GET: api/Student
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            return await _context.Students.ToListAsync();
        }

        // ✅ GET: api/Student/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDto>> GetStudent(int id)
        {
            var student = await _context.Students
            .Include(s => s.School)
            .Include(s => s.Grades)
            .Where(s => s.StudentId == id)
            .Select(s => new StudentDto
            {
                StudentId = s.StudentId,
                StudentName = s.StudentName,
                StudentSchoolNumber = s.StudentSchoolNumber,
                SchoolName = s.School.SchoolName,
                GradesId = s.GradesId,
                SchoolId = s.SchoolId,
                GradeName = s.Grades.GradeName

            })
            .FirstOrDefaultAsync();

            if (student == null)
            {
                return NotFound();
            }

            return student;
        }

        // ✅ POST: api/Student
        [HttpPost]
        public async Task<ActionResult<Student>> PostStudent(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudent), new { id = student.StudentId }, student);
        }

        // ✅ PUT: api/Student/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, Student student)
        {
            if (id != student.StudentId)
            {
                return BadRequest();
            }

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // ✅ DELETE: api/Student/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }
    }
}
