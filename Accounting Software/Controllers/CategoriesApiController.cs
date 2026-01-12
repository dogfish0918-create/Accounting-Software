using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api/categories")]
public class CategoriesApiController : ControllerBase
{
    private readonly IDbConnection _db;
    public CategoriesApiController(IDbConnection db) => _db = db;

    // 取得所有分類
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sql = @"
SELECT
  CategoryID,
  Name,
  Type
FROM Categories
ORDER BY Type, Name;
";
        var data = await _db.QueryAsync(sql);
        return Ok(data);
    }
}