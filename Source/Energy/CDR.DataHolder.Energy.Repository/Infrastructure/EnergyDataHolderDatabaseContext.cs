using CDR.DataHolder.Energy.Repository.Entities;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Energy.Repository.Infrastructure
{
    public class EnergyDataHolderDatabaseContext : DbContext, IIndustryDbContext
    {
        public EnergyDataHolderDatabaseContext()
        {
        }

        public EnergyDataHolderDatabaseContext(DbContextOptions<EnergyDataHolderDatabaseContext> options)
            : base(options)
        {
        }

        // Common schema
        public DbSet<Person> Persons => Set<Person>();

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Organisation> Organisations => Set<Organisation>();

        public DbSet<Shared.Repository.Entities.LogEventsDrService> LogEventsDrService => Set<Shared.Repository.Entities.LogEventsDrService>();

        public DbSet<Shared.Repository.Entities.LogEventsManageApi> LogEventsManageAPI => Set<Shared.Repository.Entities.LogEventsManageApi>();

        // Energy schema
        public DbSet<Plan> Plans => Set<Plan>();

        public DbSet<Account> Accounts => Set<Account>();

        public DbSet<AccountPlan> AccountPlans => Set<AccountPlan>();

        public DbSet<AccountConcession> AccountConcessions => Set<AccountConcession>();

        public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();

        public DbSet<PlanOverview> PlanOverviews => Set<PlanOverview>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType))
            {
                // Use the entity name instead of the Context.DbSet<T> name
                // refs https://docs.microsoft.com/en-us/ef/core/modeling/entity-types?tabs=fluent-api#table-name
                modelBuilder.Entity(clrType).ToTable(clrType.Name);
            }

            // Configure 1-to-1 relationship.
            modelBuilder.Entity<Person>()
                .HasOne(b => b.Customer)
                .WithOne(e => e.Person)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Organisation>()
                .HasOne(b => b.Customer)
                .WithOne(e => e.Organisation)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlanOverview>()
                .HasOne(b => b.AccountPlan)
                .WithOne(e => e.PlanOverview)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(16, 2);

            modelBuilder.Entity<Shared.Repository.Entities.LogEventsManageApi>().ToTable("LogEventsManageAPI");
        }

        public async Task RemoveExistingData()
        {
            // Remove all existing account data in the system
            var existingCustomers = await Customers.AsNoTracking().ToListAsync();
            var existingPersons = await Persons.AsNoTracking().ToListAsync();
            var existingOrgs = await Organisations.AsNoTracking().ToListAsync();

            RemoveRange(existingCustomers);

            await SaveChangesAsync();

            RemoveRange(existingPersons);

            await SaveChangesAsync();

            RemoveRange(existingOrgs);

            await SaveChangesAsync();

            // Remove all existing plans.
            var existingPlans = await Plans.AsNoTracking().ToListAsync();
            var existingPlanOverviews = await PlanOverviews.AsNoTracking().ToListAsync();

            RemoveRange(existingPlans);

            await SaveChangesAsync();

            RemoveRange(existingPlanOverviews);

            await SaveChangesAsync();
        }

        public void ReCreateParticipants(JObject participantsData)
        {
            var newPlans = participantsData[nameof(Plans)]?.ToObject<Plan[]>();
            var newCustomers = participantsData[nameof(Customers)]?.ToObject<Customer[]>();

            if (newPlans != null)
            {
                Plans.AddRange(newPlans);
            }

            if (newCustomers != null)
            {
                Customers.AddRange(newCustomers);
            }

            SaveChanges();
        }

        public async Task<bool> HasExistingData()
        {
            return await Customers.AnyAsync();
        }
    }
}
