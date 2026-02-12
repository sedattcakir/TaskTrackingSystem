using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackingSystem.Data;
using TaskTrackingSystem.Models;

namespace TaskTrackingSystem.Controllers
{
    [ApiController]
    [Route("api/tasks")]

    public class TasksApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksApiController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.Tasks
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            return Ok(tasks);
        }

       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });
            return Ok(task);
        }

        

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedDate = DateTime.Now,
                Status = TaskStatusEnum.New
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFullTaskDto updated)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            if (task.Status == TaskStatusEnum.Completed && (TaskStatusEnum)updated.Status == TaskStatusEnum.InProgress)
            {
                return BadRequest(new { message = "'Tamamlanmış' bir görev tekrar 'Yapılıyor' olarak işaretlenemez." });

            }


            task.Title = updated.Title;
            task.Description = updated.Description;
            task.Status = (TaskStatusEnum)updated.Status;
            await _context.SaveChangesAsync();

            return Ok(task);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Görev silindi." });
        }
    }
}