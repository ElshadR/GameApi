using System.Collections.Generic;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class EndedGameResponse
    {
        public string AgainKey { get; set; }
        public List<EndedGameItemResponse> List { get; set; }
    }
}