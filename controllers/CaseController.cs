using CaseRelayAPI.Models;
using CaseRelayAPI.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CaseRelayAPI.Data;
using Microsoft.Data.SqlClient;

namespace CaseRelayAPI.Controllers
{
    /// <summary>
    /// Controller for managing case-related operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CaseController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CaseController> _logger;

        /// <summary>
        /// Initializes a new instance of the CaseController
        /// </summary>
        public CaseController(ICaseService caseService, 
            ICloudinaryService cloudinaryService, 
            ApplicationDbContext context, 
            ILogger<CaseController> logger)
        {
            _caseService = caseService ?? throw new ArgumentNullException(nameof(caseService));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all cases for the logged-in user.
        /// </summary>
        /// <returns>A list of cases associated with the logged-in user.</returns>
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetCasesByUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or missing userId." });
            }

            int userId = int.Parse(userIdClaim.Value);
            var cases = await _caseService.GetCasesByUserIdAsync(userId);

            if (cases == null || cases.Count == 0)
                return NotFound(new { message = "No cases found for this user." });

            return Ok(cases);
        }

        /// <summary>
        /// Retrieves a case by its ID.
        /// </summary>
        /// <param name="caseId">The ID of the case.</param>
        /// <returns>The details of the case.</returns>
        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetCaseById(int caseId)
        {
            var caseDetails = await _caseService.GetCaseByIdAsync(caseId);
            if (caseDetails == null)
                return NotFound(new { message = "Case not found." });

            return Ok(caseDetails);
        }

        /// <summary>
        /// Creates a new case.
        /// </summary>
        /// <param name="newCase">The new case to create.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCase([FromBody] Case newCase)
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or missing userId." });
            }

            newCase.CreatedBy = int.Parse(userIdClaim.Value);

            if (User.IsInRole("Admin"))
            {
                newCase.IsApproved = true;
            }

            var success = await _caseService.CreateCaseAsync(newCase);
            if (!success)
            {
                return BadRequest(new { message = "Error creating case." });
            }

            return Ok(new { message = "Case created successfully." });
        }

        /// <summary>
        /// Approves a case.
        /// </summary>
        /// <param name="caseId">The ID of the case to approve.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPatch("{caseId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCase(int caseId)
        {
            var success = await _caseService.ApproveCaseAsync(caseId);
            if (!success)
            {
                return BadRequest(new { message = "Error approving case." });
            }

            return Ok(new { message = "Case approved successfully." });
        }

        /// <summary>
        /// Updates a case.
        /// </summary>
        /// <param name="caseId">The ID of the case to update.</param>
        /// <param name="updatedCase">The updated case details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPut("{caseId}")]
        public async Task<IActionResult> UpdateCase(int caseId, [FromBody] Case updatedCase)
        {
            updatedCase.CaseId = caseId;
            var success = await _caseService.UpdateCaseAsync(updatedCase);
            if (!success)
                return BadRequest(new { message = "Error updating case." });

            return Ok(new { message = "Case updated successfully." });
        }

        /// <summary>
        /// Retrieves all cases.
        /// </summary>
        /// <returns>A list of all cases.</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCases()
        {
            try
            {
                var cases = await _caseService.GetAllCasesAsync();
                if (cases == null || !cases.Any())
                {
                    return NotFound(new { message = "No cases available." });
                }

                return Ok(cases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving cases.", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a case.
        /// </summary>
        /// <param name="caseId">The ID of the case to delete.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteCase(int caseId)
        {
            var success = await _caseService.DeleteCaseAsync(caseId);
            if (!success)
                return NotFound(new { message = "Case not found." });

            return Ok(new { message = "Case deleted successfully." });
        }

        /// <summary>
        /// Hands over a case to a new officer.
        /// </summary>
        /// <param name="caseId">The ID of the case to hand over.</param>
        /// <param name="request">The handover request details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPost("handover/{caseId}")]
        public async Task<IActionResult> HandoverCase(int caseId, [FromBody] CaseHandoverRequest request)
        {
            if (string.IsNullOrEmpty(request.NewOfficerId))
            {
                return BadRequest(new { message = "Invalid officer ID provided." });
            }

            var success = await _caseService.HandoverCaseAsync(caseId, request.NewOfficerId);
            if (!success)
            {
                return BadRequest(new { message = "Error handing over the case. Check details and try again." });
            }

            return Ok(new { message = "Case successfully handed over to the new officer." });
        }

        /// <summary>
        /// Adds a document to a case.
        /// </summary>
        /// <param name="caseId">The ID of the case.</param>
        /// <param name="file">The document file to add.</param>
        /// <returns>A message indicating the result of the operation.</returns>
[HttpPost("{caseId}/document")]
[Consumes("multipart/form-data")]
[ApiExplorerSettings(IgnoreApi = true)]
public async Task<IActionResult> AddDocument(int caseId, [FromForm] IFormFile file)
{
    _logger.LogInformation("Starting AddDocument for caseId: {CaseId}", caseId);

    if (file == null || file.Length == 0)
    {
        _logger.LogWarning("AddDocument failed: Invalid file for caseId: {CaseId}", caseId);
        return BadRequest(new { message = "Invalid file." });
    }

    string fileUrl;

    try
    {
        _logger.LogInformation("Attempting to upload file to cloud for caseId: {CaseId}", caseId);
        fileUrl = await _cloudinaryService.UploadDocumentAsync(file);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error uploading file to cloud for caseId: {CaseId}", caseId);
        return StatusCode(500, new { message = ex.Message });
    }

    var document = new CaseDocument
    {
        CaseId = caseId,
        FileName = file.FileName,
        FileUrl = fileUrl,
        UploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown"
    };

    try
    {
        _logger.LogInformation("Attempting to save document to database for caseId: {CaseId}", caseId);
        var success = await _caseService.AddDocumentAsync(document);

        if (!success)
        {
            _logger.LogWarning("Failed to save document to database for caseId: {CaseId}", caseId);
            return BadRequest(new { message = "Error saving document to database." });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Exception occurred while saving document to database for caseId: {CaseId}", caseId);
        return StatusCode(500, new { message = ex.Message });
    }

    _logger.LogInformation("Document uploaded and saved successfully for caseId: {CaseId}, FileUrl: {FileUrl}", caseId, fileUrl);
    return Ok(new { message = "Document uploaded successfully.", fileUrl });
}


        /// <summary>
        /// Adds a comment to a case.
        /// </summary>
        /// <param name="caseId">The ID of the case.</param>
        /// <param name="newComment">The new comment to add.</param>
        /// <returns>The updated case details.</returns>
        [HttpPost("{caseId}/comment")]
        public async Task<IActionResult> AddComment(int caseId, [FromBody] CaseComment newComment)
        {
            if (newComment == null || string.IsNullOrWhiteSpace(newComment.CommentText))
            {
                return BadRequest(new { message = "Comment text is required." });
            }

            var updatedCase = await _caseService.AddCommentAsync(caseId, newComment);

            if (updatedCase == null)
            {
                return NotFound(new { message = $"Case with CaseId {caseId} does not exist." });
            }

            return Ok(new { message = "Comment added successfully.", data = updatedCase });
        }

        /// <summary>
        /// Retrieves case details with additional information.
        /// </summary>
        /// <param name="caseId">The ID of the case.</param>
        /// <returns>The case details with additional information.</returns>
        [HttpGet("{caseId}/extras")]
        public async Task<IActionResult> GetCaseByIdWithExtras(int caseId)
        {
            var caseDetails = await _caseService.GetCaseDetailsWithExtrasAsync(caseId);
            if (caseDetails == null)
                return NotFound(new { message = "Case not found." });

            return Ok(caseDetails);
        }

        /// <summary>
        /// Searches cases by keyword.
        /// </summary>
        /// <param name="keyword">The keyword to search for.</param>
        /// <returns>A list of cases matching the keyword.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchCases([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "Keyword is required for searching." });
            }

            var cases = await _caseService.SearchCasesAsync(keyword);

            if (cases == null || cases.Count == 0)
            {
                return NotFound(new { message = "No cases found matching the keyword." });
            }

            return Ok(cases);
        }

        /// <summary>
        /// Retrieves case statistics.
        /// </summary>
        /// <returns>The case statistics.</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetCaseStatistics()
        {
            var statistics = await _caseService.GetCaseStatisticsAsync();

            if (statistics == null)
            {
                return StatusCode(500, new { message = "Error retrieving case statistics." });
            }

            return Ok(statistics);
        }

        /// <summary>
        /// Updates the status of a case.
        /// </summary>
        /// <param name="caseId">The ID of the case.</param>
        /// <param name="request">The status update request details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPatch("{caseId}/status")]
        public async Task<IActionResult> UpdateCaseStatus(int caseId, [FromBody] CaseStatusUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Status is required." });
            }

            var success = await _caseService.UpdateCaseStatusAsync(caseId, request.Status);

            if (!success)
            {
                return BadRequest(new { message = "Error updating case status." });
            }

            return Ok(new { message = "Case status updated successfully." });
        }

        /// <summary>
        /// Assigns a case to an officer.
        /// </summary>
        /// <param name="caseId">The ID of the case.</param>
        /// <param name="request">The assignment request details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPatch("{caseId}/assign")]
        public async Task<IActionResult> AssignCaseToOfficer(int caseId, [FromBody] CaseAssignmentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OfficerId))
            {
                return BadRequest(new { message = "Officer ID is required." });
            }

            var success = await _caseService.AssignCaseToOfficerAsync(caseId, request.OfficerId);

            if (!success)
            {
                return BadRequest(new { message = "Error assigning case to officer." });
            }

            return Ok(new { message = "Case assigned to officer successfully." });
        }
    }
}

public class CaseStatusUpdateRequest
{
    public string Status { get; set; }
}

public class CaseAssignmentRequest
{
    public string OfficerId { get; set; }
}
