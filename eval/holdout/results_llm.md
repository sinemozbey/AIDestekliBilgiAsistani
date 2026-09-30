# Değerlendirme Sonuçları — mod: `llm` (`claude-opus-5-5`)

Çalıştırma zamanı: 2026-09-30 12:27 · Çağrı yolu: http://localhost:5081 (.NET API → FastAPI LLM servisi)

## Özet

| Tür | Başarılı / Toplam |
|---|---|
| Normal | 4 / 4 |
| Cevapsız | 3 / 3 |
| Çelişkili | 4 / 4 |
| **Toplam** | **11 / 11** |

## Karşılaştırma

| # | Tür | Soru | Beklenen | Gerçek yanıt | Kaynak (doküman › bölüm) | Sürüm/çelişki kararı | Sonuç |
|---|---|---|---|---|---|---|---|
| H1 | Normal | Evdeki internet 5GHz, termostat buna bağlanır mı? | Hayır; yalnızca 2.4 GHz ağlar desteklenir. | Hayır, Nova Termo 5 GHz Wi-Fi ağlarını desteklemez; kurulum ve bağlantı için 2.4 GHz Wi-Fi ağı gereklidir. Müşterinin cihazı 2.4 GHz bir ağa bağlaması gerekir. | nova-termo-kurulum v1.2 › Ön Koşullar<br>sorun-giderme v1.1 › Cihaz Wi-Fi Ağına Bağlanmıyor | — | ✅ |
| H2 | Normal | Cihazın ekranı hiç açılmıyor, ne yapabilirim? | Montaj plakası bağlantılarını kontrol et, sigortayı kapatıp 30 sn sonra aç; düzelmezse garanti başvurusu. | Öncelikle cihazın montaj plakasındaki bağlantıları kontrol edin. Ardından sigortayı kapatıp 30 saniye bekledikten sonra tekrar açın. Bu adımlardan sonra ekran hâlâ açılmıyorsa garanti başvurusu yapabilirsiniz; başvuru için cihazın seri numarası (cihazın arka yüzündeki etikette yer alır) ve fatura gereklidir. | sorun-giderme v1.1 › Ekran Kapalı veya Tepki Vermiyor<br>garanti-kosullari v1.0 › Garanti Başvurusu | — | ✅ |
| H3 | Normal | Şifremi unuttum, maile gelen link ne kadar süre çalışır? | 30 dakika. | Giriş ekranındaki "Şifremi Unuttum" bağlantısına tıklayıp kayıtlı e-posta adresinizi girdiğinizde size bir sıfırlama bağlantısı gönderilir. Bu bağlantı 30 dakika boyunca geçerlidir. | hesap-uyelik v1.0 › Şifre Sıfırlama | — | ✅ |
| H4 | Normal | Termostat odayı olduğundan daha sıcak gösteriyor, bunu düzeltebilir miyim? | Isı kaynaklarından uzak iç duvara monte et; uygulamadaki Sıcaklık Kalibrasyonu ile ±2 °C düzeltme. | Evet, düzeltilebilir. Cihazın güneş ışığı, radyatör veya mutfak gibi ısı kaynaklarından uzak bir iç duvara monte edildiğinden emin olun. Ayrıca Nova uygulamasındaki "Sıcaklık Kalibrasyonu" ayarıyla gösterilen sıcaklıkta ±2 °C'ye kadar düzeltme yapabilirsiniz. | sorun-giderme v1.1 › Sıcaklık Yanlış Gösteriliyor | — | ✅ |
| H5 | Cevapsız | Termostatı Google Home'a bağlayabilir miyim? | Dokümanlarda bilgi yok. | Nova Termo'nun Google Home ile uyumluluğu veya Google Home'a nasıl bağlanacağı hakkında dokümanlarda bilgi bulunmamaktadır. | — | — | ✅ |
| H6 | Cevapsız | Nova Plus üyeliğimi aile üyelerimle paylaşabilir miyim? | Dokümanlarda bilgi yok. | Nova Plus üyeliğinin aile üyeleriyle paylaşılıp paylaşılamayacağına dair bir bilgi dokümanlarda bulunmamaktadır. | — | — | ✅ |
| H7 | Cevapsız | Garanti kapsamı dışındaki bir onarım bana kaça patlar? | Dokümanlarda bilgi yok (yalnızca kapsam dışı durumlar listeleniyor, ücret yok). | Garanti kapsamı dışındaki onarımların ücretine dair bilgi dokümanlarda bulunmamaktadır. | — | — | ✅ |
| H8 | Çelişkili | Aldığım termostatı geri göndermek istiyorum, ne kadar vaktim var? | 30 gün (v1'deki 14 gün geçersiz). | Ürünü teslim aldığınız tarihten itibaren 30 gün içinde iade talebinde bulunabilirsiniz. Ürün kampanyalı olarak satın alındıysa da aynı 30 günlük süre geçerlidir. | iade-politikasi-v2 v2.0 › İade Süresi | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi | ✅ |
| H9 | Çelişkili | Pazar günü telefonla size ulaşabilir miyim? | Hayır, pazar günü telefon desteği yok (güncel sürüme göre canlı sohbet 7/24 açık). | Hayır, pazar günleri telefon desteğimiz bulunmamaktadır. Çağrı merkezimize (0850 123 45 67) hafta içi 08:00-20:00, cumartesi ise 10:00-16:00 saatleri arasında ulaşabilirsiniz. Pazar günü Nova uygulaması ve web sitesi üzerinden 7/24 hizmet veren canlı sohbet desteğini kullanabilirsiniz. | destek-kanallari-v2 v2.0 › Telefon Desteği<br>destek-kanallari-v2 v2.0 › Canlı Sohbet | destek-kanallari-v2 seçildi; destek-kanallari-v1 reddedildi | ✅ |
| H10 | Çelişkili | Geri gönderdiğim ürünün parası hesabıma kaç günde yatar? | Ürün depoya ulaştıktan sonra 5 iş günü içinde karta iade; bankaya göre +3 iş günü (v1'de 14). | İadeniz onaylandıktan sonra ücret, ürün depomuza ulaştıktan itibaren 5 iş günü içinde ödemenin yapıldığı karta iade edilir. Bankanıza bağlı olarak tutarın hesabınıza yansıması 3 iş günü daha sürebilir. | iade-politikasi-v2 v2.0 › Para İadesi | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi | ✅ |
| H11 | Çelişkili | İade ederken kargo parası benden mi çıkıyor? | HızlıKargo ile ücretsiz; başka firma kullanılırsa müşteriye ait (kargo dokümanındaki eski kural geçersiz). | Anlaşmalı kargo firmamız HızlıKargo ile yaptığınız iade gönderimleri ücretsizdir. Farklı bir kargo firması kullanırsanız kargo ücreti size ait olur. | iade-politikasi-v2 v2.0 › İade Kargo Ücreti | iade-politikasi-v2 seçildi; iade-politikasi-v1 reddedildi<br>iade-politikasi-v2 > kargo-teslimat (İade gönderimlerinde kargo ücreti) | ✅ |

## Kontroller

- **yanıtlanabilirlik**: sistemin yanıt verip vermeme kararı beklenenle aynı mı?
- **doğru_kaynak**: beklenen (güncel) doküman kaynaklar arasında mı?
- **eski_sürüm_kullanılmadı**: yanıtta yürürlükten kalkmış bir sürüm kaynak olarak gösterilmedi mi?
- **anahtar_bilgi**: yanıtta beklenen kritik bilgi (ör. "30 gün") geçiyor mu?
- **eski_bilgi_sızmadı**: yanıtta geçersiz kalmış bir bilgi (ör. eski kargo ücreti kuralı) yer almıyor mu?
- **çelişki_gösterildi**: reddedilen eski kaynak(lar) yanıtın karar alanlarında açıkça listelendi mi?
- **kaynak_uydurulmadı**: cevapsız sorularda kaynak gösterilmedi mi?
