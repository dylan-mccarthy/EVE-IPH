Imports System.Data.SQLite
Imports System.Windows.Forms

Public NotInheritable Class AssetsViewerService

    Private Sub New()
    End Sub

    Private Class LoadedAssetCharacter
        Public Property Character As Character
        Public Property DisplayName As String
    End Class

    Public Shared Function BuildViewModel(ByVal settings As AssetWindowSettings) As AssetsViewerViewModel
        Dim viewModel As New AssetsViewerViewModel

        With viewModel
            .AssetType = settings.AssetType
            .SortByName = settings.SortbyName
            .SelectedAccount = settings.SelectedAccount
            .ItemFilterText = settings.ItemFilterText
            .AllItems = settings.AllItems

            .AllRawMats = settings.AllRawMats
            .AdvancedProtectiveTechnology = settings.AdvancedProtectiveTechnology
            .Gas = settings.Gas
            .IceProducts = settings.IceProducts
            .MolecularForgingTools = settings.MolecularForgingTools
            .FactionMaterials = settings.FactionMaterials
            .NamedComponents = settings.NamedComponents
            .Minerals = settings.Minerals
            .Planetary = settings.Planetary
            .RawMaterials = settings.RawMaterials
            .Salvage = settings.Salvage
            .Misc = settings.Misc
            .BPCs = settings.BPCs

            .AdvancedMoonMats = settings.AdvancedMoonMats
            .BoosterMats = settings.BoosterMats
            .MolecularForgedMats = settings.MolecularForgedMats
            .Polymers = settings.Polymers
            .ProcessedMoonMats = settings.ProcessedMoonMats
            .RawMoonMats = settings.RawMoonMats

            .AncientRelics = settings.AncientRelics
            .Datacores = settings.Datacores
            .Decryptors = settings.Decryptors
            .RDB = settings.RDB

            .AllManufacturedItems = settings.AllManufacturedItems

            .Ships = settings.Ships
            .Charges = settings.Charges
            .Modules = settings.Modules
            .Drones = settings.Drones
            .Rigs = settings.Rigs
            .Subsystems = settings.Subsystems
            .Deployables = settings.Deployables
            .Boosters = settings.Boosters
            .Structures = settings.Structures
            .StructureRigs = settings.StructureRigs
            .Celestials = settings.Celestials
            .StructureModules = settings.StructureModules
            .Implants = settings.Implants

            .AdvancedCapComponents = settings.AdvancedCapComponents
            .AdvancedComponents = settings.AdvancedComponents
            .FuelBlocks = settings.FuelBlocks
            .ProtectiveComponents = settings.ProtectiveComponents
            .RAM = settings.RAM
            .NoBuildItems = settings.NoBuildItems
            .CapitalShipComponents = settings.CapitalShipComponents
            .StructureComponents = settings.StructureComponents
            .SubsystemComponents = settings.SubsystemComponents

            .T1 = settings.T1
            .T2 = settings.T2
            .T3 = settings.T3
            .Storyline = settings.Storyline
            .Faction = settings.Faction
            .Pirate = settings.Pirate
        End With

        Return viewModel
    End Function

    Public Shared Function BuildSettings(ByVal viewModel As AssetsViewerViewModel, ByVal selectedCharacterIds As String) As AssetWindowSettings
        Dim settings As AssetWindowSettings = Nothing

        With settings
            .AssetType = viewModel.AssetType
            .SortbyName = viewModel.SortByName
            .SelectedAccount = viewModel.SelectedAccount
            .SelectedCharacterIDs = selectedCharacterIds
            .ItemFilterText = viewModel.ItemFilterText
            .AllItems = viewModel.AllItems

            .AllRawMats = viewModel.AllRawMats
            .AdvancedProtectiveTechnology = viewModel.AdvancedProtectiveTechnology
            .Gas = viewModel.Gas
            .IceProducts = viewModel.IceProducts
            .MolecularForgingTools = viewModel.MolecularForgingTools
            .FactionMaterials = viewModel.FactionMaterials
            .NamedComponents = viewModel.NamedComponents
            .Minerals = viewModel.Minerals
            .Planetary = viewModel.Planetary
            .RawMaterials = viewModel.RawMaterials
            .Salvage = viewModel.Salvage
            .Misc = viewModel.Misc
            .BPCs = viewModel.BPCs

            .AdvancedMoonMats = viewModel.AdvancedMoonMats
            .BoosterMats = viewModel.BoosterMats
            .MolecularForgedMats = viewModel.MolecularForgedMats
            .Polymers = viewModel.Polymers
            .ProcessedMoonMats = viewModel.ProcessedMoonMats
            .RawMoonMats = viewModel.RawMoonMats

            .AncientRelics = viewModel.AncientRelics
            .Datacores = viewModel.Datacores
            .Decryptors = viewModel.Decryptors
            .RDB = viewModel.RDB

            .AllManufacturedItems = viewModel.AllManufacturedItems

            .Ships = viewModel.Ships
            .Charges = viewModel.Charges
            .Modules = viewModel.Modules
            .Drones = viewModel.Drones
            .Rigs = viewModel.Rigs
            .Subsystems = viewModel.Subsystems
            .Deployables = viewModel.Deployables
            .Boosters = viewModel.Boosters
            .Structures = viewModel.Structures
            .StructureRigs = viewModel.StructureRigs
            .Celestials = viewModel.Celestials
            .StructureModules = viewModel.StructureModules
            .Implants = viewModel.Implants

            .AdvancedCapComponents = viewModel.AdvancedCapComponents
            .AdvancedComponents = viewModel.AdvancedComponents
            .FuelBlocks = viewModel.FuelBlocks
            .ProtectiveComponents = viewModel.ProtectiveComponents
            .RAM = viewModel.RAM
            .NoBuildItems = viewModel.NoBuildItems
            .CapitalShipComponents = viewModel.CapitalShipComponents
            .StructureComponents = viewModel.StructureComponents
            .SubsystemComponents = viewModel.SubsystemComponents

            .T1 = viewModel.T1
            .T2 = viewModel.T2
            .T3 = viewModel.T3
            .Storyline = viewModel.Storyline
            .Faction = viewModel.Faction
            .Pirate = viewModel.Pirate
        End With

        Return settings
    End Function

    Public Shared Function LoadAccounts(ByVal selectedSettings As AssetWindowSettings, ByVal multiAccountsSelected As Boolean) As List(Of AssetsViewerAccountViewModel)
        Dim accounts As New List(Of AssetsViewerAccountViewModel)
        Dim selectedCharacterIds As HashSet(Of String) = ParseSelectedCharacterIds(selectedSettings.SelectedCharacterIDs)
        Dim sql As String = "SELECT CHARACTER_NAME, CORPORATION_NAME, CHARACTER_ID, ESI_CHARACTER_DATA.CORPORATION_ID FROM ESI_CHARACTER_DATA, ESI_CORPORATION_DATA "
        sql &= "WHERE ESI_CHARACTER_DATA.CORPORATION_ID = ESI_CORPORATION_DATA.CORPORATION_ID AND CHARACTER_ID <> " & CStr(DummyCharacterID)

        DBCommand = New SQLiteCommand(sql, EVEDB.DBREf)

        Using rsAccounts As SQLiteDataReader = DBCommand.ExecuteReader()
            While rsAccounts.Read()
                Dim account As New AssetsViewerAccountViewModel
                account.CharacterName = rsAccounts.GetString(0)
                account.CorporationName = rsAccounts.GetString(1)
                account.CharacterId = rsAccounts.GetInt64(2)
                account.CorporationId = rsAccounts.GetInt64(3)

                If multiAccountsSelected Then
                    If selectedCharacterIds.Count = 0 Then
                        account.IsChecked = True
                    Else
                        account.IsChecked = selectedCharacterIds.Contains(account.CharacterId.ToString())
                    End If
                End If

                accounts.Add(account)
            End While
        End Using

        Return accounts
    End Function

    Public Shared Function BuildRefreshRequest(ByVal viewModel As AssetsViewerViewModel,
                                               ByVal selectedCharacter As Character,
                                               ByVal selectedCharacterIds As IEnumerable(Of Long),
                                               ByVal shipType As String,
                                               ByVal chargeType As String,
                                               ByVal techChecksEnabled As Boolean()) As AssetsViewerRefreshRequestViewModel
        Dim request As New AssetsViewerRefreshRequestViewModel

        If viewModel.SortByName Then
            request.SortOption = SortType.Name
        Else
            request.SortOption = SortType.Quantity
        End If

        If viewModel.SelectedAccount Then
            Dim entry As New AssetsViewerAssetEntryViewModel
            entry.CharacterId = selectedCharacter.ID
            entry.CharacterName = selectedCharacter.Name
            request.AssetEntries.Add(entry)
        Else
            request.AssetEntries = LoadAssetEntries(selectedCharacterIds)
        End If

        If viewModel.BPCs And Not viewModel.AllItems Then
            request.OnlyBPCs = True
        End If

        If Not viewModel.AllItems OrElse viewModel.ItemFilterText.Trim() <> "" Then
            request.SearchItemList = LoadSearchItemList(viewModel, shipType, chargeType, techChecksEnabled)
        Else
            request.SearchItemList = New List(Of Long)
        End If

        Return request
    End Function

    Public Shared Function LoadAssetEntries(ByVal selectedCharacterIds As IEnumerable(Of Long)) As List(Of AssetsViewerAssetEntryViewModel)
        Dim assetEntries As New List(Of AssetsViewerAssetEntryViewModel)

        For Each characterId In selectedCharacterIds
            Dim sql As String = "SELECT CHARACTER_NAME FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = " & CStr(characterId)
            DBCommand = New SQLiteCommand(sql, EVEDB.DBREf)

            Using reader As SQLiteDataReader = DBCommand.ExecuteReader()
                If reader.Read() Then
                    Dim entry As New AssetsViewerAssetEntryViewModel
                    entry.CharacterId = characterId
                    entry.CharacterName = reader.GetString(0)
                    assetEntries.Add(entry)
                End If
            End Using
        Next

        Return assetEntries
    End Function

    Public Shared Function LoadSearchItemList(ByVal viewModel As AssetsViewerViewModel,
                                              ByVal shipType As String,
                                              ByVal chargeType As String,
                                              ByVal techChecksEnabled As Boolean()) As List(Of Long)
        Dim itemIdList As New List(Of Long)
        Dim sql As String

        If viewModel.AllItems Then
            sql = "SELECT typeID AS ITEM_ID FROM INVENTORY_TYPES, INVENTORY_GROUPS, INVENTORY_CATEGORIES "
            sql &= "WHERE INVENTORY_TYPES.groupID = INVENTORY_GROUPS.groupID "
            sql &= "AND INVENTORY_GROUPS.categoryID = INVENTORY_CATEGORIES.categoryID "

            If viewModel.ItemFilterText <> "" Then
                sql &= " AND typeName LIKE '%" & FormatDBString(Trim(viewModel.ItemFilterText)) & "%' "
            End If

            itemIdList.AddRange(ExecuteItemIdQuery(sql))
        Else
            sql = "SELECT ITEM_ID FROM ITEM_PRICES, INVENTORY_TYPES"
            sql &= " WHERE ITEM_PRICES.ITEM_ID = INVENTORY_TYPES.typeID AND ("

            Dim groupSql As String = BuildItemPriceGroupListSql(viewModel, shipType, chargeType, techChecksEnabled)

            If groupSql <> "" Then
                sql &= groupSql & ")"

                If viewModel.ItemFilterText <> "" Then
                    sql &= " AND ITEM_NAME LIKE '%" & FormatDBString(Trim(viewModel.ItemFilterText)) & "%' "
                End If

                itemIdList.AddRange(ExecuteItemIdQuery(sql))
            End If

            If viewModel.BPCs Then
                sql = "SELECT BLUEPRINT_ID FROM ALL_BLUEPRINTS "

                If viewModel.ItemFilterText <> "" Then
                    sql &= " WHERE ITEM_NAME LIKE '%" & FormatDBString(Trim(viewModel.ItemFilterText)) & "%' "
                End If

                itemIdList.AddRange(ExecuteItemIdQuery(sql))
            End If
        End If

        If itemIdList.Count = 0 Then
            Return Nothing
        Else
            Return itemIdList
        End If
    End Function

    Public Shared Function BuildAssetTreeNodes(ByVal refreshRequest As AssetsViewerRefreshRequestViewModel,
                                               ByVal selectedCharacter As Character,
                                               ByVal windowForm As AssetWindow,
                                               ByVal includePersonalAssets As Boolean,
                                               ByVal includeCorporationAssets As Boolean,
                                               Optional ByVal setStatus As Action(Of String) = Nothing) As List(Of TreeNode)
        Dim assetNodes As New List(Of TreeNode)
        Dim savedLocationIds As New List(Of LocationInfo)
        Dim loadedCharacters As List(Of LoadedAssetCharacter)

        loadedCharacters = LoadAssetCharacters(refreshRequest.AssetEntries, selectedCharacter, windowForm, savedLocationIds, setStatus)

        If includePersonalAssets Then
            For Each loadedCharacter In loadedCharacters
                SetStatusText(setStatus, "Loading Assets for " & loadedCharacter.DisplayName)

                Dim personalNode As TreeNode = loadedCharacter.Character.GetAssets.GetAssetTreeReturnNode(refreshRequest.SortOption,
                                                                                                          refreshRequest.SearchItemList,
                                                                                                          loadedCharacter.DisplayName & " - Personal Assets",
                                                                                                          loadedCharacter.Character.ID,
                                                                                                          savedLocationIds,
                                                                                                          refreshRequest.OnlyBPCs,
                                                                                                          loadedCharacter.Character.CharacterTokenData)

                assetNodes.Add(CType(personalNode.Clone, TreeNode))
            Next
        End If

        If includeCorporationAssets Then
            Dim loadedCorpIds As New List(Of Long)

            For Each loadedCharacter In loadedCharacters
                Dim corporation As Corporation = loadedCharacter.Character.CharacterCorporation

                If Not loadedCorpIds.Contains(corporation.CorporationID) Then
                    SetStatusText(setStatus, "Loading Assets for " & corporation.Name)

                    Dim corporationNode As TreeNode = corporation.GetAssets.GetAssetTreeReturnNode(refreshRequest.SortOption,
                                                                                                    refreshRequest.SearchItemList,
                                                                                                    corporation.Name & " - Corporation Assets",
                                                                                                    corporation.CorporationID,
                                                                                                    savedLocationIds,
                                                                                                    refreshRequest.OnlyBPCs)

                    If Not corporationNode.Text.Contains("No Assets Loaded") Then
                        loadedCorpIds.Add(corporation.CorporationID)
                        RemoveNoAssetsNode(assetNodes, corporation.CorporationID)
                    End If

                    assetNodes.Add(CType(corporationNode.Clone, TreeNode))
                End If
            Next
        End If

        Return assetNodes
    End Function

    Public Shared Sub SaveCheckedLocations(ByVal windowForm As AssetWindow, ByVal rootNode As TreeNode)
        Dim savedLocationIds As List(Of LocationInfo)
        Dim accountIds As New List(Of Long)
        Dim insertedKeys As New HashSet(Of String)(StringComparer.Ordinal)

        If IsNothing(rootNode) Then
            Exit Sub
        End If

        savedLocationIds = CollectCheckedLocations(rootNode, accountIds)

        If accountIds.Count = 0 Then
            Exit Sub
        End If

        EVEDB.BeginSQLiteTransaction()

        Try
            Dim deleteSql As String = "DELETE FROM ASSET_LOCATIONS WHERE EnumAssetType = " & CStr(windowForm) & " AND ID IN (" & String.Join(",", accountIds) & ")"
            EVEDB.ExecuteNonQuerySQL(deleteSql)

            For Each location In savedLocationIds
                Dim locationKey As String = CStr(location.AccountID) & "|" & CStr(location.LocationID) & "|" & CStr(location.FlagID)

                If insertedKeys.Add(locationKey) Then
                    Dim insertSql As String = "INSERT INTO ASSET_LOCATIONS (EnumAssetType, ID, LocationID, FlagID) VALUES "
                    insertSql &= "(" & CStr(windowForm) & "," & CStr(location.AccountID) & "," & CStr(location.LocationID) & "," & CStr(location.FlagID) & ")"
                    EVEDB.ExecuteNonQuerySQL(insertSql)
                End If
            Next

            EVEDB.CommitSQLiteTransaction()
        Catch
            EVEDB.RollbackSQLiteTransaction()
            Throw
        End Try
    End Sub

    Public Shared Function LoadPriceShipTypes() As List(Of String)
        Return LoadPriceGroupNames("Ship", "groupName NOT IN ('Rookie ship','Prototype Exploration Ship')", "All Ship Types")
    End Function

    Public Shared Function LoadPriceChargeTypes() As List(Of String)
        Return LoadPriceGroupNames("Charge", "", "All Charge Types")
    End Function

    Private Shared Function ParseSelectedCharacterIds(ByVal selectedCharacterIds As String) As HashSet(Of String)
        Dim parsedIds As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        If selectedCharacterIds Is Nothing Then
            Return parsedIds
        End If

        For Each characterId In selectedCharacterIds.Split(","c)
            Dim trimmedId As String = characterId.Trim()
            If trimmedId <> "" Then
                parsedIds.Add(trimmedId)
            End If
        Next

        Return parsedIds
    End Function

    Private Shared Function ExecuteItemIdQuery(ByVal sql As String) As List(Of Long)
        Dim itemIds As New List(Of Long)

        DBCommand = New SQLiteCommand(sql, EVEDB.DBREf)

        Using reader As SQLiteDataReader = DBCommand.ExecuteReader()
            While reader.Read()
                itemIds.Add(reader.GetInt64(0))
            End While
        End Using

        Return itemIds
    End Function

    Private Shared Function BuildItemPriceGroupListSql(ByVal viewModel As AssetsViewerViewModel,
                                                       ByVal shipType As String,
                                                       ByVal chargeType As String,
                                                       ByVal techChecksEnabled As Boolean()) As String
        Dim sql As String = ""
        Dim techSql As String = ""
        Dim itemChecked As Boolean = False
        Dim techChecked As Boolean = False

        If viewModel.AdvancedProtectiveTechnology Then
            sql &= "ITEM_GROUP = 'Advanced Protective Technology' OR "
            itemChecked = True
        End If
        If viewModel.FactionMaterials Then
            sql &= "(ITEM_GROUP IN ('Materials and Compounds','Artifacts and Prototypes','Rogue Drone Components') OR ITEM_GROUP LIKE 'Decryptors -%') OR "
            itemChecked = True
        End If
        If viewModel.Gas Then
            sql &= "ITEM_GROUP IN ('Harvestable Cloud','Compressed Gas') OR "
            itemChecked = True
        End If
        If viewModel.IceProducts Then
            sql &= "ITEM_GROUP = 'Ice Product' OR "
            itemChecked = True
        End If
        If viewModel.Minerals Then
            sql &= "ITEM_GROUP = 'Mineral' OR "
            itemChecked = True
        End If
        If viewModel.MolecularForgingTools Then
            sql &= "ITEM_GROUP = 'Molecular-Forging Tools' OR "
            itemChecked = True
        End If
        If viewModel.NamedComponents Then
            sql &= "ITEM_GROUP = 'Named Components' OR "
            itemChecked = True
        End If
        If viewModel.Planetary Then
            sql &= "ITEM_CATEGORY LIKE 'Planetary%' OR "
            itemChecked = True
        End If
        If viewModel.RawMaterials Then
            sql &= "(ITEM_CATEGORY = 'Asteroid' OR ITEM_GROUP = 'Abyssal Materials') OR "
            itemChecked = True
        End If
        If viewModel.AdvancedMoonMats Then
            sql &= "ITEM_GROUP = 'Composite' OR "
            itemChecked = True
        End If
        If viewModel.BoosterMats Then
            sql &= "ITEM_GROUP = 'Biochemical Material' OR "
            itemChecked = True
        End If
        If viewModel.MolecularForgedMats Then
            sql &= "ITEM_GROUP = 'Molecular-Forged Materials' OR "
            itemChecked = True
        End If
        If viewModel.Polymers Then
            sql &= "ITEM_GROUP = 'Hybrid Polymers' OR "
            itemChecked = True
        End If
        If viewModel.ProcessedMoonMats Then
            sql &= "ITEM_GROUP = 'Intermediate Materials' OR "
            itemChecked = True
        End If
        If viewModel.RawMoonMats Then
            sql &= "ITEM_GROUP = 'Moon Materials' OR "
            itemChecked = True
        End If
        If viewModel.Salvage Then
            sql &= "ITEM_GROUP IN ('Salvaged Materials','Ancient Salvage') OR "
            itemChecked = True
        End If
        If viewModel.AncientRelics Then
            sql &= "ITEM_CATEGORY = 'Ancient Relics' OR "
            itemChecked = True
        End If
        If viewModel.Datacores Then
            sql &= "ITEM_GROUP = 'Datacores' OR "
            itemChecked = True
        End If
        If viewModel.Decryptors Then
            sql &= "ITEM_CATEGORY = 'Decryptors' OR "
            itemChecked = True
        End If
        If viewModel.RDB Then
            sql &= "ITEM_NAME LIKE 'R.Db%' OR "
            itemChecked = True
        End If
        If viewModel.BPCs Then
            sql &= "ITEM_CATEGORY = 'Blueprint' OR "
            itemChecked = True
        End If
        If viewModel.Misc Then
            sql &= "ITEM_GROUP IN ('General','Livestock','Radioactive','Biohazard','Commodities','Empire Insignia Drops','Criminal Tags','Miscellaneous','Unknown Components','Lease') OR "
            itemChecked = True
        End If
        If viewModel.AdvancedCapComponents Then
            sql &= "ITEM_GROUP = 'Advanced Capital Construction Components' OR "
            itemChecked = True
        End If
        If viewModel.AdvancedComponents Then
            sql &= "ITEM_GROUP = 'Construction Components' OR "
            itemChecked = True
        End If
        If viewModel.FuelBlocks Then
            sql &= "ITEM_GROUP = 'Fuel Block' OR "
            itemChecked = True
        End If
        If viewModel.ProtectiveComponents Then
            sql &= "ITEM_GROUP = 'Protective Components' OR "
            itemChecked = True
        End If
        If viewModel.RAM Then
            sql &= "ITEM_NAME LIKE 'R.A.M.%' OR "
            itemChecked = True
        End If
        If viewModel.NoBuildItems Then
            sql &= "MANUFACTURE = -1 OR "
            itemChecked = True
        End If
        If viewModel.CapitalShipComponents Then
            sql &= "ITEM_GROUP = 'Capital Construction Components' OR "
            itemChecked = True
        End If
        If viewModel.StructureComponents Then
            sql &= "ITEM_GROUP = 'Structure Components' OR "
            itemChecked = True
        End If
        If viewModel.SubsystemComponents Then
            sql &= "ITEM_GROUP = 'Hybrid Tech Components' OR "
            itemChecked = True
        End If
        If viewModel.Boosters Then
            sql &= "ITEM_GROUP = 'Booster' OR "
            itemChecked = True
        End If
        If viewModel.Implants Then
            sql &= "(ITEM_GROUP = 'Cyberimplant' OR (ITEM_CATEGORY = 'Implant' AND ITEM_GROUP <> 'Booster')) OR "
            itemChecked = True
        End If
        If viewModel.Deployables Then
            sql &= "ITEM_CATEGORY = 'Deployable' OR "
            itemChecked = True
        End If
        If viewModel.StructureModules Then
            sql &= "(ITEM_CATEGORY = 'Structure Module' AND ITEM_GROUP NOT LIKE '%Rig%') OR "
            itemChecked = True
        End If
        If viewModel.Celestials Then
            sql &= "(ITEM_CATEGORY IN ('Celestial','Orbitals','Sovereignty Structures','Station','Accessories','Infrastructure Upgrades')  AND ITEM_GROUP NOT IN ('Harvestable Cloud','Compressed Gas')) OR "
            itemChecked = True
        End If

        If viewModel.Ships Or viewModel.Modules Or viewModel.Drones Or viewModel.Rigs Or viewModel.Subsystems Or viewModel.Structures Or viewModel.Charges Or viewModel.StructureRigs Then
            If techChecksEnabled.Length > 0 AndAlso techChecksEnabled(0) AndAlso viewModel.T1 Then
                techSql &= "ITEM_TYPE = 1 OR "
                techChecked = True
            End If
            If techChecksEnabled.Length > 1 AndAlso techChecksEnabled(1) AndAlso viewModel.T2 Then
                techSql &= "ITEM_TYPE = 2 OR "
                techChecked = True
            End If
            If techChecksEnabled.Length > 2 AndAlso techChecksEnabled(2) AndAlso viewModel.T3 Then
                techSql &= "ITEM_TYPE = 14 OR "
                techChecked = True
            End If
            If techChecksEnabled.Length > 3 AndAlso techChecksEnabled(3) AndAlso viewModel.Storyline Then
                techSql &= "ITEM_TYPE = 3 OR "
                techChecked = True
            End If
            If techChecksEnabled.Length > 4 AndAlso techChecksEnabled(4) AndAlso viewModel.Faction Then
                techSql &= "ITEM_TYPE = 16 OR "
                techChecked = True
            End If
            If techChecksEnabled.Length > 5 AndAlso techChecksEnabled(5) AndAlso viewModel.Pirate Then
                techSql &= "ITEM_TYPE = 15 OR "
                techChecked = True
            End If

            If Not techChecked And Not itemChecked Then
                Return ""
            End If

            If techSql <> "" Then
                techSql = "(" & techSql.Substring(0, techSql.Length - 3) & "OR ITEM_TYPE IN (21,22,23,24)) "
            End If

            If viewModel.Charges Then
                sql &= "(ITEM_CATEGORY = 'Charge' AND " & techSql
                If chargeType <> "All Charge Types" Then
                    sql &= " AND ITEM_GROUP = '" & FormatDBString(chargeType) & "'"
                End If
                sql &= ") OR "
            End If
            If viewModel.Drones Then
                sql &= "(ITEM_CATEGORY IN ('Drone', 'Fighter') AND " & techSql & ") OR "
            End If
            If viewModel.Modules Then
                sql &= "(ITEM_CATEGORY = 'Module' AND ITEM_GROUP NOT LIKE 'Rig%' AND " & techSql & ") OR "
            End If
            If viewModel.Ships Then
                sql &= "(ITEM_CATEGORY = 'Ship' AND " & techSql
                If shipType <> "All Ship Types" Then
                    sql &= " AND ITEM_GROUP = '" & FormatDBString(shipType) & "'"
                End If
                sql &= ") OR "
            End If
            If viewModel.Subsystems Then
                sql &= "(ITEM_CATEGORY = 'Subsystem' AND " & techSql & ") OR "
            End If
            If viewModel.StructureRigs Then
                sql &= "(ITEM_CATEGORY = 'Structure Rigs' AND " & techSql & ") OR "
            End If
            If viewModel.Rigs Then
                sql &= "((ITEM_CATEGORY = 'Module' AND ITEM_GROUP LIKE 'Rig%' AND " & techSql & ") OR (ITEM_CATEGORY = 'Structure Module' AND ITEM_GROUP LIKE '%Rig%')) OR "
            End If
            If viewModel.Structures Then
                sql &= "((ITEM_CATEGORY IN ('Starbase','Structure') AND " & techSql & ") OR ITEM_GROUP = 'Station Components') OR "
            End If
        End If

        If sql = "" Then
            Return ""
        End If

        Return sql.Substring(0, sql.Length - 4)
    End Function

    Private Shared Function LoadAssetCharacters(ByVal assetEntries As IEnumerable(Of AssetsViewerAssetEntryViewModel),
                                                ByVal selectedCharacter As Character,
                                                ByVal windowForm As AssetWindow,
                                                ByRef savedLocationIds As List(Of LocationInfo),
                                                ByVal setStatus As Action(Of String)) As List(Of LoadedAssetCharacter)
        Dim loadedCharacters As New List(Of LoadedAssetCharacter)

        For Each entry In assetEntries
            Dim currentCharacter As Character

            SetStatusText(setStatus, "Refreshing Assets for " & entry.CharacterName)

            If entry.CharacterId <> selectedCharacter.ID Then
                Dim tokenData As New SavedTokenData
                tokenData.CharacterID = entry.CharacterId
                currentCharacter = New Character
                currentCharacter.LoadCharacterData(tokenData, False, True, False)
            Else
                currentCharacter = selectedCharacter
            End If

            savedLocationIds.AddRange(currentCharacter.Assets.GetAssetLocationIDs(windowForm, currentCharacter.ID, currentCharacter.CharacterCorporation))

            Dim loadedCharacter As New LoadedAssetCharacter
            loadedCharacter.Character = currentCharacter
            loadedCharacter.DisplayName = entry.CharacterName
            loadedCharacters.Add(loadedCharacter)
        Next

        Return loadedCharacters
    End Function

    Private Shared Sub RemoveNoAssetsNode(ByVal assetNodes As List(Of TreeNode), ByVal corporationId As Long)
        For i = assetNodes.Count - 1 To 0 Step -1
            If assetNodes(i).Text.Contains("No Assets Loaded") AndAlso assetNodes(i).Name = CStr(corporationId) Then
                assetNodes.RemoveAt(i)
                Exit For
            End If
        Next
    End Sub

    Private Shared Sub SetStatusText(ByVal setStatus As Action(Of String), ByVal statusText As String)
        If Not IsNothing(setStatus) Then
            setStatus(statusText)
        End If
    End Sub

    Private Shared Function CollectCheckedLocations(ByVal rootNode As TreeNode, ByRef accountIds As List(Of Long)) As List(Of LocationInfo)
        Dim locationIds As New List(Of LocationInfo)

        accountIds = New List(Of Long)

        For Each accountNode As TreeNode In rootNode.Nodes
            accountIds.Add(CLng(accountNode.Name))
            locationIds.AddRange(GetCheckedLocations(accountNode, CLng(accountNode.Name)))
        Next

        Return locationIds
    End Function

    Private Shared Function GetCheckedLocations(ByVal sentNode As TreeNode, ByVal accountId As Long) As List(Of LocationInfo)
        Dim locationIdList As New List(Of LocationInfo)

        For Each subNode As TreeNode In sentNode.Nodes
            If Not IsNothing(subNode.Tag) Then
                Dim flagValue As Integer = CType(subNode.Tag, EVEAssets.TagInfo).FlagValue

                If subNode.Checked Then
                    Dim locationInfo As New LocationInfo
                    locationInfo.FlagID = flagValue
                    locationInfo.LocationID = CLng(subNode.Name)
                    locationInfo.AccountID = accountId
                    locationIdList.Add(locationInfo)
                End If

                If subNode.Nodes.Count > 0 Then
                    locationIdList.AddRange(GetCheckedLocations(subNode, accountId))
                End If
            End If
        Next

        Return locationIdList
    End Function

    Private Shared Function LoadPriceGroupNames(ByVal categoryName As String, ByVal extraWhereClause As String, ByVal allItemLabel As String) As List(Of String)
        Dim sql As String = "SELECT groupName from inventory_types, inventory_groups, inventory_categories "
        Dim groupNames As New List(Of String)

        sql &= "WHERE inventory_types.groupID = inventory_groups.groupID "
        sql &= "AND inventory_groups.categoryID = inventory_categories.categoryID "
        sql &= "AND categoryname = '" & FormatDBString(categoryName) & "' "

        If extraWhereClause <> "" Then
            sql &= "AND " & extraWhereClause & " "
        End If

        sql &= "AND inventory_types.published <> 0 and inventory_groups.published <> 0 and inventory_categories.published <> 0 "
        sql &= "GROUP BY groupName "

        DBCommand = New SQLiteCommand(sql, EVEDB.DBREf)

        groupNames.Add(allItemLabel)

        Using reader As SQLiteDataReader = DBCommand.ExecuteReader()
            While reader.Read()
                groupNames.Add(reader.GetString(0))
            End While
        End Using

        Return groupNames
    End Function

End Class
