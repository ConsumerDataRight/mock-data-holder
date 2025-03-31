using CDR.DataHolder.Banking.Repository.Entities;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Banking.Repository.Infrastructure
{
    public class BankingDataHolderDatabaseContext : DbContext, IIndustryDbContext
    {
        public BankingDataHolderDatabaseContext()
        {
        }

        public BankingDataHolderDatabaseContext(DbContextOptions<BankingDataHolderDatabaseContext> options)
            : base(options)
        {
        }

        public DbSet<Person> Persons => Set<Person>();

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Account> Accounts => Set<Account>();

        public DbSet<Transaction> Transactions => Set<Transaction>();

        public DbSet<Organisation> Organisations => Set<Organisation>();

        public DbSet<Shared.Repository.Entities.LogEventsDrService> LogEventsDrService => Set<Shared.Repository.Entities.LogEventsDrService>();

        public DbSet<Shared.Repository.Entities.LogEventsManageApi> LogEventsManageAPI => Set<Shared.Repository.Entities.LogEventsManageApi>();

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

            modelBuilder.Entity<Transaction>()
                .Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(16, 2);

            modelBuilder.Entity<Shared.Repository.Entities.LogEventsManageApi>().ToTable("LogEventsManageAPI");
        }

        public async Task RemoveExistingData()
        {
            // Remove all existing account data in the system
            var existingTxns = await Transactions.ToListAsync();
            var existingCustomers = await Customers.ToListAsync();
            var existingPersons = await Persons.ToListAsync();
            var existingOrgs = await Organisations.ToListAsync();

            RemoveRange(existingTxns);

            await SaveChangesAsync();

            RemoveRange(existingCustomers);

            await SaveChangesAsync();

            RemoveRange(existingPersons);

            await SaveChangesAsync();

            RemoveRange(existingOrgs);

            await SaveChangesAsync();
        }

        public void ReCreateParticipants(JObject participantsData)
        {
            var newCustomers = participantsData[nameof(Customers)]?.ToObject<Customer[]>();
            if (newCustomers != null)
            {
                Customers.AddRange(newCustomers);
                SaveChanges();
            }
        }

        public async Task<bool> HasExistingData()
        {
            return await Customers.AnyAsync();
        }
    }
}
