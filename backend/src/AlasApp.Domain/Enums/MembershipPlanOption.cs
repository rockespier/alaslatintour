namespace AlasApp.Domain.Enums;

/// <summary>
/// Plan de membresia individual que un competidor puede sumar a su inscripcion.
/// No confundir con <see cref="MembershipPlan"/>, que es el plan de la membresia de club/federacion.
/// </summary>
public enum MembershipPlanOption
{
    Anual = 1,
    PorEvento = 2
}
