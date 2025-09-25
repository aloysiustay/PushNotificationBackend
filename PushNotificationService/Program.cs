using Newtonsoft.Json.Linq;
using PushNotificationService;
using PushNotificationService.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()   
            .AllowAnyMethod()   
            .AllowAnyHeader();  
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    }
});
//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var pushService = app.Services.GetRequiredService<NotificationService>();

//_ = Task.Run(async () =>
//{
//    await Task.Delay(3000);
//    Console.WriteLine("Enter: Title; Message; Image");
//    while (true)
//    {
//        string? input = Console.ReadLine();
//        if (!string.IsNullOrEmpty(input))
//        {
//            string[] parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries);

//            if (parts.Length >= 3)
//            {
//                string title = parts[0].Trim();
//                string msg = parts[1].Trim();
//                string image = parts[2].Trim();
//                await pushService.SendAll(title, msg, image);
//            }
//            else
//            {
//                Console.WriteLine("You must enter at least 3 values separated by ';'");
//            }
//        }
//    }
//});

app.Run();
