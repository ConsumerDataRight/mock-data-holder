using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CDR.DataHolder.Energy.Repository.Infrastructure
{
    /// <summary>
    /// This is the DB context initialisation for the tooling such as migrations.
    /// When the tooling runs the migration, it looks first for a class that implements IDesignTimeDbContextFactory and if found,
    /// it will use that for configuring the context. Runtime behavior is not affected by any configuration set in the factory class.
    /// </summary>
    public class EnergyDataHolderDatabaseContextDesignTimeFactory : IDesignTimeDbContextFactory<EnergyDataHolderDatabaseContext>
    {
        public EnergyDataHolderDatabaseContextDesignTimeFactory()
        {
            // A parameter-less constructor is required by the EF Core CLI tools.
        }

        public EnergyDataHolderDatabaseContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<EnergyDataHolderDatabaseContext>()
               .UseSqlServer("foo") // connection string is only needed if using "dotnet ef database update ..." to actually run migrations from commandline
               .Options;

            return new EnergyDataHolderDatabaseContext(options);
        }
    }
}
