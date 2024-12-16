using CaseRelayAPI.Models;
using CaseRelayAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaseRelayAPI.Services
{
    public class CaseService
    {
        private readonly ApplicationDbContext _context;

        public CaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Case>> GetCasesByUserIdAsync(int userId)
        {
            var cases = await _context.Cases
                                      .Where(c => c.UserId == userId.ToString())
                                      .ToListAsync();
            return cases;
        }

        public async Task<Case> GetCaseByIdAsync(int caseId)
        {
            return await _context.Cases
                                .Include(c => c.Comments) 
                                 .FirstOrDefaultAsync(c => c.CaseId == caseId);
        }

        public async Task<bool> CreateCaseAsync(Case newCase)
        {
            _context.Cases.Add(newCase);
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

        public async Task<bool> HandoverCaseAsync(int caseId, int newOfficerId)
        {
            var caseToHandover = await _context.Cases
                                               .FirstOrDefaultAsync(c => c.CaseId == caseId);

            if (caseToHandover == null) return false;

            var newOfficer = await _context.Users
                                            .FirstOrDefaultAsync(u => u.UserID == newOfficerId);

            if (newOfficer == null)
                return false;

            caseToHandover.AssignedOfficerId = newOfficer.UserID.ToString();
            _context.Cases.Update(caseToHandover);
            await _context.SaveChangesAsync();
            return true;
        }

public async Task<Case> AddCommentAsync(int caseId, CaseComment newComment)
{
    var caseToUpdate = await _context.Cases
                                     .Include(c => c.Comments) 
                                     .FirstOrDefaultAsync(c => c.CaseId == caseId);

    if (caseToUpdate == null)
    {
        return null; 
    }


    caseToUpdate.Comments.Add(newComment);


    await _context.SaveChangesAsync();

    // Return the updated case with the new comment
    return caseToUpdate;
}


        public async Task<Case> GetCaseDetailsWithExtrasAsync(int caseId)
        {
            return await _context.Cases
                .Include(c => c.Comments)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.CaseId == caseId);
        }

        public async Task<bool> AddDocumentAsync(CaseDocument newDocument)
        {
            _context.CaseDocuments.Add(newDocument);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
