using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinalTecWeb.Entities;
using ProyectoFinalTecWeb.Entities.Dtos.VehicleDto;
using ProyectoFinalTecWeb.Repositories;
using ProyectoFinalTecWeb.Services;

namespace ProyectoFinalTecWeb.Controllers
{
    [ApiController]
    [Route("api/vehicle")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _service;
        private readonly IDriverService _drivers;
        private readonly IVehicleRepository _vehicles;

        public VehicleController(IVehicleService service, IDriverService drivers, IVehicleRepository vehicles)
        {
            _service = service;
            _drivers = drivers;
            _vehicles = vehicles;
        }

        // POST: api/vehicle
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        // GET: api/vehicle
        [HttpGet]
        public async Task<IActionResult> GetAllVehicles()
        {
            IEnumerable<VehicleDto> items = await _service.GetAll();
            return Ok(items);
        }

        // GET: api/vehicle/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // PUT: api/vehicle/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateVehicleDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var vehicle = await _service.UpdateAsync(dto, id);
            return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
        }

        // DELETE: api/vehicle/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // POST: api/vehicle/{vehicleId}/drivers/{driverId}
        [HttpPost("{vehicleId:guid}/drivers/{driverId:guid}")]
        public async Task<IActionResult> AssignDriver(Guid vehicleId, Guid driverId)
        {
            var vehicle = await _service.GetByIdAsync(vehicleId);
            var driver = await _drivers.GetOne(driverId);

            if (vehicle == null || driver == null)
                return NotFound();

            // Verifica si ya existe la relación
            if (!vehicle.Drivers.Any(d => d.Id == driverId))
            {
                vehicle.Drivers.Add(driver);
                await _vehicles.SaveChangesAsync();
            }

            return Ok();
        }

        // DELETE: api/vehicle/{vehicleId}/drivers/{driverId}
        [HttpDelete("{vehicleId:guid}/drivers/{driverId:guid}")]
        public async Task<IActionResult> RemoveDriver(Guid vehicleId, Guid driverId)
        {
            var vehicle = await _service.GetByIdAsync(vehicleId);

            if (vehicle == null)
                return NotFound();

            var driver = vehicle.Drivers.FirstOrDefault(d => d.Id == driverId);
            if (driver != null)
            {
                vehicle.Drivers.Remove(driver);
                await _vehicles.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
