using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/records")]
public class RecordsApiController : ControllerBase
{
    private readonly SqlConnection _db;
    public RecordsApiController(SqlConnection db) => _db = db;

    // 查全部記帳（含分類名稱與類型）
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sql = @"
SELECT
  r.RecordID,
  r.CategoryID,
  r.Title,
  r.Amount,
  r.RecordDate,
  r.Note,
  c.Name AS CategoryName,
  c.Type AS CategoryType
FROM dbo.Records r
JOIN dbo.Categories c ON r.CategoryID = c.CategoryID
ORDER BY r.RecordDate DESC, r.RecordID DESC;
";
        var data = await _db.QueryAsync(sql);
        return Ok(data);
    }

    // 新增記帳
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecordDto dto)
    {
        var sql = @"
INSERT INTO dbo.Records (CategoryID, Title, Amount, RecordDate, Note)
VALUES (@CategoryID, @Title, @Amount, @RecordDate, @Note);
SELECT CAST(SCOPE_IDENTITY() as int);
";

        var param = new
        {
            dto.CategoryID,
            dto.Title,
            dto.Amount,
            RecordDate = dto.RecordDate ?? DateTime.Now,
            dto.Note
        };

        try
        {
            var id = await _db.ExecuteScalarAsync<int>(sql, param);
            return Created($"/api/records/{id}", new { RecordID = id });
        }
        catch (SqlException ex) when (ex.Number == 547) // FK fail
        {
            return BadRequest(new { message = "CategoryID 不存在（外鍵失敗）" });
        }
    }

    // 更新記帳
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecordDto dto)
    {
        var sql = @"
UPDATE dbo.Records
SET CategoryID=@CategoryID,
    Title=@Title,
    Amount=@Amount,
    RecordDate=@RecordDate,
    Note=@Note
WHERE RecordID=@id;
";

        try
        {
            var rows = await _db.ExecuteAsync(sql, new
            {
                id,
                dto.CategoryID,
                dto.Title,
                dto.Amount,
                RecordDate = dto.RecordDate ?? DateTime.Now,
                dto.Note
            });

            return rows == 0 ? NotFound(new { message = "找不到該筆 RecordID" }) : NoContent();
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return BadRequest(new { message = "CategoryID 不存在（外鍵失敗）" });
        }
    }

    // 刪除記帳
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rows = await _db.ExecuteAsync(
            "DELETE FROM dbo.Records WHERE RecordID=@id",
            new { id });

        return rows == 0
            ? NotFound(new { message = "找不到該筆 RecordID" })
            : NoContent();
    }
    // 月統計：收入 / 支出 / 結餘
    [HttpGet("summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int year, [FromQuery] int month)
    {
        var sql = @"
SELECT
    COALESCE(SUM(CASE WHEN c.Type = 'Income' THEN r.Amount END), 0) AS TotalIncome,
    COALESCE(SUM(CASE WHEN c.Type = 'Expense' THEN r.Amount END), 0) AS TotalExpense
FROM dbo.Records r
JOIN dbo.Categories c ON r.CategoryID = c.CategoryID
WHERE YEAR(r.RecordDate) = @year
  AND MONTH(r.RecordDate) = @month;
";

        dynamic result = await _db.QueryFirstOrDefaultAsync(sql, new { year, month });

        int income = (int)result.TotalIncome;
        int expense = (int)result.TotalExpense;

        return Ok(new
        {
            Year = year,
            Month = month,
            TotalIncome = income,
            TotalExpense = expense,
            Balance = income - expense
        });
    }


    // Swagger 會顯示這些 DTO 作為輸入格式
    public record CreateRecordDto(int CategoryID, string Title, int Amount, DateTime? RecordDate, string? Note);
    public record UpdateRecordDto(int CategoryID, string Title, int Amount, DateTime? RecordDate, string? Note);

}
