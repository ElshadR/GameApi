using Microsoft.AspNetCore.Http;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public IFormFile Photo { get; set; }
    }
}