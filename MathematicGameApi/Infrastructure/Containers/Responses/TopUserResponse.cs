using System.Collections.Generic;
using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class TopUserResponse
    {
        public string Place { get; set; }
        public int Id { get; set; }
        public string UserName { get; set; }
        public int Level { get; set; }
      
        public string LevelName { get; set; }
        public string Score { get; set; }
    }
}