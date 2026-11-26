namespace SistemaRestaurante.API.Auth
{
    /// <summary>Configuración de los tokens JWT (sección "Jwt" de appsettings o variables Jwt__*).</summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>Clave secreta de firma. Mínimo 32 caracteres. En producción: variable de entorno Jwt__Key.</summary>
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "SistemaRestaurante";
        public string Audience { get; set; } = "SistemaRestaurante.Front";

        /// <summary>Duración de la sesión. Por defecto 12 horas: cubre un turno completo.</summary>
        public int ExpiresMinutes { get; set; } = 720;
    }
}
