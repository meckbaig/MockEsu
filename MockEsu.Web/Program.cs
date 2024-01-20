using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using MockEsu.Web.Structure.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var exceptionMiddleware = new CustomExceptionHandler();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MockEsu.Web.xml"));
    options.OperationFilter<SwaggerDefaultValues>();
});
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
//builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();

        // Build a swagger endpoint for each discovered API version
        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();
            options.SwaggerEndpoint(url, name);
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception e)
    {
        await exceptionMiddleware.TryHandleAsync(context, e, new CancellationToken());
    }
});

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FilesController}");
app.UseCors(builder => builder
     .AllowAnyOrigin()
     .AllowAnyMethod()
     .AllowAnyHeader());
//app.MapEndpoints();

app.Run();


public partial class Program { }