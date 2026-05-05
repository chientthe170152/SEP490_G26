using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs.Promotion;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/promotion-requests")]
[Authorize(Roles = RoleIds.Teacher)]
public class PromotionRequestController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionRequestController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePromotionRequest request)
    {
        var result = await _promotionService.CreateAsync(request);
        if (!result.IsSuccess) return result.ToActionResult(this);

        return CreatedAtAction(nameof(GetDetailAsync), new { id = result.Value.PromotionRequestId }, result.Value);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> ListMineAsync([FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _promotionService.ListMineAsync(status, page, pageSize);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetailAsync(int id)
    {
        var result = await _promotionService.GetDetailAsync(id, isAdmin: false);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> WithdrawAsync(int id)
    {
        var result = await _promotionService.WithdrawAsync(id);
        return result.ToActionResult(this);
    }
}
