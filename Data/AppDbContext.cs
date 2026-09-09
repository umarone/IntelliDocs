using AIChatAssistant.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace AIChatAssistant.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
        {
        }

        public DbSet<VectorDocument> VectorDocuments { get; set; }
        public DbSet<VectorRecordEntity> VectorRecords { get; set; }
    }
}
