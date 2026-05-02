Public Class AssetsViewerViewModel

    Public Property AssetType As String
    Public Property SortByName As Boolean
    Public Property SelectedAccount As Boolean
    Public Property ItemFilterText As String
    Public Property AllItems As Boolean

    Public Property AllRawMats As Boolean
    Public Property AdvancedProtectiveTechnology As Boolean
    Public Property Gas As Boolean
    Public Property IceProducts As Boolean
    Public Property MolecularForgingTools As Boolean
    Public Property FactionMaterials As Boolean
    Public Property NamedComponents As Boolean
    Public Property Minerals As Boolean
    Public Property Planetary As Boolean
    Public Property RawMaterials As Boolean
    Public Property Salvage As Boolean
    Public Property Misc As Boolean
    Public Property BPCs As Boolean

    Public Property AdvancedMoonMats As Boolean
    Public Property BoosterMats As Boolean
    Public Property MolecularForgedMats As Boolean
    Public Property Polymers As Boolean
    Public Property ProcessedMoonMats As Boolean
    Public Property RawMoonMats As Boolean

    Public Property AncientRelics As Boolean
    Public Property Datacores As Boolean
    Public Property Decryptors As Boolean
    Public Property RDB As Boolean

    Public Property AllManufacturedItems As Boolean

    Public Property Ships As Boolean
    Public Property Charges As Boolean
    Public Property Modules As Boolean
    Public Property Drones As Boolean
    Public Property Rigs As Boolean
    Public Property Subsystems As Boolean
    Public Property Deployables As Boolean
    Public Property Boosters As Boolean
    Public Property Structures As Boolean
    Public Property StructureRigs As Boolean
    Public Property Celestials As Boolean
    Public Property StructureModules As Boolean
    Public Property Implants As Boolean

    Public Property AdvancedCapComponents As Boolean
    Public Property AdvancedComponents As Boolean
    Public Property FuelBlocks As Boolean
    Public Property ProtectiveComponents As Boolean
    Public Property RAM As Boolean
    Public Property NoBuildItems As Boolean
    Public Property CapitalShipComponents As Boolean
    Public Property StructureComponents As Boolean
    Public Property SubsystemComponents As Boolean

    Public Property T1 As Boolean
    Public Property T2 As Boolean
    Public Property T3 As Boolean
    Public Property Storyline As Boolean
    Public Property Faction As Boolean
    Public Property Pirate As Boolean

    Public Sub New()
        AssetType = ""
        ItemFilterText = ""
    End Sub

End Class

Public Class AssetsViewerAccountViewModel

    Public Property CharacterName As String
    Public Property CorporationName As String
    Public Property CharacterId As Long
    Public Property CorporationId As Long
    Public Property IsChecked As Boolean

    Public Sub New()
        CharacterName = ""
        CorporationName = ""
        CharacterId = 0
        CorporationId = 0
        IsChecked = False
    End Sub

End Class

Public Class AssetsViewerAssetEntryViewModel

    Public Property CharacterId As Long
    Public Property CharacterName As String

    Public Sub New()
        CharacterId = 0
        CharacterName = ""
    End Sub

End Class

Public Class AssetsViewerRefreshRequestViewModel

    Public Property SortOption As SortType
    Public Property SearchItemList As List(Of Long)
    Public Property OnlyBPCs As Boolean
    Public Property AssetEntries As List(Of AssetsViewerAssetEntryViewModel)

    Public Sub New()
        SortOption = SortType.Name
        SearchItemList = New List(Of Long)
        OnlyBPCs = False
        AssetEntries = New List(Of AssetsViewerAssetEntryViewModel)
    End Sub

End Class
