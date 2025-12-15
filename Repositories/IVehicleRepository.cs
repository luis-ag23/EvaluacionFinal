using ProyectoFinalTecWeb.Entities;
using ProyectoFinalTecWeb.Entities.Dtos.VehicleDto;

namespace ProyectoFinalTecWeb.Repositories
{
    public interface IVehicleRepository
    {
        Task AddAsync(Vehicle vehicle);
        Task<Vehicle?> GetByIdAsync(Guid id);
        Task<bool> PlateExistsAsync(string plate);
        Task<int> SaveChangesAsync();
        Task<IEnumerable<VehicleDto>> GetAll();
        Task<IEnumerable<Vehicle>> GetAllV();

        Task Update(Vehicle vehicle);
        Task Delete(Vehicle vehicle);
    }
}
