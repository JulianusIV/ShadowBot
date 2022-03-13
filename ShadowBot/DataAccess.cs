using ShadowBot.DatabaseModels;
using System.Data.SqlClient;

namespace ShadowBot
{
    internal class DataAccess
    {
        public SqlCommand DbCommand { get; set; }
        public SqlConnection DbConnection { get; set; }

        public DataAccess(string? connectionString)
        {
            if (connectionString is null)
                throw new ArgumentNullException(nameof(connectionString));
            DbConnection = new SqlConnection(connectionString);
            DbCommand = new SqlCommand("", DbConnection);
        }

        public Guild? GetGuild(ulong id)
        {
            Guild? entity = null;

            try
            {
                DbCommand.CommandText = "SELECT * FROM Guilds WHERE Id = @id;";
                DbCommand.Parameters.Clear();
                DbCommand.Parameters.AddWithValue("id", (long)id);
                DbConnection.Open();
                using SqlDataReader reader = DbCommand.ExecuteReader();
                reader.Read();
                long? tempModAlerts = reader["ModelAlertsChannelId"] is DBNull ? null : (long)reader["ModelAlertsChannelId"];
                long? tempReports = reader["ReportChannelId"] is DBNull ? null : (long)reader["ReportChannelId"];
                entity = new()
                {
                    Id = id,
                    ModelAlertsChannelId = (ulong?)tempModAlerts,
                    ReportChannelId = (ulong?)tempReports
                };
                DbConnection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (DbConnection.State == System.Data.ConnectionState.Open)
                {
                    DbConnection.Close();
                }
            }

            return entity;
        }

        public void CreateGuild(Guild guild)
        {
            try
            {
                DbCommand.CommandText = "INSERT INTO Guilds (Id) VALUES (@id);";
                DbCommand.Parameters.Clear();
                DbCommand.Parameters.AddWithValue("id", (long)guild.Id);
                DbConnection.Open();
                DbCommand.ExecuteNonQuery();
                DbConnection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (DbConnection.State == System.Data.ConnectionState.Open)
                {
                    DbConnection.Close();
                }
            }
        }

        public void UpdateGuild(Guild guild)
        {
            try
            {
                DbCommand.CommandText = "UPDATE Guilds SET ModelAlertsChannelId = @alerts, ReportChannelId = @reports WHERE Id = @id;";
                DbCommand.Parameters.Clear();
                DbCommand.Parameters.AddWithValue("id", (long)guild.Id);
                DbCommand.Parameters.AddWithValue("alerts", guild.ModelAlertsChannelId is null ? DBNull.Value : (long?)guild.ModelAlertsChannelId);
                DbCommand.Parameters.AddWithValue("reports", guild.ReportChannelId is null ? DBNull.Value : (long?)guild.ReportChannelId);
                DbConnection.Open();
                DbCommand.ExecuteNonQuery();
                DbConnection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (DbConnection.State == System.Data.ConnectionState.Open)
                {
                    DbConnection.Close();
                }
            }
        }
    }
}
