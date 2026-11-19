using CarGallery.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Notifications.Infrastructure.Configurations;

internal sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscription> builder)
    {
        builder.ToTable("newsletter_subscriptions");

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).HasColumnName("id");
        builder.Property(subscription => subscription.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(subscription => subscription.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254);
        builder.Property(subscription => subscription.Locale).HasColumnName("locale").HasMaxLength(3);
        builder.Property(subscription => subscription.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(subscription => subscription.ConfirmationTokenHash).HasColumnName("confirmation_token_hash");
        builder.Property(subscription => subscription.ConfirmationExpiresAt).HasColumnName("confirmation_expires_at");
        builder.Property(subscription => subscription.CreatedAt).HasColumnName("created_at");
        builder.Property(subscription => subscription.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(subscription => subscription.UnsubscribedAt).HasColumnName("unsubscribed_at");
        builder.Property(subscription => subscription.Version).HasColumnName("version").IsConcurrencyToken();

        // D10 — aynı yeni adrese eşzamanlı iki istek TEK kayıt üretir (AC-22): yarışı okuma
        // değil bu indeks kapatır.
        builder.HasIndex(subscription => subscription.NormalizedEmail)
            .HasDatabaseName("ux_newsletter_subscriptions_normalized_email")
            .IsUnique();

        // D25 — teyit bağlantısı özetiyle aranır.
        builder.HasIndex(subscription => subscription.ConfirmationTokenHash)
            .HasDatabaseName("ux_newsletter_subscriptions_confirmation_token_hash")
            .IsUnique()
            .HasFilter("confirmation_token_hash IS NOT NULL");

        builder.HasIndex(subscription => subscription.Status)
            .HasDatabaseName("ix_newsletter_subscriptions_status");
    }
}

internal sealed class ConsentEventConfiguration : IEntityTypeConfiguration<ConsentEvent>
{
    public void Configure(EntityTypeBuilder<ConsentEvent> builder)
    {
        builder.ToTable("consent_events");

        builder.HasKey(consent => consent.Id);
        builder.Property(consent => consent.Id).HasColumnName("id");
        builder.Property(consent => consent.SubscriptionId).HasColumnName("subscription_id");
        builder.Property(consent => consent.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(consent => consent.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(consent => new { consent.SubscriptionId, consent.OccurredAt })
            .HasDatabaseName("ix_consent_events_subscription_occurred_at");
    }
}

internal sealed class NewsletterCandidateConfiguration : IEntityTypeConfiguration<NewsletterCandidate>
{
    public void Configure(EntityTypeBuilder<NewsletterCandidate> builder)
    {
        builder.ToTable("newsletter_candidates");

        // D4 — ilan kimliği TEKİLDİR: yeniden yayın ve tekrar teslim ikinci aday üretmez.
        builder.HasKey(candidate => candidate.ListingId);
        builder.Property(candidate => candidate.ListingId).HasColumnName("listing_id");
        builder.Property(candidate => candidate.PublishedAt).HasColumnName("published_at");
        builder.Property(candidate => candidate.RecordedAt).HasColumnName("recorded_at");
        builder.Property(candidate => candidate.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(candidate => candidate.PeriodId).HasColumnName("period_id");

        builder.HasIndex(candidate => candidate.Status).HasDatabaseName("ix_newsletter_candidates_status");
    }
}

internal sealed class NewsletterPeriodConfiguration : IEntityTypeConfiguration<NewsletterPeriod>
{
    public void Configure(EntityTypeBuilder<NewsletterPeriod> builder)
    {
        builder.ToTable("newsletter_periods");

        builder.HasKey(period => period.Id);
        builder.Property(period => period.Id).HasColumnName("id");
        builder.Property(period => period.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(period => period.CutoffAt).HasColumnName("cutoff_at");
        builder.Property(period => period.ProcessedAt).HasColumnName("processed_at");

        // D13 — başlangıç kaydı bir kez yazılır (AC-72).
        builder.HasIndex(period => period.Kind)
            .HasDatabaseName("ux_newsletter_periods_baseline")
            .IsUnique()
            .HasFilter("kind = 1");

        // D13 — aynı dönem iki kez işlenmez (AC-69, AC-70).
        builder.HasIndex(period => period.CutoffAt)
            .HasDatabaseName("ux_newsletter_periods_regular_cutoff")
            .IsUnique()
            .HasFilter("kind = 2");
    }
}

internal sealed class NewsletterPeriodListingConfiguration : IEntityTypeConfiguration<NewsletterPeriodListing>
{
    public void Configure(EntityTypeBuilder<NewsletterPeriodListing> builder)
    {
        builder.ToTable("newsletter_period_listings");

        builder.HasKey(item => new { item.PeriodId, item.ListingId });
        builder.Property(item => item.PeriodId).HasColumnName("period_id");
        builder.Property(item => item.ListingId).HasColumnName("listing_id");
        builder.Property(item => item.PublishedAt).HasColumnName("published_at");
    }
}
