# Değerlendirme Sonuçları — mod: `llm` (`claude-opus-5-5`)

Çalıştırma zamanı: 2026-09-30 12:26 · Çağrı yolu: http://localhost:5081 (.NET API → FastAPI LLM servisi)

## Özet

| Tür | Başarılı / Toplam |
|---|---|
| Normal | 6 / 6 |
| Cevapsız | 4 / 4 |
| Çelişkili | 7 / 7 |
| **Toplam** | **17 / 17** |

## Karşılaştırma

| # | Tür | Soru | Beklenen | Gerçek yanıt | Kaynak (doküman › bölüm) | Sürüm/çelişki kararı | Sonuç |
|---|---|---|---|---|---|---|---|
| N1 | Normal | Nova Termo 5 GHz Wi-Fi ağlarına bağlanabiliyor mu? | Hayır; yalnızca 2.4 GHz ağlar desteklenir. | Hayır, Nova Termo 5 GHz Wi-Fi ağlarını desteklemez; kurulum ve bağlantı için 2.4 GHz Wi-Fi ağı gereklidir. Cihaz ağa bağlanmıyorsa öncelikle ağın 2.4 GHz olduğundan emin olunmalıdır. | nova-termo-kurulum v1.2 › Ön Koşullar<br>sorun-giderme v1.1 › Cihaz Wi-Fi Ağına Bağlanmıyor | — | ✅ |
| N2 | Normal | Termostatı fabrika ayarlarına nasıl sıfırlarım? | Yan düğmeye 10 saniye basılı tutulur; ekran turuncu yanıp sönünce bırakılır. | Nova Termo'yu fabrika ayarlarına sıfırlamak için cihazın yan tarafındaki düğmeye 10 saniye basılı tutun ve ekran turuncu renkte yanıp sönmeye başladığında düğmeyi bırakın. Sıfırlama sonrasında tüm programlar ve Wi-Fi ayarları silinir. | sorun-giderme v1.1 › Fabrika Ayarlarına Sıfırlama | — | ✅ |
| N3 | Normal | Cihazların garanti süresi ne kadar? | Fatura tarihinden itibaren 2 yıl (Nova Plus abonelerinde 3 yıl). | Tüm Nova cihazları fatura tarihinden itibaren 2 yıl garantilidir. Nova Plus aboneleri için bu süre, abonelik devam ettiği sürece 3 yıla uzatılır. | garanti-kosullari v1.0 › Garanti Süresi | — | ✅ |
| N4 | Normal | Nova Plus aboneliğimi iptal edersem kalan süre için para iadesi alabilir miyim? | Hayır; iptal dönem sonunda geçerli olur, kısmi ücret iadesi yapılmaz. | Hayır, Nova Plus aboneliğini iptal ettiğinizde kalan süre için kısmi ücret iadesi yapılmaz. İptal, "Profil > Abonelik" menüsünden istediğiniz zaman yapılabilir ve mevcut ödeme döneminin sonunda geçerli olur; bu nedenle dönem sonuna kadar abonelik avantajlarından yararlanmaya devam edersiniz. | nova-plus-abonelik v1.0 › Abonelik İptali | — | ✅ |
| N5 | Normal | Kaç TL üzeri siparişlerde kargo ücretsiz? | 500 TL ve üzeri siparişlerde. | 500 TL ve üzeri siparişlerde kargo ücretsizdir. | kargo-teslimat v1.0 › Kargo Ücreti | — | ✅ |
| N6 | Normal | Şifre sıfırlama bağlantısı ne kadar süre geçerli? | 30 dakika. | Şifre sıfırlama bağlantısı, gönderildikten sonra 30 dakika geçerlidir. Bağlantıyı almak için giriş ekranındaki "Şifremi Unuttum" bağlantısına tıklayıp kayıtlı e-posta adresinizi girmeniz yeterlidir. | hesap-uyelik v1.0 › Şifre Sıfırlama | — | ✅ |
| U1 | Cevapsız | Nova Termo, Amazon Alexa ile uyumlu mu? | Dokümanlarda bilgi yok. | Nova Termo'nun Amazon Alexa ile uyumlu olup olmadığına dair bilgi dokümanlarda bulunmamaktadır. | — | — | ✅ |
| U2 | Cevapsız | Yurt dışına gönderim yapıyor musunuz? | Dokümanlarda bilgi yok. | Yurt dışına gönderim yapılıp yapılmadığına dair bilgi dokümanlarda bulunmamaktadır. | — | — | ✅ |
| U3 | Cevapsız | Nova Termo'nun satış fiyatı nedir? | Dokümanlarda bilgi yok (yalnızca Nova Plus abonelik ücreti var). | Nova Termo'nun satış fiyatına ilişkin bilgi sağlanan dokümanlarda bulunmamaktadır. | — | — | ✅ |
| U4 | Cevapsız | Siparişlerde kapıda ödeme seçeneği var mı? | Dokümanlarda bilgi yok (yalnızca abonelik ödeme yöntemleri var). | Siparişlerde kapıda ödeme seçeneği olup olmadığına dair bilgi dokümanlarda bulunmamaktadır. | — | — | ✅ |
| C1 | Çelişkili | Ürünü kaç gün içinde iade edebilirim? | 30 gün (v1'deki 14 gün geçersiz). | Ürünü teslim aldığınız tarihten itibaren 30 gün içinde iade talebinde bulunabilirsiniz. Bu süre kampanyalı ürünler için de aynıdır. | iade-politikasi-v2 v2.0 › İade Süresi | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi | ✅ |
| C2 | Çelişkili | Çağrı merkezi cumartesi günleri açık mı? | Evet, cumartesi 10:00-16:00 (v1'de hafta sonu kapalı). | Evet, çağrı merkezimiz (0850 123 45 67) cumartesi günleri 10:00-16:00 saatleri arasında hizmet vermektedir. Pazar günleri ise telefon desteği bulunmamaktadır. | destek-kanallari-v2 v2.0 › Telefon Desteği | destek-kanallari-v2 seçildi; destek-kanallari-v1 reddedildi | ✅ |
| C3 | Çelişkili | Canlı sohbet desteğiniz var mı? | Evet, 7 gün 24 saat (v1'de sunulmuyordu). | Evet, Nova uygulaması ve web sitesi üzerinden canlı sohbet desteğimiz 7 gün 24 saat hizmet vermektedir. Mesai saatleri dışında ilk yanıtı yapay zekâ asistanı verir; gerekirse talep bir temsilciye aktarılır. | destek-kanallari-v2 v2.0 › Canlı Sohbet | destek-kanallari-v2 seçildi; destek-kanallari-v1 reddedildi | ✅ |
| C4 | Çelişkili | Mağazadan satın aldığım ürünü iade edebilir miyim? | Evet; v2 mağaza satışlarını kapsıyor (v1'de kapsam dışıydı). | Evet, iade ve değişim politikası mağazalardan satın alınan ürünler için de geçerlidir. Ürünü teslim aldığınız tarihten itibaren 30 gün içinde iade talebinde bulunabilirsiniz. Talep, Nova uygulamasında "Siparişlerim > İade Talebi Oluştur" menüsünden açılır ve ürün, orijinal kutusu ve aksesuarlarıyla birlikte sistemin ürettiği iade koduyla HızlıKargo şubesine teslim edilir. | iade-politikasi-v2 v2.0 › Kapsam<br>iade-politikasi-v2 v2.0 › İade Süresi<br>iade-politikasi-v2 v2.0 › İade Adımları | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi | ✅ |
| C5 | Çelişkili | Para iadesi kaç iş gününde yapılır? | Ürün depoya ulaştıktan sonra 5 iş günü (v1'de 14). | Onaylanan iadelerde ücret, ürün depoya ulaştıktan sonra 5 iş günü içinde ödemenin yapıldığı karta iade edilir. Bankaya bağlı olarak tutarın hesaba yansıması 3 iş günü daha sürebilir. | iade-politikasi-v2 v2.0 › Para İadesi | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi | ✅ |
| C6 | Çelişkili | Destek e-postalarına kaç günde yanıt veriliyor? | En geç 1 iş günü (v1'de 3). | destek@novaev.com.tr adresine gönderilen e-postalar en geç 1 iş günü içinde yanıtlanır. | destek-kanallari-v2 v2.0 › E-posta Desteği | destek-kanallari-v2 seçildi; destek-kanallari-v1 reddedildi | ✅ |
| C7 | Çelişkili | Ürün iadesinde kargo ücretini kim ödüyor? | HızlıKargo ile ücretsiz; iade v1 ve 2024 tarihli Kargo dokümanındaki 'müşteriye aittir' ifadesi eski. | Anlaşmalı kargo firması HızlıKargo ile yapılan iade gönderimleri ücretsizdir, yani kargo ücretini müşteri ödemez. Müşteri iadeyi farklı bir kargo firmasıyla gönderirse kargo ücreti müşteriye aittir. | iade-politikasi-v2 v2.0 › İade Kargo Ücreti | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi<br>iade-politikasi-v2 > kargo-teslimat (İade gönderimlerinde kargo ücreti) | ✅ |

## Kontroller

- **yanıtlanabilirlik**: sistemin yanıt verip vermeme kararı beklenenle aynı mı?
- **doğru_kaynak**: beklenen (güncel) doküman kaynaklar arasında mı?
- **eski_sürüm_kullanılmadı**: yanıtta yürürlükten kalkmış bir sürüm kaynak olarak gösterilmedi mi?
- **anahtar_bilgi**: yanıtta beklenen kritik bilgi (ör. "30 gün") geçiyor mu?
- **eski_bilgi_sızmadı**: yanıtta geçersiz kalmış bir bilgi (ör. eski kargo ücreti kuralı) yer almıyor mu?
- **çelişki_gösterildi**: reddedilen eski kaynak(lar) yanıtın karar alanlarında açıkça listelendi mi?
- **kaynak_uydurulmadı**: cevapsız sorularda kaynak gösterilmedi mi?
