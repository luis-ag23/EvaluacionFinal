namespace ProyectoFinalTecWeb.Entities.Dtos.Auth
{
    public record ResponceForgotpasswordDto
    {
        public string token { get; set; } = string.Empty;
    }
}
