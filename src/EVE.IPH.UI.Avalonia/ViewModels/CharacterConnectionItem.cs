using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class CharacterConnectionItem(CharacterRecord character, CharacterTokenStatus tokenStatus)
{
    public CharacterRecord Character { get; } = character ?? throw new ArgumentNullException(nameof(character));

    public CharacterTokenStatus TokenStatus { get; } = tokenStatus ?? throw new ArgumentNullException(nameof(tokenStatus));

    public string Name => Character.Name;

    public CharacterId CharacterId => Character.CharacterId;

    public CorporationId CorporationId => Character.CorporationId;

    public string AllianceId => Character.AllianceId.HasValue ? Character.AllianceId.Value.Value.ToString() : "No alliance";

    public bool IsDefault => Character.IsDefault;

    public string TokenStatusText => TokenStatus.StatusText;

    public bool HasHealthyToken => TokenStatus.HasStoredToken && !TokenStatus.IsExpired;
}