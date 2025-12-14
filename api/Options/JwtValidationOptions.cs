namespace AnimatedPersona.Api.Options;

public class JwtValidationOptions
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
