using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CaseRelayAPI.Models
{
    public class Case
    {
        [Key]
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string AssignedOfficerId { get; set; } = string.Empty;  
        public string? PreviousOfficerId { get; set; }
        public string? UserId { get; set; } 
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string Severity { get; set; } = "Normal";
        public string? EvidenceFiles { get; set; }
        public string? Category { get; set; }
        public bool IsArchived { get; set; } = false;
    }



    public class CaseComment
    {
        [Key]
        public int CommentId { get; set; }
        public int CaseId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
