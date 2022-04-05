using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathematicGameApi.Infrastructure.Models
{
    public class Variant
    {
        public Variant()
        {
            AddedDate = DateTime.Now.AddHours(-1);
        }
        public DateTime AddedDate { get; set; }
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        public string Text { get; set; }
        public bool IsAnswer { get; set; }
        public ICollection<UserQuestionHistory> UserQuestionHistories { get; set; }
    }
}
