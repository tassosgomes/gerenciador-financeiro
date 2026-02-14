using GestorFinanceiro.Financeiro.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanceiro.Financeiro.Infra.Config;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(refreshToken => refreshToken.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.Token)
            .HasColumnName("token")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.IsRevoked)
            .HasColumnName("is_revoked")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(refreshToken => refreshToken.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("varchar(100)");

        builder.Property(refreshToken => refreshToken.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(refreshToken => refreshToken.Token)
            .HasDatabaseName("ix_refresh_tokens_token")
            .IsUnique();

        builder.HasIndex(refreshToken => refreshToken.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");
    }
}
