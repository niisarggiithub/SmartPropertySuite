using Microsoft.AspNetCore.Mvc;

namespace SmartPropertySuite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : Controller
    {
        [HttpPost("login")]
        public async Task<IActionResult> IsValidLogin()
        {
            var authHeader = Request.Headers["Authorization"].ToString();

            var token = authHeader.Substring("Bearer ".Length).Trim();

            if (token != null)
            {
                return Ok("Token is valid");
            }

            return Unauthorized();
        }
    }
}
