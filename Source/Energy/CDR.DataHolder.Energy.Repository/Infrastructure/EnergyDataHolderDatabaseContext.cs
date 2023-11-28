using CDR.DataHolder.Energy.Repository.Entities;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace CDR.DataHolder.Energy.Repository.Infrastructure
{
    public class EnergyDataHolderDatabaseContext : DbContext, IIndustryDbContext
    {
		public EnergyDataHolderDatabaseContext()
		{

		}

		public EnergyDataHolderDatabaseContext(DbContextOptions<EnergyDataHolderDatabaseContext> options) : base(options)
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
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Use the entity name instead of the Context.DbSet<T> name
				// refs https://docs.microsoft.com/en-us/ef/core/modeling/entity-types?tabs=fluent-api#table-name
				modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name);
				
				// Convert all date time to UTC when saving and fetching.
				ConvertDateTimePropertiesToUTc(entityType);
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

		private static readonly ValueConverter<DateTime, DateTime> dateTimeConverter = new ValueConverter<DateTime, DateTime>(
			v => v.ToUniversalTime(),
			v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
		private static readonly ValueConverter<DateTime?, DateTime?> nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
			v => v.HasValue ? v.Value.ToUniversalTime() : v,
			v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
		private static void ConvertDateTimePropertiesToUTc(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
		{
			// Convert all DateTime(?) to UTC when saving and fetching back from the DB
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(DateTime))
				{
					property.SetValueConverter(dateTimeConverter);
				}
				else if (property.ClrType == typeof(DateTime?))
				{
					property.SetValueConverter(nullableDateTimeConverter);
				}
			}
		}

        public async Task RemoveExistingData()
        {
            // Remove all existing account data in the system
            var existingCustomers = await Customers.AsNoTracking().ToListAsync();
            var existingPersons = await Persons.AsNoTracking().ToListAsync();
            var existingOrgs = await Organisations.AsNoTracking().ToListAsync();

            RemoveRange(existingCustomers);
            SaveChanges();

            RemoveRange(existingPersons);
            SaveChanges();

            RemoveRange(existingOrgs);
            SaveChanges();

            // Remove all existing plans.
            var existingPlans = await Plans.AsNoTracking().ToListAsync();
            var existingPlanOverviews = await PlanOverviews.AsNoTracking().ToListAsync();

            RemoveRange(existingPlans);
            SaveChanges();

            RemoveRange(existingPlanOverviews);
            SaveChanges();
        }

        public void ReCreateParticipants(JObject participantsData)
        {
            var newPlans = participantsData[nameof(Plans)]?.ToObject<Plan[]>();
            var newCustomers = participantsData[nameof(Customers)]?.ToObject<Customer[]>();

			if(newPlans!= null)
			{
				Plans.AddRange(newPlans);
			}
			if(newCustomers != null)
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
