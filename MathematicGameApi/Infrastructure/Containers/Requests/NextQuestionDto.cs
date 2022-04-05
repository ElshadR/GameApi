using Microsoft.AspNetCore.Http;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class NextQuestionDto
    {
        public int NextQuestion { get; set; }
        public int VariantId { get; set; }
        public int RoomId { get; set; }
    }
}