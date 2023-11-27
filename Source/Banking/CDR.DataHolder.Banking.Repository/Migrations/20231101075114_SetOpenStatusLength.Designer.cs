﻿// <auto-generated />
using System;
using CDR.DataHolder.Banking.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    [DbContext(typeof(BankingDataHolderDatabaseContext))]
    [Migration("20231101075114_SetOpenStatusLength")]
    partial class SetOpenStatusLength
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Account", b =>
                {
                    b.Property<string>("AccountId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("CustomerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("MaskedName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("NickName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("OpenStatus")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ProductCategory")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("AccountId");

                    b.HasIndex("CustomerId");

                    b.ToTable("Account", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Customer", b =>
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

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.LogEventsDrService", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Environment")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Exception")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Level")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MethodName")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ProcessId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ProcessName")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("SourceContext")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ThreadId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("LogEventsDrService", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.LogEventsManageApi", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Environment")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Exception")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Level")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MethodName")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ProcessId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ProcessName")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("SourceContext")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ThreadId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("LogEventsManageAPI", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Organisation", b =>
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

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Person", b =>
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

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Transaction", b =>
                {
                    b.Property<string>("TransactionId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AccountId")
                        .HasMaxLength(100)
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

                    b.ToTable("Transaction", (string)null);
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Account", b =>
                {
                    b.HasOne("CDR.DataHolder.Banking.Repository.Entities.Customer", "Customer")
                        .WithMany("Accounts")
                        .HasForeignKey("CustomerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Customer");
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Customer", b =>
                {
                    b.HasOne("CDR.DataHolder.Banking.Repository.Entities.Organisation", "Organisation")
                        .WithOne("Customer")
                        .HasForeignKey("CDR.DataHolder.Banking.Repository.Entities.Customer", "OrganisationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("CDR.DataHolder.Banking.Repository.Entities.Person", "Person")
                        .WithOne("Customer")
                        .HasForeignKey("CDR.DataHolder.Banking.Repository.Entities.Customer", "PersonId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Organisation");

                    b.Navigation("Person");
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Transaction", b =>
                {
                    b.HasOne("CDR.DataHolder.Banking.Repository.Entities.Account", "Account")
                        .WithMany("Transactions")
                        .HasForeignKey("AccountId");

                    b.Navigation("Account");
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Account", b =>
                {
                    b.Navigation("Transactions");
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Customer", b =>
                {
                    b.Navigation("Accounts");
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Organisation", b =>
                {
                    b.Navigation("Customer")
                        .IsRequired();
                });

            modelBuilder.Entity("CDR.DataHolder.Banking.Repository.Entities.Person", b =>
                {
                    b.Navigation("Customer")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
