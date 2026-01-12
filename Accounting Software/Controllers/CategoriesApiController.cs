using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/categories")]
public class CategoriesApiController : ControllerBase
{
    private readonly SqlConnection _db;
    public CategoriesApiController(SqlConnection db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sql = "SELECT CategoryID, Name, Type FROM dbo.Categories ORDER BY Type, Name";
        var data = await _db.QueryAsync(sql);
        return Ok(data);
    }
}