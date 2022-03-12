using Microsoft.ML.Data;

namespace ShadowBot.MLComponent.DataModels
{
    internal class ToxicOutputModel
    {
        [ColumnName(@"comment_text")]
        public float[] Comment_text { get; set; }

        [ColumnName(@"toxic")]
        public bool Toxic { get; set; }

        [ColumnName(@"Features")]
        public float[] Features { get; set; }

        [ColumnName(@"PredictedLabel")]
        public bool PredictedLabel { get; set; }

        [ColumnName(@"Score")]
        public float Score { get; set; }

        [ColumnName(@"Probability")]
        public float Probability { get; set; }
    }
}
