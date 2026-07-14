using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Services;

public interface ITokenService
{
    string CreateToken(IdentityUser user);
}