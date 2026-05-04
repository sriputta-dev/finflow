using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using FinFlow.API.Data;

#nullable disable

namespace FinFlow.API.Data.Migrations
{
    [DbContext(typeof(FinFlowDbContext))]
    partial class FinFlowDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "6.0.25")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("FinFlow.API.Models.Transaction", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd()
                    .HasColumnType("uuid");
                b.Property<decimal>("Amount").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
                b.Property<string>("Currency").IsRequired().HasColumnType("text");
                b.Property<string>("SenderId").IsRequired().HasColumnType("text");
                b.Property<string>("ReceiverId").IsRequired().HasColumnType("text");
                b.Property<string>("Description").IsRequired().HasColumnType("text");
                b.Property<string>("Status").IsRequired().HasColumnType("text");
                b.Property<string>("IdempotencyKey").IsRequired().HasColumnType("text");
                b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<DateTime?>("UpdatedAt").HasColumnType("timestamp with time zone");
                b.HasKey("Id");
                b.HasIndex("IdempotencyKey").IsUnique();
                b.ToTable("Transactions");
            });

            modelBuilder.Entity("FinFlow.API.Models.OutboxMessage", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                b.Property<string>("Topic").IsRequired().HasColumnType("text");
                b.Property<string>("Payload").IsRequired().HasColumnType("text");
                b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<DateTime?>("ProcessedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("Error").HasColumnType("text");
                b.Property<int>("RetryCount").HasColumnType("integer");
                b.HasKey("Id");
                b.HasIndex("ProcessedAt");
                b.ToTable("OutboxMessages");
            });
        }
    }
}
