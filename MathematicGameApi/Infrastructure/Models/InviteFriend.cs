using System;

namespace MathematicGameApi.Infrastructure.Models
{
    public class InviteFriend
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int FriendId { get; set; }
        public int RoomId { get; set; }

        public DateTime AddedDate { get; set; }
        public DateTime ApprovedDate { get; set; }
    }
}
