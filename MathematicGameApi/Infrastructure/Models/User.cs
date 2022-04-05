using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Models
{
    public class User
    {
        public User()
        {
            AddedDate = DateTime.Now.AddHours(-1);
        }
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Photo { get; set; }
        public DateTime AddedDate { get; set; }
        public int Level { get; set; }
        public int Score { get; set; }
        public int Life { get; set; }
        public double LifeTime { get; set; }
        public DateTime LastGameTime { get; set; }
        public UserPosition UserPosition { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public ICollection<UserQuestionHistory> UserQuestionHistories { get; set; }
        public ICollection<UserFriend> UserFriends { get; set; }
        public ICollection<Room> Rooms { get; set; }
    }
}
