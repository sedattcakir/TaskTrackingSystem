A- Proje Nasıl Ayağa Kaldırılır?

  1- Proje GitHub’dan klonlandıktan sonra .sln uzantılı solution dosyası Visual Studio ile açılır.

  2- Visual Studio açıldıktan sonra üst menüden Build → Build Solution seçilerek projenin hatasız şekilde derlendiği kontrol edilir.

  3- Veritabanının oluşturulması için üst menüden Tools → NuGet Package Manager → Package Manager Console açılır.

  4- Açılan konsolda aşağıdaki komut çalıştırılır:

		Update-Database

		Bu işlem Entity Framework Core migration’larını çalıştırır ve veritabanını oluşturur.

  5- Migration daha önce oluşturulmadıysa önce şu komut çalıştırılır:

		Add-Migration InitialCreate

		Ardından tekrar:

		Update-Database

  6- Veritabanı oluşturulduktan sonra Visual Studio üzerinde bulunan Start (▶) butonuna basılarak uygulama çalıştırılır.

  7- Uygulama varsayılan olarak https://localhost adresinde ayağa kalkar ve tarayıcı üzerinden erişilebilir.
  
  
  
B- Hangi Versiyonlar Gerekir?

	Projeyi sorunsuz şekilde çalıştırabilmek için aşağıdaki yazılım ve sürümlerin yüklü olması gerekmektedir:

		* Visual Studio 2022 ve üst sürümleri.

		* .NET SDK (.NET 10)

		* SQL Server veya SQL Server LocalDB

		* Entity Framework Core (projede kullanılan sürüm, .NET sürümü ile uyumlu olmalıdır)

		* .NET sürümünü kontrol etmek için komut satırına şu komut yazılabilir:

			dotnet --version


C- Migration Nasıl Çalıştırılır?

	* Proje içerisinde veritabanı işlemleri Entity Framework Core Migration yapısı ile yönetilmektedir. Migration işlemleri Visual Studio üzerinden Package Manager Console kullanılarak gerçekleştirilir.

	* Öncelikle Visual Studio’da üst menüden Tools → NuGet Package Manager → Package Manager Console açılır.

	* Eğer projede daha önce migration oluşturulmadıysa, aşağıdaki komut çalıştırılarak ilk migration oluşturulur:

		Add-Migration InitialCreate

	 Bu işlem mevcut model yapısına göre migration dosyasını oluşturur.

	* Ardından veritabanını oluşturmak ve migration’ı uygulamak için şu komut çalıştırılır:

		Update-Database

	 Bu komut, oluşturulan migration’ı veritabanına uygular ve tabloları oluşturur.

	* Eğer projede zaten migration dosyaları mevcutsa, sadece:

		Update-Database

	 komutunu çalıştırmak yeterlidir.

	* Bu işlemler tamamlandıktan sonra veritabanı hazır hale gelir.
	
	
D- API Endpoint’leri Nelerdir?

	* Proje içerisinde görev yönetimi işlemlerini gerçekleştiren API endpoint’leri bulunmaktadır. Bu endpoint’ler temel CRUD (Create, Read, Update, Delete) işlemlerini kapsar ve /api/tasks temel yolu altında çalışır.

	* Aşağıdaki işlemler desteklenmektedir:

		1. Görevleri Listeleme
		 * GET /api/tasks
		 * Sistemde kayıtlı tüm görevleri listeler.

		2. Belirli Bir Görevi Getirme
		 * GET /api/tasks/{id}
		 * Belirtilen id değerine sahip görevin detay bilgilerini getirir.

		3. Yeni Görev Oluşturma
		 * POST /api/tasks
		 * Yeni bir görev kaydı oluşturur.

		4. Görev Güncelleme
		 * PUT /api/tasks/{id}
		 * Belirtilen id’ye sahip görevin bilgilerini günceller.

		5. Görev Silme
		 * DELETE /api/tasks/{id}
		 * Belirtilen id’ye sahip görevi sistemden siler.

	*Bu endpoint’ler HTTP metodlarına göre ayrılmış olup REST prensiplerine uygun şekilde tasarlanmıştır.
	
	
	
	
E- Örnek Request / Response Nedir?

	Aşağıda API üzerinde en sık kullanılan işlemler için örnek istek ve cevap yapıları gösterilmiştir.

		1. Tüm Görevleri Listeleme

			Endpoint:
			GET /api/tasks

			Başarılı Response – 200 OK

			[
			  {
				"id": "8f3b2c8d-5b9c-4b92-a1c4-2f8e1a2b3c4d",
				"title": "Test Görevi",
				"description": "Açıklama",
				"createdDate": "2026-02-12T14:30:00",
				"status": 0
			  }
			]

	2. ID’ye Göre Görev Getirme

		Endpoint:
		GET /api/tasks/{id}

		   Başarılı Response – 200 OK

			{
			  "id": "8f3b2c8d-5b9c-4b92-a1c4-2f8e1a2b3c4d",
			  "title": "Test Görevi",
			  "description": "Açıklama",
			  "createdDate": "2026-02-12T14:30:00",
			  "status": 0
			}


		   Bulunamazsa – 404 Not Found

			{
			  "message": "Görev bulunamadı."
			}

	3. Yeni Görev Oluşturma

		Endpoint:
		POST /api/tasks

		  Request Body:

			{
			  "title": "Yeni Görev",
			  "description": "Bu bir örnek görevdir."
			}


		  Başarılı Response – 201 Created

			{
			  "id": "c1a2b3d4-e5f6-7890-abcd-123456789abc",
			  "title": "Yeni Görev",
			  "description": "Bu bir örnek görevdir.",
			  "createdDate": "2026-02-12T15:00:00",
			  "status": 0
			}


		Not: Status alanı varsayılan olarak New (0) olarak oluşturulur.

	4. Görev Güncelleme

		Endpoint:
		PUT /api/tasks/{id}

		Request Body:

			{
			  "title": "Güncellenmiş Görev",
			  "description": "Yeni açıklama",
			  "status": 1
			}


		Başarılı Response – 200 OK

			{
			  "id": "c1a2b3d4-e5f6-7890-abcd-123456789abc",
			  "title": "Güncellenmiş Görev",
			  "description": "Yeni açıklama",
			  "createdDate": "2026-02-12T15:00:00",
			  "status": 1
			}


		Eğer tamamlanmış bir görev tekrar "Yapılıyor" yapılmak istenirse:

			400 Bad Request

			{
			  "message": "'Tamamlanmış' bir görev tekrar 'Yapılıyor' olarak işaretlenemez."
			}

	5. Görev Silme

		Endpoint:
		DELETE /api/tasks/{id}

		Başarılı Response – 200 OK

			{
			  "message": "Görev silindi."
			}


		Bulunamazsa – 404 Not Found

			{
			  "message": "Görev bulunamadı."
			}


F- Proje Yapısı Nasıl Organize?

	Proje ASP.NET Core Web API mimarisine uygun şekilde katmanlı ve düzenli bir klasör yapısı ile organize edilmiştir. Her klasörün belirli bir sorumluluğu bulunmaktadır.

	Controllers
		API endpoint’lerinin bulunduğu katmandır. HTTP isteklerini karşılar ve gerekli işlemleri yaparak uygun HTTP cevaplarını döner.
		Örneğin: TasksApiController görevlerle ilgili tüm CRUD işlemlerini yönetir.

	Models
		Veritabanı tablolarını temsil eden entity sınıfları bu klasörde yer alır.
		Örneğin: TaskItem modeli görev bilgilerini (Id, Title, Description, Status, CreatedDate vb.) içerir.
		Ayrıca TaskStatusEnum gibi enum yapıları da burada bulunur.

	Data
		Veritabanı bağlantısı ve Entity Framework Core yapılandırmaları bu klasörde yer alır.
		AppDbContext sınıfı, veritabanı tablolarını temsil eden DbSet’leri içerir ve EF Core ile veritabanı arasındaki bağlantıyı sağlar.

	DTO (Data Transfer Objects)
		API’ye gelen ve API’den dönen veri modellerini temsil eder.
		Örneğin:

		CreateTaskDto → Yeni görev oluştururken kullanılır.

		UpdateFullTaskDto → Görev güncellerken kullanılır.

		Bu yapı sayesinde entity modelleri ile dış dünyaya açılan veri modelleri birbirinden ayrılmış olur.

	Migrations
		Entity Framework Core tarafından oluşturulan migration dosyaları bu klasörde yer alır. Veritabanı şema değişiklikleri burada tutulur.

	Program.cs
		Uygulamanın başlangıç noktasıdır. Servis kayıtları ve middleware yapılandırmaları burada yapılır.