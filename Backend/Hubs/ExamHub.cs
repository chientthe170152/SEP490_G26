using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Backend.Hubs
{
    public class ExamHub : Hub
    {
        // Tracks ConnectionId -> (StudentId, ExamId) for disconnection handling
        private static readonly ConcurrentDictionary<string, StudentExamSession> ActiveSessions = new();

        public async Task JoinExamGroup(int examId, int studentId, string studentName)
        {
            string groupName = $"Exam_{examId}";
            
            // Add connection to the specific exam group
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Record session details
            var session = new StudentExamSession 
            { 
                ExamId = examId, 
                StudentId = studentId, 
                StudentName = studentName,
                JoinedAt = DateTime.UtcNow
            };
            ActiveSessions.TryAdd(Context.ConnectionId, session);

            // Broadcast to the group that the teacher is listening to
            await Clients.Group(groupName).SendAsync("StudentStatusChanged", new 
            {
                StudentId = studentId,
                StudentName = studentName,
                Status = "Online",
                Timestamp = DateTime.UtcNow
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ActiveSessions.TryRemove(Context.ConnectionId, out var session))
            {
                string groupName = $"Exam_{session.ExamId}";
                
                // Broadcast that the student has disconnected (offline)
                await Clients.Group(groupName).SendAsync("StudentStatusChanged", new 
                {
                    StudentId = session.StudentId,
                    StudentName = session.StudentName,
                    Status = "Offline",
                    Timestamp = DateTime.UtcNow,
                    Reason = exception?.Message ?? "Client disconnected gracefully"
                });
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public class StudentExamSession
    {
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
