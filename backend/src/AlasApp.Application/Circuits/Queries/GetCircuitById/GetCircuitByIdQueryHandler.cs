using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.Circuits.Queries.GetCircuitById;

public sealed class GetCircuitByIdQueryHandler(ICircuitRepository circuitRepository)
    : IRequestHandler<GetCircuitByIdQuery, CircuitDto>
{
    public async Task<CircuitDto> Handle(GetCircuitByIdQuery request, CancellationToken cancellationToken)
    {
        var circuit = await circuitRepository.GetByIdAsync(request.CircuitId, cancellationToken);

        return circuit ?? throw new NotFoundException("Circuito no encontrado.");
    }
}
