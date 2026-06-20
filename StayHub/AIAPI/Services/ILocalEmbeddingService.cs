namespace AIAPI.Services;

public interface ILocalEmbeddingService
{
    float[] EmbedText(string text);
    float CosineSimilarity(float[] left, float[] right);
}
