using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Circuits.Commands.DeleteCircuit;

public sealed class DeleteCircuitCommandHandler(
    ICircuitRepository circuitRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCircuitCommand, bool>
{
    public async Task<bool> Handle(DeleteCircuitCommand request, CancellationToken cancellationToken)
    {
        if (request.CircuitId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("circuitId", "El identificador del circuito es invalido.")]);
        }

        var circuit = await circuitRepository.GetEntityByIdAsync(request.CircuitId, cancellationToken)
            ?? throw new NotFoundException("Circuito no encontrado.");

        if (await circuitRepository.HasEventsAsync(request.CircuitId, cancellationToken))
        {
            throw new ConflictException("No se puede eliminar un circuito con eventos asociados.");
        }

        try
        {
            circuit.EnsureCanBeDeleted();
            circuitRepository.Remove(circuit);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DomainRuleException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }
}
