using Domain.Entities.CustomerSatisfaction;
using Domain.Entities.CoreAssets;
using Domain.Entities.DisplayDevices;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
namespace Data.DbContexts
{
    public class HgsDbContext : DbContext
    {
        public HgsDbContext(DbContextOptions<HgsDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users => Set<Users>();
        public DbSet<AuditLogs> AuditLogs => Set<AuditLogs>();
        public DbSet<Roles> Roles => Set<Roles>();
        public DbSet<UserRoles> UserRoles => Set<UserRoles>();
        public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();
        public DbSet<Menus> Menus => Set<Menus>();
        public DbSet<UserMenus> UserMenus => Set<UserMenus>();
        public DbSet<RoleMenus> RoleMenus => Set<RoleMenus>();
        public DbSet<OrganizationUnits> OrganizationUnits => Set<OrganizationUnits>();

        #region DisplayDevices
        public DbSet<DisplayDevices> DisplayDevices => Set<DisplayDevices>();
        #endregion

        #region CoreAssets
        public DbSet<CoreAssets> CoreAssets => Set<CoreAssets>();
        #endregion

        #region CustomerSatisfaction
        public DbSet<Devices> Devices => Set<Devices>();
        public DbSet<Evaluations> Evaluations => Set<Evaluations>();
        public DbSet<UnsatisfiedReasons> UnsatisfiedReasons => Set<UnsatisfiedReasons>();
        public DbSet<EvaluationReasonLinks> EvaluationReasonLinks => Set<EvaluationReasonLinks>();

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //var entities = modelBuilder.Model.GetEntityTypes();
            //foreach (var e in modelBuilder.Model.GetEntityTypes())
            //{
            //    Console.WriteLine($"Entity: {e.ClrType.FullName}");

            //    foreach (var p in e.GetProperties())
            //    {
            //        Console.WriteLine($"   {p.Name} : {p.ClrType.FullName}");
            //    }
            //}
            base.OnModelCreating(modelBuilder);



            modelBuilder.Entity<EvaluationReasonLinks>()
        .HasKey(x => new { x.EvaluationId, x.ReasonId });

            modelBuilder.Entity<EvaluationReasonLinks>()
                .HasOne(x => x.Evaluation)
                .WithMany(x => x.EvaluationReasonLinks)
                .HasForeignKey(x => x.EvaluationId);

            modelBuilder.Entity<EvaluationReasonLinks>()
                .HasOne(x => x.Reason)
                .WithMany(x => x.EvaluationReasonLinks)
                .HasForeignKey(x => x.ReasonId);
        }
    }
}
