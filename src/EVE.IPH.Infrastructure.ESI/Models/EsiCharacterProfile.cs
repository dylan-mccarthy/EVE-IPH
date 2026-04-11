using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// Character profile data returned by ESI.
/// </summary>
public sealed record EsiCharacterProfile(
    CharacterId CharacterId,
    string Name,
    CorporationId CorporationId,
    Maybe<AllianceId> AllianceId);