Public NotInheritable Class SettingsService

    Private Sub New()
    End Sub

    Public Shared Function BuildViewModel(ByVal settings As ApplicationSettings, ByVal defaults As ProgramSettings) As SettingsViewModel
        Dim viewModel As New SettingsViewModel

        With viewModel
            .CheckforUpdatesonStart = settings.CheckforUpdatesonStart
            .DataExportFormat = settings.DataExportFormat
            .ShowToolTips = settings.ShowToolTips
            .DisableSound = settings.DisableSound

            .LoadAssetsonStartup = settings.LoadAssetsonStartup
            .LoadBPsonStartup = settings.LoadBPsonStartup
            .LoadESIMarketDataonStartup = settings.LoadESIMarketDataonStartup
            .LoadESISystemCostIndiciesDataonStartup = settings.LoadESISystemCostIndiciesDataonStartup
            .LoadESIPublicStructuresonStartup = settings.LoadESIPublicStructuresonStartup
            .SupressESIStatusMessages = settings.SupressESIStatusMessages

            .IncludeInGameLinksinCopyText = settings.IncludeInGameLinksinCopyText
            .SaveFacilitiesbyChar = settings.SaveFacilitiesbyChar
            .LoadBPsbyChar = settings.LoadBPsbyChar

            .UseBrokerCorpStanding = (settings.BrokerCorpStanding <> defaults.DefaultBrokerCorpStanding)
            .BrokerCorpStandingText = FormatNumber(If(.UseBrokerCorpStanding, settings.BrokerCorpStanding, defaults.DefaultBrokerCorpStanding), 2)
            .UseBrokerFactionStanding = (settings.BrokerFactionStanding <> defaults.DefaultBrokerFactionStanding)
            .BrokerFactionStandingText = FormatNumber(If(.UseBrokerFactionStanding, settings.BrokerFactionStanding, defaults.DefaultBrokerFactionStanding), 2)

            .UseManufacturingImplant = settings.ManufacturingImplantValue > 0
            .ManufacturingImplantName = ResolveManufacturingImplantName(settings.ManufacturingImplantValue, defaults)
            .UseRefiningImplant = settings.RefiningImplantValue > 0
            .RefiningImplantName = ResolveRefiningImplantName(settings.RefiningImplantValue, defaults)
            .UseCopyImplant = settings.CopyImplantValue > 0
            .CopyImplantName = ResolveCopyImplantName(settings.CopyImplantValue, defaults)

            .CheckBuildBuy = settings.CheckBuildBuy
            .SuggestBuildBPNotOwned = settings.SuggestBuildBPNotOwned
            .BuildWhenNotEnoughItemsonMarket = settings.BuildWhenNotEnoughItemsonMarket
            .ManualPriceOverride = settings.ManualPriceOverride
            .SaveBPRelicsDecryptors = settings.SaveBPRelicsDecryptors
            .AlwaysBuyFuelBlocks = settings.AlwaysBuyFuelBlocks
            .AlwaysBuyRAMs = settings.AlwaysBuyRAMs
            .SaveBPCCostperBP = settings.SaveBPCCostperBP

            .DisableSVR = settings.DisableSVR
            .DisableGATracking = settings.DisableGATracking
            .ShareSavedFacilities = settings.ShareSavedFacilities

            .AlphaAccount = settings.AlphaAccount
            .UseActiveSkillLevels = settings.UseActiveSkillLevels
            .LoadMaxAlphaSkills = settings.LoadMaxAlphaSkills

            .ShopListIncludeInventMats = settings.ShopListIncludeInventMats
            .ShopListIncludeCopyMats = settings.ShopListIncludeCopyMats

            .UseDefaultME = (settings.DefaultBPME <> 0)
            .DefaultMEText = CStr(If(.UseDefaultME, settings.DefaultBPME, defaults.DefaultSettingME))
            .UseDefaultTE = (settings.DefaultBPTE <> 0)
            .DefaultTEText = CStr(If(.UseDefaultTE, settings.DefaultBPTE, defaults.DefaultSettingTE))
            .UseCustomPriceRefreshInterval = (settings.UpdatePricesRefreshInterval <> defaults.DefaultUpdatePricesRefreshInterval)
            .PriceRefreshIntervalText = CStr(If(.UseCustomPriceRefreshInterval, settings.UpdatePricesRefreshInterval, defaults.DefaultUpdatePricesRefreshInterval))

            .AutoUpdateSVRonBPTab = settings.AutoUpdateSVRonBPTab
            .ProxyAddress = settings.ProxyAddress
            .ProxyPortText = CStr(settings.ProxyPort)
        End With

        Return viewModel
    End Function

    Public Shared Function BuildApplicationSettings(ByVal viewModel As SettingsViewModel, ByVal existingSettings As ApplicationSettings) As ApplicationSettings
        Dim updatedSettings As ApplicationSettings = existingSettings

        With updatedSettings
            .CheckforUpdatesonStart = viewModel.CheckforUpdatesonStart
            .DataExportFormat = viewModel.DataExportFormat
            .ShowToolTips = viewModel.ShowToolTips
            .DisableSound = viewModel.DisableSound

            .RefiningImplantValue = ResolveRefiningImplantValue(viewModel.RefiningImplantName, viewModel.UseRefiningImplant)
            .ManufacturingImplantValue = ResolveManufacturingImplantValue(viewModel.ManufacturingImplantName, viewModel.UseManufacturingImplant)
            .CopyImplantValue = ResolveCopyImplantValue(viewModel.CopyImplantName, viewModel.UseCopyImplant)

            .LoadAssetsonStartup = viewModel.LoadAssetsonStartup
            .LoadBPsonStartup = viewModel.LoadBPsonStartup
            .LoadESIMarketDataonStartup = viewModel.LoadESIMarketDataonStartup
            .LoadESISystemCostIndiciesDataonStartup = viewModel.LoadESISystemCostIndiciesDataonStartup
            .LoadESIPublicStructuresonStartup = viewModel.LoadESIPublicStructuresonStartup
            .SupressESIStatusMessages = viewModel.SupressESIStatusMessages
            .IncludeInGameLinksinCopyText = viewModel.IncludeInGameLinksinCopyText

            .SaveFacilitiesbyChar = viewModel.SaveFacilitiesbyChar
            .LoadBPsbyChar = viewModel.LoadBPsbyChar

            .BrokerCorpStanding = CDbl(viewModel.BrokerCorpStandingText)
            .BrokerFactionStanding = CDbl(viewModel.BrokerFactionStandingText)

            .DefaultBPME = CInt(viewModel.DefaultMEText)
            .DefaultBPTE = CInt(viewModel.DefaultTEText)

            .CheckBuildBuy = viewModel.CheckBuildBuy
            .SuggestBuildBPNotOwned = viewModel.SuggestBuildBPNotOwned
            .BuildWhenNotEnoughItemsonMarket = viewModel.BuildWhenNotEnoughItemsonMarket
            .ManualPriceOverride = viewModel.ManualPriceOverride
            .SaveBPRelicsDecryptors = viewModel.SaveBPRelicsDecryptors
            .AlwaysBuyFuelBlocks = viewModel.AlwaysBuyFuelBlocks
            .AlwaysBuyRAMs = viewModel.AlwaysBuyRAMs
            .SaveBPCCostperBP = viewModel.SaveBPCCostperBP

            .DisableSVR = viewModel.DisableSVR
            .DisableGATracking = viewModel.DisableGATracking
            .ShareSavedFacilities = viewModel.ShareSavedFacilities

            .AlphaAccount = viewModel.AlphaAccount
            .UseActiveSkillLevels = viewModel.UseActiveSkillLevels
            .LoadMaxAlphaSkills = viewModel.LoadMaxAlphaSkills

            .ShopListIncludeInventMats = viewModel.ShopListIncludeInventMats
            .ShopListIncludeCopyMats = viewModel.ShopListIncludeCopyMats

            .UpdatePricesRefreshInterval = CInt(viewModel.PriceRefreshIntervalText)
            .AutoUpdateSVRonBPTab = viewModel.AutoUpdateSVRonBPTab

            .ProxyAddress = If(viewModel.ProxyAddress <> "", viewModel.ProxyAddress, "")
            If Trim(viewModel.ProxyPortText) <> "" Then
                .ProxyPort = CInt(viewModel.ProxyPortText)
            Else
                .ProxyPort = 0
            End If
        End With

        Return updatedSettings
    End Function

    Private Shared Function ResolveManufacturingImplantName(ByVal implantValue As Double, ByVal defaults As ProgramSettings) As String
        Return ResolveImplantName(
            implantValue,
            New String() {defaults.MBeanCounterName & "1", defaults.MBeanCounterName & "2", defaults.MBeanCounterName & "4"},
            Function(attributes, implantName) -1 * attributes.GetAttribute(implantName, ItemAttributes.manufacturingTimeBonus) / 100)
    End Function

    Private Shared Function ResolveRefiningImplantName(ByVal implantValue As Double, ByVal defaults As ProgramSettings) As String
        Return ResolveImplantName(
            implantValue,
            New String() {defaults.RBeanCounterName & "1", defaults.RBeanCounterName & "2", defaults.RBeanCounterName & "4"},
            Function(attributes, implantName) attributes.GetAttribute(implantName, ItemAttributes.refiningYieldMutator) / 100)
    End Function

    Private Shared Function ResolveCopyImplantName(ByVal implantValue As Double, ByVal defaults As ProgramSettings) As String
        Return ResolveImplantName(
            implantValue,
            New String() {defaults.CBeanCounterName & "1", defaults.CBeanCounterName & "3", defaults.CBeanCounterName & "5"},
            Function(attributes, implantName) -1 * attributes.GetAttribute(implantName, ItemAttributes.copySpeedBonus) / 100)
    End Function

    Private Shared Function ResolveImplantName(ByVal implantValue As Double, ByVal implantNames As IEnumerable(Of String), ByVal valueSelector As Func(Of EVEAttributes, String, Double)) As String
        If implantValue <= 0 Then
            Return ""
        End If

        Dim attributeLookup As New EVEAttributes

        For Each implantName In implantNames
            If implantValue = valueSelector(attributeLookup, implantName) Then
                Return implantName
            End If
        Next

        Return ""
    End Function

    Private Shared Function ResolveManufacturingImplantValue(ByVal implantName As String, ByVal isEnabled As Boolean) As Double
        If Not isEnabled Then
            Return 0
        End If

        Dim attributeLookup As New EVEAttributes
        Return -1 * attributeLookup.GetAttribute(implantName, ItemAttributes.manufacturingTimeBonus) / 100
    End Function

    Private Shared Function ResolveRefiningImplantValue(ByVal implantName As String, ByVal isEnabled As Boolean) As Double
        If Not isEnabled Then
            Return 0
        End If

        Dim attributeLookup As New EVEAttributes
        Return attributeLookup.GetAttribute(implantName, ItemAttributes.refiningYieldMutator) / 100
    End Function

    Private Shared Function ResolveCopyImplantValue(ByVal implantName As String, ByVal isEnabled As Boolean) As Double
        If Not isEnabled Then
            Return 0
        End If

        Dim attributeLookup As New EVEAttributes
        Return -1 * attributeLookup.GetAttribute(implantName, ItemAttributes.copySpeedBonus) / 100
    End Function

End Class
