# VMWT Veri Analizi (Python)

Bu klasor, Unity'nin olusturdugu VMWT CSV dosyalarindan kontrol grafikleri uretir.

## Kurulum (Windows / PowerShell)

Komutlari repo kokunde (LabirentVR) calistirin.

    py -m venv .venv
    .\.venv\Scripts\python.exe -m pip install -r analysis\requirements.txt

## Yeni deney kayit klasor yapisi

Unity her oturum icin su yapida kayit olusturur:

    Belgeler\VMWT_Data\<KatilimciID>\<Oturum>\<YYYY-MM-DD>\<HHmmss_fff>\
        trials.csv
        path_points.csv
        experiment_config.csv
        graphs\

- trials.csv: Deneme bazinda sonuc ve hedef koordinatlari.
- path_points.csv: Her denemenin zamanli X/Z rota ornekleri.
- experiment_config.csv: Oturumdaki havuz, hedef, deneme sayisi ve sure/hareket ayarlari.
- graphs: O oturumdan uretilen PNG grafikler ve kalite raporu.

Quest uzerinde veri kok dizini Application.persistentDataPath/VMWT_Data altindadir.

## Grafik uretme

En yeni oturumu otomatik bulmak icin repo kokunde calistirin:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py

Yeni kayit yapisinda grafikler oturum klasorunun graphs altina kaydedilir. Eski adlandirmali CSV dosyalari da okunabilir; onlarda varsayilan cikti analysis\output klasorudur.

Belgeler klasoru farkliysa:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py --data-dir "C:\Tam\Yol\VMWT_Data"

Belirli dosyalari kullanmak icin:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py --trials "C:\Tam\Yol\trials.csv" --paths "C:\Tam\Yol\path_points.csv"

Config dosyasi olmayan eski kayitlarda havuz dairesini ancak cap degeri dogrulanmissa belirtin:

    .\.venv\Scripts\python.exe analysis\generate_graphs.py --trials "C:\Tam\Yol\eski.csv" --paths "C:\Tam\Yol\eski_path_points.csv" --pool-diameter 10

## Olusturulan grafikler ve kontroller

- trajectory_*.png: Her faz/deneme rotasi; yeni kayitlarda gercek havuz siniri ve hedef konumu da gosterilir.
- trial_metrics.png: Deneme bazinda hedefe ulasma suresi ve ham yol uzunlugu.
- hidden_phase_learning_curve.png: Gizli Hedef denemelerine gore sure.
- probe_quadrant_time.png: Prob denemesinde dort bolgede gecirilen zaman yuzdeleri.
- path_length_validation.csv: Kaydedilen mesafe ile rota orneklerinden hesaplanan mesafenin karsilastirmasi.
- data_quality_report.txt: Kaynak dosyalar ve satir sayilari.

## Bilimsel yorum

- Tek katilimci veya kisa test verisi ogrenme sonucu ya da grup bulgusu olarak yorumlanmamali.
- Grup ortalamalari, SEM ve yogunluk grafigi icin birden fazla katilimcinin oturumlari gerekir.
- Katilimci verilerini GitHub'a gondermeyin; yerel veri klasorunu guvenli tutun.
- Eski CSV dosyalari degistirilmez. Yeni format yalnizca yeni deney oturumlarinda kullanilir.
