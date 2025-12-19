using Microsoft.EntityFrameworkCore;
using ProyectoFinalTecWeb.Data;
using ProyectoFinalTecWeb.Entities;

namespace ProyectoFinalTecWeb.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly AppDbContext _ctx;
        public DriverRepository(AppDbContext ctx) { _ctx = ctx; }

        public async Task AddAsync(Driver driver)
        {
            _ctx.Drivers.Add(driver);
            await _ctx.SaveChangesAsync();
        }

        public async Task Delete(Driver driver)
        {
            _ctx.Drivers.Remove(driver);
            await _ctx.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(Guid id) =>
            _ctx.Drivers.AnyAsync(s => s.Id == id);

        
        public async Task<IEnumerable<Driver>> GetAll()
        {
            return await _ctx.Drivers
                .Include(d => d.Vehicles)
                    .ThenInclude(v => v.Model)
                .Include(d => d.Trips)
                .ToListAsync();
        }

        public async Task<Driver?> GetByEmailAddress(string email) =>
            await _ctx.Drivers.FirstOrDefaultAsync(u => u.Email == email);

        public Task<Driver?> GetByRefreshToken(string refreshToken)=>
            _ctx.Drivers.FirstOrDefaultAsync(d => d.RefreshToken == refreshToken);

        public async Task<Driver?> GetOne(Guid id)
        {
            return await _ctx.Drivers
         .Include(d => d.Trips) // Incluir Trips
         .Include(d => d.Vehicles) // Incluir Vehicles
             .ThenInclude(v => v.Model) // Y el Model de cada Vehicle
         .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Driver?> GetByIdWithTripsAsync(Guid id)
        {
            return await _ctx.Drivers
                .Include(d => d.Trips)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Driver?> GetByIdWithVehiclesAsync(Guid id)
        {
            return await _ctx.Drivers
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Driver?> GetTripsAsync(Guid id)
        {
            return await _ctx.Drivers
                .Include(c => c.Trips)
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.Id == id);
        }


        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

        public async Task Update(Driver driver)
        {
            _ctx.Drivers.Update(driver);
            await _ctx.SaveChangesAsync();
        }

        public async Task<Driver?> GetByEmailAddressAsync(string Email)
        {
            return await _ctx.Drivers.FirstOrDefaultAsync(d => d.Email == Email);
        }
    }
}
