namespace Backend.Services.Interfaces;

/// <summary>
/// Gọi pynum-sep490 API để so sánh hai biểu thức toán học LaTeX có tương đương hay không.
/// </summary>
public interface IMathGradingService
{
    /// <summary>
    /// Trả về true khi <paramref name="studentAnswer"/> tương đương toán học
    /// với <paramref name="expectedAnswer"/> (cả hai ở dạng LaTeX).
    /// </summary>
    Task<bool> IsEquivalentAsync(string expectedAnswer, string studentAnswer, CancellationToken ct = default);
}
