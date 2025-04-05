using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using Xiaolongshu2Model;
using Xiaolongshu2Server.Dtos;

namespace Xiaolongshu2Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController(UserManager<WorldCitiesUser> userManager, JwtHandler jwtHandler) : ControllerBase
    {
        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponse>> LoginAsync(Dtos.LoginRequest loginRequest)
        {
            WorldCitiesUser? user = await userManager.FindByNameAsync(loginRequest.Username);

            if (user == null)
            {
                return Unauthorized("Unknown user");
            }

            bool success = await userManager.CheckPasswordAsync(user, loginRequest.Password);

            if (!success)
            {
                return Unauthorized("Incorrect password");
            }

            var jwtToken = await jwtHandler.GetTokenAsync(user);

            string tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return Ok(new LoginResponse()
            {
                Success = true,
                Message = "Login successful",
                Token = tokenString
            });
        }
    }
}
