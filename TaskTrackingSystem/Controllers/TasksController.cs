using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackingSystem.Data;
using TaskTrackingSystem.Models;

namespace TaskTrackingSystem.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        private string GetCurrentRole() {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        private bool IsAdmin() {
            return GetCurrentRole() == "Admin";
        }


        [HttpGet("api/tasks")]
        public async Task<IActionResult> GetTasks()
        {
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .AsQueryable();
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserId == userId));
            }

            var tasks = await query
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
                    ProjectTitle = t.Project!.Title,
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

            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (!task.AssignedUsers.Any(u => u.Id == userId))
                    return StatusCode(403, new { message = "Bu göreve erişim yetkiniz yok." });
            }

            return Ok(task);
        }

        [HttpPost("api/tasks")]
        [Authorize(Roles = "Admin")]
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
                CreatedTime = DateTime.Now
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
            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new { message = "Görev bulunamadı." });
            }

            if(!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (!task.TaskAssignments.Any(ta => ta.UserId == userId))
                    return StatusCode(403, new { message = "Bu göreve erişim yetkiniz yok." });
                task.StatusCode = dto.StatusCode;
                await _context.SaveChangesAsync();
                return Ok(task);
            }

            if(task.StatusCode == 2 && dto.StatusCode == 1)
            {
                return BadRequest(new { message = "Tamamlanmış bir görevin durumunu değiştiremezsiniz." });
            }

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.ProjectId = dto.ProjectId;
            task.PriorityCode = dto.Priority;
            task.StatusCode = dto.StatusCode;
            task.CompletionDate = dto.CompletionDate;
            task.StartDate = dto.StartDate;

            var oldAssignments = _context.TaskAssignments.Where(ta => ta.TaskId == id).ToList();
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
            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpDelete("api/tasks/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteApi(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });
            var assignments = _context.TaskAssignments.Where(ta => ta.TaskId == id);
            _context.TaskAssignments.RemoveRange(assignments);
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Görev silindi." });
        }

        [HttpGet("api/users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.ProfileImage,
                    u.Role,
                    u.CreatedTime
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("api/users")]
        [Authorize("Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Kullanıcı adı zorunludur." });

            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email zorunludur." });

            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Parola zorunludur." });

            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
            {
                return BadRequest(new { message = "Bu email zaten kullanılıyor." });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role ?? "Personel",
                ProfileImage = dto.ProfileImage,
                CreatedTime = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);

        }

        [HttpDelete("api/users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _context.TaskAssignments
                .Where(ta => ta.UserId == id)
                .ExecuteDeleteAsync();

            var deleted = await _context.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();

            if (deleted == 0)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(new { message = "Kullanıcı silindi." });
        }
        [HttpGet("api/projects")]
        public async Task<IActionResult> GetProjects()
        {
            var projects = await _context.Projects.OrderByDescending(p => p.CreatedTime).ToListAsync();
            return Ok(projects);
        }

        [HttpPost("api/projects")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        [HttpGet("api/me")]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                Id = GetCurrentUserId(),
                Name = User.FindFirst(ClaimTypes.Name)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Role = GetCurrentRole()
            });
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