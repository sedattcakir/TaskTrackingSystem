Roller ve Yetkiler

Admin:

* Tüm görevleri görüntüleyebilir

* Görev ekleyebilir, düzenleyebilir ve silebilir

* Görev içeriğinde tüm alanları düzenleyebilir

* Projelere ekleme ve silme işlemi yapabilir

* Tüm projeleri görüntüleyebilir

* Kullanıcı ekleyebilir ve silebilir

* Tüm kullanıcıları görüntüleyebilir

Personel:

* Sadece kendisine atanan görevleri görüntüleyebilir

* Görevlerde yalnızca durum alanını güncelleyebilir

* Görev içeriğinde başka değişiklik yapamaz

* Tüm projeleri görüntüleyebilir ancak işlem yapamaz


Güvenlik ve Kimlik Doğrulama:

* Kullanıcı oluşturma sırasında girilen şifre API’ye düz metin olarak gelir. Controller, işlemi yapan kullanıcının Admin rolünde olup olmadığını kontrol eder.

* Kontrol sağlandıktan sonra şifre BCrypt kütüphanesi ile hash’lenir. BCrypt her şifre için rastgele bir salt üretir ve hash içine gömer. Böylece aynı şifreyi kullanan kullanıcıların veritabanındaki hash değerleri farklı olur.

Login Akışı:

* Kullanıcı giriş yaparken email ve şifre bilgileri alınır. Girilen şifre veritabanındaki hash ile karşılaştırılır. Doğrulama başarılı olursa kullanıcı bilgilerini içeren bir Claim listesi oluşturulur:

        *Kullanıcı ID

        *Kullanıcı Adı

        *Email

        *Rol (Admin / Personel)

*Bu bilgiler şifreli bir cookie içinde saklanır ve her istekte sunucuya otomatik gönderilir.

*Sunucu cookie’yi çözerek Claim bilgilerini okur ve yetkilendirme kontrollerini uygular:

*[Authorize] → Kullanıcı giriş yapmış mı kontrol eder

*[Authorize(Roles)] → Kullanıcının rolüne göre erişim sağlar

*Çıkış yapıldığında cookie silinir ve oturum sonlandırılır.