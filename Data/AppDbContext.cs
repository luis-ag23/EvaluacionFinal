using Microsoft.EntityFrameworkCore;
using ProyectoFinalTecWeb.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace ProyectoFinalTecWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Passenger> Passengers => Set<Passenger>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Model> Models => Set<Model>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //EN PRUBA

            // 1:N Driver -> Trips
            modelBuilder.Entity<Driver>(d =>
            {
                d.HasMany(d => d.Trips)
                .WithOne(trip => trip.Driver)
                .HasForeignKey(trip => trip.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            // 1:N Passenger -> Trips
            modelBuilder.Entity<Passenger>(p =>
            {
                p.HasMany(p => p.Trips)
                .WithOne(trip => trip.Passenger)
                .HasForeignKey(trip => trip.PassengerId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            // Relación Driver - Vehículo (M:N)
            modelBuilder.Entity<Driver>()
                .HasMany(d=> d.Vehicles)
                .WithMany(v => v.Drivers)
                .UsingEntity<Dictionary<string, object>>(
                    "DriverVehicle",
                    j => j.HasOne<Vehicle>().WithMany().HasForeignKey("VehicleId"),
                    j => j.HasOne<Driver>().WithMany().HasForeignKey("DriverId"),
                    j =>{
                        j.HasKey("DriverId", "VehicleId");
                        j.ToTable("DriverVehicles");
                    });


            // Relación  Vehículo - Model (1:1)
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Model)
                .WithOne(m => m.Vehicle)
                .HasForeignKey<Vehicle>(v => v.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hacer ModelId único para relación 1:1
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.ModelId)
                .IsUnique();
        }
    }
}