using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace MqttWebSocket
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>();
                
    }
}
