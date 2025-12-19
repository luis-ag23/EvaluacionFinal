namespace ProyectoFinalTecWeb.Entities.Dtos.Auth
{
    public record ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
