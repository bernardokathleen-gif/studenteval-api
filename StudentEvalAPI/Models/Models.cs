namespace StudentEvalAPI.Models
{
    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int UserID { get; set; }
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string StudentNo { get; set; } = "";
        public int? CourseID { get; set; }
    }

    public class DashboardStats
    {
        public int TotalStudents { get; set; }
        public int WithLackings { get; set; }
        public int GradCandidates { get; set; }
        public int TotalSubjects { get; set; }
    }

    public class SubjectGrade
    {
        public int EvaluationID { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectTitle { get; set; } = "";
        public int Units { get; set; }
        public int SemNumber { get; set; }
        public string SchoolYear { get; set; } = "";
        public decimal? Grade { get; set; }
        public string Remarks { get; set; } = "";
        public bool IsComplete { get; set; }
    }

    public class Lacking
    {
        public int LackingID { get; set; }
        public string LackingType { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Document
    {
        public int DocumentID { get; set; }
        public string DocumentType { get; set; } = "";
        public string FileName { get; set; } = "";
        public bool IsVerified { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class GraduationApp
    {
        public int AppID { get; set; }
        public string SemDisplay { get; set; } = "";
        public string Status { get; set; } = "";
        public string Remarks { get; set; } = "";
        public DateTime AppliedAt { get; set; }
    }

    public class RecentLacking
    {
        public string StudentNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string LackingType { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class RecentGradApp
    {
        public string StudentNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AppliedAt { get; set; }
    }
    public class GraduationRequest
    {
        public int StudentId { get; set; }
        public int SemesterId { get; set; }
        public string? Remarks { get; set; }
    }
}
