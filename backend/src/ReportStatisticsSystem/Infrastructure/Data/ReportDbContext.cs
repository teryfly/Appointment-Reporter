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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentEntity>().ToTable("appointment", "resourcepool");
            modelBuilder.Entity<AppointmentItemEntity>().ToTable("appointment_item", "resourcepool");
            modelBuilder.Entity<AppointmentNumberEntity>().ToTable("appointment_number", "resourcepool");
            modelBuilder.Entity<AppointmentPropertyEntity>().ToTable("appointment_property", "resourcepool");
            modelBuilder.Entity<AppointmentFailedRecordEntity>().ToTable("appointmentfailedrecord", "resourcepool");
        }
    }
}