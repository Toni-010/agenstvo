var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


app.UseDefaultFiles();
app.UseStaticFiles();


app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Path}");
    await next();
});


app.MapFallbackToFile("index.html");

app.Run();