using MathematicGameApi.Infrastructure.Enums;

namespace MathematicGameApi.Infrastructure.Containers.Requests
{
    public class CreateRoomDto
    {
        public int UserCount { get; set; }
        public RoomType Type { get; set; }      
    }
}