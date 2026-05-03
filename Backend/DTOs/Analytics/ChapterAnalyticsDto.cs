using System;
using System.Text.Json.Serialization;

namespace Backend.DTOs.Analytics;

public class ChapterAnalyticsDto
{
    public int ChapterId { get; set; }
    public string ChapterName { get; set; } = null!;
    
    // Tổng số câu trả lời của tất cả học sinh cho chương này
    public int TotalAnswers { get; set; }
    
    // Số câu trả lời đúng
    public int CorrectAnswers { get; set; }
    
    // Tỉ lệ đúng (0 - 100%)
    [JsonInclude]
    public double AccuracyRate => TotalAnswers == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalAnswers * 100, 2);
    
    // Status removed as it's no longer used.
}
