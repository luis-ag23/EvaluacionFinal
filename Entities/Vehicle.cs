namespace ProyectoFinalTecWeb.Entities
{

    public class Vehicle
    {
        public Guid Id { get; set; }
        public string Plate { get; set; }

        // N:M vechicle -> driver
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();

        // 1:1 vehicle-> model
        public Guid ModelId { get; set; }
        public Model Model { get; set; } = default!;

    }
}
