using System.ComponentModel.DataAnnotations;

public class Case
{
    public static readonly string[] ValidStatuses = new[]
    {
        "Pending",
        "Open",
        "Investigating",
        "Closed",
        "Resolved"
    };

    [Key]
    public int CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";  // Changed default from "Open" to "Pending"
    public string AssignedOfficerId { get; set; } = string.Empty;  
    public string? PreviousOfficerId { get; set; }
    public string? UserId { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string Severity { get; set; } = "Normal";
    public string? EvidenceFiles { get; set; }
    public string? Category { get; set; }
    public bool IsArchived { get; set; } = false;
    public int CreatedBy { get; set; }
    public bool IsApproved { get; set; } = false;
    public bool IsClosed { get; set; } = false;

    public ICollection<CaseComment> Comments { get; set; } = new List<CaseComment>();
    public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
}

public class CaseComment
{
    [Key]
    public int CommentId { get; set; }
    public int CaseId { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string AuthorId { get; set; } = "System";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CaseDocument
{
    [Key]
    public int DocumentId { get; set; }
    public int CaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

