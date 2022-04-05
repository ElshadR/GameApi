using System;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class InviteFriendResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserResponse UserResponse { get; set; }
        public int FriendId { get; set; }
        public int RoomId { get; set; }

        public DateTime AddedDate { get; set; }
        public DateTime ApprovedDate { get; set; }
    }
}
