using System.ComponentModel.DataAnnotations;

public class CaseDocumentUploadRequest
{
    [Required]
    public IFormFile File { get; set; }
}
