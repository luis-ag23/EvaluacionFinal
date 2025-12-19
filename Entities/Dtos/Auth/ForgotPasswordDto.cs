namespace ProyectoFinalTecWeb.Entities.Dtos.Auth
{
    public record ForgotPassword
    {
        public string Email { get; set; } = string.Empty;
    }
}
