using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class AgainPlayDto
    {
        public string AgainKey { get; set; }
        public int UserCount { get; set; }
        public RoomType Type { get; set; }      
    }
}