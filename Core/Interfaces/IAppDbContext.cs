using Domain.Entities.CustomerSatisfaction;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace Core.Interfaces;

public interface IAppDbContext
{
    DbSet<Users> Users { get; }
    DbSet<Roles> Roles { get; }
    DbSet<UserRoles> UserRoles { get; }
    DbSet<RefreshTokens> RefreshTokens { get; }
    DbSet<Menus> Menus { get; }
    DbSet<OrganizationUnits> OrganizationUnits { get; }

    DbSet<Devices> Devices { get; }
    DbSet<Evaluations> Evaluations { get; }
    DbSet<UnsatisfiedReasons> UnsatisfiedReasons { get; }
    DbSet<EvaluationReasonLinks> EvaluationReasonLinks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
