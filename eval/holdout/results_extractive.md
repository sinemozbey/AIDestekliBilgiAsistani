# Değerlendirme Sonuçları — mod: `extractive`

Çalıştırma zamanı: 2026-09-30 12:27 · Çağrı yolu: süreç içi .NET QaPipeline

## Özet

| Tür | Başarılı / Toplam |
|---|---|
| Normal | 1 / 4 |
| Cevapsız | 3 / 3 |
| Çelişkili | 0 / 4 |
| **Toplam** | **4 / 11** |

## Karşılaştırma

| # | Tür | Soru | Beklenen | Gerçek yanıt | Kaynak (doküman › bölüm) | Sürüm/çelişki kararı | Sonuç |
|---|---|---|---|---|---|---|---|
| H1 | Normal | Evdeki internet 5GHz, termostat buna bağlanır mı? | Hayır; yalnızca 2.4 GHz ağlar desteklenir. | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |
| H2 | Normal | Cihazın ekranı hiç açılmıyor, ne yapabilirim? | Montaj plakası bağlantılarını kontrol et, sigortayı kapatıp 30 sn sonra aç; düzelmezse garanti başvurusu. | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |
| H3 | Normal | Şifremi unuttum, maile gelen link ne kadar süre çalışır? | 30 dakika. | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |
| H4 | Normal | Termostat odayı olduğundan daha sıcak gösteriyor, bunu düzeltebilir miyim? | Isı kaynaklarından uzak iç duvara monte et; uygulamadaki Sıcaklık Kalibrasyonu ile ±2 °C düzeltme. | Cihazı güneş ışığı, radyatör veya mutfak gibi ısı kaynaklarından uzak bir iç duvara monte edin. Uygulamadaki "Sıcaklık Kalibrasyonu" ayarıyla ±2 °C düzeltme yapılabilir. | sorun-giderme v1.1 › Sıcaklık Yanlış Gösteriliyor | — | ✅ |
| H5 | Cevapsız | Termostatı Google Home'a bağlayabilir miyim? | Dokümanlarda bilgi yok. | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ✅ |
| H6 | Cevapsız | Nova Plus üyeliğimi aile üyelerimle paylaşabilir miyim? | Dokümanlarda bilgi yok. | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ✅ |
| H7 | Cevapsız | Garanti kapsamı dışındaki bir onarım bana kaça patlar? | Dokümanlarda bilgi yok (yalnızca kapsam dışı durumlar listeleniyor, ücret yok). | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ✅ |
| H8 | Çelişkili | Aldığım termostatı geri göndermek istiyorum, ne kadar vaktim var? | 30 gün (v1'deki 14 gün geçersiz). | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |
| H9 | Çelişkili | Pazar günü telefonla size ulaşabilir miyim? | Hayır, pazar günü telefon desteği yok (güncel sürüme göre canlı sohbet 7/24 açık). | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |
| H10 | Çelişkili | Geri gönderdiğim ürünün parası hesabıma kaç günde yatar? | Ürün depoya ulaştıktan sonra 5 iş günü içinde karta iade; bankaya göre +3 iş günü (v1'de 14). | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |
| H11 | Çelişkili | İade ederken kargo parası benden mi çıkıyor? | HızlıKargo ile ücretsiz; başka firma kullanılırsa müşteriye ait (kargo dokümanındaki eski kural geçersiz). | Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı. | — | — | ❌ yanıtlanabilirlik |

## Kontroller

- **yanıtlanabilirlik**: sistemin yanıt verip vermeme kararı beklenenle aynı mı?
- **doğru_kaynak**: beklenen (güncel) doküman kaynaklar arasında mı?
- **eski_sürüm_kullanılmadı**: yanıtta yürürlükten kalkmış bir sürüm kaynak olarak gösterilmedi mi?
- **anahtar_bilgi**: yanıtta beklenen kritik bilgi (ör. "30 gün") geçiyor mu?
- **eski_bilgi_sızmadı**: yanıtta geçersiz kalmış bir bilgi (ör. eski kargo ücreti kuralı) yer almıyor mu?
- **çelişki_gösterildi**: reddedilen eski kaynak(lar) yanıtın karar alanlarında açıkça listelendi mi?
- **kaynak_uydurulmadı**: cevapsız sorularda kaynak gösterilmedi mi?
