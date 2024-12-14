﻿// <auto-generated />
using System;
using CaseRelayAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CaseRelayAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241211184437_UpdateUserModel2")]
    partial class UpdateUserModel2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CaseRelayAPI.Models.Case", b =>
                {
                    b.Property<int>("CaseID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CaseID"));

                    b.Property<string>("CaseNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CaseTitle")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("OfficerAssigned")
                        .HasColumnType("int");

                    b.Property<int>("OfficerUserID")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("CaseID");

                    b.HasIndex("OfficerUserID");

                    b.ToTable("Cases");
                });

            modelBuilder.Entity("CaseRelayAPI.Models.User", b =>
                {
                    b.Property<int>("UserID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserID"));

                    b.Property<string>("BadgeNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Clearance")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Department")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Division")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("FailedLoginAttempts")
                        .HasColumnType("int");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLogin")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastPasswordChange")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LockoutEnd")
                        .HasColumnType("datetime2");

                    b.Property<string>("MobilePhone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PasscodeHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PersonalIdentificationNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PoliceId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Precinct")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfileImageUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Rank")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("RequirePasswordReset")
                        .HasColumnType("bit");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SpecialUnit")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Station")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SupervisorId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("WorkPhone")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CaseRelayAPI.Models.Case", b =>
                {
                    b.HasOne("CaseRelayAPI.Models.User", "Officer")
                        .WithMany()
                        .HasForeignKey("OfficerUserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Officer");
                });
#pragma warning restore 612, 618
        }
    }
}
