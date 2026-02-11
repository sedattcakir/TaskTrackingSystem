Bu projeyi geliştirirken mümkün olduğunca yapay zekâdan doğrudan kod üretimi şeklinde yoğun destek almamaya özen gösterdim. Özellikle verilen Word dokümanında yer alan bazı teknik terimler ve kavramlar başlangıçta zorlayıcı oldu. ASP.NET MVC konusunda geçmişten gelen bir aşinalığım bulunmasına rağmen uzun süredir aktif olarak kullanmadığım için tekrar çalışmam gerekti. Bu süreçte hem Microsoft’un resmi dokümantasyonlarını inceleyerek hem de dil yapısını yeniden öğrenerek ilerledim. Yer yer ChatGPT’den kavramsal açıklamalar ve konu pekiştirmeye yönelik destek aldım.

Veritabanı tasarımında kullanılan Id uniqueidentifier (GUID) Primary Key, otomatik üretilir yapısını, GUID kullanımına dair bir diğer stajyer arkadaşım Sude’nin tavsiyesi üzerine tercih ettim. GUID konusunu ayrıca araştırarak projeye entegre ettim.

DTO (Data Transfer Object) yapısının neden kullanıldığı konusunu araştırarak öğrendim ve projede uyguladım. Ancak public class UpdateFullTaskDto sınıfını bireysel olarak tasarlamakta zorlandığım için bu kısmı yapay zekâdan destek alarak oluşturdum. Sonrasında yapının ne işe yaradığını ve neden gerekli olduğunu inceleyerek anlamlandırmaya çalıştım.

public class AppDbContext : DbContext yapısını da yapay zekâ desteğiyle oluşturdum. Entity Framework Core’un çalışma mantığını ve DbContext’in projedeki rolünü ayrıca araştırarak kavramaya çalıştım.

MVC Controller yapısını oluştururken Microsoft’un resmi dokümantasyon sayfasındaki örnekleri inceledim ve bu örneklerden faydalanarak kendi kod yapımı oluşturdum.

API Controller kısmında ise kodu nasıl daha iyi ve düzenli hale getirebileceğim konusunda genel iyileştirme önerileri almak amacıyla yapay zekâdan destek aldım. Önerilen yapıyı doğrudan kopyalamak yerine, araştırarak ve anlamaya çalışarak projeye entegre etmeye özen gösterdim. Bu süreçte mümkün olduğunca sınırlı ve bilinçli kullanım hedefledim.

Genel olarak projeyi geliştirirken temel amacım, hazır kod üretmekten ziyade konuları öğrenerek ve anlayarak ilerlemek oldu. Yapay zekâ ve resmi dokümantasyonları destekleyici kaynak olarak kullandım; ancak her aşamada mantığını kavramaya çalışarak uygulama yapmaya özen gösterdim.
