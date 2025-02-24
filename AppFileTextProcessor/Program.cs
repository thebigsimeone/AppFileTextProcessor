using AppFileTextProcessor.Interface;
using AppFileTextProcessor.Service;
using AppFileTextProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura Kestrel per ascoltare su IP e porta specifica
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5180); // Usa la stessa porta di AppComunicazioni
});

// Add services to the container.
builder.Services.AddScoped<IAppTextProcessingService, AppTextProcessingService>();
builder.Services.AddScoped<IAnagraficaService, AnagraficaService>();
builder.Services.AddScoped<IComuniService, ComuniService>();
builder.Services.AddScoped<ITextProcessingService, TextProcessingService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

// Registrazione dei nuovi servizi
builder.Services.AddScoped<IExcelProcessingMassService, ExcelProcessingMassService>();
builder.Services.AddScoped<IAtecoService, AtecoService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

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
