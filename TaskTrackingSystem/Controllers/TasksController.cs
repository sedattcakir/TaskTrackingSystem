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

     
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.Tasks
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            return View(tasks);
        }

       
        public IActionResult Create()
        {
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem task)
        {
            if (ModelState.IsValid)
            {
                task.CreatedDate = DateTime.Now;
                task.Status = TaskStatusEnum.Bekliyor;
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

      
        public async Task<IActionResult> UpdateStatus(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();
            return View(task);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, string title, string description, TaskStatusEnum status)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("Title", "Başlık zorunludur.");
                return View(task);
            }

            task.Title = title;
            task.Description = description;
            task.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}