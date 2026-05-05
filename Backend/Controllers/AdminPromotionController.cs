using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs.Promotion;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/promotion-requests")]
[Authorize(Roles = RoleIds.Admin)]
public class AdminPromotionController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public AdminPromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpGet]
    public async Task<IActionResult> ListAdminAsync(
        [FromQuery] int? status, 
        [FromQuery] int? subjectId, 
        [FromQuery] int? teacherId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var result = await _promotionService.ListAdminAsync(status, subjectId, teacherId, page, pageSize);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetailAsync(int id)
    {
        var result = await _promotionService.GetDetailAsync(id, isAdmin: true);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:int}/finalize")]
    public async Task<IActionResult> FinalizeAsync(int id, [FromBody] FinalizePromotionRequest request)
    {
        var result = await _promotionService.FinalizeAsync(id, request);
        return result.ToActionResult(this);
    }
}
