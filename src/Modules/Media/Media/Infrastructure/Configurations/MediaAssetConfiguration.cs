using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Media.Infrastructure.Configurations;

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("assets");

        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Id).HasColumnName("id");

        builder.Property(asset => asset.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(asset => asset.OriginalObjectKey)
            .HasColumnName("original_object_key")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(asset => asset.ByteSize).HasColumnName("byte_size");
        builder.Property(asset => asset.SourceWidth).HasColumnName("source_width");
        builder.Property(asset => asset.SourceHeight).HasColumnName("source_height");

        // Yasak Listesi 22 — veritabanında çevrilmiş durum adı değil KOD saklanır.
        // `int` olarak saklanıyor; gösterim uçta, çeviriden gelir.
        builder.Property(asset => asset.Status)
            .HasColumnName("status")
            .HasConversion<int>();

        builder.Property(asset => asset.AttemptCount).HasColumnName("attempt_count");

        builder.Property(asset => asset.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(100);

        builder.Property(asset => asset.UploadedAt).HasColumnName("uploaded_at");
        builder.Property(asset => asset.UpdatedAt).HasColumnName("updated_at");
        builder.Property(asset => asset.NextAttemptAt).HasColumnName("next_attempt_at");

        // AC-12 — SAHİPLENMENİN kilidi. PostgreSQL'in sistem kolonu `xmin` eşzamanlılık
        // belirteci olarak kullanılıyor: ikinci tur aynı satırı güncellemeye kalkarsa
        // `SaveChanges` `DbUpdateConcurrencyException` ile düşer.
        //
        // **Neden satır kilidi DEĞİL:** `0007` R3 satır kilidini yalnız
        // `BuildingBlocks/Concurrency/` altına koyuyor ve bir mimari test (`0007`/AC-9)
        // modüllerde kilit sözdizimini tarıyor — yorumlarda da, ve haklı olarak: dizeyi
        // anmak bir sonraki okuyucuyu onu kopyalamaya davet eder. İlk yazımda ham SQL
        // yazdım ve o test beni kırdı. `InvariantGuard` de bu iş için uygun değil, çünkü
        // o "satır kümesini kilitle, SAY, karar ver" içindir; buradaki ihtiyaç bir
        // kuyruktan bir satır kapmak.
        //
        // Belirteç ayrıca kaybolan güncellemeyi de kapatıyor: ölçüldü, kilitsiz hâlde
        // iki tur sahiplenip `attempt_count` 1'de kalıyordu — oysa o sayaç AC-11'in
        // ölçütü.
        //
        // **Ek kolon YOK:** `xmin` her tabloda zaten var, bu yüzden migration gerekmez.
        builder.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(asset => asset.AltText)
            .HasColumnName("alt_text")
            .HasColumnType("jsonb")
            .HasConversion(LocalizedAltTextStorage.Converter, LocalizedAltTextStorage.Comparer);

        // Türevler agregatın parçası, ayrı agregat değil (`docs/conventions.md` § 10).
        //
        // Koleksiyon salt okunur bir özellikle dışarı veriliyor (`=> derivatives`); EF ona
        // ALAN üzerinden yazmak zorunda. Erişim modu `OwnsMany`'den SONRA ayarlanıyor —
        // öncesinde navigation henüz tanımlı değildir. İlk yazımda bu sırayı ters kurdum
        // ve `dotnet ef` "Navigation 'MediaAsset.Derivatives' was not found" dedi.
        builder.OwnsMany(asset => asset.Derivatives, derivative =>
        {
            derivative.ToTable("asset_derivatives");
            derivative.WithOwner().HasForeignKey("asset_id");
            derivative.Property<Guid>("asset_id").HasColumnName("asset_id");
            derivative.Property(value => value.VariantCode).HasColumnName("variant_code").HasMaxLength(20);
            derivative.Property(value => value.Width).HasColumnName("width");
            derivative.Property(value => value.Height).HasColumnName("height");
            derivative.Property(value => value.ObjectKey).HasColumnName("object_key").HasMaxLength(512);
            derivative.HasKey("asset_id", nameof(MediaDerivative.VariantCode));
        });

        builder.Navigation(asset => asset.Derivatives)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // D14 — işleme turu "sırası gelmiş" varlıkları bu indeksle buluyor. İndeks olmadan
        // sorgu her turda tüm tabloyu tarar ve varlık sayısı arttıkça tur süresi büyür.
        builder.HasIndex(asset => new { asset.Status, asset.NextAttemptAt })
            .HasDatabaseName("ix_assets_status_next_attempt_at");
    }
}
