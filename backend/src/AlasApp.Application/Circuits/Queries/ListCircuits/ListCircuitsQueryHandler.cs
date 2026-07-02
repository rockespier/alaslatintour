using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Circuits.Queries.ListCircuits;

public sealed class ListCircuitsQueryHandler(ICircuitRepository circuitRepository)
    : IRequestHandler<ListCircuitsQuery, PagedResult<CircuitDto>>
{
    public Task<PagedResult<CircuitDto>> Handle(ListCircuitsQuery request, CancellationToken cancellationToken)
    {
        return circuitRepository.ListAsync(request.Filter, cancellationToken);
    }
}
