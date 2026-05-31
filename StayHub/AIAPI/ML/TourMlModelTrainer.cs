using System.Numerics.Tensors;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace AIAPI.ML;

public class TourMlModelTrainer
{
    private readonly MLContext _mlContext = new(seed: 42);

    public (ITransformer Model, double Accuracy) TrainIntentModel(IEnumerable<IntentExample> samples)
    {
        var data = _mlContext.Data.LoadFromEnumerable(samples);
        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey("LabelKey", nameof(IntentExample.Label))
            .Append(_mlContext.Transforms.Text.FeaturizeText("Features", nameof(IntentExample.Text)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                new SdcaMaximumEntropyMulticlassTrainer.Options
                {
                    LabelColumnName = "LabelKey",
                    FeatureColumnName = "Features"
                }))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "LabelKey");

        return (model, metrics.MacroAccuracy);
    }

    public ITransformer TrainTextSearchModel(IEnumerable<TourDocument> documents)
    {
        var data = _mlContext.Data.LoadFromEnumerable(documents.Select(d => new TextRow { Text = d.Text }));
        var pipeline = _mlContext.Transforms.Text.NormalizeText("Normalized", nameof(TextRow.Text))
            .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "Normalized"));

        return pipeline.Fit(data);
    }

    public ITransformer TrainTourismSearchModel(IEnumerable<TourismDocument> documents)
    {
        var data = _mlContext.Data.LoadFromEnumerable(documents.Select(d => new TextRow { Text = d.Text }));
        var pipeline = _mlContext.Transforms.Text.NormalizeText("Normalized", nameof(TextRow.Text))
            .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "Normalized"));

        return pipeline.Fit(data);
    }

    public float[] GetFeatureVector(ITransformer model, string text)
    {
        var engine = _mlContext.Model.CreatePredictionEngine<TextRow, FeatureRow>(model);
        return engine.Predict(new TextRow { Text = text }).Features ?? Array.Empty<float>();
    }

    public float CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || right.Length == 0 || left.Length != right.Length)
        {
            return 0f;
        }

        return TensorPrimitives.CosineSimilarity(left.AsSpan(), right.AsSpan());
    }

    private class TextRow
    {
        public string Text { get; set; } = "";
    }

    private class FeatureRow
    {
        [VectorType]
        public float[]? Features { get; set; }
    }
}
