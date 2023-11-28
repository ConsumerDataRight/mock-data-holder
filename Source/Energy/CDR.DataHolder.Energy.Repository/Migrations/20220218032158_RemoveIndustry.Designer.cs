﻿// <auto-generated />
using System;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    [DbContext(typeof(EnergyDataHolderDatabaseContext))]
    [Migration("20220218032158_RemoveIndustry")]
    partial class RemoveIndustry
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Account", b =>
                {
                    b.Property<string>("AccountId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AccountNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreationDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("CustomerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("AccountId");

                    b.HasIndex("CustomerId");

                    b.ToTable("Account", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.AccountConcession", b =>
                {
                    b.Property<string>("AccountConcessionId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AccountId")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AdditionalInfo")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("AdditionalInfoUri")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<decimal?>("DailyDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("MonthlyDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("PercentageDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("YearlyDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("AccountConcessionId");

                    b.HasIndex("AccountId");

                    b.ToTable("AccountConcession", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.AccountPlan", b =>
                {
                    b.Property<string>("AccountPlanId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AccountId")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Nickname")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PlanId")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("AccountPlanId");

                    b.HasIndex("AccountId");

                    b.HasIndex("PlanId");

                    b.ToTable("AccountPlan", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Brand", b =>
                {
                    b.Property<Guid>("BrandId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("BrandName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid>("LegalEntityId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("LogoUri")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(25)
                        .HasColumnType("nvarchar(25)");

                    b.HasKey("BrandId");

                    b.HasIndex("LegalEntityId");

                    b.ToTable("Brand", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Customer", b =>
                {
                    b.Property<Guid>("CustomerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CustomerUType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LoginId")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("nvarchar(8)");

                    b.Property<Guid?>("OrganisationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("PersonId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("CustomerId");

                    b.HasIndex("OrganisationId")
                        .IsUnique()
                        .HasFilter("[OrganisationId] IS NOT NULL");

                    b.HasIndex("PersonId")
                        .IsUnique()
                        .HasFilter("[PersonId] IS NOT NULL");

                    b.ToTable("Customer", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.LegalEntity", b =>
                {
                    b.Property<Guid>("LegalEntityId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("LegalEntityName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("LogoUri")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(25)
                        .HasColumnType("nvarchar(25)");

                    b.HasKey("LegalEntityId");

                    b.ToTable("LegalEntity", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Organisation", b =>
                {
                    b.Property<Guid>("OrganisationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Abn")
                        .HasMaxLength(11)
                        .HasColumnType("nvarchar(11)");

                    b.Property<string>("Acn")
                        .HasMaxLength(9)
                        .HasColumnType("nvarchar(9)");

                    b.Property<string>("AgentFirstName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AgentLastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AgentRole")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("BusinessName")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("EstablishmentDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("IndustryCode")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("IndustryCodeVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool?>("IsAcnCRegistered")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastUpdateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("LegalName")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("OrganisationType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RegisteredCountry")
                        .HasMaxLength(3)
                        .HasColumnType("nvarchar(3)");

                    b.Property<string>("ShortName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("OrganisationId");

                    b.ToTable("Organisation", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Person", b =>
                {
                    b.Property<Guid>("PersonId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("FirstName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("LastUpdateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("MiddleNames")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("OccupationCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("OccupationCodeVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Prefix")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Suffix")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("PersonId");

                    b.ToTable("Person", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Plan", b =>
                {
                    b.Property<string>("PlanId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ApplicationUri")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Brand")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("BrandName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CustomerType")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("DisplayName")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<DateTime?>("EffectiveFrom")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EffectiveTo")
                        .HasColumnType("datetime2");

                    b.Property<string>("FuelType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("PlanId");

                    b.ToTable("Plan", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.PlanOverview", b =>
                {
                    b.Property<string>("PlanOverviewId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AccountPlanId")
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("PlanOverviewId");

                    b.HasIndex("AccountPlanId")
                        .IsUnique()
                        .HasFilter("[AccountPlanId] IS NOT NULL");

                    b.ToTable("PlanOverview", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.ServicePoint", b =>
                {
                    b.Property<string>("ServicePointId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AccountPlanId")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool?>("IsGenerator")
                        .HasColumnType("bit");

                    b.Property<string>("JurisdictionCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("LastUpdateDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("NationalMeteringId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ServicePointClassification")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ServicePointStatus")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("ValidFromDate")
                        .HasColumnType("datetime2");

                    b.HasKey("ServicePointId");

                    b.HasIndex("AccountPlanId");

                    b.ToTable("ServicePoint", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.SoftwareProduct", b =>
                {
                    b.Property<Guid>("SoftwareProductId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("LogoUri")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("SoftwareProductDescription")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("SoftwareProductName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(25)
                        .HasColumnType("nvarchar(25)");

                    b.HasKey("SoftwareProductId");

                    b.HasIndex("BrandId");

                    b.ToTable("SoftwareProduct", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Transaction", b =>
                {
                    b.Property<string>("TransactionId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AccountId")
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal>("Amount")
                        .HasPrecision(16, 2)
                        .HasColumnType("decimal(16,2)");

                    b.Property<string>("ApcaNumber")
                        .HasMaxLength(6)
                        .HasColumnType("nvarchar(6)");

                    b.Property<string>("BillerCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("BillerName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Crn")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Currency")
                        .HasMaxLength(3)
                        .HasColumnType("nvarchar(3)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("ExecutionDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("MerchantCategoryCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("MerchantName")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("PostingDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TransactionType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ValueDateTime")
                        .HasColumnType("datetime2");

                    b.HasKey("TransactionId");

                    b.HasIndex("AccountId");

                    b.ToTable("Transaction");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Account", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.Customer", "Customer")
                        .WithMany("Accounts")
                        .HasForeignKey("CustomerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Customer");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.AccountConcession", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.Account", "Account")
                        .WithMany("AccountConcessions")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.AccountPlan", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.Account", "Account")
                        .WithMany("AccountPlans")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CDR.DataHolder.Repository.Entities.Plan", "Plan")
                        .WithMany()
                        .HasForeignKey("PlanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");

                    b.Navigation("Plan");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Brand", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.LegalEntity", "LegalEntity")
                        .WithMany("Brands")
                        .HasForeignKey("LegalEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LegalEntity");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Customer", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.Organisation", "Organisation")
                        .WithOne("Customer")
                        .HasForeignKey("CDR.DataHolder.Repository.Entities.Customer", "OrganisationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("CDR.DataHolder.Repository.Entities.Person", "Person")
                        .WithOne("Customer")
                        .HasForeignKey("CDR.DataHolder.Repository.Entities.Customer", "PersonId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Organisation");

                    b.Navigation("Person");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.PlanOverview", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.AccountPlan", "AccountPlan")
                        .WithOne("PlanOverview")
                        .HasForeignKey("CDR.DataHolder.Repository.Entities.PlanOverview", "AccountPlanId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("AccountPlan");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.ServicePoint", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.AccountPlan", "AccountPlan")
                        .WithMany("ServicePoints")
                        .HasForeignKey("AccountPlanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AccountPlan");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.SoftwareProduct", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.Brand", "Brand")
                        .WithMany("SoftwareProducts")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Brand");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Transaction", b =>
                {
                    b.HasOne("CDR.DataHolder.Repository.Entities.Account", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId");

                    b.Navigation("Account");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Account", b =>
                {
                    b.Navigation("AccountConcessions");

                    b.Navigation("AccountPlans");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.AccountPlan", b =>
                {
                    b.Navigation("PlanOverview");

                    b.Navigation("ServicePoints");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Brand", b =>
                {
                    b.Navigation("SoftwareProducts");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Customer", b =>
                {
                    b.Navigation("Accounts");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.LegalEntity", b =>
                {
                    b.Navigation("Brands");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Organisation", b =>
                {
                    b.Navigation("Customer");
                });

            modelBuilder.Entity("CDR.DataHolder.Repository.Entities.Person", b =>
                {
                    b.Navigation("Customer");
                });
#pragma warning restore 612, 618
        }
    }
}
