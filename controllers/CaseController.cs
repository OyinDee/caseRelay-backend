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
    [ApiController]
    [Route("api/[controller]")]
    public class CaseController : ControllerBase
    {
        private readonly CaseService _caseService;
        private readonly CloudinaryService _cloudinaryService;
        private readonly ApplicationDbContext _context; // Injected DbContext

        public CaseController(CaseService caseService, CloudinaryService cloudinaryService, ApplicationDbContext context)
        {
            _caseService = caseService;
            _cloudinaryService = cloudinaryService;
            _context = context; // Assign the context
        }

        // Endpoint to retrieve all cases for the logged-in user
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

        // Endpoint to retrieve a case by its ID
        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetCaseById(int caseId)
        {
            var caseDetails = await _caseService.GetCaseByIdAsync(caseId);
            if (caseDetails == null)
                return NotFound(new { message = "Case not found." });

            return Ok(caseDetails);
        }

        // Endpoint to create a new case
        [HttpPost]
        public async Task<IActionResult> CreateCase([FromBody] Case newCase)
        {
            var success = await _caseService.CreateCaseAsync(newCase);
            if (!success)
                return BadRequest(new { message = "Error creating case." });

            return Ok(new { message = "Case created successfully." });
        }

        // Endpoint to update a case
        [HttpPut("{caseId}")]
        public async Task<IActionResult> UpdateCase(int caseId, [FromBody] Case updatedCase)
        {
            updatedCase.CaseId = caseId;
            var success = await _caseService.UpdateCaseAsync(updatedCase);
            if (!success)
                return BadRequest(new { message = "Error updating case." });

            return Ok(new { message = "Case updated successfully." });
        }

        // Endpoint to delete a case
        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteCase(int caseId)
        {
            var success = await _caseService.DeleteCaseAsync(caseId);
            if (!success)
                return NotFound(new { message = "Case not found." });

            return Ok(new { message = "Case deleted successfully." });
        }

        // Endpoint for case handover
        [HttpPost("handover/{caseId}")]
        public async Task<IActionResult> HandoverCase(int caseId, [FromBody] CaseHandoverRequest request)
        {
            var success = await _caseService.HandoverCaseAsync(caseId, request.NewOfficerId);
            if (!success)
                return BadRequest(new { message = "Error handing over the case." });

            return Ok(new { message = "Case successfully handed over to the new officer." });
        }

        // Endpoint for uploading document to a case
        [HttpPost("{caseId}/document")]
        public async Task<IActionResult> AddDocument(int caseId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Invalid file." });

            string fileUrl;

            try
            {
                fileUrl = await _cloudinaryService.UploadDocumentAsync(file);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }

            var document = new CaseDocument
            {
                CaseId = caseId,
                FileName = file.FileName,
                FileUrl = fileUrl,
                UploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown"
            };

            var success = await _caseService.AddDocumentAsync(document);

            if (!success)
                return BadRequest(new { message = "Error saving document to database." });

            return Ok(new { message = "Document uploaded successfully.", fileUrl });
        }

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

            // Return the updated Case with the new comment
            return Ok(new 
            { 
                message = "Comment added successfully.", 
                data = updatedCase 
            });
        }


        // Endpoint for getting case details with additional information
        [HttpGet("{caseId}/extras")]
        public async Task<IActionResult> GetCaseByIdWithExtras(int caseId)
        {
            var caseDetails = await _caseService.GetCaseDetailsWithExtrasAsync(caseId);
            if (caseDetails == null)
                return NotFound(new { message = "Case not found." });

            return Ok(caseDetails);
        }
    }
}
