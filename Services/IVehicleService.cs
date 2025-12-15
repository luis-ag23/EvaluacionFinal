using ProyectoFinalTecWeb.Entities;
using ProyectoFinalTecWeb.Entities.Dtos.VehicleDto;

namespace ProyectoFinalTecWeb.Services
{
    public interface IVehicleService
    {
        Task<Guid> CreateAsync(CreateVehicleDto dto);
        Task<Vehicle> GetByIdAsync(Guid id);
        Task<Vehicle> UpdateAsync(UpdateVehicleDto dto, Guid id);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<VehicleDto>> GetAll();
        Task<IEnumerable<Vehicle>> GetAllV();



    }
}
