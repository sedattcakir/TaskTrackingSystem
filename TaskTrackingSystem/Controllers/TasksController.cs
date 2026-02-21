using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackingSystem.Data;
using TaskTrackingSystem.Models;

namespace TaskTrackingSystem.Controllers
{
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("api/tasks")]
        public async Task<IActionResult> GetTasks()
        {
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .OrderByDescending(t => t.CreatedTime)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.StatusCode,
                    Priority = t.PriorityCode,
                    t.CompletionDate,
                    t.StartDate,
                    t.CreatedTime,
                    t.ProjectId,
                    ProjectTitle = t.Project!.Title,
                    AssignedUsers = t.TaskAssignments.Select(ta => new
                    {
                        ta.User.Id,
                        ta.User.Name,
                        ta.User.ProfileImage
                    }).ToList()
                })
                .ToListAsync();

            return Ok(tasks);
        }
        
        [HttpGet("api/tasks/{id}")]
        public async Task<IActionResult> GetTask(Guid id)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.StatusCode,
                    Priority = t.PriorityCode,
                    t.CompletionDate,
                    t.StartDate,
                    t.CreatedTime,
                    t.ProjectId,
                    ProjectTitle = t.Project.Title,
                    AssignedUsers = t.TaskAssignments.Select(ta => new
                    {
                        ta.User.Id,
                        ta.User.Name,
                        ta.User.ProfileImage
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            return Ok(task);
        }

        [HttpPost("api/tasks")]
        public async Task<IActionResult> CreateApi([FromBody] CreateTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Başlık zorunludur." });

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                ProjectId = dto.ProjectId,
                PriorityCode = dto.Priority,
                StatusCode = 0,
                CompletionDate = dto.CompletionDate,
                StartDate = dto.StartDate,
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            if (dto.UserIds != null && dto.UserIds.Any())
            {
                foreach (var userId in dto.UserIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = task.Id,
                        UserId = userId,
                        CreatedTime = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
        }

        [HttpPut("api/tasks/{id}")]
        public async Task<IActionResult> EditApi(Guid id, [FromBody] UpdateTaskDto dto)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            if (task.StatusCode == 2 && dto.StatusCode == 1)
                return BadRequest(new { message = "Tamamlanmış görev tekrar 'Yapılıyor' durumuna alınamaz." });

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.StatusCode = dto.StatusCode;
            task.PriorityCode = dto.Priority;
            task.CompletionDate = dto.CompletionDate;
            task.StartDate = dto.StartDate;

            var oldAssignments = _context.TaskAssignments
                .Where(ta => ta.TaskId == id)
                .ToList();

            _context.TaskAssignments.RemoveRange(oldAssignments);

            if (dto.UserIds != null && dto.UserIds.Any())
            {
                foreach (var userId in dto.UserIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = id,
                        UserId = userId,
                        CreatedTime = DateTime.Now
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpDelete("api/tasks/{id}")]
        public async Task<IActionResult> DeleteApi(Guid id)
        {
            try
            {
                var task = new TaskItem { Id = id };

                _context.Tasks.Attach(task);
                _context.Tasks.Remove(task);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Atamalar ve görevler silinmiştir." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("api/users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.OrderByDescending(u => u.Name).ToListAsync();
            return Ok(users);
        }

        [HttpPost("api/users")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
                return BadRequest(new { message = "Kullanıcı adı zorunludur." });

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = "Email zorunludur." });

            user.Id = Guid.NewGuid();
            user.CreatedTime = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }


        [HttpDelete("api/users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            var assignments = _context.TaskAssignments.Where(ta => ta.UserId == id);
            _context.TaskAssignments.RemoveRange(assignments);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Kullanıcı silindi." });
        }

        [HttpGet("api/projects")]
        public async Task<IActionResult> GetProjects()
        {
            var projects = await _context.Projects.OrderByDescending(p => p.Title).ToListAsync();
            return Ok(projects);
        }

        [HttpPost("api/projects")]
        public async Task<IActionResult> CreateProject([FromBody] Project project)
        {
            if (string.IsNullOrWhiteSpace(project.Title))
                return BadRequest(new { message = "Proje başlığı zorunludur." });

            project.Id = Guid.NewGuid();
            project.CreatedTime = DateTime.Now;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return Ok(project);
        }

        [HttpDelete("api/projects/{id}")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { message = "Proje bulunamadı." });

            var hasTasks = await _context.Tasks.AnyAsync(t => t.ProjectId == id);
            if (hasTasks)
                return BadRequest(new { message = "Bu projeye bağlı görevler var. Önce görevleri silin." });

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Proje silindi." });
        }

        [HttpGet("api/statuses")]
        public async Task<IActionResult> GetStatuses()
        {
            var statuses = await _context.StatusTypes.OrderBy(s => s.Code).ToListAsync();
            return Ok(statuses);
        }

        [HttpGet("api/priorities")]
        public async Task<IActionResult> GetPriorities()
        {
            var priorities = await _context.PriorityTypes.OrderBy(p => p.Code).ToListAsync();
            return Ok(priorities);
        }

        [HttpPut("api/tasks/{id}/assignments")]
        public async Task<IActionResult> UpdateAssignments(Guid id, [FromBody] List<Guid> userIds)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            var oldAssignments = await _context.TaskAssignments
                .Where(ta => ta.TaskId == id)
                .ToListAsync();

            _context.TaskAssignments.RemoveRange(oldAssignments);
            if (userIds != null && userIds.Any())
            {
                foreach (var userId in userIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = id,
                        UserId = userId,
                        CreatedTime = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Atamalar güncellendi." });
        }


        public async Task<IActionResult> Index()
        {
            var tasks = await _context.Tasks
                .OrderByDescending(t => t.CreatedTime)
                .ToListAsync();
            return View(tasks);
        }
    }
}