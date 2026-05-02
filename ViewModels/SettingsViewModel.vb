Public Class SettingsViewModel

    Public Property CheckforUpdatesonStart As Boolean
    Public Property DataExportFormat As String
    Public Property ShowToolTips As Boolean
    Public Property DisableSound As Boolean

    Public Property LoadAssetsonStartup As Boolean
    Public Property LoadBPsonStartup As Boolean
    Public Property LoadESIMarketDataonStartup As Boolean
    Public Property LoadESISystemCostIndiciesDataonStartup As Boolean
    Public Property LoadESIPublicStructuresonStartup As Boolean
    Public Property SupressESIStatusMessages As Boolean

    Public Property IncludeInGameLinksinCopyText As Boolean
    Public Property SaveFacilitiesbyChar As Boolean
    Public Property LoadBPsbyChar As Boolean

    Public Property UseBrokerCorpStanding As Boolean
    Public Property BrokerCorpStandingText As String
    Public Property UseBrokerFactionStanding As Boolean
    Public Property BrokerFactionStandingText As String

    Public Property UseManufacturingImplant As Boolean
    Public Property ManufacturingImplantName As String
    Public Property UseRefiningImplant As Boolean
    Public Property RefiningImplantName As String
    Public Property UseCopyImplant As Boolean
    Public Property CopyImplantName As String

    Public Property CheckBuildBuy As Boolean
    Public Property SuggestBuildBPNotOwned As Boolean
    Public Property BuildWhenNotEnoughItemsonMarket As Boolean
    Public Property ManualPriceOverride As Boolean
    Public Property SaveBPRelicsDecryptors As Boolean
    Public Property AlwaysBuyFuelBlocks As Boolean
    Public Property AlwaysBuyRAMs As Boolean
    Public Property SaveBPCCostperBP As Boolean

    Public Property DisableSVR As Boolean
    Public Property DisableGATracking As Boolean
    Public Property ShareSavedFacilities As Boolean

    Public Property AlphaAccount As Boolean
    Public Property UseActiveSkillLevels As Boolean
    Public Property LoadMaxAlphaSkills As Boolean

    Public Property ShopListIncludeInventMats As Boolean
    Public Property ShopListIncludeCopyMats As Boolean

    Public Property UseDefaultME As Boolean
    Public Property DefaultMEText As String
    Public Property UseDefaultTE As Boolean
    Public Property DefaultTEText As String
    Public Property UseCustomPriceRefreshInterval As Boolean
    Public Property PriceRefreshIntervalText As String

    Public Property AutoUpdateSVRonBPTab As Boolean
    Public Property ProxyAddress As String
    Public Property ProxyPortText As String

    Public Sub New()
        DataExportFormat = ""
        BrokerCorpStandingText = ""
        BrokerFactionStandingText = ""
        ManufacturingImplantName = ""
        RefiningImplantName = ""
        CopyImplantName = ""
        DefaultMEText = ""
        DefaultTEText = ""
        PriceRefreshIntervalText = ""
        ProxyAddress = ""
        ProxyPortText = ""
    End Sub

End Class
