namespace ShadowBot.DatabaseModels
{
    internal class Guild
    {
        public ulong Id { get; set; }
        public ulong? ModelAlertsChannelId { get; set; }
        public ulong? ReportChannelId { get; set; }
    }
}
