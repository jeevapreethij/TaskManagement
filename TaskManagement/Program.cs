using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using TaskManagement.Data;
using TaskManagement.Middleware;
using TaskManagement.Repositories;
using TaskManagement.Services;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.WriteTo.Console()
	.CreateBootstrapLogger();

// Sink failures (file/database) are best-effort: reported to stderr, never fail the request.
Serilog.Debugging.SelfLog.Enable(message => Console.Error.WriteLine(message));

try
{
	Log.Information("Starting TaskManagement application");

	var builder = WebApplication.CreateBuilder(args);

	var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
	if (string.IsNullOrWhiteSpace(connectionString))
	{
		throw new InvalidOperationException("Configuration value 'ConnectionStrings:DefaultConnection' is required.");
	}

	builder.Host.UseSerilog((context, services, configuration) =>
	{
		var settings = context.Configuration.GetSection("Serilog");
		var filePath = settings["FilePath"] ?? "logs/taskmanagement-.log";
		var retained = settings.GetValue("RetainedFileCount", 30);
		var sizeLimit = settings.GetValue("FileSizeLimitBytes", 52428800L);
		var dbLevel = settings.GetValue("DatabaseMinimumLevel", LogEventLevel.Warning);

		configuration
			.MinimumLevel.Is(context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.WriteTo.File(
				filePath,
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: retained,
				fileSizeLimitBytes: sizeLimit,
				rollOnFileSizeLimit: true,
				outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.MSSqlServer(
				connectionString,
				new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = false },
				restrictedToMinimumLevel: dbLevel);
	});

	builder.Services.AddControllersWithViews(options =>
	{
		options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
	});

	builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
	builder.Services.AddScoped<ITaskRepository, TaskRepository>();
	builder.Services.AddScoped<ITaskService, TaskService>();

	var app = builder.Build();

	app.UseMiddleware<CorrelationIdMiddleware>();
	app.UseSerilogRequestLogging();

	if (!app.Environment.IsDevelopment())
	{
		app.UseExceptionHandler("/Home/Error");
		app.UseHsts();
	}

	app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");
	app.UseHttpsRedirection();
	app.UseRouting();

	app.UseAuthorization();

	app.MapStaticAssets();

	app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Tasks}/{action=Index}/{id?}")
		.WithStaticAssets();

	app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
	throw;
}
finally
{
	Log.Information("Shutting down TaskManagement application");
	Log.CloseAndFlush();
}
