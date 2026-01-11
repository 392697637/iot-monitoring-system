using IotMonitoringSystem.API.Data;
using IotMonitoringSystem.API.Hubs;
using IotMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog日志
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "IoT Monitoring System API",
        Version = "v1",
        Description = "物联网设备监测系统API"
    });
    c.EnableAnnotations();
});

// 添加SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100KB
});

// 添加数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// 添加CORS策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueAppPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Pagination");
    });
});

// 添加HttpClient
builder.Services.AddHttpClient();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加响应压缩
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IoT Monitoring API v1");
        c.RoutePrefix = "api-docs";
    });
    
    // 开发环境启用详细错误页面
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 应用中间件
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("VueAppPolicy");

// 启用静态文件服务（可选）
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// 配置端点
app.MapControllers();
app.MapHub<DeviceHub>("/hubs/device");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

//// 数据库迁移（开发环境）
//if (app.Environment.IsDevelopment())
//{
//    using var scope = app.Services.CreateScope();
//    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    try
//    {
//        await dbContext.Database.EnsureCreatedAsync();
//        await SeedData.InitializeAsync(dbContext);
//        Log.Information("数据库已成功初始化和种子数据已添加");
//    }
//    catch (Exception ex)
//    {
//        Log.Error(ex, "数据库初始化失败");
//        throw;
//    }
//}

try
{
    Log.Information("启动IoT监测系统API...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}