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
        private readonly ILogger<CaseService> _logger;
        public CaseService(ApplicationDbContext context,  ILogger<CaseService> logger)
        {
            _context = context;
             _logger = logger;
        }

public async Task<List<Case>> GetCasesByUserIdAsync(int userId)
{
    // Fetch the user to get their PoliceId
    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

    if (user == null || string.IsNullOrEmpty(user.PoliceId))
    {
        return new List<Case>(); // Return an empty list if the user or PoliceId doesn't exist
    }

    var officerPoliceId = user.PoliceId;

    // Fetch all cases assigned to the officer using PoliceId
    var cases = await _context.Cases
        .Include(c => c.Documents) 
        .Include(c => c.Comments)
        .Where(c => c.AssignedOfficerId == officerPoliceId && !c.IsArchived)
        .OrderByDescending(c => c.ReportedAt)
        .ToListAsync();

    return cases;
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
    // Fetch the case to validate its existence
    var caseToHandover = await _context.Cases
        .FirstOrDefaultAsync(c => c.CaseId == caseId);

    if (caseToHandover == null)
    {
        Console.WriteLine($"Case with ID {caseId} not found."); // Debugging
        return false; // Case doesn't exist
    }

    // Fetch the new officer to validate their ID and PoliceId
    var newOfficer = await _context.Users
        .FirstOrDefaultAsync(u => u.PoliceId == newOfficerUserId);
    if (newOfficer == null || string.IsNullOrEmpty(newOfficer.PoliceId))
    {
        Console.WriteLine($"New Officer with PoliceId {newOfficerUserId} is invalid.");
        return false; // Officer doesn't exist or PoliceId is invalid
    }

    // Prevent handover to the same officer
    if (caseToHandover.AssignedOfficerId == newOfficer.PoliceId)
    {
        Console.WriteLine($"New Officer ID matches the current AssignedOfficerId."); // Debugging
        return false;
    }

    // Perform the handover
    caseToHandover.PreviousOfficerId = caseToHandover.AssignedOfficerId;
    caseToHandover.AssignedOfficerId = newOfficer.PoliceId;

    // Add a comment about the handover
    var handoverComment = new CaseComment
    {
        CaseId = caseId,
        CommentText = $"Case handed over from Officer ID {caseToHandover.PreviousOfficerId} to Officer ID {caseToHandover.AssignedOfficerId}."
    };

    _context.CaseComments.Add(handoverComment);

    // Update the case in the database
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

    }
}
