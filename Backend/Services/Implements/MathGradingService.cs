using Backend.Services.Interfaces;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Backend.Services.Implements;

/// <summary>
/// Gọi pynum-sep490 API (POST /api/compare) để so sánh hai biểu thức toán học LaTeX.
/// 
/// Ba lớp bảo vệ cho chấm điểm đồng thời cao (150 HS × 30 câu):
///   1. Fast Path — khớp chuỗi chính xác → bỏ qua API
///   2. Cache + Coalescing — các cặp (đáp án, câu trả lời) giống nhau dùng chung 1 HTTP call
///   3. Semaphore Throttle — giới hạn số HTTP call đồng thời để bảo vệ pynum
/// </summary>
public sealed class MathGradingService : IMathGradingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MathGradingService> _logger;

    // ── Lớp 2: Cache trong bộ nhớ ────────────────────────────
    // Key = "expected\0student"  →  Task<bool> khởi tạo lazy
    // Dùng Lazy<Task<bool>> đảm bảo 150 HS gửi cùng đáp án → chỉ 1 HTTP call, 149 await cùng Task.
    private static readonly ConcurrentDictionary<string, (Lazy<Task<bool>> Result, long Timestamp)> _cache = new();
    private const int MaxCacheSize = 10_000;

    // ── Lớp 3: Semaphore throttle ────────────────────────────
    // Bảo vệ container pynum khỏi thundering herd (tối đa 20 HTTP call đồng thời)
    private static readonly SemaphoreSlim _throttle = new(20, 20);

    public MathGradingService(HttpClient httpClient, ILogger<MathGradingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsEquivalentAsync(string expectedAnswer, string studentAnswer, CancellationToken ct = default)
    {
        // Chuẩn hóa
        var trimExpected = expectedAnswer.Trim();
        var trimStudent = studentAnswer.Trim();

        // ── Lớp 1: Fast path — khớp chuỗi chính xác ─────────
        if (trimExpected.Equals(trimStudent, StringComparison.OrdinalIgnoreCase))
            return true;

        // ── Lớp 2: Cache + Coalescing ────────────────────────
        var cacheKey = $"{trimExpected}\0{trimStudent}";
        var now = Environment.TickCount64;

        // Xóa từng entry cũ khi cache quá lớn (tránh race condition của Clear())
        if (_cache.Count > MaxCacheSize)
        {
            var keysToRemove = _cache
                .OrderBy(kv => kv.Value.Timestamp)
                .Take(_cache.Count / 2)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in keysToRemove)
                _cache.TryRemove(key, out _);
        }

        var entry = _cache.GetOrAdd(cacheKey, _ =>
            (new Lazy<Task<bool>>(() => CallPynumAsync(trimExpected, trimStudent, ct)), now));

        try
        {
            return await entry.Result.Value;
        }
        catch (Exception ex)
        {
            // Xóa entry lỗi để lần sau retry
            _cache.TryRemove(cacheKey, out _);
            _logger.LogWarning(ex, "Pynum API gọi thất bại cho '{Expected}' vs '{Student}', fallback = false",
                trimExpected, trimStudent);
            return false;
        }
    }

    private async Task<bool> CallPynumAsync(string expression1, string expression2, CancellationToken ct)
    {
        // ── Lớp 3: Throttle ─────────────────────────────────
        await _throttle.WaitAsync(ct);
        try
        {
            var payload = new PynumRequest(expression1, expression2);
            var response = await _httpClient.PostAsJsonAsync("api/compare", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pynum API trả về {StatusCode} cho '{Expr1}' vs '{Expr2}'",
                    response.StatusCode, expression1, expression2);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<PynumResponse>(cancellationToken: ct);
            return result?.Equivalent ?? false;
        }
        finally
        {
            _throttle.Release();
        }
    }

    // ── DTO cho pynum API ────────────────────────────────────

    private sealed record PynumRequest(
        [property: JsonPropertyName("expression1")] string Expression1,
        [property: JsonPropertyName("expression2")] string Expression2);

    private sealed record PynumResponse(
        [property: JsonPropertyName("equivalent")] bool Equivalent);
}
