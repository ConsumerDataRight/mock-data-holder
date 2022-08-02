using CDR.DataHolder.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace CDR.DataHolder.Repository.Infrastructure
{
    public class DataHolderDatabaseContext : DbContext
	{
		public DataHolderDatabaseContext()
		{

		}

		public DataHolderDatabaseContext(DbContextOptions<DataHolderDatabaseContext> options) : base(options)
		{
		}

		public DbSet<Person> Persons { get; set; }
		public DbSet<Customer> Customers { get; set; }
		public DbSet<Account> Accounts { get; set; }
		public DbSet<Transaction> Transactions { get; set; }
		public DbSet<Organisation> Organisations { get; set; }
		public DbSet<LegalEntity> LegalEntities { get; set; }
        public DbSet<Brand> Brands { get; set; }
		public DbSet<SoftwareProduct> SoftwareProducts { get; set; }

		public DbSet<LogEventsDrService> LogEventsDrService { get; set; }
		public DbSet<LogEventsManageAPI> LogEventsManageAPI { get; set; }

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

			modelBuilder.Entity<Transaction>()
				.Property(x => x.Amount)
				.IsRequired()
				.HasPrecision(16, 2);
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
	}
}
