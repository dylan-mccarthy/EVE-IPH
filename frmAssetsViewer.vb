
Imports System.Runtime.InteropServices

Public Class frmAssetsViewer

    Private ToggleAllOpen As Boolean
    Private UpdatingChecks As Boolean
    Private RefreshAssetButton As Boolean

    Private SelectedSettings As AssetWindowSettings

    Private m_ControlsCollection As ControlsCollection
    Private TechCheckBoxes(6) As CheckBox
    Private UpdateAllTechChecks As Boolean = True ' Whether to update all Tech checks in items or not
    Private FirstPriceShipTypesComboLoad As Boolean
    Private FirstPriceChargeTypesComboLoad As Boolean

    ' For checks that are enabled
    Private PriceCheckT1Enabled As Boolean
    Private PriceCheckT2Enabled As Boolean
    Private PriceCheckT3Enabled As Boolean
    Private PriceCheckT4Enabled As Boolean
    Private PriceCheckT5Enabled As Boolean
    Private PriceCheckT6Enabled As Boolean

    ' Where the form was loaded from
    Private WindowForm As AssetWindow

    ' For drawing checkboxes
    Private Const TVIF_STATE As Integer = &H8
    Private Const TVIS_STATEIMAGEMASK As Integer = &HF000
    Private Const TV_FIRST As Integer = &H1100
    Private Const TVM_SETITEM As Integer = TV_FIRST + 63

#Region "Special processing for checks"
    <StructLayout(LayoutKind.Sequential)>
    Public Structure TVITEM
        Public mask As Integer
        Public hItem As IntPtr
        Public state As Integer
        Public stateMask As Integer
        <MarshalAs(UnmanagedType.LPTStr)>
        Public lpszText As String
        Public cchTextMax As Integer
        Public iImage As Integer
        Public iSelectedImage As Integer
        Public cChildren As Integer
        Public lParam As IntPtr
    End Structure

    Private Declare Auto Function SendMessage Lib "User32.dll" (ByVal hwnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByRef lParam As TVITEM) As Integer

    Private Sub HideRootCheckBox(ByVal node As TreeNode)
        Dim tvi As New TVITEM
        tvi.hItem = node.Handle
        tvi.mask = TVIF_STATE
        tvi.stateMask = TVIS_STATEIMAGEMASK
        tvi.state = 0
        SendMessage(AssetTree.Handle, TVM_SETITEM, IntPtr.Zero, tvi)
    End Sub

    Private Sub AssetTree_DrawNode(ByVal sender As Object, ByVal e As DrawTreeNodeEventArgs) Handles AssetTree.DrawNode
        ' Don't show the top node with a check box
        If e.Node.Parent Is Nothing Then
            HideRootCheckBox(e.Node)
        End If

        ' For high, mid, rigs, and low slots on ships, don't show check boxes
        If e.Node.Text.Contains("power slot") Or e.Node.Text.Contains("Personal Assets") Or e.Node.Text.Contains("Corporation Assets") Then
            HideRootCheckBox(e.Node)
        End If

        ' Don't show a checkbox on any nodes without children
        If e.Node.Nodes.Count = 0 Then
            HideRootCheckBox(e.Node)
        End If

        e.DrawDefault = True

    End Sub

    Public ReadOnly Property MyControls() As Collection
        Get
            Return m_ControlsCollection.Controls
        End Get
    End Property

#End Region

    Public Sub New(ByVal AssetType As AssetWindow)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        WindowForm = AssetType

        ' Mark where the window is attached - can have multiple open
        Select Case AssetType
            Case AssetWindow.DefaultView
                Me.Text = "Default Asset Viewer"
                SelectedSettings = UserAssetWindowDefaultSettings
            Case AssetWindow.ManufacturingTab
                Me.Text = "Manufacturing Asset Viewer"
                SelectedSettings = UserAssetWindowManufacturingTabSettings
            Case AssetWindow.ShoppingList
                Me.Text = "Shopping List Assets"
                SelectedSettings = UserAssetWindowShoppingListSettings
            Case AssetWindow.ReprocessingPlant
                Me.Text = "Refinery Assets"
                SelectedSettings = UserAssetWindowRefinerySettings
        End Select

        ' Width 253, 21 for scrollbar, 25 for check (207)
        lstCharacters.Columns.Add("", -2, HorizontalAlignment.Left)
        lstCharacters.Columns.Add("Character Name", 90, HorizontalAlignment.Left)
        lstCharacters.Columns.Add("Character Corporation", 117, HorizontalAlignment.Left)
        lstCharacters.Columns.Add("CharID", 0, HorizontalAlignment.Left) ' Hidden
        lstCharacters.Columns.Add("CorpID", 0, HorizontalAlignment.Left) ' Hidden

        ' For the disabling of the price update form
        PriceCheckT1Enabled = True
        PriceCheckT2Enabled = True
        PriceCheckT3Enabled = True
        PriceCheckT4Enabled = True
        PriceCheckT5Enabled = True
        PriceCheckT6Enabled = True

        AssetTree.DrawMode = TreeViewDrawMode.OwnerDrawAll
        AssetTree.CheckBoxes = True

    End Sub

    Private Sub frmAssetsViewer_Activated(sender As Object, e As System.EventArgs) Handles Me.Activated
        ' If we have no assets, then refresh the table to show that
        If SelectedCharacter.GetAssets.GetAssetCount = 0 Then
            Call RefreshTree()
        End If
    End Sub

    ' Initialize the form based on user settings
    Private Sub frmAssetsViewer_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
        Dim viewModel As AssetsViewerViewModel

        Application.DoEvents()

        FirstLoad = True

        Timer1.Interval = 1000 ' 1 second
        Timer1.Enabled = True

        FirstPriceChargeTypesComboLoad = True
        FirstPriceShipTypesComboLoad = True

        btnScanCorpAssets.Enabled = False
        btnScanPersonalAssets.Enabled = False

        ' Get Region check boxes (note index starts at 1)
        TechCheckBoxes(1) = chkItemsT1
        TechCheckBoxes(2) = chkItemsT2
        TechCheckBoxes(3) = chkItemsT3
        TechCheckBoxes(4) = chkItemsT4
        TechCheckBoxes(5) = chkItemsT5
        TechCheckBoxes(6) = chkItemsT6

        pnlStatus.Text = ""
        pnlProgressBar.Visible = False

        lblReloadCorpAssets.Text = "---"
        lblReloadPersonalAssets.Text = "---"

        viewModel = AssetsViewerService.BuildViewModel(SelectedSettings)
        ApplyAssetsViewerViewModel(viewModel)

        ' Regardless, load up all account info
        Call LoadAccountList()

        FirstLoad = False

        ' Everything will be just normal at first - add to settings for the format they save? Also, check the locations they have checked only TODO-AV!
        ToggleAllOpen = False

        Call RefreshTree()

        Application.DoEvents()

    End Sub

    ' Refreshes the account list with character account info
    Private Sub LoadAccountList()
        Dim accounts As List(Of AssetsViewerAccountViewModel)

        accounts = AssetsViewerService.LoadAccounts(SelectedSettings, rbtnMultiAccounts.Checked)

        lstCharacters.BeginUpdate()
        lstCharacters.Items.Clear()

        For Each account In accounts
            Dim lstCharacterRow As New ListViewItem("")
            lstCharacterRow.SubItems.Add(account.CharacterName)
            lstCharacterRow.SubItems.Add(account.CorporationName)
            lstCharacterRow.SubItems.Add(CStr(account.CharacterId))
            lstCharacterRow.SubItems.Add(CStr(account.CorporationId))
            lstCharacterRow.Checked = account.IsChecked
            lstCharacters.Items.Add(lstCharacterRow)
        Next

        lstCharacters.EndUpdate()
    End Sub

    ' Returns true if an selected item is checked
    Private Function ItemsChecked() As Boolean

        If chkAdvancedProtectiveTechnology.Checked Then Return True
        If chkGas.Checked Then Return True
        If chkIceProducts.Checked Then Return True
        If chkMolecularForgingTools.Checked Then Return True
        If chkFactionMaterials.Checked Then Return True
        If chkNamedComponents.Checked Then Return True
        If chkMinerals.Checked Then Return True
        If chkPlanetary.Checked Then Return True
        If chkRawMaterials.Checked Then Return True
        If chkSalvage.Checked Then Return True
        If chkMisc.Checked Then Return True
        If chkBPCs.Checked Then Return True

        If chkAdvancedMats.Checked Then Return True
        If chkBoosterMats.Checked Then Return True
        If chkMolecularForgedMaterials.Checked Then Return True
        If chkPolymers.Checked Then Return True
        If chkProcessedMats.Checked Then Return True
        If chkRawMoonMats.Checked Then Return True

        If chkAncientRelics.Checked Then Return True
        If chkDatacores.Checked Then Return True
        If chkDecryptors.Checked Then Return True
        If chkRDb.Checked Then Return True

        If chkShips.Checked Then Return True
        If chkCharges.Checked Then Return True
        If chkModules.Checked Then Return True
        If chkDrones.Checked Then Return True
        If chkRigs.Checked Then Return True
        If chkSubsystems.Checked Then Return True
        If chkDeployables.Checked Then Return True
        If chkBoosters.Checked Then Return True
        If chkStructures.Checked Then Return True
        If chkStructureRigs.Checked Then Return True
        If chkCelestials.Checked Then Return True
        If chkStructureModules.Checked Then Return True
        If chkImplants.Checked Then Return True

        If chkCapT2Components.Checked Then Return True
        If chkAdvancedComponents.Checked Then Return True
        If chkFuelBlocks.Checked Then Return True
        If chkProtectiveComponents.Checked Then Return True
        If chkRAM.Checked Then Return True
        If chkCapitalShipComponents.Checked Then Return True
        If chkStructureComponents.Checked Then Return True
        If chkSubsystemComponents.Checked Then Return True

        ' If we got here, nothing checked
        Return False

    End Function

    ' Main function that refresh's the tree
    Public Sub RefreshTree()
        Dim AnchorNode As New TreeNode
        Dim viewModel As AssetsViewerViewModel
        Dim refreshRequest As AssetsViewerRefreshRequestViewModel

        pnlStatus.Text = ""
        ActiveForm.Cursor = Cursors.WaitCursor
        Application.DoEvents()

        ' Make sure they have an item selected
        If Not ItemsChecked() And Not rbtnAllItems.Checked Then
            MsgBox("You must select an item category to display.", vbExclamation, Application.ProductName)
            tabMain.SelectedTab = tabSearchSettings
            ActiveForm.Cursor = Cursors.Default
            Exit Sub
        End If

        ' Set the tree object
        AssetTree.BeginUpdate()
        AssetTree.Nodes.Clear()

        viewModel = ReadAssetsViewerViewModel()
        refreshRequest = AssetsViewerService.BuildRefreshRequest(viewModel, SelectedCharacter, GetSelectedCharacterIds(), cmbPriceShipTypes.Text, cmbPriceChargeTypes.Text, GetTechCheckEnabledStates())

        ' If we get nothing back from the search item list, then just clear the assets and exit - we have no items to display
        If IsNothing(refreshRequest.SearchItemList) Then
            AssetTree.EndUpdate()
            AssetTree.Refresh()

            ActiveForm.Cursor = Cursors.Default
            Application.DoEvents()
            MsgBox("No items found.", vbInformation, Application.ProductName)
            Me.Refresh()
            Exit Sub
        End If

        If Not rbtnSelectedAccount.Checked And refreshRequest.AssetEntries.Count = 0 Then
            MsgBox("You must select a character for assets", vbExclamation, Application.ProductName)
            ActiveForm.Cursor = Cursors.Default
            Application.DoEvents()
            Exit Sub
        End If

        ' Add the base node
        AnchorNode = AssetTree.Nodes.Add("Asset List")
        For Each assetNode In AssetsViewerService.BuildAssetTreeNodes(refreshRequest,
                                                                      SelectedCharacter,
                                                                      WindowForm,
                                                                      rbtnPersonalAssets.Checked Or rbtnAllAssets.Checked,
                                                                      rbtnCorpAssets.Checked Or rbtnAllAssets.Checked,
                                                                      AddressOf UpdateStatus)
            AnchorNode.Nodes.Add(assetNode)
        Next

        ' Update
        pnlStatus.Text = ""
        Application.DoEvents()
        AssetTree.EndUpdate()
        AssetTree.Refresh()

        ' Open up the top node and the personal/corp nodes. Plus reset toggle since we just reloaded
        ToggleAllOpen = False
        AssetTree.TopNode.Expand()

        ' expand all nodes for each character/corp loaded
        For i = 0 To AssetTree.Nodes(0).Nodes.Count - 1
            AssetTree.Nodes(0).Nodes(i).Expand()
        Next

        ' Expand all parents for check boxes that have values checked
        Call ExpandCheckedNodes(AssetTree.Nodes(0).Nodes, AssetTree)

        ' Scroll to top
        AssetTree.TopNode = AssetTree.Nodes(0)

        On Error Resume Next
        ActiveForm.Cursor = Cursors.Default
        Application.DoEvents()
        Me.Refresh()

    End Sub

    Private Sub UpdateStatus(ByVal statusText As String)
        pnlStatus.Text = statusText
        Application.DoEvents()
    End Sub

    Private Sub PopulatePriceTypeCombo(ByVal comboBox As ComboBox, ByVal values As IEnumerable(Of String), ByVal defaultValue As String)
        comboBox.Items.Clear()

        For Each value In values
            comboBox.Items.Add(value)
        Next

        comboBox.Text = defaultValue
    End Sub

    Private Sub ExpandCheckedNodes(NodeSet As TreeNodeCollection, ByRef BaseTree As TreeView)
        Dim node As New TreeNode

        For Each node In NodeSet
            FindCheckedNode(node, BaseTree)
        Next

    End Sub

    Private Sub FindCheckedNode(SentNode As TreeNode, ByRef BaseTree As TreeView)
        Dim tn As TreeNode

        For Each tn In SentNode.Nodes
            If tn.Checked = True Then
                BaseTree.SelectedNode = tn
                BaseTree.SelectedNode.Parent.Expand()
            End If
            FindCheckedNode(tn, BaseTree)
        Next
    End Sub

    ' Just loads the assets from API then DB
    Private Sub ScanForAssets(ByVal BPScanType As ScanType)

        ' New scan, so run update and reload assets
        Call CharacterDataService.RefreshAssets(SelectedCharacter, BPScanType, True)

        ' Reload the tree
        Call RefreshTree()

    End Sub

    ' Will use CAK and scan for bps in the user's items and store a temp table of these bps for loading in the grid
    Private Sub btnScanPersonalBPs_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnScanPersonalAssets.Click

        RefreshAssetButton = True
        ActiveForm.Cursor = Cursors.WaitCursor
        If rbtnAllAssets.Checked = False Then
            rbtnPersonalAssets.Checked = True
        End If

        Application.DoEvents()

        If SelectedCharacter.AssetsAccess Then
            Call ScanForAssets(ScanType.Personal)
        Else
            MsgBox("You have not enabled access to Assets with this key.", vbExclamation, Application.ProductName)
        End If

        ActiveForm.Cursor = Cursors.Default
        Application.DoEvents()

        RefreshAssetButton = False

    End Sub

    ' Will use CAK and scan for bps in the corps items and store a temp table of these bps for loading in the grid
    Private Sub btnScanCorpBPs_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnScanCorpAssets.Click

        RefreshAssetButton = True
        ActiveForm.Cursor = Cursors.WaitCursor
        If rbtnAllAssets.Checked = False Then
            rbtnCorpAssets.Checked = True
        End If

        Application.DoEvents()

        If SelectedCharacter.CharacterCorporation.AssetAccess Then
            Call ScanForAssets(ScanType.Corporation)
        Else
            MsgBox("You do not have a corporation key installed with access to Assets.", vbExclamation, Application.ProductName)
        End If

        ActiveForm.Cursor = Cursors.Default
        Application.DoEvents()

        RefreshAssetButton = False

    End Sub

    ' Displays the time remaining on refreshing assets
    Private Sub Timer1_Tick(sender As System.Object, e As System.EventArgs) Handles Timer1.Tick
        Dim TempTime As Long

        ' On each tick just update the labels
        If SelectedCharacter.AssetsAccess Then
            TempTime = DateDiff(DateInterval.Second, Date.UtcNow, SelectedCharacter.GetAssets.CachedUntil)
            If TempTime <= 0 Then
                lblReloadPersonalAssets.Text = "Now"
            Else
                lblReloadPersonalAssets.Text = FormatTimeToComplete(TempTime)
            End If
        Else
            lblReloadPersonalAssets.Text = "No Access"
        End If

        If lblReloadPersonalAssets.Text = "Now" Then
            ' Enable refresh button
            btnScanPersonalAssets.Enabled = True
        Else
            btnScanPersonalAssets.Enabled = False
        End If

        If SelectedCharacter.CharacterCorporation.AssetAccess Then
            TempTime = DateDiff(DateInterval.Second, Date.UtcNow, SelectedCharacter.CharacterCorporation.GetAssets.CachedUntil)
            If TempTime <= 0 Then
                lblReloadCorpAssets.Text = "Now"
            Else
                lblReloadCorpAssets.Text = FormatTimeToComplete(TempTime)
            End If
        Else
            lblReloadCorpAssets.Text = "No Access"
        End If

        If lblReloadCorpAssets.Text = "Now" Then
            ' Enable refresh button
            btnScanCorpAssets.Enabled = True
        Else
            btnScanCorpAssets.Enabled = False
        End If

    End Sub

    ' Saves the settings on the Selected Items Form for later loading
    Private Sub btnSaveSettings_Click(sender As System.Object, e As System.EventArgs) Handles btnSaveSettings.Click

        Call SaveWindowSettings()

    End Sub

    ' Saves the main settings for the general form
    Private Sub btnSaveMainSettings_Click(sender As System.Object, e As System.EventArgs) Handles btnSaveMainSettings.Click

        Call SaveWindowSettings()

    End Sub

    Private Function GetCharIDs() As String
        Dim CharIDs As String = ""

        If rbtnSelectedAccount.Checked Then
            CharIDs = CStr(SelectedCharacter.ID)
        Else
            For i = 0 To lstCharacters.CheckedItems.Count - 1
                CharIDs = CharIDs & CStr(lstCharacters.CheckedItems(i).SubItems(3).Text) & ","
            Next

            ' Strip the last comma
            If CharIDs <> "" Then
                CharIDs = CharIDs.Substring(0, Len(CharIDs) - 1)
            End If
        End If

        Return CharIDs

    End Function

    Private Function GetSelectedCharacterIds() As List(Of Long)
        Dim selectedCharacterIds As New List(Of Long)

        If rbtnSelectedAccount.Checked Then
            selectedCharacterIds.Add(SelectedCharacter.ID)
        Else
            For i = 0 To lstCharacters.CheckedItems.Count - 1
                selectedCharacterIds.Add(CLng(lstCharacters.CheckedItems(i).SubItems(3).Text))
            Next
        End If

        Return selectedCharacterIds
    End Function

    Private Function GetTechCheckEnabledStates() As Boolean()
        Return New Boolean() {chkItemsT1.Enabled, chkItemsT2.Enabled, chkItemsT3.Enabled, chkItemsT4.Enabled, chkItemsT5.Enabled, chkItemsT6.Enabled}
    End Function

    Private Sub ApplyAssetsViewerViewModel(ByVal viewModel As AssetsViewerViewModel)
        Select Case viewModel.AssetType
            Case rbtnAllAssets.Text
                rbtnAllAssets.Checked = True
            Case rbtnPersonalAssets.Text
                rbtnPersonalAssets.Checked = True
            Case rbtnCorpAssets.Text
                rbtnCorpAssets.Checked = True
        End Select

        If viewModel.SortByName Then
            rbtnSortName.Checked = True
        Else
            rbtnSortQuantity.Checked = True
        End If

        If viewModel.AllItems Then
            rbtnAllItems.Checked = True
        Else
            rbtnBPMats.Checked = True
        End If

        If viewModel.SelectedAccount Then
            rbtnSelectedAccount.Checked = True
        Else
            rbtnMultiAccounts.Checked = True
        End If

        txtItemFilter.Text = viewModel.ItemFilterText

        chkMaterialResearchEqPrices.Checked = viewModel.AllRawMats

        chkAdvancedProtectiveTechnology.Checked = viewModel.AdvancedProtectiveTechnology
        chkGas.Checked = viewModel.Gas
        chkIceProducts.Checked = viewModel.IceProducts
        chkMolecularForgingTools.Checked = viewModel.MolecularForgingTools
        chkFactionMaterials.Checked = viewModel.FactionMaterials
        chkNamedComponents.Checked = viewModel.NamedComponents
        chkMinerals.Checked = viewModel.Minerals
        chkPlanetary.Checked = viewModel.Planetary
        chkRawMaterials.Checked = viewModel.RawMaterials
        chkSalvage.Checked = viewModel.Salvage
        chkMisc.Checked = viewModel.Misc
        chkBPCs.Checked = viewModel.BPCs

        chkAdvancedMats.Checked = viewModel.AdvancedMoonMats
        chkBoosterMats.Checked = viewModel.BoosterMats
        chkMolecularForgedMaterials.Checked = viewModel.MolecularForgedMats
        chkPolymers.Checked = viewModel.Polymers
        chkProcessedMats.Checked = viewModel.ProcessedMoonMats
        chkRawMoonMats.Checked = viewModel.RawMoonMats

        chkAncientRelics.Checked = viewModel.AncientRelics
        chkDatacores.Checked = viewModel.Datacores
        chkDecryptors.Checked = viewModel.Decryptors
        chkRDb.Checked = viewModel.RDB

        chkManufacturedItems.Checked = viewModel.AllManufacturedItems

        chkShips.Checked = viewModel.Ships
        chkCharges.Checked = viewModel.Charges
        chkModules.Checked = viewModel.Modules
        chkDrones.Checked = viewModel.Drones
        chkRigs.Checked = viewModel.Rigs
        chkSubsystems.Checked = viewModel.Subsystems
        chkDeployables.Checked = viewModel.Deployables
        chkBoosters.Checked = viewModel.Boosters
        chkStructures.Checked = viewModel.Structures
        chkStructureRigs.Checked = viewModel.StructureRigs
        chkCelestials.Checked = viewModel.Celestials
        chkStructureModules.Checked = viewModel.StructureModules
        chkImplants.Checked = viewModel.Implants

        chkCapT2Components.Checked = viewModel.AdvancedCapComponents
        chkAdvancedComponents.Checked = viewModel.AdvancedComponents
        chkFuelBlocks.Checked = viewModel.FuelBlocks
        chkProtectiveComponents.Checked = viewModel.ProtectiveComponents
        chkRAM.Checked = viewModel.RAM
        chkNobuild.Checked = viewModel.NoBuildItems
        chkCapitalShipComponents.Checked = viewModel.CapitalShipComponents
        chkStructureComponents.Checked = viewModel.StructureComponents
        chkSubsystemComponents.Checked = viewModel.SubsystemComponents

        chkItemsT1.Checked = viewModel.T1
        chkItemsT2.Checked = viewModel.T2
        chkItemsT3.Checked = viewModel.T3
        chkItemsT4.Checked = viewModel.Storyline
        chkItemsT5.Checked = viewModel.Faction
        chkItemsT6.Checked = viewModel.Pirate
    End Sub

    Private Function ReadAssetsViewerViewModel() As AssetsViewerViewModel
        Dim viewModel As New AssetsViewerViewModel

        With viewModel
            .SortByName = rbtnSortName.Checked

            If rbtnAllAssets.Checked Then
                .AssetType = rbtnAllAssets.Text
            ElseIf rbtnPersonalAssets.Checked Then
                .AssetType = rbtnPersonalAssets.Text
            ElseIf rbtnCorpAssets.Checked Then
                .AssetType = rbtnCorpAssets.Text
            End If

            .AllItems = rbtnAllItems.Checked
            .SelectedAccount = rbtnSelectedAccount.Checked
            .ItemFilterText = txtItemFilter.Text

            .AllRawMats = chkMaterialResearchEqPrices.Checked

            .AdvancedProtectiveTechnology = chkAdvancedProtectiveTechnology.Checked
            .Gas = chkGas.Checked
            .IceProducts = chkIceProducts.Checked
            .MolecularForgingTools = chkMolecularForgingTools.Checked
            .FactionMaterials = chkFactionMaterials.Checked
            .NamedComponents = chkNamedComponents.Checked
            .Minerals = chkMinerals.Checked
            .Planetary = chkPlanetary.Checked
            .RawMaterials = chkRawMaterials.Checked
            .Salvage = chkSalvage.Checked
            .Misc = chkMisc.Checked
            .BPCs = chkBPCs.Checked

            .AdvancedMoonMats = chkAdvancedMats.Checked
            .BoosterMats = chkBoosterMats.Checked
            .MolecularForgedMats = chkMolecularForgedMaterials.Checked
            .Polymers = chkPolymers.Checked
            .ProcessedMoonMats = chkProcessedMats.Checked
            .RawMoonMats = chkRawMoonMats.Checked

            .AncientRelics = chkAncientRelics.Checked
            .Datacores = chkDatacores.Checked
            .Decryptors = chkDecryptors.Checked
            .RDB = chkRDb.Checked

            .AllManufacturedItems = chkManufacturedItems.Checked

            .Ships = chkShips.Checked
            .Charges = chkCharges.Checked
            .Modules = chkModules.Checked
            .Drones = chkDrones.Checked
            .Rigs = chkRigs.Checked
            .Subsystems = chkSubsystems.Checked
            .Deployables = chkDeployables.Checked
            .Boosters = chkBoosters.Checked
            .Structures = chkStructures.Checked
            .StructureRigs = chkStructureRigs.Checked
            .Celestials = chkCelestials.Checked
            .StructureModules = chkStructureModules.Checked
            .Implants = chkImplants.Checked

            .AdvancedCapComponents = chkCapT2Components.Checked
            .AdvancedComponents = chkAdvancedComponents.Checked
            .FuelBlocks = chkFuelBlocks.Checked
            .ProtectiveComponents = chkProtectiveComponents.Checked
            .RAM = chkRAM.Checked
            .NoBuildItems = chkNobuild.Checked
            .CapitalShipComponents = chkCapitalShipComponents.Checked
            .StructureComponents = chkStructureComponents.Checked
            .SubsystemComponents = chkSubsystemComponents.Checked

            .T1 = chkItemsT1.Checked
            .T2 = chkItemsT2.Checked
            .T3 = chkItemsT3.Checked
            .Storyline = chkItemsT4.Checked
            .Faction = chkItemsT5.Checked
            .Pirate = chkItemsT6.Checked
        End With

        Return viewModel
    End Function

    ' Saves the settings on both tabs for the asset window
    Private Sub SaveWindowSettings()
        Dim TempSettings As AssetWindowSettings
        Dim TempViewModel As AssetsViewerViewModel

        TempViewModel = ReadAssetsViewerViewModel()
        TempSettings = AssetsViewerService.BuildSettings(TempViewModel, GetCharIDs())

        AssetsViewerService.SaveCheckedLocations(WindowForm, If(AssetTree.Nodes.Count > 0, AssetTree.Nodes(0), Nothing))

        ' Save the data in the XML file
        Call AllSettings.SaveAssetWindowSettings(TempSettings, WindowForm)

        ' Save the data to the local variable
        Select Case WindowForm
            Case AssetWindow.DefaultView
                UserAssetWindowDefaultSettings = TempSettings
            Case AssetWindow.ManufacturingTab
                UserAssetWindowManufacturingTabSettings = TempSettings
            Case AssetWindow.ShoppingList
                UserAssetWindowShoppingListSettings = TempSettings
            Case AssetWindow.ReprocessingPlant
                UserAssetWindowRefinerySettings = TempSettings
        End Select

        MsgBox("Asset Window Settings Saved", vbInformation, Application.ProductName)
        btnSaveSettings.Focus()

    End Sub

#Region "Click Events"

    Private Sub btnCloseAssets_Click(sender As System.Object, e As System.EventArgs) Handles btnCloseAssets.Click
        Me.Dispose()
        Me.Hide()
    End Sub

    Private Sub txtItemFilter_KeyDown(sender As Object, e As System.Windows.Forms.KeyEventArgs) Handles txtItemFilter.KeyDown
        Call ProcessCutCopyPasteSelect(txtItemFilter, e)
        If e.KeyCode = Keys.Enter Then
            Call RefreshTree()
        End If
    End Sub

    Private Sub btnSearchRefresh_Click(sender As System.Object, e As System.EventArgs) Handles btnSearchRefresh.Click
        Call RefreshTree()
    End Sub

    Private Sub btnMainRefresh_Click(sender As Object, e As EventArgs) Handles btnMainRefresh.Click
        Call RefreshTree()
    End Sub

    Private Sub rbtnCorpAssets_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnCorpAssets.CheckedChanged
        If rbtnCorpAssets.Checked And Not FirstLoad And Not RefreshAssetButton Then
            Call RefreshTree()
        End If
    End Sub

    Private Sub rbtnPersonalAssets_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnPersonalAssets.CheckedChanged
        If rbtnPersonalAssets.Checked And Not FirstLoad And Not RefreshAssetButton Then
            Call RefreshTree()
        End If
    End Sub

    Private Sub btnAllAssets_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnAllAssets.CheckedChanged
        If rbtnAllAssets.Checked And Not FirstLoad And Not RefreshAssetButton Then
            Call RefreshTree()
        End If
    End Sub

    Private Sub rbtnSortQuantity_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnSortQuantity.CheckedChanged
        If rbtnSortQuantity.Checked And Not FirstLoad Then
            Call RefreshTree()
        End If
    End Sub

    Private Sub rbtnSortName_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnSortName.CheckedChanged
        If rbtnSortName.Checked And Not FirstLoad Then
            Call RefreshTree()
        End If
    End Sub

    Private Sub btnResetItemFilter_Click(sender As System.Object, e As System.EventArgs) Handles btnResetItemFilter.Click
        txtItemFilter.Text = ""
        UpdatingChecks = True
        chkManufacturedItems.Checked = True
        chkMaterialResearchEqPrices.Checked = True
        Call CheckAllManufacturedItems()
        Call CheckAllRawItems()
        UpdatingChecks = False
        For i = 1 To TechCheckBoxes.Length - 1
            TechCheckBoxes(i).Checked = True
        Next i
    End Sub

    Private Sub chkMaterialResearchEqPrices_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkMaterialResearchEqPrices.CheckedChanged
        Call CheckAllRawItems()
    End Sub

    Private Sub chkManufacturedItems_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkManufacturedItems.CheckedChanged
        Call CheckAllManufacturedItems()
    End Sub

    ' Checks or unchecks just the prices for raw material items
    Private Sub CheckAllRawItems()

        UpdatingChecks = False

        ' Check all item boxes and do not run updates
        If chkMaterialResearchEqPrices.Checked = True Then
            chkAdvancedProtectiveTechnology.Checked = True
            chkGas.Checked = True
            chkIceProducts.Checked = True
            chkMolecularForgingTools.Checked = True
            chkFactionMaterials.Checked = True
            chkNamedComponents.Checked = True
            chkMinerals.Checked = True
            chkPlanetary.Checked = True
            chkRawMaterials.Checked = True
            chkSalvage.Checked = True
            chkMisc.Checked = True
            chkBPCs.Checked = True

            chkAdvancedMats.Checked = True
            chkBoosterMats.Checked = True
            chkMolecularForgedMaterials.Checked = True
            chkPolymers.Checked = True
            chkProcessedMats.Checked = True
            chkRawMoonMats.Checked = True

            chkAncientRelics.Checked = True
            chkDatacores.Checked = True
            chkDecryptors.Checked = True
            chkRDb.Checked = True

        Else ' Turn off all item checks
            chkAdvancedProtectiveTechnology.Checked = False
            chkGas.Checked = False
            chkIceProducts.Checked = False
            chkMolecularForgingTools.Checked = False
            chkFactionMaterials.Checked = False
            chkNamedComponents.Checked = False
            chkMinerals.Checked = False
            chkPlanetary.Checked = False
            chkRawMaterials.Checked = False
            chkSalvage.Checked = False
            chkMisc.Checked = False
            chkBPCs.Checked = False

            chkAdvancedMats.Checked = False
            chkBoosterMats.Checked = False
            chkMolecularForgedMaterials.Checked = False
            chkPolymers.Checked = False
            chkProcessedMats.Checked = False
            chkRawMoonMats.Checked = False

            chkAncientRelics.Checked = False
            chkDatacores.Checked = False
            chkDecryptors.Checked = False
            chkRDb.Checked = False
        End If

        UpdatingChecks = True

    End Sub

    ' Checks or unchecks just the prices for manufactured items
    Private Sub CheckAllManufacturedItems()

        UpdatingChecks = False

        ' Check all item boxes and do not run updates
        If chkManufacturedItems.Checked = True Then
            chkShips.Checked = True
            chkCharges.Checked = True
            chkModules.Checked = True
            chkDrones.Checked = True
            chkRigs.Checked = True
            chkSubsystems.Checked = True
            chkDeployables.Checked = True
            chkBoosters.Checked = True
            chkStructures.Checked = True
            chkStructureRigs.Checked = True
            chkCelestials.Checked = True
            chkStructureModules.Checked = True
            chkImplants.Checked = True

            chkCapT2Components.Checked = True
            chkAdvancedComponents.Checked = True
            chkFuelBlocks.Checked = True
            chkProtectiveComponents.Checked = True
            chkRAM.Checked = True
            chkCapitalShipComponents.Checked = True
            chkStructureComponents.Checked = True
            chkSubsystemComponents.Checked = True

        Else ' Turn off all item checks
            chkShips.Checked = False
            chkCharges.Checked = False
            chkModules.Checked = False
            chkDrones.Checked = False
            chkRigs.Checked = False
            chkSubsystems.Checked = False
            chkDeployables.Checked = False
            chkBoosters.Checked = False
            chkStructures.Checked = False
            chkStructureRigs.Checked = False
            chkCelestials.Checked = False
            chkStructureModules.Checked = False
            chkImplants.Checked = False

            chkCapT2Components.Checked = False
            chkAdvancedComponents.Checked = False
            chkFuelBlocks.Checked = False
            chkProtectiveComponents.Checked = False
            chkRAM.Checked = False
            chkCapitalShipComponents.Checked = False
            chkStructureComponents.Checked = False
            chkSubsystemComponents.Checked = False

        End If

        UpdatingChecks = True

    End Sub

    ' Makes sure a tech is enabled and checked for items that require tech based on saved values, not current due to disabling form
    Private Function CheckTechChecks() As Boolean

        If PriceCheckT1Enabled Then
            If TechCheckBoxes(1).Checked Then
                Return True
            End If
        End If

        If PriceCheckT2Enabled Then
            If TechCheckBoxes(2).Checked Then
                Return True
            End If
        End If

        If PriceCheckT3Enabled Then
            If TechCheckBoxes(3).Checked Then
                Return True
            End If
        End If

        If PriceCheckT4Enabled Then
            If TechCheckBoxes(4).Checked Then
                Return True
            End If
        End If

        If PriceCheckT5Enabled Then
            If TechCheckBoxes(5).Checked Then
                Return True
            End If
        End If

        If PriceCheckT6Enabled Then
            If TechCheckBoxes(6).Checked Then
                Return True
            End If
        End If

        Return False

    End Function

    ' Updates the T1, T2 and T3 check boxes depending on item selections
    Private Sub UpdateTechChecks()
        Dim T1 As Boolean = False
        Dim T2 As Boolean = False
        Dim T3 As Boolean = False
        Dim Storyline As Boolean = False
        Dim Navy As Boolean = False
        Dim Pirate As Boolean = False

        Dim ItemsSelected As Boolean = False
        Dim i As Integer
        Dim TechChecks As Boolean = False

        ' For check all 
        If Not UpdatingChecks And UpdateAllTechChecks Then
            UpdateAllTechChecks = False
            ' Check all and leave
            For i = 1 To TechCheckBoxes.Length - 1
                TechCheckBoxes(i).Enabled = True
                ' Check this one and leave
                TechCheckBoxes(i).Checked = True
            Next i
            Exit Sub
        End If

        ' Check each item checked and set the check boxes accordingly
        If chkShips.Checked Then
            T1 = True
            T2 = True
            T3 = True
            Navy = True
            Pirate = True
            ItemsSelected = True
        End If

        If chkModules.Checked Then
            T1 = True
            T2 = True
            Navy = True
            Storyline = True
            ItemsSelected = True
        End If

        If chkSubsystems.Checked Then
            T3 = True
            ItemsSelected = True
        End If

        If chkDrones.Checked Then
            T1 = True
            T2 = True
            ItemsSelected = True
        End If

        If chkRigs.Checked Then
            T1 = True
            T2 = True
            ItemsSelected = True
        End If

        If chkBoosters.Checked Then
            T1 = True
            ItemsSelected = True
        End If

        If chkStructures.Checked Then
            T1 = True
            ItemsSelected = True
        End If

        If chkCharges.Checked Then
            T1 = True
            T2 = True
            ItemsSelected = True
        End If

        ' If none are checked, then uncheck and un-enable all
        If ItemsSelected Then

            ' Enable the Checks
            If T1 Then
                chkItemsT1.Enabled = True
            Else
                chkItemsT1.Enabled = False
            End If

            If T2 Then
                chkItemsT2.Enabled = True
            Else
                chkItemsT2.Enabled = False
            End If

            If T3 Then
                chkItemsT3.Enabled = True
            Else
                chkItemsT3.Enabled = False
            End If

            If Storyline Then
                chkItemsT4.Enabled = True
            Else
                chkItemsT4.Enabled = False
            End If

            If Navy Then
                chkItemsT5.Enabled = True
            Else
                chkItemsT5.Enabled = False
            End If

            If Pirate Then
                chkItemsT6.Enabled = True
            Else
                chkItemsT6.Enabled = False
            End If

            ' Make sure we have at le=t one checked
            For i = 1 To TechCheckBoxes.Length - 1
                If TechCheckBoxes(i).Enabled Then
                    If TechCheckBoxes(i).Checked Then
                        TechChecks = True
                        ' Found one enabled and checked, so leave for
                        Exit For
                    End If
                End If
            Next i

            If Not TechChecks Then
                ' Need to check at le=t one
                For i = 1 To TechCheckBoxes.Length - 1
                    If TechCheckBoxes(i).Enabled Then
                        ' Check this one and leave
                        TechCheckBoxes(i).Checked = True
                    End If
                Next i
            End If

        Else
            chkItemsT1.Enabled = False
            chkItemsT2.Enabled = False
            chkItemsT3.Enabled = False
            chkItemsT4.Enabled = False
            chkItemsT5.Enabled = False
            chkItemsT6.Enabled = False
        End If

        ' Save status of the Tech check boxes
        PriceCheckT1Enabled = chkItemsT1.Enabled
        PriceCheckT2Enabled = chkItemsT2.Enabled
        PriceCheckT3Enabled = chkItemsT3.Enabled
        PriceCheckT4Enabled = chkItemsT4.Enabled
        PriceCheckT5Enabled = chkItemsT5.Enabled
        PriceCheckT6Enabled = chkItemsT6.Enabled

    End Sub

    Private Sub TechChecks_CheckedChanged(sender As Object, e As EventArgs) Handles chkItemsT1.CheckedChanged, chkItemsT2.CheckedChanged, chkItemsT3.CheckedChanged,
                                                                                    chkItemsT4.CheckedChanged, chkItemsT5.CheckedChanged, chkItemsT6.CheckedChanged
        UpdateTechChecks()
    End Sub

    Private Sub cmbPriceShipTypes_DropDown(sender As Object, e As System.EventArgs) Handles cmbPriceShipTypes.DropDown
        If FirstPriceShipTypesComboLoad Then
            PopulatePriceTypeCombo(cmbPriceShipTypes, AssetsViewerService.LoadPriceShipTypes(), "All Ship Types")
            FirstPriceShipTypesComboLoad = False
        End If
    End Sub

    Private Sub cmbPriceChargeTypes_DropDown(sender As Object, e As System.EventArgs) Handles cmbPriceChargeTypes.DropDown
        If FirstPriceChargeTypesComboLoad Then
            PopulatePriceTypeCombo(cmbPriceChargeTypes, AssetsViewerService.LoadPriceChargeTypes(), "All Charge Types")
            FirstPriceChargeTypesComboLoad = False
        End If
    End Sub

    Private Sub chkBoosters_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBoosters.CheckedChanged
        Call UpdateTechChecks()
    End Sub

    Private Sub chkRigs_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkRigs.CheckedChanged
        Call UpdateTechChecks()
    End Sub

    Private Sub chkShips_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkShips.CheckedChanged

        If chkShips.Checked = True Then
            cmbPriceShipTypes.Enabled = True
        ElseIf chkShips.Checked = False Then
            cmbPriceShipTypes.Enabled = False
        End If

        Call UpdateTechChecks()

    End Sub

    Private Sub chkModules_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkModules.CheckedChanged
        Call UpdateTechChecks()
    End Sub

    Private Sub chkDrones_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkDrones.CheckedChanged
        Call UpdateTechChecks()
    End Sub

    Private Sub chkCharges_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkCharges.CheckedChanged

        If chkCharges.Checked = True Then
            cmbPriceChargeTypes.Enabled = True
        ElseIf chkCharges.Checked = False Then
            cmbPriceChargeTypes.Enabled = False
        End If

        Call UpdateTechChecks()

    End Sub

    Private Sub chkSubsystems_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkSubsystems.CheckedChanged
        Call UpdateTechChecks()
    End Sub

    Private Sub chkStructures_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkStructures.CheckedChanged
        Call UpdateTechChecks()
    End Sub

    Private Sub rbtnAllItems_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnAllItems.CheckedChanged
        If rbtnAllItems.Checked Then
            ' Don't enable checks
            gbRawMaterials.Enabled = False
            gbManufacturedItems.Enabled = False
        End If
    End Sub

    Private Sub rbtnBPMats_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnBPMats.CheckedChanged
        If rbtnBPMats.Checked Then
            ' Enable checks
            gbRawMaterials.Enabled = True
            gbManufacturedItems.Enabled = True
        End If
    End Sub

    Private Sub ToggleNodeChecks(SentNode As TreeNode, Check As Boolean)
        ActiveForm.Cursor = Cursors.WaitCursor
        For Each child As TreeNode In SentNode.Nodes
            Application.DoEvents()
            child.Checked = Check
            If child.Nodes.Count > 0 Then ToggleNodeChecks(child, Check)
        Next
        ActiveForm.Cursor = Cursors.Default
        Application.DoEvents()
    End Sub

    Private Sub mnuCheckAll_Click(sender As Object, e As EventArgs) Handles mnuCheckAll.Click
        AssetTree.SelectedNode.Checked = True
        Call ToggleNodeChecks(AssetTree.SelectedNode, True)
    End Sub

    Private Sub mnuUncheckAll_Click(sender As Object, e As EventArgs) Handles mnuUncheckAll.Click
        AssetTree.SelectedNode.Checked = False
        Call ToggleNodeChecks(AssetTree.SelectedNode, False)
    End Sub

    Private Sub mnuExpandNodes_Click(sender As Object, e As EventArgs) Handles mnuExpandNodes.Click
        Call AssetTree.SelectedNode.ExpandAll()
    End Sub

    Private Sub mnuCollapseNodes_Click(sender As Object, e As EventArgs) Handles mnuCollapseNodes.Click
        Call AssetTree.SelectedNode.Collapse(False)
    End Sub

    Private Sub AssetTree_MouseDown(sender As Object, e As MouseEventArgs) Handles AssetTree.MouseDown
        If e.Button = MouseButtons.Right Then
            AssetTree.SelectedNode = AssetTree.GetNodeAt(e.X, e.Y)
        End If
    End Sub

    Private Sub AccountToggle_CheckedChanged(sender As Object, e As EventArgs) Handles rbtnSelectedAccount.CheckedChanged, rbtnMultiAccounts.CheckedChanged
        If rbtnSelectedAccount.Checked Then
            lstCharacters.Enabled = False
        ElseIf rbtnMultiAccounts.Checked Then
            lstCharacters.Enabled = True
        End If
    End Sub

#End Region

End Class
