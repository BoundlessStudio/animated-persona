using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using AnimatedPersona.Api.Options;

namespace AnimatedPersona.Api.Services;

public class TokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenValidator> _logger;
    private readonly JwtValidationOptions _options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration>? _configManager;

    public TokenValidator(IConfiguration configuration, ILogger<TokenValidator> logger, IOptions<JwtValidationOptions> options)
    {
        _configuration = configuration;
        _logger = logger;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.Authority))
        {
            var wellKnown = $"https://{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(wellKnown, new OpenIdConnectConfigurationRetriever());
        }
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string token)
    {
        if (_configManager == null)
        {
            _logger.LogWarning("Auth0 domain not configured; rejecting token");
            return null;
        }

        var config = await _configManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidAudience = _options.Audience,
            ValidIssuer = config.Issuer,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            IssuerSigningKeys = config.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate token");
            return null;
        }
    }
}
