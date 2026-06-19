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
            .Append(_mlContext.MulticlassClassification.Trainers.LightGbm(
                new Microsoft.ML.Trainers.LightGbm.LightGbmMulticlassTrainer.Options
                {
                    LabelColumnName = "LabelKey",
                    FeatureColumnName = "Features",
                    NumberOfLeaves = 15,
                    MinimumExampleCountPerLeaf = 2,
                    LearningRate = 0.1
                }))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "LabelKey");

        return (model, metrics.MacroAccuracy);
    }



    public ITransformer TrainProfileMatchRegressionModel(IEnumerable<TourProfileMatchExample> examples)
    {
        var data = _mlContext.Data.LoadFromEnumerable(examples);
        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(TourProfileMatchExample.TourPrice),
                nameof(TourProfileMatchExample.TourDuration),
                nameof(TourProfileMatchExample.ProfileBudget),
                nameof(TourProfileMatchExample.ProfileDuration),
                nameof(TourProfileMatchExample.CityMatch),
                nameof(TourProfileMatchExample.InterestMatchScore),
                nameof(TourProfileMatchExample.AdventureLevel),
                nameof(TourProfileMatchExample.AgeSuitability),
                nameof(TourProfileMatchExample.DifficultyScore))
            .Append(_mlContext.Regression.Trainers.LightGbm(
                new Microsoft.ML.Trainers.LightGbm.LightGbmRegressionTrainer.Options
                {
                    LabelColumnName = nameof(TourProfileMatchExample.Label),
                    FeatureColumnName = "Features",
                    NumberOfLeaves = 31,
                    MinimumExampleCountPerLeaf = 5,
                    LearningRate = 0.05
                }));

        return pipeline.Fit(data);
    }


}
