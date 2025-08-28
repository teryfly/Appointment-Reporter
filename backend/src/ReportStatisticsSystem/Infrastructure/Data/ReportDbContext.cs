using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace Infrastructure.Data
{
    public class ReportDbContext : DbContext
    {
        public ReportDbContext(DbContextOptions<ReportDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppointmentEntity> Appointments { get; set; }
        public DbSet<AppointmentItemEntity> AppointmentItems { get; set; }
        public DbSet<AppointmentNumberEntity> AppointmentNumbers { get; set; }
        public DbSet<AppointmentPropertyEntity> AppointmentProperties { get; set; }
        public DbSet<AppointmentFailedRecordEntity> AppointmentFailedRecords { get; set; }
        public DbSet<NumberProviderEntity> NumberProviders { get; set; }

        // Read-model mapping for scheduler QueueRecords aggregation (SQL-2)
        public DbSet<QueueRecordReadModel> QueueRecordReadModels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentEntity>().ToTable("appointment", "resourcepool");
            modelBuilder.Entity<AppointmentItemEntity>().ToTable("appointment_item", "resourcepool");
            modelBuilder.Entity<AppointmentNumberEntity>().ToTable("appointment_number", "resourcepool");
            modelBuilder.Entity<AppointmentPropertyEntity>().ToTable("appointment_property", "resourcepool");
            modelBuilder.Entity<AppointmentFailedRecordEntity>().ToTable("appointmentfailedrecord", "resourcepool");
            modelBuilder.Entity<NumberProviderEntity>().ToTable("number_provider", "resourcepool");

            // If QueueRecords is in another database [isp-scheduler].[schedule].[QueueRecords],
            // we map it with schema 'schedule' but need the database prefix.
            // EF Core doesn't support cross-database schema directly; use a view or a query type.
            // Here we configure it as a keyless entity with FromSql to specify three-part name.
            modelBuilder.Entity<QueueRecordReadModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToView(null); // prevent EF from trying to CREATE or expect a local table
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
                entity.Property(e => e.QueueType).HasColumnName("QueueType");
                entity.Property(e => e.ItemCode).HasColumnName("itemCode");
                entity.Property(e => e.ItemName).HasColumnName("itemName");
                entity.Property(e => e.DoctorName).HasColumnName("doctorname");
                entity.Property(e => e.AppointmentSourceStartTime).HasColumnName("appointmentSourceStartTime");
            });
        }
    }
}