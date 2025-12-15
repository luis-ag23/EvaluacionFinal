namespace ProyectoFinalTecWeb.Entities
{
    public class Model
    {
        public Guid Id { get; set; }
        public string Brand { get; set; }
        public int Year { get; set; }

        // 1:1 Vehicle->Model
        public Vehicle Vehicle { get; set; } = default!;
    }
}
