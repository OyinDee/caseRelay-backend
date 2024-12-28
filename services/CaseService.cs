using CaseRelayAPI.Models;
using CaseRelayAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaseRelayAPI.Services
{
    public interface ICaseService
    {
        Task<List<Case>> GetCasesByUserIdAsync(int userId);
        Task<Case> GetCaseByIdAsync(int caseId);
        Task<bool> CreateCaseAsync(Case newCase);
        Task<bool> ApproveCaseAsync(int caseId);
        Task<bool> UpdateCaseAsync(Case updatedCase);
        Task<bool> DeleteCaseAsync(int caseId);
        Task<List<Case>> GetAllCasesAsync();
        Task<bool> HandoverCaseAsync(int caseId, string newOfficerUserId);
        Task<Case> AddCommentAsync(int caseId, CaseComment newComment);
        Task<Case> GetCaseDetailsWithExtrasAsync(int caseId);
        Task<bool> AddDocumentAsync(CaseDocument newDocument);
        Task<List<Case>> SearchCasesAsync(string keyword);
        Task<CaseStatistics> GetCaseStatisticsAsync();
        Task<bool> UpdateCaseStatusAsync(int caseId, string status);
        Task<bool> AssignCaseToOfficerAsync(int caseId, string officerId);
    }

    public class CaseService : ICaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CaseService> _logger;

        public CaseService(ApplicationDbContext context, ILogger<CaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Case>> GetCasesByUserIdAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null || string.IsNullOrEmpty(user.PoliceId))
            {
                return new List<Case>();
            }

            var officerPoliceId = user.PoliceId;

            return await _context.Cases
                .Where(c => c.AssignedOfficerId == officerPoliceId && !c.IsArchived)
                .Include(c => c.Documents)
                .Include(c => c.Comments)
                .OrderByDescending(c => c.ReportedAt)
                .ToListAsync();
        }

        public async Task<Case> GetCaseByIdAsync(int caseId)
        {
            return await _context.Cases
                .Include(c => c.Documents)
                .Include(c => c.Comments)
                .FirstOrDefaultAsync(c => c.CaseId == caseId);
        }

        public async Task<bool> CreateCaseAsync(Case newCase)
        {
            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveCaseAsync(int caseId)
        {
            var caseToApprove = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId);
            if (caseToApprove == null) return false;

            caseToApprove.IsApproved = true;
            _context.Cases.Update(caseToApprove);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCaseAsync(Case updatedCase)
        {
            _context.Cases.Update(updatedCase);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCaseAsync(int caseId)
        {
            var caseToDelete = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId);
            if (caseToDelete == null) return false;

            _context.Cases.Remove(caseToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Case>> GetAllCasesAsync()
        {
            return await _context.Cases
                .Include(c => c.Documents)
                .Include(c => c.Comments)
                .ToListAsync();
        }

        public async Task<bool> HandoverCaseAsync(int caseId, string newOfficerUserId)
        {
            var caseToHandover = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId);
            if (caseToHandover == null) return false;

            var newOfficer = await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == newOfficerUserId);
            if (newOfficer == null || string.IsNullOrEmpty(newOfficer.PoliceId)) return false;

            if (caseToHandover.AssignedOfficerId == newOfficer.PoliceId) return false;

            caseToHandover.PreviousOfficerId = caseToHandover.AssignedOfficerId;
            caseToHandover.AssignedOfficerId = newOfficer.PoliceId;

            var handoverComment = new CaseComment
            {
                CaseId = caseId,
                CommentText = $"Case handed over from Officer ID {caseToHandover.PreviousOfficerId} to Officer ID {caseToHandover.AssignedOfficerId}."
            };

            _context.CaseComments.Add(handoverComment);
            _context.Cases.Update(caseToHandover);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Case> AddCommentAsync(int caseId, CaseComment newComment)
        {
            var caseToUpdate = await _context.Cases
                                             .Include(c => c.Comments)
                                             .FirstOrDefaultAsync(c => c.CaseId == caseId);

            if (caseToUpdate == null) return null;

            caseToUpdate.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            return caseToUpdate;
        }

        public async Task<Case> GetCaseDetailsWithExtrasAsync(int caseId)
        {
            var caseDetails = await _context.Cases
                .Include(c => c.Comments)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.CaseId == caseId);

            return caseDetails ?? new Case(); // Ensure a non-null return value
        }

        public async Task<bool> AddDocumentAsync(CaseDocument newDocument)
        {
            _logger.LogInformation("Attempting to add document to database for caseId: {CaseId}, FileName: {FileName}", newDocument.CaseId, newDocument.FileName);

            try
            {
                _context.CaseDocuments.Add(newDocument);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Document added to database successfully for caseId: {CaseId}, FileName: {FileName}", newDocument.CaseId, newDocument.FileName);
                    return true;
                }
                else
                {
                    _logger.LogWarning("No changes were made to the database for caseId: {CaseId}, FileName: {FileName}", newDocument.CaseId, newDocument.FileName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving document to database for caseId: {CaseId}, FileName: {FileName}", newDocument.CaseId, newDocument.FileName);
                return false;
            }
        }

        public async Task<List<Case>> SearchCasesAsync(string keyword)
        {
            return await _context.Cases
                .Include(c => c.Documents)
                .Include(c => c.Comments)
                .Where(c => c.Title.Contains(keyword) || c.Description.Contains(keyword))
                .ToListAsync();
        }

        public async Task<CaseStatistics> GetCaseStatisticsAsync()
        {
            return new CaseStatistics
            {
                TotalCases = await _context.Cases.CountAsync(),
                PendingCases = await _context.Cases.CountAsync(c => c.Status == "Pending"),
                OpenCases = await _context.Cases.CountAsync(c => c.Status == "Open"),
                InvestigatingCases = await _context.Cases.CountAsync(c => c.Status == "Investigating"),
                ClosedCases = await _context.Cases.CountAsync(c => c.Status == "Closed"),
                ResolvedCases = await _context.Cases.CountAsync(c => c.Status == "Resolved")
            };
        }

        public async Task<bool> UpdateCaseStatusAsync(int caseId, string status)
        {
            var caseToUpdate = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId);
            if (caseToUpdate == null) return false;

            caseToUpdate.Status = status;
            _context.Cases.Update(caseToUpdate);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignCaseToOfficerAsync(int caseId, string officerId)
        {
            var caseToAssign = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId);
            if (caseToAssign == null) return false;

            var officer = await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == officerId);
            if (officer == null) return false;

            caseToAssign.AssignedOfficerId = officer.PoliceId;
            _context.Cases.Update(caseToAssign);
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class CaseStatistics
    {
        public int TotalCases { get; set; }
        public int PendingCases { get; set; }
        public int OpenCases { get; set; }
        public int InvestigatingCases { get; set; }
        public int ClosedCases { get; set; }
        public int ResolvedCases { get; set; }
    }
}
