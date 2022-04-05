using System.Collections.Generic;
using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Photo { get; set; }
        public int Level { get; set; }
        public int Life { get; set; }

        public string LevelName { get; set; }
        public int Score { get; set; }
        public UserPosition UserPosition { get; set; }
        public List<UserResponse> UserFriends { get; set; }
    }
}