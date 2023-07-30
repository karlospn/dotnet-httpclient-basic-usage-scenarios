
namespace HttpClientUsageScenarios.Net7.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient("typicode", c =>
            {
                c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
                c.Timeout = TimeSpan.FromSeconds(15);
                c.DefaultRequestHeaders.Add(
                    "accept", "application/json");
            }).SetHandlerLifetime(TimeSpan.FromSeconds(15));

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}