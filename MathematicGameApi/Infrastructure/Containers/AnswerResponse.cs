namespace MathematicGameApi.Infrastructure.Containers
{
    public class AnswerResponse<T>
    {
        public ResultCodes Code { get; set; }
        public T Data { get; set; }
    }
}