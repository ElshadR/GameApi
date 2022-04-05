using MathematicGameApi.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathematicGameApi.Infrastructure.Extensions;

namespace MathematicGameApi.Infrastructure.Models
{
    public class Room
    {
        public Room()
        {
            AddedDate = HelperExtensions.BakuDateNowToTurkeyDate();
        }
        public DateTime AddedDate { get; set; }
        public DateTime? StartinDate { get; set; }
        public DateTime? EndedDate { get; set; }
        public int Id { get; set; }
        public RoomType Type { get; set; }
        public int UserCount { get; set; }
        public int ResponseCount { get; set; }
        public string AgainKey { get; set; }
        public int CreatedUserId { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<UserRoom> UserRooms { get; set; }

    }
}
