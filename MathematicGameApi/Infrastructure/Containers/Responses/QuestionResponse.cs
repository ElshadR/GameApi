using System.Collections.Generic;

namespace MathematicGameApi.Infrastructure.Containers.Responses
{
    public class QuestionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; }
        
        public int BeforeCorrectVariantId { get; set; }
        public int CurrentCorrectVariantId { get; set; }
        public int Score { get; set; }
        public List<VariantResponse> VariantResponses { get; set; }
    }
}