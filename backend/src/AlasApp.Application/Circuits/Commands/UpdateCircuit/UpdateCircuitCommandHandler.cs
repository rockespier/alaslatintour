using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Circuits.Commands.UpdateCircuit;

public sealed class UpdateCircuitCommandHandler(
    ICircuitRepository circuitRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateCircuitCommand, CircuitDto>
{
    public async Task<CircuitDto> Handle(UpdateCircuitCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var circuit = await circuitRepository.GetEntityByIdAsync(request.CircuitId, cancellationToken)
            ?? throw new NotFoundException("Circuito no encontrado.");

        try
        {
            circuit.Update(
                request.Nombre,
                request.Temporada,
                request.Descripcion,
                request.Region,
                request.Modalidad,
                request.Estado,
                request.SurfScoresCode);

            circuit.SetUpdated(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await circuitRepository.GetByIdAsync(circuit.Id, cancellationToken)
                ?? throw new NotFoundException("Circuito no encontrado despues de actualizarlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(UpdateCircuitCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.CircuitId == Guid.Empty)
        {
            errors.Add(new ValidationError("circuitId", "El identificador del circuito es invalido."));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (request.Temporada is < 2000 or > 2030)
        {
            errors.Add(new ValidationError("temporada", "La temporada debe estar entre 2000 y 2030."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}
