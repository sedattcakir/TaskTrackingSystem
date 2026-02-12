using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackingSystem.Controllers;
using TaskTrackingSystem.Data;
using TaskTrackingSystem.Models;
using Xunit;

namespace TaskTrackingSystem.Tests
{
    public class TasksApiControllerTests
    {
        private AppDbContext GetInMemoryDbContext() // InMemory kullanarak başka bir veritabanı oluşturup
                                                    // controller çalışırken ana veritabanına karışmasını engellemek için bu yöntem kullanılır. 
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb") // Ram üzerinde geçici bir TestDb veritabanı oluşturur.
                .Options;

            return new AppDbContext(options);
        }



        // Test metodu oluşturup. TaskApiController içerisinde bulunan Create işleminin başarılı olduğunu kontrol edeceğiz.

        [Fact]
        public async Task Create_Task_Should_Return_Created() 
        {// Task Görev oluştuma işlemi oluşturuldu durumunu döndermesini kontrol eder.
         // İsimlendirmenin böyle olmasının sebebi de Medium platformunda okuduğum yazıya göre
         // [MethodName_StateUnderTest_ExpectedBehavior] böyle olduğu için bunu kullandım.
            
            var context = GetInMemoryDbContext(); // Test için in-memory veritabanı oluştururuz.
            var controller = new TasksApiController(context); // Controller'ı oluştururken in-memory veritabanını kullanarak bir AppDbContext örneği oluştururuz.

            var dto = new CreateTaskDto
            {
                Title = "Test Başlık",
                Description = "Test Açıklama"
            };

            var result = await controller.Create(dto); // Controller'ın Create metodunu çağırarak yeni bir görev oluştururuz ve sonucunu result değişkenine atarız.

            var createdResult = Assert.IsType<CreatedAtActionResult>(result); // Sonucun CreatedAtActionResult türünde olduğunu doğrular. Yani 201 Created döndermesini sağlar.
            var task = Assert.IsType<TaskItem>(createdResult.Value);

            Assert.Equal("Test Başlık", task.Title);
            Assert.Equal(TaskStatusEnum.New, task.Status);
        }


        // ----------------------------------------------------------------------------------------------------------------------------------


        [Fact]
        public async Task Completed_Task_Cannot_Be_Set_To_InProgress() // Tamamlanan görev yeni güncellemede devam ediyor durumuna gelemez.
        {
            var context = GetInMemoryDbContext(); // veritabanı oluşturulur.

            var task = new TaskItem // Tamamlanmış bir görev oluşturulur.
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                Description = "Test",
                Status = TaskStatusEnum.Completed
            };

            context.Tasks.Add(task); // Veritabanına gönderilir.
            await context.SaveChangesAsync(); // Veritabanına kaydedilir.

            var controller = new TasksApiController(context); //

            var updateDto = new UpdateFullTaskDto // Güncelleme işlemi yapılır ve Tamamlanmış görevi Devam ediyor durumuna getirmeye çalışılır.
            {
                Title = "Updated",
                Description = "Updated",
                Status = (int)TaskStatusEnum.InProgress
            };

            var result = await controller.Update(task.Id, updateDto); // Güncelleme işlemi yapılır.

            var badRequest = Assert.IsType<BadRequestObjectResult>(result); // Sonucun BadRequest olduğunu doğrular. Yani 400 döndermesini sağlar.

            Assert.Equal(400, badRequest.StatusCode); // Status kodunun 400 olduğunu doğrular. Yani güncelleme işleminin başarısız olduğunu gösterir.
        }


        // ----------------------------------------------------------------------------------------------------------------------------------

        [Fact]

        public async Task Created_Task_The_Deleting() // Oluşturulan görevin silinmesi işlemi yapılır ve başarılı olduğunu kontrol eder.
        {
            var context = GetInMemoryDbContext(); // veritabanı oluşturulur.

            var task = new TaskItem // Yeni bir görev oluşturulur.
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                Description = "Test",
                Status = TaskStatusEnum.New
            };
            context.Tasks.Add(task); // Veritabanına gönderilir.
            await context.SaveChangesAsync(); // Veritabanına kaydedilir.

            var controller = new TasksApiController(context); // Controller oluşturulur.
            var result = await controller.Delete(task.Id); // Silme işlemi yapılır.

            var okResult = Assert.IsType<OkObjectResult>(result); // Sonucun OkObjectResult türünde olduğunu doğrular. Yani 200 OK döndermesini sağlar.
                                                                  // Silme işleminin başarılı olduğunu gösterir.
            Assert.NotNull(okResult.Value); // Silinen görevin bilgisi olduğu için null dönmez bu da silme işleminin başarılı olduğunu gösterir.

        }

        // ---------------------------------------------------------------------------------------------------------------------------------- 


        // Burada kasıtlı olarak başarısızlık oluşturuldu bunun da sebebi veritabanına başlıksız ekleme yapılıyor ve bunun da başarılı olması beklendi.
        // Fakat kasıtlı olarak bunun başarısız yani oluşturulduğunda bize hata verdiğini testte de göstermek istedim.
        [Fact]

        public async Task AddDefinition_EmptyDefinitionProvided_ShouldThrowException()
        {
            var context = GetInMemoryDbContext(); // veritabanı oluşturulur.

            var task = new TaskItem // Yeni bir görev oluşturulur.
            {
                Id = Guid.NewGuid(),
                Title = "",
                Description = "Test",
                Status = TaskStatusEnum.New
            };
            context.Tasks.Add(task); // Veritabanına gönderilir.
            await context.SaveChangesAsync(); // Veritabanına kaydedilir.

            var controller = new TasksApiController(context); // Controller oluşturulur.
            var result = await controller.Create(new CreateTaskDto { Title = "", Description = "Test" }); // Boş başlıkla görev oluşturma işlemi yapılır.
            Assert.IsType<NoContentResult>(result); // Sonucun NoContentResult türünde olduğunu doğrular. Yani 204 No Content döndermesini sağlar. Boş başlıkla görev oluşturulamaz.
        }


    }
}
