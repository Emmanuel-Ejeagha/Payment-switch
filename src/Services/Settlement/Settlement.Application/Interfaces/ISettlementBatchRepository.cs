using Settlement.Application.DTOs;
using Settlement.Domain.Entities;

namespace Settlement.Application.Interfaces;

public interface ISettlementBatchRepository
{
    Task<SettlementBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SettlementBatch?> GetByBatchDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task AddAsync(SettlementBatch batch, CancellationToken cancellationToken = default);
    Task UpdateAsync(SettlementBatch batch, CancellationToken cancellationToken = default);
    Task<List<SettlementBatchDto>> ListAsync(DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default);
}