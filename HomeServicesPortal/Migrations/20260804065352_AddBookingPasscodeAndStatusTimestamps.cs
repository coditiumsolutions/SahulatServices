using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPasscodeAndStatusTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.UID);
                });

            migrationBuilder.CreateTable(
                name: "UserOTP",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MobileNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OTPCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OTPType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    VerifiedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOTP", x => x.UID);
                });

            migrationBuilder.CreateTable(
                name: "UsersLogin",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MobileNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    LastLogin = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersLogin", x => x.UID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceUID = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ServiceCategories_Services",
                        column: x => x.ServiceUID,
                        principalTable: "Services",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserUID = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.UID);
                    table.ForeignKey(
                        name: "FK_Clients_UsersLogin",
                        column: x => x.UserUID,
                        principalTable: "UsersLogin",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserUID = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.UID);
                    table.ForeignKey(
                        name: "FK_Staff_UsersLogin",
                        column: x => x.UserUID,
                        principalTable: "UsersLogin",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserUID = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", nullable: false, defaultValue: 0m),
                    TotalReviews = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalJobsCompleted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AvailableTiming = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CategoryUID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.UID);
                    table.ForeignKey(
                        name: "FK_Providers_ServiceCategories",
                        column: x => x.CategoryUID,
                        principalTable: "ServiceCategories",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_UsersLogin",
                        column: x => x.UserUID,
                        principalTable: "UsersLogin",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientAddresses",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientUID = table.Column<int>(type: "int", nullable: false),
                    AddressTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientAddresses", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ClientAddresses_Clients",
                        column: x => x.ClientUID,
                        principalTable: "Clients",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CategoryUID = table.Column<int>(type: "int", nullable: true),
                    ProviderUID = table.Column<int>(type: "int", nullable: true),
                    RuleType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.UID);
                    table.ForeignKey(
                        name: "FK_CommissionRules_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionRules_ServiceCategories",
                        column: x => x.CategoryUID,
                        principalTable: "ServiceCategories",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderDocuments",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderUID = table.Column<int>(type: "int", nullable: false),
                    ProfilePhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CNICFrontImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CNICBackImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VerifiedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    VerificationRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderDocuments", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ProviderDocuments_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderPayouts",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderUID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    PaidOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderPayouts", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ProviderPayouts_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerServiceRequests",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientUID = table.Column<int>(type: "int", nullable: false),
                    CategoryUID = table.Column<int>(type: "int", nullable: false),
                    ClientAddressUID = table.Column<int>(type: "int", nullable: false),
                    ServiceTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PreferredServiceTime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsUrgent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstimatedBudget = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerServiceRequests", x => x.UID);
                    table.ForeignKey(
                        name: "FK_CustomerServiceRequests_ClientAddresses",
                        column: x => x.ClientAddressUID,
                        principalTable: "ClientAddresses",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerServiceRequests_Clients",
                        column: x => x.ClientUID,
                        principalTable: "Clients",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerServiceRequests_ServiceCategories",
                        column: x => x.CategoryUID,
                        principalTable: "ServiceCategories",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBookings",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestUID = table.Column<int>(type: "int", nullable: false),
                    ClientUID = table.Column<int>(type: "int", nullable: false),
                    ProviderUID = table.Column<int>(type: "int", nullable: false),
                    ServiceDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    VisitCharges = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    AdditionalCharges = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CustomerPaid = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaymentMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerRemaining = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CommissionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CommissionValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ProviderEarning = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Completed"),
                    Passcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AcceptedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompletedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBookings", x => x.UID);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Clients",
                        column: x => x.ClientUID,
                        principalTable: "Clients",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_CustomerServiceRequests",
                        column: x => x.RequestUID,
                        principalTable: "CustomerServiceRequests",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentLedger",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingUID = table.Column<int>(type: "int", nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProviderUID = table.Column<int>(type: "int", nullable: true),
                    EntryType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentLedger", x => x.UID);
                    table.ForeignKey(
                        name: "FK_PaymentLedger_Providers",
                        column: x => x.ProviderUID,
                        principalTable: "Providers",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentLedger_ServiceBookings",
                        column: x => x.BookingUID,
                        principalTable: "ServiceBookings",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientAddresses_ClientUID",
                table: "ClientAddresses",
                column: "ClientUID");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_UserUID",
                table: "Clients",
                column: "UserUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_CategoryUID",
                table: "CommissionRules",
                column: "CategoryUID");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_ProviderUID",
                table: "CommissionRules",
                column: "ProviderUID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceRequests_CategoryUID",
                table: "CustomerServiceRequests",
                column: "CategoryUID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceRequests_ClientAddressUID",
                table: "CustomerServiceRequests",
                column: "ClientAddressUID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceRequests_ClientUID",
                table: "CustomerServiceRequests",
                column: "ClientUID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLedger_BookingUID",
                table: "PaymentLedger",
                column: "BookingUID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLedger_ProviderUID",
                table: "PaymentLedger",
                column: "ProviderUID");

            migrationBuilder.CreateIndex(
                name: "UQ_ProviderDocuments_ProviderUID",
                table: "ProviderDocuments",
                column: "ProviderUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPayouts_ProviderUID",
                table: "ProviderPayouts",
                column: "ProviderUID");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_CategoryUID",
                table: "Providers",
                column: "CategoryUID");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_UserUID",
                table: "Providers",
                column: "UserUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_ClientUID",
                table: "ServiceBookings",
                column: "ClientUID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_ProviderUID",
                table: "ServiceBookings",
                column: "ProviderUID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_RequestUID",
                table: "ServiceBookings",
                column: "RequestUID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_ServiceUID",
                table: "ServiceCategories",
                column: "ServiceUID");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_UserUID",
                table: "Staff",
                column: "UserUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOTP_IsVerified",
                table: "UserOTP",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_UserOTP_MobileNo",
                table: "UserOTP",
                column: "MobileNo");

            migrationBuilder.CreateIndex(
                name: "IX_UsersLogin_MobileNo",
                table: "UsersLogin",
                column: "MobileNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionRules");

            migrationBuilder.DropTable(
                name: "PaymentLedger");

            migrationBuilder.DropTable(
                name: "ProviderDocuments");

            migrationBuilder.DropTable(
                name: "ProviderPayouts");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "UserOTP");

            migrationBuilder.DropTable(
                name: "ServiceBookings");

            migrationBuilder.DropTable(
                name: "CustomerServiceRequests");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "ClientAddresses");

            migrationBuilder.DropTable(
                name: "ServiceCategories");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "UsersLogin");
        }
    }
}
