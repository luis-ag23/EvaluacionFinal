using System.ComponentModel.DataAnnotations;

namespace ProyectoFinalTecWeb.Entities.Dtos.VehicleDto
{
    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string Plate { get; set; }
        public Guid ModelId { get; set; }

        // Solo información básica del Model, sin incluir Vehicle
        public ModelInfoDto Model { get; set; }

        // Drivers (si necesitas)
        public List<DriverInfoDto> Drivers { get; set; } = new();
    }

    public class ModelInfoDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; }
        public int Year { get; set; }
        // NO incluir Vehicle aquí
    }

    public class DriverInfoDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Licence { get; set; } = default!;
        public string Phone { get; set; }
        public string Role { get; set; } = "Driver";
        // NO incluir Vehicles aquí
    }
}
