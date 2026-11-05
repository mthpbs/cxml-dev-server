using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CxmlDevServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierPartId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SupplierPartAuxiliaryId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    UnitPrice = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Unspsc = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ManufacturerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ManufacturerPartId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LeadTimeDays = table.Column<int>(type: "INTEGER", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierIdentities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Identity = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingPartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    FromDomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FromIdentity = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SenderDomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SenderIdentity = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ToDomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ToIdentity = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SharedSecret = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    AuthMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ValidationMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PunchOutPostEncoding = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OperationAllowed = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DeploymentMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PayloadId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InReplyTo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RemoteEndpoint = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HttpStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    CxmlStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    CxmlStatusText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RawXml = table.Column<string>(type: "TEXT", nullable: false),
                    ResponseXml = table.Column<string>(type: "TEXT", nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ScenarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageLogs_TradingPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ResponseScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MatchContains = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingUses = table.Column<int>(type: "INTEGER", nullable: true),
                    Behavior = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StatusDetail = table.Column<string>(type: "TEXT", nullable: true),
                    HttpStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    DelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    MalformedBody = table.Column<string>(type: "TEXT", nullable: true),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastHitAt = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseScenarios_TradingPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PunchOutSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BuyerCookie = table.Column<string>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BrowserFormPostUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SetupPayloadId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DeploymentMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BuyerDomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BuyerIdentity = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ContactsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExtrinsicsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ShipToJson = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedSupplierPartId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OriginalLinesJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    OpenedAt = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    SetupMessageLogId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PunchOutSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunchOutSessions_MessageLogs_SetupMessageLogId",
                        column: x => x.SetupMessageLogId,
                        principalTable: "MessageLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PunchOutSessions_TradingPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrderDate = table.Column<string>(type: "TEXT", nullable: true),
                    DeploymentMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TotalAmount = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PayloadId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MessageLogId = table.Column<int>(type: "INTEGER", nullable: true),
                    ShipToJson = table.Column<string>(type: "TEXT", nullable: true),
                    BillToJson = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentJson = table.Column<string>(type: "TEXT", nullable: true),
                    ContactsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CommentsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExtrinsicsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingJson = table.Column<string>(type: "TEXT", nullable: true),
                    TaxJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_MessageLogs_MessageLogId",
                        column: x => x.MessageLogId,
                        principalTable: "MessageLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_TradingPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ValidationIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageLogId = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Line = table.Column<int>(type: "INTEGER", nullable: true),
                    Position = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationIssues_MessageLogs_MessageLogId",
                        column: x => x.MessageLogId,
                        principalTable: "MessageLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PunchOutCartLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CatalogItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    SupplierPartId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SupplierPartAuxiliaryId = table.Column<string>(type: "TEXT", nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    UnitPrice = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Unspsc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClassificationDomain = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ManufacturerName = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartId = table.Column<string>(type: "TEXT", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "INTEGER", nullable: true),
                    IsAdHoc = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PunchOutCartLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunchOutCartLines_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PunchOutCartLines_PunchOutSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PunchOutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierPartId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SupplierPartAuxiliaryId = table.Column<string>(type: "TEXT", nullable: true),
                    BuyerPartId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    UnitPrice = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClassificationDomain = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Classification = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ManufacturerPartId = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerName = table.Column<string>(type: "TEXT", nullable: true),
                    RequestedDeliveryDate = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Category",
                table: "CatalogItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_SupplierPartId",
                table: "CatalogItems",
                column: "SupplierPartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_CorrelationId",
                table: "MessageLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_CreatedAt",
                table: "MessageLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_PartnerId_DocumentType",
                table: "MessageLogs",
                columns: new[] { "PartnerId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_PayloadId",
                table: "MessageLogs",
                column: "PayloadId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutCartLines_CatalogItemId",
                table: "PunchOutCartLines",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutCartLines_SessionId",
                table: "PunchOutCartLines",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutSessions_CorrelationId",
                table: "PunchOutSessions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutSessions_PartnerId",
                table: "PunchOutSessions",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutSessions_SetupMessageLogId",
                table: "PunchOutSessions",
                column: "SetupMessageLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutSessions_SetupPayloadId",
                table: "PunchOutSessions",
                column: "SetupPayloadId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchOutSessions_Token",
                table: "PunchOutSessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_MessageLogId",
                table: "PurchaseOrders",
                column: "MessageLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderId_PartnerId",
                table: "PurchaseOrders",
                columns: new[] { "OrderId", "PartnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PartnerId",
                table: "PurchaseOrders",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PayloadId",
                table: "PurchaseOrders",
                column: "PayloadId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseScenarios_Enabled_Priority",
                table: "ResponseScenarios",
                columns: new[] { "Enabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseScenarios_PartnerId",
                table: "ResponseScenarios",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierIdentities_Domain_Identity",
                table: "SupplierIdentities",
                columns: new[] { "Domain", "Identity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_FromDomain_FromIdentity",
                table: "TradingPartners",
                columns: new[] { "FromDomain", "FromIdentity" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_SenderDomain_SenderIdentity",
                table: "TradingPartners",
                columns: new[] { "SenderDomain", "SenderIdentity" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationIssues_MessageLogId",
                table: "ValidationIssues",
                column: "MessageLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PunchOutCartLines");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "ResponseScenarios");

            migrationBuilder.DropTable(
                name: "SupplierIdentities");

            migrationBuilder.DropTable(
                name: "ValidationIssues");

            migrationBuilder.DropTable(
                name: "CatalogItems");

            migrationBuilder.DropTable(
                name: "PunchOutSessions");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "MessageLogs");

            migrationBuilder.DropTable(
                name: "TradingPartners");
        }
    }
}
