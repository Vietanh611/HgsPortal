using Domain.Entities.FlyOps;
using Microsoft.EntityFrameworkCore;

namespace Data.DbContexts
{
    public class FlyOpsDbContext : DbContext
    {
        public FlyOpsDbContext(DbContextOptions<FlyOpsDbContext> options) : base(options)
        {
        }
        public DbSet<NhanVien> NhanVien => Set<NhanVien>();
    }
}
