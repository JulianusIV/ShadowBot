using ShadowBot.MLComponent;

namespace ShadowBot
{
    internal class Program
    {
        public static void Main(string[] _)
        {
#if DEBUG
            foreach (var line in File.ReadAllLines("settings.env"))
                Environment.SetEnvironmentVariable(line[..line.IndexOf('=')], line[(line.IndexOf('=') + 1)..]);
#endif
            ToxicModelManager.Init();
            new Bot().RunAsync().GetAwaiter().GetResult();
        }
    }
}