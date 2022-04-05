using Microsoft.AspNetCore.Http;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class CheckConfirmationCodeDto
    {
        
        public string Code { get; set; }
    }
}