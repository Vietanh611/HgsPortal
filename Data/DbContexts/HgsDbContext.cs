using Core.Interfaces;
using Domain.Entities.CustomerSatisfaction;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
namespace Data.DbContexts
{
    public class HgsDbContext : DbContext, IAppDbContext
    {
        public HgsDbContext(DbContextOptions<HgsDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users => Set<Users>();
        public DbSet<Roles> Roles => Set<Roles>();
        public DbSet<UserRoles> UserRoles => Set<UserRoles>();
        public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();
        public DbSet<Menus> Menus => Set<Menus>();
        public DbSet<OrganizationUnits> OrganizationUnits => Set<OrganizationUnits>();

        #region CustomerSatisfaction
        public DbSet<Devices> Devices => Set<Devices>();
        public DbSet<Evaluations> Evaluations => Set<Evaluations>();
        public DbSet<UnsatisfiedReasons> UnsatisfiedReasons => Set<UnsatisfiedReasons>();
        public DbSet<EvaluationReasonLinks> EvaluationReasonLinks => Set<EvaluationReasonLinks>();

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HgsDbContext).Assembly);
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
