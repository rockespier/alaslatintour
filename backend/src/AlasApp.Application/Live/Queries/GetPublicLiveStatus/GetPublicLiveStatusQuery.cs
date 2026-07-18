using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Live.Models;

namespace AlasApp.Application.Live.Queries.GetPublicLiveStatus;

public sealed record GetPublicLiveStatusQuery : IRequest<PublicLiveStatusDto>;
