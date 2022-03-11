namespace ShadowBot
{
    internal class Program
    {
        public static void Main(string[] _)
        {
            new Bot().RunAsync().GetAwaiter().GetResult();
        }
    }
}