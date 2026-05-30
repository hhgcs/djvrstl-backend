using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace djvrstl_backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBackendFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AdminUserId = table.Column<string>(type: "text", nullable: false),
                    SessionTokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "booking_calendar_blocks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: true),
                    DurationHours = table.Column<int>(type: "integer", nullable: true),
                    AttendeeRange = table.Column<string>(type: "text", nullable: true),
                    Customer_Name = table.Column<string>(type: "text", nullable: true),
                    Customer_Email = table.Column<string>(type: "text", nullable: true),
                    Customer_Phone = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_Street = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_ExteriorNumber = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_Neighborhood = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_City = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_State = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_PostalCode = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_Country = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_References = table.Column<string>(type: "text", nullable: true),
                    Total = table.Column<int>(type: "integer", nullable: true),
                    DepositTotal = table.Column<int>(type: "integer", nullable: true),
                    RemainingBalance = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_calendar_blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "booking_holds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BookingId = table.Column<string>(type: "text", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Customer_Name = table.Column<string>(type: "text", nullable: false),
                    Customer_Email = table.Column<string>(type: "text", nullable: false),
                    Customer_Phone = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_Street = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_ExteriorNumber = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_Neighborhood = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_City = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_State = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_PostalCode = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_Country = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_References = table.Column<string>(type: "text", nullable: true),
                    QuoteAddress_Street = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_ExteriorNumber = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    QuoteAddress_Neighborhood = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_City = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_State = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_PostalCode = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_Country = table.Column<string>(type: "text", nullable: false),
                    QuoteAddress_References = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_holds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PackageId = table.Column<string>(type: "text", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DurationHours = table.Column<int>(type: "integer", nullable: false),
                    AttendeeRange = table.Column<string>(type: "text", nullable: false),
                    EventAddress_Street = table.Column<string>(type: "text", nullable: false),
                    EventAddress_ExteriorNumber = table.Column<string>(type: "text", nullable: false),
                    EventAddress_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    EventAddress_Neighborhood = table.Column<string>(type: "text", nullable: false),
                    EventAddress_City = table.Column<string>(type: "text", nullable: false),
                    EventAddress_State = table.Column<string>(type: "text", nullable: false),
                    EventAddress_PostalCode = table.Column<string>(type: "text", nullable: false),
                    EventAddress_Country = table.Column<string>(type: "text", nullable: false),
                    EventAddress_References = table.Column<string>(type: "text", nullable: true),
                    Customer_Name = table.Column<string>(type: "text", nullable: false),
                    Customer_Email = table.Column<string>(type: "text", nullable: false),
                    Customer_Phone = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_Street = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_ExteriorNumber = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_Neighborhood = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_City = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_State = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_PostalCode = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_Country = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_References = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<int>(type: "integer", nullable: false),
                    AttendeeFee = table.Column<int>(type: "integer", nullable: false),
                    ExtraHoursFee = table.Column<int>(type: "integer", nullable: false),
                    LocationFee = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    DepositTotal = table.Column<int>(type: "integer", nullable: false),
                    RemainingBalance = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ProviderPreferenceId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Address_Street = table.Column<string>(type: "text", nullable: false),
                    Address_ExteriorNumber = table.Column<string>(type: "text", nullable: false),
                    Address_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    Address_Neighborhood = table.Column<string>(type: "text", nullable: false),
                    Address_City = table.Column<string>(type: "text", nullable: false),
                    Address_State = table.Column<string>(type: "text", nullable: false),
                    Address_PostalCode = table.Column<string>(type: "text", nullable: false),
                    Address_Country = table.Column<string>(type: "text", nullable: false),
                    Address_References = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Customer_Name = table.Column<string>(type: "text", nullable: false),
                    Customer_Email = table.Column<string>(type: "text", nullable: false),
                    Customer_Phone = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_Street = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_ExteriorNumber = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_InteriorNumber = table.Column<string>(type: "text", nullable: true),
                    Customer_Address_Neighborhood = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_City = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_State = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_PostalCode = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_Country = table.Column<string>(type: "text", nullable: false),
                    Customer_Address_References = table.Column<string>(type: "text", nullable: true),
                    Subtotal = table.Column<int>(type: "integer", nullable: false),
                    ShippingFee = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ProviderPreferenceId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ProviderEventId = table.Column<string>(type: "text", nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "text", nullable: true),
                    ProviderPreferenceId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BookingId = table.Column<string>(type: "text", nullable: true),
                    OrderId = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawPayloadJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Dimensions_Height = table.Column<decimal>(type: "numeric", nullable: false),
                    Dimensions_Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Dimensions_Length = table.Column<decimal>(type: "numeric", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Colors = table.Column<string[]>(type: "text[]", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Images = table.Column<string[]>(type: "text[]", nullable: false),
                    AmazonUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<string>(type: "text", nullable: false),
                    ProductSlug = table.Column<string>(type: "text", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<int>(type: "integer", nullable: false),
                    LineTotal = table.Column<int>(type: "integer", nullable: false),
                    Dimensions_Height = table.Column<decimal>(type: "numeric", nullable: false),
                    Dimensions_Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Dimensions_Length = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_sessions_AdminUserId",
                table: "admin_sessions",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_sessions_ExpiresAt",
                table: "admin_sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_admin_sessions_SessionTokenHash",
                table: "admin_sessions",
                column: "SessionTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_Email",
                table: "admin_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_calendar_blocks_Date_Status",
                table: "booking_calendar_blocks",
                columns: new[] { "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_holds_EventDate_Status",
                table: "booking_holds",
                columns: new[] { "EventDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_holds_ExpiresAt",
                table: "booking_holds",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_EventDate",
                table: "bookings",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_Status",
                table: "bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Email",
                table: "customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_leads_CreatedAt",
                table: "leads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_leads_Email",
                table: "leads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductId",
                table: "order_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt",
                table: "orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status",
                table: "orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_ProviderEventId",
                table: "payment_events",
                column: "ProviderEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_ProviderPaymentId",
                table: "payment_events",
                column: "ProviderPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_ProviderPreferenceId",
                table: "payment_events",
                column: "ProviderPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_products_Slug",
                table: "products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_sessions");

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "booking_calendar_blocks");

            migrationBuilder.DropTable(
                name: "booking_holds");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "payment_events");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
