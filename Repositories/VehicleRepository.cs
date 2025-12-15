// Repositories/VehicleRepository.cs
using Microsoft.EntityFrameworkCore;
using ProyectoFinalTecWeb.Data;
using ProyectoFinalTecWeb.Entities;
using ProyectoFinalTecWeb.Entities.Dtos.VehicleDto;

namespace ProyectoFinalTecWeb.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly AppDbContext _ctx;
        public VehicleRepository(AppDbContext ctx) => _ctx = ctx;

        public Task<Vehicle?> GetByIdAsync(Guid id) =>
            _ctx.Vehicles
                .Include(v => v.Model)
                .Include(v => v.Drivers)
                .FirstOrDefaultAsync(v => v.Id == id);

        public async Task AddAsync(Vehicle vehiculo) =>
            await _ctx.Vehicles.AddAsync(vehiculo);

        public Task<bool> PlateExistsAsync(string plate) =>
            _ctx.Vehicles.AnyAsync(v => v.Plate == plate);

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

        public async Task<IEnumerable<VehicleDto>> GetAll()
        {
            var vehicles = await _ctx.Vehicles
                .Include(v => v.Model)
                .Include(v => v.Drivers)
                .ToListAsync();

            return vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                Plate = v.Plate,
                ModelId = v.ModelId,
                Model = new ModelInfoDto
                {
                    Id = v.Model.Id,
                    Brand = v.Model.Brand,
                    Year = v.Model.Year
                },
                Drivers = v.Drivers.Select(d => new DriverInfoDto
                {
                    Id = d.Id,
                    Name = d.Name // ajusta según tus propiedades
                }).ToList()
            });
        }

        public async Task<IEnumerable<Vehicle>> GetAllV()
        {
            return await _ctx.Vehicles.ToListAsync();
        }


        public async Task Update(Vehicle vehiculo)
        {
            _ctx.Vehicles.Update(vehiculo);
            await _ctx.SaveChangesAsync();
        }

        public async Task Delete(Vehicle vehiculo)
        {
            _ctx.Vehicles.Remove(vehiculo);
            await _ctx.SaveChangesAsync();
        }
    }
}
