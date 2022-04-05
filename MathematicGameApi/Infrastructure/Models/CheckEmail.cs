using System;

namespace MathematicGameApi.Infrastructure.Models
{
    public class CheckEmail
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
