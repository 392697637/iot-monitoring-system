namespace IotMonitoringSystem.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 记录请求开始
            _logger.LogInformation("收到请求: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            
            try
            {
                await _next(context);
                
                stopwatch.Stop();
                
                // 记录请求完成
                _logger.LogInformation("请求完成: {Method} {Path} - {StatusCode} in {Elapsed}ms", 
                    context.Request.Method, context.Request.Path, 
                    context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "请求失败: {Method} {Path} - {StatusCode} in {Elapsed}ms", 
                    context.Request.Method, context.Request.Path, 
                    context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}