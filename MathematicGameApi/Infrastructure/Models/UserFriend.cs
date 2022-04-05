using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Models
{
    public class UserFriend
    {
        public UserFriend()
        {
            AddedDate = DateTime.Now.AddHours(-1);
        }
        public DateTime AddedDate { get; set; }
        public UserFriendStatus UserFriendStatus { get; set; }
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int FriendId { get; set; }
    }
}
