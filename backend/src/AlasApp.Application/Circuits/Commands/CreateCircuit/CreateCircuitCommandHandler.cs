using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Circuits.Commands.CreateCircuit;

public sealed class CreateCircuitCommandHandler(
    ICircuitRepository circuitRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateCircuitCommand, CircuitDto>
{
    public async Task<CircuitDto> Handle(CreateCircuitCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        try
        {
            var circuit = Circuit.Create(
                request.Nombre,
                request.Temporada,
                request.Descripcion,
                request.Region,
                request.Modalidad,
                request.Estado,
                request.SurfScoresCode);

            circuit.SetCreated(clock.UtcNow);

            await circuitRepository.AddAsync(circuit, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await circuitRepository.GetByIdAsync(circuit.Id, cancellationToken)
                ?? throw new NotFoundException("Circuito no encontrado despues de crearlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(CreateCircuitCommand request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (request.Temporada is < 2020 or > 2030)
        {
            errors.Add(new ValidationError("temporada", "La temporada debe estar entre 2020 y 2030."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}
