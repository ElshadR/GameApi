using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathematicGameApi.Infrastructure.Models
{
    public class Question
    {
        public Question()
        {
            AddedDate = DateTime.Now.AddHours(-1);
        }
        public DateTime AddedDate { get; set; }
        public int Id { get; set; }

        public string Text { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public ICollection<Variant> Variants { get; set; }
    }
}
