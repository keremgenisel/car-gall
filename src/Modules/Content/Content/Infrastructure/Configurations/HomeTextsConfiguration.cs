using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Content.Infrastructure.Configurations;

/// <summary>
/// `0022` — ana sayfa metinleri: tek kayıt (<c>home_texts</c>) ve dil başına satır
/// (<c>home_text_localizations</c>), yalnız <c>content</c> şemasında (C1, C3). Merkezi bir
/// çeviri tablosu değildir (`0017` C2); kolonlar alanların kendisidir.
/// </summary>
internal sealed class HomeTextsConfiguration : IEntityTypeConfiguration<HomeTexts>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HomeTexts> builder)
    {
        builder.ToTable("home_texts");
        builder.HasKey(texts => texts.Id);
        builder.Property(texts => texts.Id).HasColumnName("id").ValueGeneratedNever();

        // R8 — iyimser eşzamanlılık `0017`'nin deseniyle: güncelleme `WHERE version = <okunan>`
        // ile yazılır; aynı sürümden iki paralel kayıttan biri sıfır satır günceller ve `409` olur.
        builder.Property(texts => texts.Version).HasColumnName("version").IsConcurrencyToken();

        builder.OwnsMany(texts => texts.Localizations, satir =>
        {
            satir.ToTable("home_text_localizations");
            satir.WithOwner().HasForeignKey("HomeTextsId");
            satir.Property<Guid>("HomeTextsId").HasColumnName("home_texts_id");
            satir.HasKey("HomeTextsId", nameof(HomeTextLocalization.Locale));
            satir.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(10);

            // Sınır kolonda değil kuralda (ContentRules): kod noktası sayımı veritabanının
            // karakter sayımıyla aynı değil ve iki yerde yaşayan sınır ayrışır.
            satir.Property(item => item.IntroTitle).HasColumnName("intro_title");
            satir.Property(item => item.IntroBody).HasColumnName("intro_body");
            satir.Property(item => item.FormTitle).HasColumnName("form_title");
            satir.Property(item => item.FeaturedTitle).HasColumnName("featured_title");
            satir.Property(item => item.EmptyTitle).HasColumnName("empty_title");
            satir.Property(item => item.EmptyBody).HasColumnName("empty_body");
        });

        // C2 — yeni kurulumda kayıt VAR ama metni yok: sayfa bugünkü varsayılanlarla açılır.
        builder.HasData(new { Id = HomeTexts.SingletonId, Version = 1 });
    }
}
