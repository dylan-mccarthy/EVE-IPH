using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class CharacterManagementViewModel : ObservableObject
{
    private readonly ICharacterManagementQueryService _characterManagementQueryService;
    private readonly ICharacterManagementCommandService _characterManagementCommandService;
    private IReadOnlyList<CharacterConnectionItem> _characters = [];
    private CharacterConnectionItem? _selectedCharacter;
    private IReadOnlyList<CorporationConnectionItem> _corporations = [];
    private CorporationConnectionItem? _selectedCorporation;
    private string _statusText = "Loading stored characters...";
    private bool _isBusy;

    public CharacterManagementViewModel(
        ICharacterManagementQueryService characterManagementQueryService,
        ICharacterManagementCommandService characterManagementCommandService)
    {
        _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));
        _characterManagementCommandService = characterManagementCommandService ?? throw new ArgumentNullException(nameof(characterManagementCommandService));
        LoadTask = ReloadCharactersAsync();
    }

    public Task LoadTask { get; }

    public IReadOnlyList<CharacterConnectionItem> Characters
    {
        get => _characters;
        private set => SetProperty(ref _characters, value);
    }

    public CharacterConnectionItem? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (SetProperty(ref _selectedCharacter, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(CanRefreshSelectedCharacter));
                OnPropertyChanged(nameof(CanConnectCorporation));
                OnPropertyChanged(nameof(CanSetDefaultSelectedCharacter));
                OnPropertyChanged(nameof(CanDeleteSelectedCharacter));
            }
        }
    }

    public IReadOnlyList<CorporationConnectionItem> Corporations
    {
        get => _corporations;
        private set => SetProperty(ref _corporations, value);
    }

    public CorporationConnectionItem? SelectedCorporation
    {
        get => _selectedCorporation;
        set
        {
            if (SetProperty(ref _selectedCorporation, value))
            {
                OnPropertyChanged(nameof(CanRefreshSelectedCorporation));
                OnPropertyChanged(nameof(CanDeleteSelectedCorporation));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanConnectCharacter));
                OnPropertyChanged(nameof(CanRefreshSelectedCharacter));
                OnPropertyChanged(nameof(CanSetDefaultSelectedCharacter));
                OnPropertyChanged(nameof(CanDeleteSelectedCharacter));
            }
        }
    }

    public bool HasSelection => SelectedCharacter is not null;

    public bool CanConnectCharacter => !IsBusy;

    public bool CanConnectCorporation => !IsBusy && SelectedCharacter is not null && !SpecialCharacters.IsAllSkillsV(SelectedCharacter.CharacterId);

    public bool CanRefreshSelectedCharacter => !IsBusy && SelectedCharacter is not null && !SpecialCharacters.IsAllSkillsV(SelectedCharacter.CharacterId);

    public bool CanSetDefaultSelectedCharacter => !IsBusy && SelectedCharacter is not null && !SelectedCharacter.IsDefault;

    public bool CanDeleteSelectedCharacter => !IsBusy && SelectedCharacter is not null && !SpecialCharacters.IsAllSkillsV(SelectedCharacter.CharacterId);

    public bool CanRefreshSelectedCorporation => !IsBusy && SelectedCorporation is not null;

    public bool CanDeleteSelectedCorporation => !IsBusy && SelectedCorporation is not null;

    public async Task ConnectCharacterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var connectResult = await _characterManagementCommandService.AuthenticateAndRefreshAsync().ConfigureAwait(false);
            if (connectResult.IsFailure)
            {
                StatusText = $"Unable to connect character: {connectResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(connectResult.Value.CharacterId.Value).ConfigureAwait(false);
            StatusText = $"Synced {connectResult.Value.Name} from ESI.";
        }).ConfigureAwait(false);
    }

    public async Task ConnectCorporationFromSelectedCharacterAsync()
    {
        CharacterConnectionItem? selectedCharacter = SelectedCharacter;
        if (selectedCharacter is null || IsBusy)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var connectResult = await _characterManagementCommandService.ConnectCorporationAsync(selectedCharacter.Character.CharacterId).ConfigureAwait(false);
            if (connectResult.IsFailure)
            {
                StatusText = $"Unable to connect corporation: {connectResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(selectedCharacter.Character.CharacterId.Value, connectResult.Value.CorporationId.Value).ConfigureAwait(false);
            StatusText = $"Connected corporation {connectResult.Value.Name} using {selectedCharacter.Name}.";
        }).ConfigureAwait(false);
    }

    public async Task RefreshSelectedCharacterAsync()
    {
        CharacterConnectionItem? selectedCharacter = SelectedCharacter;
        if (selectedCharacter is null || IsBusy)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var refreshResult = await _characterManagementCommandService.RefreshAsync(selectedCharacter.Character.CharacterId).ConfigureAwait(false);
            if (refreshResult.IsFailure)
            {
                StatusText = $"Unable to refresh character: {refreshResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(refreshResult.Value.CharacterId.Value, SelectedCorporation?.CorporationId.Value).ConfigureAwait(false);
            StatusText = $"Refreshed {refreshResult.Value.Name} from ESI.";
        }).ConfigureAwait(false);
    }

    public async Task RefreshSelectedCorporationAsync()
    {
        CorporationConnectionItem? selectedCorporation = SelectedCorporation;
        if (selectedCorporation is null || IsBusy)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var refreshResult = await _characterManagementCommandService.RefreshCorporationAsync(selectedCorporation.Corporation.CorporationId).ConfigureAwait(false);
            if (refreshResult.IsFailure)
            {
                StatusText = $"Unable to refresh corporation: {refreshResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(SelectedCharacter?.CharacterId.Value, selectedCorporation.CorporationId.Value).ConfigureAwait(false);
            StatusText = $"Refreshed corporation {refreshResult.Value.Name} from ESI.";
        }).ConfigureAwait(false);
    }

    public async Task SetSelectedCharacterDefaultAsync()
    {
        CharacterConnectionItem? selectedCharacter = SelectedCharacter;
        if (selectedCharacter is null || IsBusy || selectedCharacter.IsDefault)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var setDefaultResult = await _characterManagementCommandService.SetDefaultAsync(selectedCharacter.Character.CharacterId).ConfigureAwait(false);
            if (setDefaultResult.IsFailure)
            {
                StatusText = $"Unable to set default character: {setDefaultResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(selectedCharacter.CharacterId.Value, SelectedCorporation?.CorporationId.Value).ConfigureAwait(false);
            StatusText = $"{selectedCharacter.Name} is now the default character.";
        }).ConfigureAwait(false);
    }

    public async Task DeleteSelectedCharacterAsync()
    {
        CharacterConnectionItem? selectedCharacter = SelectedCharacter;
        if (selectedCharacter is null || IsBusy)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var deleteResult = await _characterManagementCommandService.DeleteAsync(selectedCharacter.Character.CharacterId).ConfigureAwait(false);
            if (deleteResult.IsFailure)
            {
                StatusText = $"Unable to remove character: {deleteResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(null, SelectedCorporation?.CorporationId.Value).ConfigureAwait(false);
            StatusText = $"Removed {selectedCharacter.Name} from the local store.";
        }).ConfigureAwait(false);
    }

    public async Task DeleteSelectedCorporationAsync()
    {
        CorporationConnectionItem? selectedCorporation = SelectedCorporation;
        if (selectedCorporation is null || IsBusy)
        {
            return;
        }

        await RunBusyActionAsync(async () =>
        {
            var deleteResult = await _characterManagementCommandService.DeleteCorporationAsync(selectedCorporation.Corporation.CorporationId).ConfigureAwait(false);
            if (deleteResult.IsFailure)
            {
                StatusText = $"Unable to remove corporation: {deleteResult.Error.Message}";
                return;
            }

            await ReloadCharactersAsync(SelectedCharacter?.CharacterId.Value, null).ConfigureAwait(false);
            StatusText = $"Removed corporation {selectedCorporation.Name} from the local store.";
        }).ConfigureAwait(false);
    }

    private async Task ReloadCharactersAsync(
        long? selectedCharacterId = null,
        long? selectedCorporationId = null)
    {
        var screenDataResult = await _characterManagementQueryService.GetScreenDataAsync().ConfigureAwait(false);

        if (screenDataResult.IsFailure)
        {
            Characters = [];
            SelectedCharacter = null;
            Corporations = [];
            SelectedCorporation = null;
            StatusText = $"Unable to load stored characters: {screenDataResult.Error.Message}";
            return;
        }

        CharacterManagementScreenData screenData = screenDataResult.Value;

        Characters = screenData.Characters
            .Select(character => new CharacterConnectionItem(character.Character, character.TokenStatus))
            .ToArray();
        SelectedCharacter = SelectCharacter(Characters, selectedCharacterId);

        Corporations = screenData.Corporations
            .Select(corporation => new CorporationConnectionItem(corporation.Corporation, corporation.AuthorizedCharacterName))
            .ToArray();
        SelectedCorporation = SelectCorporation(Corporations, selectedCorporationId);

        StatusText = screenData.StatusText;
    }

    private static CharacterConnectionItem? SelectCharacter(IReadOnlyList<CharacterConnectionItem> characters, long? selectedCharacterId)
    {
        if (characters.Count == 0)
        {
            return null;
        }

        if (selectedCharacterId.HasValue)
        {
            CharacterConnectionItem? matchingCharacter = characters.FirstOrDefault(character => character.CharacterId.Value == selectedCharacterId.Value);
            if (matchingCharacter is not null)
            {
                return matchingCharacter;
            }
        }

        return characters.FirstOrDefault(character => character.IsDefault) ?? characters[0];
    }

    private static CorporationConnectionItem? SelectCorporation(IReadOnlyList<CorporationConnectionItem> corporations, long? selectedCorporationId)
    {
        if (corporations.Count == 0)
        {
            return null;
        }

        if (selectedCorporationId.HasValue)
        {
            CorporationConnectionItem? matchingCorporation = corporations.FirstOrDefault(corporation => corporation.CorporationId.Value == selectedCorporationId.Value);
            if (matchingCorporation is not null)
            {
                return matchingCorporation;
            }
        }

        return corporations[0];
    }

    private async Task RunBusyActionAsync(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }
}