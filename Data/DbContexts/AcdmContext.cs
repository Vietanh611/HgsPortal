using Domain.Entities.ACDM;
using Microsoft.EntityFrameworkCore;

namespace Data.DbContexts
{
    public class AcdmContext : DbContext
    {
        public AcdmContext(DbContextOptions<AcdmContext> options) : base(options) { }
        public DbSet<FlightACDM> Flight { get; set; }
    }
}
