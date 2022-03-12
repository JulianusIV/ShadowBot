using Microsoft.ML;
using Microsoft.ML.Data;
using ShadowBot.MLComponent.DataModels;
using System.Data.SqlClient;

namespace ShadowBot.MLComponent
{
    internal static class DataService
    {
        public static IDataView GetData(MLContext mLContext)
        {
            var loader = mLContext.Data.CreateDatabaseLoader<DataModel>();

            string sqlCommand = "SELECT comment_text, toxic FROM dbo.CommentData";

            DatabaseSource dbSource = new(SqlClientFactory.Instance, Environment.GetEnvironmentVariable("ConnectionString"), sqlCommand);

            return loader.Load(dbSource);
        }
    }
}
