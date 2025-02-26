using AppFileTextProcessor.Helpers;
using AppFileTextProcessor.Interface;
using AppFileTextProcessor.Services;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add services to the container.
builder.Services.AddScoped<IAppTextProcessingService, AppTextProcessingService>();
builder.Services.AddScoped<IAnagraficaService, AnagraficaService>();
builder.Services.AddScoped<IComuniService, ComuniService>();
builder.Services.AddScoped<ITextProcessingService, TextProcessingService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();

// Registrazione dei nuovi servizi
builder.Services.AddScoped<IExcelProcessingMassService, ExcelProcessingMassService>();
builder.Services.AddScoped<IAtecoService, AtecoService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TextFileProcessor API V2", Version = "v1" });

    // Abilita il supporto per `multipart/form-data` nei modelli con `IFormFile`
    c.SchemaFilter<SwaggerFileSchemaFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TextFileProcessor API V2");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
});

app.UseAuthorization();

app.MapControllers();

app.Run();
