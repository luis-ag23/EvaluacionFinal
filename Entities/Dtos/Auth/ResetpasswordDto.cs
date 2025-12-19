namespace ProyectoFinalTecWeb.Entities.Dtos.Auth
{
    public record ResetpasswordDto
    {
        public string token { get; set; } = string.Empty;
        public string newpassword { get; set; } = string.Empty;
    }
}
