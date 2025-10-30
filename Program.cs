using Concatena.Pdf.Controllers;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Newtonsoft;
using System.Reflection;
using System.Runtime.InteropServices;

try
{
    string dllPath = Path.Combine(AppContext.BaseDirectory, "libSkiaSharp.dll");
    if (File.Exists(dllPath))
    {
        NativeLibrary.Load(dllPath);
        Console.WriteLine($"✅ libSkiaSharp.dll loaded successfully from: {dllPath}");
    }
    else
    {
        Console.WriteLine($"⚠️ libSkiaSharp.dll not found at: {dllPath}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Error loading libSkiaSharp.dll: " + ex.Message);
}

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// ?? Configuración log4net
// ----------------------
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
builder.Services.AddSingleton<ILog>(LogManager.GetLogger(typeof(Program)));

// ----------------------
// ?? Configuración de servicios
// ----------------------
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // Habilita Newtonsoft.Json en todo el proyecto

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Concatenamela", Version = "v1" });
});

builder.Services.AddSwaggerGenNewtonsoftSupport(); // Soporte Newtonsoft en Swagger

builder.Services.Configure<ConcatenadorPdfSettings>(
    builder.Configuration.GetSection("ConcatenadorPdfSettings"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<ConcatenadorPdfSettings>>().Value);

builder.Services.AddCors();

var app = builder.Build();

// ----------------------
// ?? Asegurar carga de libSkiaSharp.dll
// ----------------------
try
{
    var currentPath = AppContext.BaseDirectory;

    // Agrega la ruta actual al PATH del sistema en tiempo de ejecución
    Environment.SetEnvironmentVariable(
        "PATH",
        Environment.GetEnvironmentVariable("PATH") + ";" + currentPath
    );

    var log = app.Services.GetRequiredService<ILog>();
    log.Info($"PATH actualizado para SkiaSharp: {currentPath}");
}
catch (Exception ex)
{
    var log = app.Services.GetRequiredService<ILog>();
    log.Error("Error configurando PATH para SkiaSharp", ex);
}

// ----------------------
// ?? Endpoint /endpoints auxiliar
// ----------------------
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/endpoints")
    {
        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var endpoints = endpointDataSource.Endpoints;

        var listado = endpoints
            .Select(e => e.DisplayName)
            .Where(n => !string.IsNullOrEmpty(n));

        await context.Response.WriteAsync(string.Join("\n", listado));
    }
    else
    {
        await next();
    }
});

// ----------------------
// ?? Middleware principal
// ----------------------
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());
app.UseDeveloperExceptionPage();
//app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
