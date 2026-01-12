using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// MVC（你原本的頁面）
builder.Services.AddControllersWithViews();

// Swagger（API 文件/測試）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB 連線（給 Dapper 用）
builder.Services.AddScoped<SqlConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// 先強制開 Swagger（新手最好這樣，避免環境判斷問題）
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers(); // ★ 一定要有：API Controller 才會生效
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();