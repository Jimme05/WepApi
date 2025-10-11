using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;
using SimpleAuthBasicApi.Models;


[Route("api/[controller]")]
[ApiController]
public class GamesController : ControllerBase
{
    private readonly AppDbContext _db;

    public GamesController(AppDbContext db)
    {
        _db = db;
    }

    // ✅ GET /api/games
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Game>>> GetGames()
    {
        return await _db.Games.ToListAsync();
    }

    // ✅ POST /api/games
    [HttpPost]
    public async Task<IActionResult> AddGame([FromBody] Game game)
    {
        game.ReleaseDate = DateTime.Now;
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return Ok(game);
    }

    // ✅ PUT /api/games/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGame(int id, [FromBody] Game updated)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound();

        game.Title = updated.Title;
        game.Genre = updated.Genre;
        game.Description = updated.Description;
        game.Price = updated.Price;

        // ✅ เก็บชื่อไฟล์หรือ URL ของรูป
        if (!string.IsNullOrEmpty(updated.ImagePath))
            game.ImagePath = updated.ImagePath;

        await _db.SaveChangesAsync();
        return Ok(game);
    }

    // ✅ DELETE /api/games/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGame(int id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound();
        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Game deleted" });
    }
}
