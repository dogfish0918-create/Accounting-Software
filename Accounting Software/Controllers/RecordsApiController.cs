using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api/records")]
public class RecordsApiController : ControllerBase
{
    private readonly IDbConnection _db;
    public RecordsApiController(IDbConnection db) => _db = db;

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
FROM Records r
JOIN Categories c ON r.CategoryID = c.CategoryID
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
INSERT INTO Records (CategoryID, Title, Amount, RecordDate, Note)
VALUES (@CategoryID, @Title, @Amount, @RecordDate, @Note);
SELECT last_insert_rowid();
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
            // SQLite last_insert_rowid() 通常回 long
            var id = await _db.ExecuteScalarAsync<long>(sql, param);
            return Created($"/api/records/{id}", new { RecordID = id });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "新增失敗：CategoryID 不存在或資料不合法" });
        }
    }

    // 更新記帳
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecordDto dto)
    {
        var sql = @"
UPDATE Records
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

            return rows == 0
                ? NotFound(new { message = "找不到該筆 RecordID" })
                : NoContent();
        }
        catch (Exception)
        {
            return BadRequest(new { message = "更新失敗：CategoryID 不存在或資料不合法" });
        }
    }

    // 刪除記帳
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rows = await _db.ExecuteAsync(
            "DELETE FROM Records WHERE RecordID=@id",
            new { id });

        return rows == 0
            ? NotFound(new { message = "找不到該筆 RecordID" })
            : NoContent();
    }

    // 月統計：收入 / 支出 / 結餘（SQLite 版）
    [HttpGet("summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int year, [FromQuery] int month)
    {
        var sql = @"
SELECT
  COALESCE(SUM(CASE WHEN c.Type = 'Income' THEN r.Amount ELSE 0 END), 0) AS TotalIncome,
  COALESCE(SUM(CASE WHEN c.Type = 'Expense' THEN r.Amount ELSE 0 END), 0) AS TotalExpense
FROM Records r
JOIN Categories c ON r.CategoryID = c.CategoryID
WHERE strftime('%Y', r.RecordDate) = printf('%04d', @year)
  AND strftime('%m', r.RecordDate) = printf('%02d', @month);
";

        var result = await _db.QueryFirstOrDefaultAsync(sql, new { year, month });

        // SQLite/Dapper 可能回 long/double，這樣轉最穩
        long income = result?.TotalIncome == null ? 0 : Convert.ToInt64(result.TotalIncome);
        long expense = result?.TotalExpense == null ? 0 : Convert.ToInt64(result.TotalExpense);

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
