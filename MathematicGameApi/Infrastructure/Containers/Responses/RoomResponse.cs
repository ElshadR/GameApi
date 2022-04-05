using System;
using System.Collections.Generic;
using MathematicGameApi.Infrastructure.Enums;
using MathematicGameApi.Infrastructure.Models;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class RoomResponse
    {
        public int Id { get; set; }
        public RoomType Type { get; set; }
        public int UserCount { get; set; }
        public int CurrentUserCount { get; set; }
        public int CreatedUserId { get; set; }
        public UserResponse CreatedUser { get; set; }
        public List<UserResponse> AtRoomUsers { get; set; }

    }
}