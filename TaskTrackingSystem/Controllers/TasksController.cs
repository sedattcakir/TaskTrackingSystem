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

     
        public async Task<IActionResult> Index() // Görevleri listeleme
        {
            var tasks = await _context.Tasks 
                .OrderByDescending(t => t.CreatedDate)  // Görevleri oluşturulma tarihine göre sıralama
                .ToListAsync(); 
            return View(tasks); // Görevleri Index'e görünümüne gönderme
        }

       
        public IActionResult Create() 
        {
            return View();
        }

       
        [HttpPost] // Yeni görev oluşturma
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma
        public async Task<IActionResult> Create(TaskItem task) 
        {
            if (ModelState.IsValid) 
            {

      
        public async Task<IActionResult> UpdateStatus(Guid id) 
        {
            var task = await _context.Tasks.FindAsync(id); 
            if (task == null)
                return NotFound();
            return View(task);
        }

        [HttpPost] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, string title) 
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
            if (task.Status == TaskStatusEnum.Completed && status == TaskStatusEnum.InProgress) // Tamamlanmış bir görev tekrar 'Yapılıyor' durumuna alınamaz.
            {
                TempData["Error"] = "Tamamlanmış görev tekrar 'Yapılıyor.' durumuna alınamaz.";
                return RedirectToAction(nameof(Index), new { editId = id });
            }
            
            await _context.SaveChangesAsync(); 
            return RedirectToAction(nameof(Index)); 
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(TaskItem)
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