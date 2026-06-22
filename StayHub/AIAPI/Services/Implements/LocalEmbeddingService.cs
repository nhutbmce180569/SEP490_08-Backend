using System;
using System.Linq;
using System.Numerics.Tensors;
using AIAPI.Services;
using SmartComponents.LocalEmbeddings;

namespace AIAPI.Services.Implements;

public class LocalEmbeddingService : ILocalEmbeddingService
{
    private readonly LocalEmbedder _embedder;

    public LocalEmbeddingService()
    {
        _embedder = new LocalEmbedder();
    }

    public float[] EmbedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }
        var embedding = _embedder.Embed(text);
        return embedding.Values.ToArray();
    }

    public float CosineSimilarity(float[] left, float[] right)
    {
        if (left == null || right == null || left.Length == 0 || right.Length == 0 || left.Length != right.Length)
        {
            return 0f;
        }
        return TensorPrimitives.CosineSimilarity(left.AsSpan(), right.AsSpan());
    }
}
