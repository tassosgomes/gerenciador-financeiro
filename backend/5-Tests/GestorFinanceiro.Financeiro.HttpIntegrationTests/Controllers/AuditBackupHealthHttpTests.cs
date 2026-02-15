using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class AuditBackupHealthHttpTests : IntegrationTestBase
{
    public AuditBackupHealthHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task HealthCheck_WithoutAuthentication_ReturnsOk()
    {
        var response = await Client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DockerAvailableFact]
    public async Task Audit_AdminCanAccess_MemberIsForbidden()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var memberClient = await AuthenticateAsMemberAsync();

        var adminResponse = await adminClient.GetAsync("/api/v1/audit");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var memberResponse = await memberClient.GetAsync("/api/v1/audit");
        memberResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DockerAvailableFact]
    public async Task Audit_FilterByEntityType_ReturnsFilteredEntries()
    {
        var adminClient = await AuthenticateAsAdminAsync();

        await adminClient.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Auditoria",
            type = "Corrente",
            initialBalance = 12m,
            allowNegativeBalance = false
        });

        var response = await adminClient.GetAsync("/api/v1/audit?entityType=Account");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await ReadJsonObjectAsync(response);
        payload["data"]!.AsArray().Should().OnlyContain(item => item!["entityType"]!.GetValue<string>() == "Account");
    }

    [DockerAvailableFact]
    public async Task BackupExport_AdminOnly_AndWithoutPasswordHash()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var memberClient = await AuthenticateAsMemberAsync();

        var adminResponse = await adminClient.GetAsync("/api/v1/backup/export");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await adminResponse.Content.ReadAsStringAsync();
        payload.Should().NotContain("passwordHash");
        payload.Should().NotContain("password_hash");

        var memberResponse = await memberClient.GetAsync("/api/v1/backup/export");
        memberResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DockerAvailableFact]
    public async Task BackupImport_WithInvalidReference_ReturnsBadRequestProblemDetails()
    {
        var adminClient = await AuthenticateAsAdminAsync();

        var invalidImport = new
        {
            users = Array.Empty<object>(),
            accounts = Array.Empty<object>(),
            categories = Array.Empty<object>(),
            recurrenceTemplates = Array.Empty<object>(),
            transactions = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    accountId = Guid.NewGuid(),
                    categoryId = Guid.NewGuid(),
                    type = "Debit",
                    amount = 100,
                    description = "Transacao invalida",
                    competenceDate = DateTime.UtcNow.Date,
                    dueDate = DateTime.UtcNow.Date,
                    status = "Pending",
                    isAdjustment = false,
                    originalTransactionId = (Guid?)null,
                    hasAdjustment = false,
                    installmentGroupId = (Guid?)null,
                    installmentNumber = (int?)null,
                    totalInstallments = (int?)null,
                    isRecurrent = false,
                    recurrenceTemplateId = (Guid?)null,
                    transferGroupId = (Guid?)null,
                    cancellationReason = (string?)null,
                    cancelledBy = (string?)null,
                    cancelledAt = (DateTime?)null,
                    operationId = "import-invalid",
                    createdBy = "seed",
                    createdAt = DateTime.UtcNow,
                    updatedBy = (string?)null,
                    updatedAt = (DateTime?)null
                }
            }
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/backup/import", invalidImport);
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task BackupExportImportExport_RoundTripPreservesCounts()
    {
        var adminClient = await AuthenticateAsAdminAsync();

        var firstExportResponse = await adminClient.GetAsync("/api/v1/backup/export");
        firstExportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstExport = await firstExportResponse.Content.ReadFromJsonAsync<JsonObject>(JsonSerializerOptions);

        var importPayload = firstExport!["data"]!.AsObject();
        var importResponse = await adminClient.PostAsJsonAsync("/api/v1/backup/import", importPayload);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondExportResponse = await adminClient.GetAsync("/api/v1/backup/export");
        secondExportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondExport = await secondExportResponse.Content.ReadFromJsonAsync<JsonObject>(JsonSerializerOptions);

        var firstData = firstExport["data"]!.AsObject();
        var secondData = secondExport!["data"]!.AsObject();

        secondData["users"]!.AsArray().Count.Should().Be(firstData["users"]!.AsArray().Count);
        secondData["accounts"]!.AsArray().Count.Should().Be(firstData["accounts"]!.AsArray().Count);
        secondData["categories"]!.AsArray().Count.Should().Be(firstData["categories"]!.AsArray().Count);
        secondData["transactions"]!.AsArray().Count.Should().Be(firstData["transactions"]!.AsArray().Count);
        secondData["recurrenceTemplates"]!.AsArray().Count.Should().Be(firstData["recurrenceTemplates"]!.AsArray().Count);
    }
}
