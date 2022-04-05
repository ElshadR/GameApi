using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class CheckPositionDto
    {
        public int Id { get; set; }
        public UserPosition Status { get; set; }
    }
}
