using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AlesyaTheTraveller.Entities
{
    public partial class CorvegaContext : DbContext
    {
        public CorvegaContext()
        {
        }

        public CorvegaContext(DbContextOptions<CorvegaContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Responses> Responses { get; set; }
        public virtual DbSet<Sorting> Sorting { get; set; }
        public virtual DbSet<Utterances> Utterances { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    if (!optionsBuilder.IsConfigured)
        //    { 
        //        optionsBuilder.UseSqlServer("Server=DESKTOP-FU2G2VK;Database=Corvega;Trusted_Connection=True;");
        //    }
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity<Responses>(entity =>
            {
                entity.ToTable("responses");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Active).HasColumnName("active");

                entity.Property(e => e.Respid).HasColumnName("respid");

                entity.Property(e => e.Word)
                    .IsRequired()
                    .HasColumnName("word")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasOne(d => d.Resp)
                    .WithMany(p => p.Responses)
                    .HasPrincipalKey(p => p.Respid)
                    .HasForeignKey(d => d.Respid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__responses__activ__52593CB8");
            });

            modelBuilder.Entity<Sorting>(entity =>
            {
                entity.ToTable("sorting");

                entity.HasIndex(e => new { e.Type, e.Typeid })
                    .HasName("UC_OneTypeTypeid")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Active).HasColumnName("active");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Typeid).HasColumnName("typeid");

                entity.Property(e => e.Typename)
                    .HasColumnName("typename")
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Utterances>(entity =>
            {
                entity.ToTable("utterances");

                entity.HasIndex(e => e.Respid)
                    .HasName("UQ__utteranc__2C2806A0EA17A916")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasColumnName("content")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Respid).HasColumnName("respid");
            });
        }
    }
}
