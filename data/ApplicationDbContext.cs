using CaseRelayAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseRelayAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<CaseComment> CaseComments { get; set; }
        public DbSet<CaseDocument> CaseDocuments { get; set; }
        public DbSet<Notification> Notifications { get; set; } 
    }
} 
