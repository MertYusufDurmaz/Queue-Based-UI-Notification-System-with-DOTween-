# Queue-Based-UI-Notification-System-with-DOTween-
Queue-Based UI Notification System (with DOTween)
Bu modül, oyun içi bilgilendirmeleri (İpuçları, Görev Güncellemeleri, Düşük Pil Uyarıları vb.) ekrana pürüzsüz animasyonlarla ve bir kuyruk (Queue) mantığıyla yansıtan evrensel bir UI yöneticisidir.

Özellikler:

Akıllı Kuyruk (Queue) Sistemi: Aynı anda birden fazla bildirim tetiklendiğinde UI birbirine girmez. Bildirimler sıraya alınır ve ekrandaki bildirim kaybolduğunda bir sonraki pürüzsüzce ekrana gelir.

Kategoriye Özel Animasyonlar: Uyarılar (Warning) ekranın altından vurucu bir şekilde çıkarken, görevler soldan, pil bildirimleri ise sağdan kayarak gelir. Tüm bu yönlendirmeler modüler alt metotlarla yönetilir.

Çözünürlük Dostu (Off-Screen Params): Koda gömülü sihirli sayılar (magic numbers) içermez. Bildirimlerin ekran dışında bekleyeceği X ve Y koordinatları Inspector üzerinden her çözünürlüğe uygun şekilde ayarlanabilir.

DOTween Entegrasyonu: Tüm UI hareketleri (Ease.OutBack, Ease.InQuad vb.) DOTween motoru ile kusursuz bir "game feel" sağlayacak şekilde optimize edilmiştir.

Kurulum:

Projenizde DOTween kurulu olduğundan emin olun.

NotificationManager scriptini boş bir objeye atayıp Singleton olarak sahnenizde bulundurun.

Inspector üzerinden sağ, sol ve alt ekran dışı (Off-Screen) sınırlarını monitörünüzün çözünürlüğüne göre ayarlayın (Örn: Sol için -2000f).

Diğer scriptlerden NotificationManager.Instance.ShowNotification(...) metodunu çağırarak sistemi tetikleyin.
