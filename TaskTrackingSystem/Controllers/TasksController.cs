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
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .Where(t => t.Id == id);

            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserId == userId));
            }

            var task = await query
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

            return Ok(task);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("api/tasks")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateApi([FromBody] CreateTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Başlık zorunludur." });

            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
                return BadRequest(new { message = "Seçilen proje geçersiz." });

            if (dto.UserIds != null && dto.UserIds.Any())
            {
                var existingUserIds = await _context.Users
                    .Where(u => dto.UserIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();
                if (existingUserIds.Count != dto.UserIds.Count)
                    return BadRequest(new { message = "Kullanıcılar arasında geçersiz ID'ler var." });
            }

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


        [Authorize(Roles = "Admin")]
        [HttpPut("api/tasks/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApi(Guid id, [FromBody] UpdateTaskDto dto)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (!task.TaskAssignments.Any(ta => ta.UserId == userId))
                    return StatusCode(403, new { message = "Bu göreve erişim yetkiniz yok." });
                if (task.StatusCode == 2)
                    return BadRequest(new { message = "Tamamlanmış görev güncellenemez." });
                if (dto.StatusCode < task.StatusCode)
                    return BadRequest(new { message = "Görev durumu geriye alınamaz." });
                task.StatusCode = dto.StatusCode;
                await _context.SaveChangesAsync();
                return Ok(task);
            }
            if (task.StatusCode == 2 && dto.StatusCode == 1)
                return BadRequest(new { message = "Tamamlanmış bir görevin durumunu değiştiremezsiniz." });
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
                return BadRequest(new { message = "Seçilen proje geçersiz." });
            if (dto.UserIds != null && dto.UserIds.Any())
            {
                var existingUserIds = await _context.Users
                    .Where(u => dto.UserIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();
                if (existingUserIds.Count != dto.UserIds.Count)
                    return BadRequest(new { message = "Kullanıcılar arasında geçersiz ID'ler var." });
            }
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.Title))
                errors.Add("Başlık zorunludur.");
            else if (dto.Title.Length < 5 || dto.Title.Length > 100)
                errors.Add("Başlık 5-100 karakter olmalıdır.");
            if (dto.ProjectId == Guid.Empty)
                errors.Add("Proje seçimi zorunludur.");
            if (!string.IsNullOrWhiteSpace(dto.Description) &&
                (dto.Description.Length < 10 || dto.Description.Length > 500))
                errors.Add("Açıklama 10-500 karakter olmalıdır.");
            if (dto.StartDate != null && dto.CompletionDate != null &&
                dto.StartDate > dto.CompletionDate)
                errors.Add("Başlangıç tarihi, tamamlanma tarihinden büyük olamaz.");
            if (dto.UserIds == null || !dto.UserIds.Any())
                errors.Add("En az bir kullanıcı atanmalıdır.");
            if (errors.Any())
                return BadRequest(new { message = "Form hataları mevcut.", errors });
            task.Title = dto.Title;
            task.Description = dto.Description;
            task.ProjectId = dto.ProjectId;
            task.PriorityCode = dto.Priority;
            task.StatusCode = dto.StatusCode;
            task.CompletionDate = dto.CompletionDate;
            task.StartDate = dto.StartDate;
            var oldAssignments = _context.TaskAssignments.Where(ta => ta.TaskId == id).ToList();
            _context.TaskAssignments.RemoveRange(oldAssignments);
            foreach (var uid in dto.UserIds)
            {
                _context.TaskAssignments.Add(new TaskAssignment
                {
                    TaskId = id,
                    UserId = uid,
                    CreatedTime = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("api/tasks/{id}")]
        [ValidateAntiForgeryToken]
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
        [Authorize(Roles = "Admin")]
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

        [Authorize("Admin")]
        [HttpPost("api/users")]
        [ValidateAntiForgeryToken]
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

            var passwordError = AccountController.ValidatePassword(dto.Password);
            if (passwordError != null)
                return BadRequest(new { message = passwordError });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = (dto.Role == "Admin" && IsAdmin()) ? "Admin" : "Personel",
                ProfileImage = dto.ProfileImage,
                CreatedTime = DateTime.Now
            };

            if (!string.IsNullOrEmpty(dto.ProfileImage))
            {
                if (!dto.ProfileImage.StartsWith("data:image/png;base64,") &&
                    !dto.ProfileImage.StartsWith("data:image/jpeg;base64,") &&
                    !dto.ProfileImage.StartsWith("data:image/jpg;base64,"))
                {
                    return BadRequest(new { message = "Geçersiz profil resmi formatı. Sadece PNG, JPEG ve JPG kabul edilir." });
                }
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);

        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("api/users/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (id == currentUserId)
                return BadRequest(new { message = "Kendi hesabınızı silemezsiniz." });

            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            var targetUser = await _context.Users.FindAsync(id);
            if (targetUser != null && targetUser.Role == "Admin" && adminCount <= 1)
                return BadRequest(new { message = "Sistemdeki son admin silinemez." });

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
        {
            if (string.IsNullOrEmpty(dto.Title))
                return BadRequest(new { message = "Proje başlığı zorunludur." });

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                CreatedTime = DateTime.Now
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return Ok(project);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("api/projects/{id}")]
        [ValidateAntiForgeryToken]
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

        [Authorize(Roles = "Admin")]
        [HttpPut("api/tasks/{id}/assignments")]
        [ValidateAntiForgeryToken]
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

        [HttpGet("api/auditlogs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.CreatedTime)
                .Take(200)
                .Select(l => new
                {
                    l.Id,
                    UserEmail = l.UserEmail ?? "",
                    Action = l.Action ?? "",
                    IpAddress = l.IpAddress ?? "",
                    CreatedTime = (DateTime?)l.CreatedTime
                })
                .ToListAsync();

            return Ok(logs);
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