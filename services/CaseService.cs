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

        // Get all cases for a specific user
        public async Task<List<Case>> GetCasesByUserIdAsync(int userId)
        {
            var cases = await _context.Cases
                                      .Where(c => c.UserId == userId.ToString()) // Filter by UserId
                                      .ToListAsync(); // Return as list
            return cases;
        }

        // Get a case by its ID
        public async Task<Case> GetCaseByIdAsync(int caseId)
        {
            return await _context.Cases
                                 .FirstOrDefaultAsync(c => c.CaseId == caseId); // Return single case or null
        }

        // Create a new case
        public async Task<bool> CreateCaseAsync(Case newCase)
        {
            _context.Cases.Add(newCase); // Add the new case to the context
            await _context.SaveChangesAsync(); // Save changes to the database
            return true;
        }

        // Update an existing case
        public async Task<bool> UpdateCaseAsync(Case updatedCase)
        {
            _context.Cases.Update(updatedCase); // Update the case in the context
            await _context.SaveChangesAsync(); // Save changes to the database
            return true;
        }

        // Delete a case by its ID
        public async Task<bool> DeleteCaseAsync(int caseId)
        {
            var caseToDelete = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId);
            if (caseToDelete == null) return false;

            _context.Cases.Remove(caseToDelete); // Remove the case from the context
            await _context.SaveChangesAsync(); // Save changes to the database
            return true;
        }

        // Handover the case to a new officer by their officer ID
        public async Task<bool> HandoverCaseAsync(int caseId, int newOfficerId)
        {
            var caseToHandover = await _context.Cases
                                               .FirstOrDefaultAsync(c => c.CaseId == caseId);
            
            if (caseToHandover == null) return false;

            var newOfficer = await _context.Users
                                            .FirstOrDefaultAsync(u => u.UserID == newOfficerId);

            if (newOfficer == null)
                return false;

            // Update the officer assigned to the case
            caseToHandover.AssignedOfficerId = newOfficer.UserID.ToString(); 
            
            _context.Cases.Update(caseToHandover); // Update the case in the context
            await _context.SaveChangesAsync(); // Save changes to the database
            return true;
        }
    }
}
