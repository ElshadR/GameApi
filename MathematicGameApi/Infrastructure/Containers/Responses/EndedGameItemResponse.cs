using System.Collections.Generic;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class EndedGameItemResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int Score { get; set; }
        public string LevelName { get; set; }
        public int CorrectAnswer { get; set; }
        public int WrongAnswer { get; set; }
        public string Photo { get; set; }
    }
}