using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class ApproveOrCancelFriendRequestDto
    {
        public int Id { get; set; }  
        public UserFriendStatus Status { get; set; }  
    }
}