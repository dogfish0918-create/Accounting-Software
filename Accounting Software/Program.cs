using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// MVC（你原本的頁面）
builder.Services.AddControllersWithViews();

// Swagger（API 文件/測試）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB 連線（給 Dapper 用）=> 改成 SQLite
builder.Services.AddScoped<IDbConnection>(_ =>
    new SqliteConnection(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// 第一次啟動自動建表 + 種子分類
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

    db.Execute(@"
CREATE TABLE IF NOT EXISTS Categories (
  CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT NOT NULL UNIQUE,
  Type TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Records (
  RecordID INTEGER PRIMARY KEY AUTOINCREMENT,
  CategoryID INTEGER NOT NULL,
  Title TEXT NOT NULL,
  Amount INTEGER NOT NULL,
  RecordDate TEXT NOT NULL,
  Note TEXT NULL,
  FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
");

    db.Execute(@"
INSERT OR IGNORE INTO Categories (Name, Type) VALUES
('薪水','Income'),
('獎金','Income'),
('飲食','Expense'),
('交通','Expense'),
('娛樂','Expense');
");
}

// 先強制開 Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers(); // API Controller

//  首頁 / 直接導到 wwwroot/index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

//  其他非 /api 的路徑，也回到前端（SPA 用）
app.MapFallbackToFile("index.html");

//  如果你還想保留 MVC（Home/Privacy 那些頁），就留著
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// ⭐ 啟動後自動開瀏覽器（只在非測試時）
var url = "http://localhost:5000";

Task.Run(async () =>
{
    await Task.Delay(1000); // 等伺服器啟動完成
    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch { }
});

app.Run();