using Microsoft.AspNetCore.Http;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class SendConfirmationCodeDto
    {
        public string Email { get; set; }
    }
}