using BuildingBlocks.Contracts.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UserManagement.API.Common;

public class JwtTokenService(IOptions<AuthenticationOptions> AuthenticationOption, IConfiguration config) : ITokenService
{
    public string GenerateToken(User user, IEnumerable<string> permissionCodes)
    {

        var claims = new List<Claim>
            {
                     new("MasterIdentityId", user.IdentityId.ToString()),
                     new("UserId", user.Id.ToString()),
                     new(ClaimTypes.Name, $"{user.Name} {user.Family}" ?? string.Empty),
                     new(ClaimTypes.MobilePhone, user.MasterIdentity.Mobile),
                     new("NationalCode", user.NationalCode ?? string.Empty)
            };

        claims.AddRange(permissionCodes.Select(code => new Claim("permission", code)));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthenticationOption.Value.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
               issuer: AuthenticationOption.Value.Issuer,
               audience: AuthenticationOption.Value.Audience,
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(AuthenticationOption.Value.TokenExpirationMinutes),
               signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(AuthenticationOption.Value.SecretKey);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = AuthenticationOption.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = AuthenticationOption.Value.Audience,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}