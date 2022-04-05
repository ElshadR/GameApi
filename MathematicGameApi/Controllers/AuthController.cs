using System.Threading.Tasks;
using MathematicGameApi.Infrastructure.Containers.Requests;
using MathematicGameApi.Infrastructure.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MathematicGameApi.Controllers
{
    public class AuthController : BaseController
    {
        private readonly ICoreService _coreService;

        public AuthController(ICoreService coreService)
        {
            _coreService = coreService;
        }
        
        [HttpPost("checkUserName")]
        public async Task<IActionResult> CheckUserName([FromBody] CheckUserNameDto request)
        {
            var response = await _coreService.CheckUserName(request.UserName);

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var response = await _coreService.Register(request.UserName, request.Email, request.Password);

            return (response == null) ? BadRequest() : Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var response = await _coreService.Login(request.UserName, request.Password);

            return (response == null) ? NotFound("User") : Ok(response);
        }

        [HttpPost("updateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserDto request)
        {
            var response = await _coreService.UpdateUser(request.Id, request.UserName, request.Photo);

            return Ok(response);
        }
        
        [HttpPost("sendConfirmationCode")]
        public async Task<IActionResult> SendConfirmationCode([FromBody] SendConfirmationCodeDto request)
        {
            var response = await _coreService.SendConfirmationCode(request.Email);

            return Ok(response);
        }
        [HttpPost("checkConfirmationCode")]
        public async Task<IActionResult> CheckConfirmationCode([FromBody] CheckConfirmationCodeDto request)
        {
            var response = await _coreService.CheckConfirmationCode(request.Code);

            return Ok(response);
        }
    }
}