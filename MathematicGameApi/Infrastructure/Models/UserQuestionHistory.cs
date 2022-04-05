using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathematicGameApi.Infrastructure.Models
{
    public class UserQuestionHistory
    {
        public UserQuestionHistory()
        {
            AddedDate = DateTime.Now.AddHours(-1);
        }
        public DateTime AddedDate { get; set; }
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public int VariantId { get; set; }
        public Variant Variant { get; set; }
    }
}
