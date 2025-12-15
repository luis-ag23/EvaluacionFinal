using ProyectoFinalTecWeb.Entities;
using ProyectoFinalTecWeb.Entities.Dtos.VehicleDto;
using ProyectoFinalTecWeb.Repositories;

namespace ProyectoFinalTecWeb.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicles;
        private readonly IModelRepository _models;
        private readonly IDriverRepository _drivers;

        public VehicleService(IVehicleRepository vehicles, IModelRepository models, IDriverRepository drivers)
        {
            _vehicles = vehicles;
            _models = models;
            _drivers = drivers;
        }
        public async Task<Guid> CreateAsync(CreateVehicleDto dto)
        {
            // Verificar que el Model existe
            var model = await _models.GetByIdAsync(dto.ModelId);
            if (model == null)
                throw new Exception($"Model with ID {dto.ModelId} not found");

            // Verificar que el Model no tenga ya un Vehicle (1:1)
            var existingVehicle = await _vehicles.GetByIdAsync(dto.ModelId);
            if (existingVehicle != null)
                throw new Exception($"Model with ID {dto.ModelId} is already assigned to a vehicle");

            // Crear el Vehicle
            var entity = new Vehicle
            {
                ModelId = dto.ModelId,
                Plate = dto.Plate
            };

            await _vehicles.AddAsync(entity);
            await _vehicles.SaveChangesAsync();

            // Actualizar la relación bidireccional
            model.Vehicle = entity;
            await _models.SaveChangesAsync();

            return entity.Id;
        }

        public async Task DeleteAsync(Guid id)
        {
            Vehicle? vehicle = (await GetAllV()).FirstOrDefault(h => h.Id == id);
            if (vehicle == null) return;
            await _vehicles.Delete(vehicle);
        }
        public async Task<IEnumerable<Vehicle>> GetAllV()
        {
            return await _vehicles.GetAllV();

        }

        public async Task<IEnumerable<VehicleDto>> GetAll()
        {
            return await _vehicles.GetAll();

        }

        public async Task<Vehicle> GetByIdAsync(Guid id)
        {
            return await _vehicles.GetByIdAsync(id);
        }

        public async Task<Vehicle> UpdateAsync(UpdateVehicleDto dto, Guid id)
        {
            Vehicle? vehicle = await GetByIdAsync(id);
            if (vehicle == null) throw new Exception("Vehicle doesnt exist.");

            vehicle.Plate = dto.Plate;

            await _vehicles.Update(vehicle);
            return vehicle;
        }
    }
}
