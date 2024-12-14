using CaseRelayAPI.Models;
using CaseRelayAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CaseRelayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CaseController : ControllerBase
    {
        private readonly CaseService _caseService;

        public CaseController(CaseService caseService)
        {
            _caseService = caseService;
        }

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

        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetCaseById(int caseId)
        {
            var caseDetails = await _caseService.GetCaseByIdAsync(caseId);
            if (caseDetails == null)
                return NotFound(new { message = "Case not found." });

            return Ok(caseDetails);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCase([FromBody] Case newCase)
        {
            var success = await _caseService.CreateCaseAsync(newCase);
            if (!success)
                return BadRequest(new { message = "Error creating case." });

            return Ok(new { message = "Case created successfully." });
        }

        [HttpPut("{caseId}")]
        public async Task<IActionResult> UpdateCase(int caseId, [FromBody] Case updatedCase)
        {
            updatedCase.CaseId = caseId;
            var success = await _caseService.UpdateCaseAsync(updatedCase);
            if (!success)
                return BadRequest(new { message = "Error updating case." });

            return Ok(new { message = "Case updated successfully." });
        }

        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteCase(int caseId)
        {
            var success = await _caseService.DeleteCaseAsync(caseId);
            if (!success)
                return NotFound(new { message = "Case not found." });

            return Ok(new { message = "Case deleted successfully." });
        }

        [HttpPost("handover/{caseId}")]
        public async Task<IActionResult> HandoverCase(int caseId, [FromBody] CaseHandoverRequest request)
        {
            var success = await _caseService.HandoverCaseAsync(caseId, request.NewOfficerId);
            if (!success)
                return BadRequest(new { message = "Error handing over the case." });

            return Ok(new { message = "Case successfully handed over to the new officer." });
        }
    }
}
