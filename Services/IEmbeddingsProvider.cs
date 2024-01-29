using System.Threading.Tasks;

public interface IEmbeddingsProvider
{
    Task<float[]> GetEmbeddingsAsync(string text);
}
