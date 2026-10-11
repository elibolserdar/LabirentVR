# VMWT Veri Analizi (Python)

Bu klasör, Unity'nin oluşturduğu VMWT CSV dosyalarından ilk kontrol grafiklerini üretir.

## İlk kurulum (Windows / PowerShell)

Komutları repo kökünde (LabirentVR) çalıştırın.

1. Python sanal ortamını oluşturun (bir kez yeterli):

    py -m venv .venv

2. Grafik kütüphanelerini kurun:

    .\.venv\Scripts\python.exe -m pip install -r analysis\requirements.txt

## Grafik üretme

Unity Editor'da oluşan CSV dosyaları normalde Belgeler\VMWT_Data altındadır. Repo kökünde çalıştırın:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py

Script, klasördeki en yeni deneme CSV'sini ve onun eşleşen _path_points.csv dosyasını seçer. Grafikler analysis\output klasörüne yazılır.

Belgeler klasörünüz farklı bir konumdaysa veri klasörünü açıkça verebilirsiniz:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py --data-dir "C:\Tam\Yol\VMWT_Data"

Belirli iki dosyayı analiz etmek için:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py --trials "C:\Tam\Yol\participant_Baseline.csv" --paths "C:\Tam\Yol\participant_Baseline_path_points.csv"

## Oluşturulan çıktılar

- trajectory_*.png: Her faz/deneme için rota çizimi.
- trial_metrics.png: Denemelere göre hedefe ulaşma süresi ve ham yol uzunluğu.
- hidden_phase_learning_curve.png: Gizli hedef süresi; tek deneme varsa uyarı gösterilir.
- probe_quadrant_time.png: Prob denemesinde dört bölgede geçirilen süre yüzdeleri (veri varsa).
- path_length_validation.csv: Loglanan ham yol uzunluğunu rota koordinatlarından hesaplanan örneklenmiş uzunlukla karşılaştırır.
- data_quality_report.txt: Kaynak dosyalar ve kayıt sayıları özeti.

## Notlar

- Script varsayılan olarak yalnızca en yeni oturumu analiz eder.
- Tek katılımcı veya kısa test verisi araştırma bulgusu ya da öğrenme eğrisi olarak yorumlanmamalıdır.
- PNG ve CSV raporları analysis/output altındadır ve .gitignore ile Git dışında tutulur.
- Katılımcı verilerini GitHub'a göndermeyin; yerel analiz verilerini güvenli tutun.
