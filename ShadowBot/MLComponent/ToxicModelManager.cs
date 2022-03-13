using Microsoft.ML;
using ShadowBot.MLComponent.DataModels;

namespace ShadowBot.MLComponent
{
    internal static class ToxicModelManager
    {
        static readonly MLContext MLContext;
        static ITransformer Model;

        static ToxicModelManager()
        {
            MLContext = new();
            if (File.Exists("model.zip"))
            {
                Model = MLContext.Model.Load("model.zip", out _);
            }
            else
                InitializeModel();
        }

        public static void Init() { }

        public static void InitializeModel()
        {
            var dataPrepEstimator = MLContext.Transforms.Text.FeaturizeText(inputColumnName: @"comment_text", outputColumnName: @"comment_text")
                                        .Append(MLContext.Transforms.Concatenate(@"Features", new[] { @"comment_text" }))
                                        .Append(MLContext.Transforms.NormalizeMinMax(@"Features", @"Features"))
                                        .Append(MLContext.BinaryClassification.Trainers.LbfgsLogisticRegression("toxic", "Features"));

            var data = DataService.GetData(MLContext);

            Model = dataPrepEstimator.Fit(data);

            MLContext.Model.Save(Model, data.Schema, "model.zip");
            //Model = MLContext.Model.Load("model.zip", out _);
        }

        public static void RetrainModel()
        {
            InitializeModel();
            //var preTrainedModel = MLContext.Model.Load("model.zip", out _);

            //{Microsoft.ML.Data.TransformerChain<Microsoft.ML.ITransformer>}
            //var originalParams = ((ISingleFeaturePredictionTransformer<object>)(Model as TransformerChain<ITransformer>).LastTransformer).Model; //((ISingleFeaturePredictionTransformer<object>)preTrainedModel).Model;

            //var originalParamsAsLMP = (originalParams as CalibratedModelParametersBase).SubModel as LinearModelParameters;

            //var newData = DataService.GetData(MLContext, true);

            //var transformedNewData = Model.Transform(newData);

            //var something = MLContext.BinaryClassification.Trainers.LbfgsLogisticRegression("toxic", "Features").Fit(transformedNewData, originalParamsAsLMP);

            //MLContext.Model.Save(something, newData.Schema, "something.zip");
        }

        public static ToxicOutputModel Predict(string text)
            => MLContext.Model.CreatePredictionEngine<DataModel, ToxicOutputModel>(Model).Predict(new() { comment_text = text });
    }
}
