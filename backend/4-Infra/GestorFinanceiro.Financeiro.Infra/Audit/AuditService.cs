using System.Text.Json;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;

namespace GestorFinanceiro.Financeiro.Infra.Audit;

public class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string userId,
        object? previousData,
        CancellationToken cancellationToken)
    {
        var serializedPreviousData = previousData is null
            ? null
            : JsonSerializer.Serialize(previousData, JsonSerializerOptions);

        var auditLog = AuditLog.Create(entityType, entityId, action, userId, serializedPreviousData);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }
}
