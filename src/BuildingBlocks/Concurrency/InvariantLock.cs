namespace CarGallery.BuildingBlocks.Concurrency;

/// <summary>
/// Kilitlenecek satır kümesinin TARİFİ. Modül doldurur, `InvariantGuard` uygular.
///
/// Burada iş kuralı YOKTUR (Yasak Listesi 11): "son aktif Yönetici" veya "son aktif şube"
/// diye bir şey bu tipin bilgisinde değil. Tip yalnızca "şu şemadaki şu tablodan şu yüklemi
/// sağlayan satırlar" der; kaç tanesi kalınca reddedileceğine modülün kendi domain metodu
/// karar verir.
///
/// <para><b>Yüklem neden dize?</b> `FOR UPDATE` LINQ'te ifade edilemiyor ve EF Core'un
/// sorgu ağacında karşılığı yok. Tip güvenliğini taklit eden bir ifade ağacı kurmak yerine
/// yüklem ham bırakıldı ve enjeksiyon yüzeyi iki kuralla kapatıldı:</para>
/// <list type="number">
/// <item><b>Yüklem her zaman `const`'tur</b> — kullanıcı girdisi ona hiç dokunmaz.</item>
/// <item><b>Değerler her zaman parametredir</b> — `{0}`, `{1}` ile sıralanır, dizeye
/// gömülmez.</item>
/// </list>
/// <para>Mimari test (0007/AC-9) bu tipin kuruluşunda dize interpolasyonu bulunmadığını
/// doğrular: kullanıcı girdisinin SQL'e karışabileceği tek yol odur.</para>
/// </summary>
/// <param name="Schema">Modülün KENDİ şeması. Başka şema yazmak Yasak Listesi 1'dir.</param>
/// <param name="Table">Kilitlenecek tablo.</param>
/// <param name="Predicate">`WHERE` gövdesi; değerler `{0}`, `{1}` ile sıralanır.</param>
/// <param name="Parameters">Yüklemin sıralı değerleri.</param>
public sealed record InvariantLock(
    string Schema,
    string Table,
    string Predicate,
    IReadOnlyList<object> Parameters);
