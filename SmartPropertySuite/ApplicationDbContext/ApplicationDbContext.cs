using Microsoft.EntityFrameworkCore;
using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.ApplicationDbContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<CRMPropertySuiteUserInfo> CRMPropertySuiteUserInfo { get; set; }
        public DbSet<CRMPropertySuiteUserChats> CRMPropertySuiteUserChats { get; set; }
        public DbSet<CRMPropertySuiteUserConversations> CRMPropertySuiteUserConversations { get; set; }
        public DbSet<CRMPropertySuiteUserChatMessages> CRMPropertySuiteUserChatMessages { get; set; }
        public DbSet<CRMPropertyMobileUserDeviceInfo> CRMPropertyMobileUserDeviceInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure ChatId is a unique key
            modelBuilder.Entity<CRMPropertySuiteUserChats>()
                .HasIndex(c => c.ChatId)
                .IsUnique();

            // Fix relationship mapping on ChatId (which is a GUID, not the PK)
            modelBuilder.Entity<CRMPropertySuiteUserConversations>()
                .HasOne(c => c.Chat)
                .WithMany(c => c.Conversations)
                .HasForeignKey(c => c.ChatId)
                .HasPrincipalKey(c => c.ChatId);

            modelBuilder.Entity<CRMPropertySuiteUserConversations>()
                .HasIndex(c => c.ConversationId)
                .IsUnique();

            modelBuilder.Entity<CRMPropertySuiteUserChatMessages>()
                .HasOne(m => m.Conversation)                            // navigation property
                .WithMany(c => c.Messages)                              // inverse nav: conversation has many messages
                .HasForeignKey(m => m.ConversationId)                   // FK on message
                .HasPrincipalKey(c => c.ConversationId);                // PK on conversation (GUID-based key)
        }
    }
}
