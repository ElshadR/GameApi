using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class ApproveOrCancelPlayRequestDto
    {
        public int Id { get; set; }
        public UserPlayRequest Status { get; set; }
    }
}
