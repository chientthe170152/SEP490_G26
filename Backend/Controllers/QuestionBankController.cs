using Backend.Common;
using Backend.DTOs.QuestionBank;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
public class QuestionBankController(IQuestionBankService _service) : ControllerBase
{
    // ── Teacher endpoints ────────────────────────────────────────────────────

    [HttpGet("api/question-banks")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> List([FromQuery] QuestionBankListQueryDto query)
    {
        var result = await _service.ListAsync(query);
        return result.ToActionResult(this);
    }

    [HttpGet("api/question-banks/{id:int}")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Admin)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPost("api/question-banks/personal")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> CreatePersonal([FromBody] CreatePersonalBankRequest request)
    {
        var result = await _service.CreatePersonalBankAsync(request);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.QuestionBankId }, result.Value);
    }

    [HttpPut("api/question-banks/personal/{id:int}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> UpdatePersonal(int id, [FromBody] UpdatePersonalBankRequest request)
    {
        var result = await _service.UpdatePersonalBankAsync(id, request);
        return result.ToActionResult(this);
    }

    [HttpPatch("api/question-banks/personal/{id:int}/archive")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> Archive(int id)
    {
        var result = await _service.ArchivePersonalBankAsync(id);
        return result.ToActionResult(this);
    }

    // ── Admin endpoints ──────────────────────────────────────────────────────

    [HttpPost("api/admin/question-banks/repair-shared")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> RepairShared()
    {
        var result = await _service.RepairSharedBanksAsync();
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Ok(new { createdSharedBankCount = result.Value });
    }
}
