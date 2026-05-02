Imports System.Data.SQLite

Public Class frmSettings

    Private SSLoaded As Boolean
    Private RegionLoaded As Boolean
    Private FirstLoad As Boolean
    Private SelectedReset As Boolean
    Private SVRComboLoaded As Boolean

    Private ReloadSkills As Boolean

    Private Defaults As New ProgramSettings ' For default constants

    Private Sub MarkDirty()
        btnSave.Text = "Save"
    End Sub

    Private Sub ToggleComboBoxSetting(toggle As CheckBox, comboBox As ComboBox, defaultText As String)
        If Not FirstLoad Then
            If toggle.Checked Then
                comboBox.Enabled = True
                comboBox.Text = defaultText
            Else
                comboBox.Enabled = False
                comboBox.Text = ""
            End If
        End If

        MarkDirty()
    End Sub

    Private Sub ToggleTextBoxSetting(toggle As CheckBox, textBox As TextBox, defaultValue As String, focusWhenEnabled As Boolean)
        If toggle.Checked Then
            textBox.Enabled = True
            If focusWhenEnabled Then
                textBox.Focus()
            End If
        Else
            textBox.Enabled = False
            textBox.Text = defaultValue
        End If

        MarkDirty()
    End Sub

#Region "Click object Functions"

    Private Sub chkBeanCounterManufacturing_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBeanCounterManufacturing.CheckedChanged
        ToggleComboBoxSetting(chkBeanCounterManufacturing, cmbBeanCounterManufacturing, "Zainou 'Beancounter' Industry BX-802")
    End Sub

    Private Sub chkBeanCounterRefining_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBeanCounterRefining.CheckedChanged
        ToggleComboBoxSetting(chkBeanCounterRefining, cmbBeanCounterRefining, "Zainou 'Beancounter' Reprocessing RX-802")
    End Sub

    Private Sub chkBeanCounterCopy_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBeanCounterCopy.CheckedChanged
        ToggleComboBoxSetting(chkBeanCounterCopy, cmbBeanCounterCopy, "Zainou 'Beancounter' Science SC-803")
    End Sub

    Private Sub chkBrokerCorpStanding_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBrokerCorpStanding.CheckedChanged
        ToggleTextBoxSetting(chkBrokerCorpStanding, txtBrokerCorpStanding, FormatNumber(Defaults.DefaultBrokerCorpStanding, 2), True)
    End Sub

    Private Sub chkBrokerFactionStanding_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBrokerFactionStanding.CheckedChanged
        ToggleTextBoxSetting(chkBrokerFactionStanding, txtBrokerFactionStanding, FormatNumber(Defaults.DefaultBrokerFactionStanding, 2), True)
    End Sub

    Private Sub txtEVEMarketerInterval_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtFuzzworksMarketInterval.KeyPress
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            ' Only integer values
            If allowedRunschars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub txtBrokerFactionStandings_keypress(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtBrokerFactionStanding.KeyPress
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedDecimalChars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub txtBrokerCorpStandings_keypress(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtBrokerCorpStanding.KeyPress
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedDecimalChars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub txtRefineCorpStanding_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs)
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedDecimalChars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub chkShowToolTips_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkShowToolTips.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkCheckUpdatesStartup_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkCheckUpdatesStartup.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkBuildBuyDefault_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBuildBuyDefault.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkDefaultME_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkDefaultME.CheckedChanged
        ToggleTextBoxSetting(chkDefaultME, txtDefaultME, FormatNumber(Defaults.DefaultSettingME, 0), True)
    End Sub

    Private Sub chkEVEMarketerInterval_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkFuzzworksMarketInterval.CheckedChanged
        ToggleTextBoxSetting(chkFuzzworksMarketInterval, txtFuzzworksMarketInterval, FormatNumber(Defaults.DefaultUpdatePricesRefreshInterval, 0), True)
    End Sub

    Private Sub chkDefaultPE_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkDefaultTE.CheckedChanged
        ToggleTextBoxSetting(chkDefaultTE, txtDefaultTE, FormatNumber(Defaults.DefaultSettingTE, 0), True)
    End Sub

    Private Sub chkDisableSVR_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkDisableSVR.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkDisableSound_CheckedChanged(sender As Object, e As EventArgs) Handles chkDisableSound.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkSaveFacilitiesbyChar_CheckedChanged(sender As Object, e As EventArgs) Handles chkSaveFacilitiesbyChar.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkLoadBPsbyChar_CheckedChanged(sender As Object, e As EventArgs) Handles chkLoadBPsbyChar.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub cmbRefineTax_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs)
        ' Only allow numbers, period or percent and backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedPercentChars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub chkRefreshMarketDataonStartup_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkRefreshMarketDataonStartup.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkRefreshFacilityDataonStartup_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkRefreshSystemCostIndiciesDataonStartup.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub rbtnExportDefault_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnExportDefault.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub rbtnExportCSV_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnExportCSV.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub rbtnExportSSV_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles rbtnExportSSV.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkSaveBPRelicsDecryptors_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkSaveBPRelicsDecryptors.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkBuyFuelBlocks_CheckedChanged(sender As Object, e As EventArgs) Handles chkAlwaysBuyFuelBlocks.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkSaveBPCCostperBP_CheckedChanged(sender As Object, e As EventArgs) Handles chkSaveBPCCostperBP.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkBuyRAMs_CheckedChanged(sender As Object, e As EventArgs) Handles chkAlwaysBuyRAMs.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub txtDefaultME_KeyPress(sender As System.Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtDefaultME.KeyPress
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedRunschars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub txtDefaultTE_KeyPress(sender As System.Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtDefaultTE.KeyPress
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedRunschars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub txtProxyPort_KeyPress(sender As System.Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtProxyPort.KeyPress
        ' Only allow numbers or backspace
        If e.KeyChar <> ControlChars.Back Then
            If allowedRunschars.IndexOf(e.KeyChar) = -1 Then
                ' Invalid Character
                e.Handled = True
            Else
                MarkDirty()
            End If
        End If
    End Sub

    Private Sub txtProxyAddress_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtProxyAddress.TextChanged
        MarkDirty()
    End Sub

#End Region

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        SSLoaded = False
        RegionLoaded = False
        btnSave.Text = "Save"
        FirstLoad = True
        SelectedReset = False
        SVRComboLoaded = False
        ReloadSkills = False

        If UserApplicationSettings.ShowToolTips Then
            With ToolTip1
                ' General
                .SetToolTip(chkShowToolTips, "Toogles tool tips through out IPH")
                .SetToolTip(chkLinksInCopyText, "Copying data to the clipboard will contain formatted text that enables in-game links to show when pasted in game")
                .SetToolTip(chkDisableSVR, "If you have issues with SVR updates on the Manufacturing Tab (ie website down, etc), you can disable those queries here")
                .SetToolTip(rbtnExportCSV, "Exports data in Common Separated Values with periods for decimals")
                .SetToolTip(chkDisableSound, "Disables sound functions in IPH")
                .SetToolTip(chkSaveFacilitiesbyChar, "When checked, saved facilities will only apply to the selected character. If unchecked, all characters will share saved facilities")
                .SetToolTip(chkLoadBPsbyChar, "When checked, blueprints loaded into IPH will be different for each character. If unchecked, all characters share the same BPs")
                .SetToolTip(chkDisableTracking, "When checked, IPH will not send anonymous useage data to Google Analytics")
                .SetToolTip(chkShareFacilities, "When checked, IPH will use the same facility type saved on any form when used on any other form. If unchecked, IPH will save each facility uniquely")

                ' Startup Options
                .SetToolTip(chkCheckUpdatesStartup, "IPH will check for program updates when the program starts")
                .SetToolTip(chkRefreshAssetsonStartup, "When checked, IPH will refresh assets (if cache date has past) for the selected character")
                .SetToolTip(chkRefreshBPsonStartup, "When checked, IPH will refresh blueprints (if cache date has past) for the selected character")
                .SetToolTip(chkRefreshMarketDataonStartup, "When checked, IPH will refresh average and adjusted market prices (if cache date has past) on startup for use in industry calcuations")
                .SetToolTip(chkRefreshSystemCostIndiciesDataonStartup, "When checked, IPH will refresh the system industry indicies on startup (if cache date has past) for use in industry calculations")
                .SetToolTip(chkRefreshPublicStructureDataonStartup, "When checked, IPH will refresh data on public structures (if cache date has past) for use in price updates")
                .SetToolTip(chkSupressESImsgs, "When checked, supresses messages if there are ESI Status errors.")

                ' Price Settings
                .SetToolTip(chkManualPriceOverride, "When set, any prices the user inputs manually into IPH will not be overriden when doing an update to prices.")
                .SetToolTip(chkAutoUpdateSVRBPTab, "When set, the Sales to Volume Ratio will be updated on the BP tab when a Blueprint is selected.")

                ' Export Data
                .SetToolTip(rbtnExportSSV, "Exports data in SemiColon Separated Values with commas for decimals")
                .SetToolTip(rbtnExportDefault, "Exports data in basic space or dashes to separate data for easy readability")
                .SetToolTip(rbtnExportCSV, "Exports data in Comma Separated Values")

                ' Character Options
                .SetToolTip(chkAlphaAccount, "When checked, IPH will calculate costs adding the 2% industry tax on industry and science jobs")
                .SetToolTip(chkUseActiveSkills, "When checked, IPH will use active skills instead of trained skills for calculations (useful for unsubscribed Omega accounts in Alpha)")
                .SetToolTip(chkLoadMaxAlphaSkills, "When checked, IPH will load the maximum trainable alpha skills for a dummy character.")

                ' Tips by Group box
                .SetToolTip(gbImplants, "Select implants to use with selected characters for industry calculations")
                .SetToolTip(gbDefaultMEPE, "On the BP and Manufacturing tabs, these default ME and TE values will be used for non-owned blueprints")
                .SetToolTip(gbShoppingList, "If checked, then IPH will send invention or copy materials needed to the shopping list when saving the build information for a blueprint")
                .SetToolTip(gb3rdpartyMarketRefresh, "The value stored here is the cache date (how often IPH will update) for EVE Marketer prices")
                .SetToolTip(gbStationStandings, "Station standings affect broker fees and some other industry related fees based on standing. These values here will be used in those calculations.")
                .SetToolTip(gbProxySettings, "When proxy information is in both the port and address, IPH will use this to connect to CCP servers. Note this information will also be used with the EVE IPH updater")

                .SetToolTip(chkAlwaysBuyFuelBlocks, "When selected, IPH will always force buying of fuel blocks as components in Build/Buy calculations")
                .SetToolTip(chkAlwaysBuyRAMs, "When selected, IPH will always force buying of R.A.M.s as components in Build/Buy calculations")
                .SetToolTip(chkSaveBPCCostperBP, "When selected and users check BPC Cost on the BP Tab and then Save BP, IPH will save the option to include the BPC cost only for this blueprint. ")

                .SetToolTip(chkSuggestBuildwhenBPnotOwned, "When selected, IPH will always Build the item if the BP is not owned")
                .SetToolTip(chkBuildWhenNotEnoughItemsonMarket, "When selected, IPH will build items if suggesting buy components without enough components on market to buy")
                .SetToolTip(chkManualPriceOverride, "When selected, IPH will not update prices that have had a price set manually")

            End With
        End If

    End Sub

    Private Sub frmSettings_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        ' Load the settings for the program from DB
        Call LoadFormSettings()
    End Sub

    Private Sub LoadFormSettings()
        Dim viewModel As SettingsViewModel

        FirstLoad = True
        viewModel = SettingsService.BuildViewModel(UserApplicationSettings, Defaults)
        ApplySettingsViewModel(viewModel)

        FirstLoad = False

        btnSave.Focus()

    End Sub

    Private Sub ApplySettingsViewModel(ByVal viewModel As SettingsViewModel)
        chkCheckUpdatesStartup.Checked = viewModel.CheckforUpdatesonStart

        rbtnExportCSV.Checked = (rbtnExportCSV.Text = viewModel.DataExportFormat)
        rbtnExportSSV.Checked = (rbtnExportSSV.Text = viewModel.DataExportFormat)
        rbtnExportDefault.Checked = (rbtnExportDefault.Text = viewModel.DataExportFormat)

        chkShowToolTips.Checked = viewModel.ShowToolTips
        chkRefreshAssetsonStartup.Checked = viewModel.LoadAssetsonStartup
        chkRefreshBPsonStartup.Checked = viewModel.LoadBPsonStartup
        chkDisableSound.Checked = viewModel.DisableSound

        chkLoadBPsbyChar.Checked = viewModel.LoadBPsbyChar
        chkSaveFacilitiesbyChar.Checked = viewModel.SaveFacilitiesbyChar

        chkRefreshSystemCostIndiciesDataonStartup.Checked = viewModel.LoadESISystemCostIndiciesDataonStartup
        chkRefreshMarketDataonStartup.Checked = viewModel.LoadESIMarketDataonStartup
        chkRefreshPublicStructureDataonStartup.Checked = viewModel.LoadESIPublicStructuresonStartup
        chkSupressESImsgs.Checked = viewModel.SupressESIStatusMessages

        chkBrokerCorpStanding.Checked = viewModel.UseBrokerCorpStanding
        txtBrokerCorpStanding.Enabled = viewModel.UseBrokerCorpStanding
        txtBrokerCorpStanding.Text = viewModel.BrokerCorpStandingText

        chkBrokerFactionStanding.Checked = viewModel.UseBrokerFactionStanding
        txtBrokerFactionStanding.Enabled = viewModel.UseBrokerFactionStanding
        txtBrokerFactionStanding.Text = viewModel.BrokerFactionStandingText

        chkBeanCounterManufacturing.Checked = viewModel.UseManufacturingImplant
        cmbBeanCounterManufacturing.Enabled = viewModel.UseManufacturingImplant
        cmbBeanCounterManufacturing.Text = viewModel.ManufacturingImplantName

        chkBeanCounterRefining.Checked = viewModel.UseRefiningImplant
        cmbBeanCounterRefining.Enabled = viewModel.UseRefiningImplant
        cmbBeanCounterRefining.Text = viewModel.RefiningImplantName

        chkBeanCounterCopy.Checked = viewModel.UseCopyImplant
        cmbBeanCounterCopy.Enabled = viewModel.UseCopyImplant
        cmbBeanCounterCopy.Text = viewModel.CopyImplantName

        chkBuildBuyDefault.Checked = viewModel.CheckBuildBuy
        chkSuggestBuildwhenBPnotOwned.Checked = viewModel.SuggestBuildBPNotOwned
        chkBuildWhenNotEnoughItemsonMarket.Checked = viewModel.BuildWhenNotEnoughItemsonMarket
        chkManualPriceOverride.Checked = viewModel.ManualPriceOverride
        chkSaveBPRelicsDecryptors.Checked = viewModel.SaveBPRelicsDecryptors
        chkAlwaysBuyFuelBlocks.Checked = viewModel.AlwaysBuyFuelBlocks
        chkAlwaysBuyRAMs.Checked = viewModel.AlwaysBuyRAMs
        chkSaveBPCCostperBP.Checked = viewModel.SaveBPCCostperBP

        chkDisableSVR.Checked = viewModel.DisableSVR
        chkDisableTracking.Checked = viewModel.DisableGATracking
        chkShareFacilities.Checked = viewModel.ShareSavedFacilities

        chkAlphaAccount.Checked = viewModel.AlphaAccount
        chkUseActiveSkills.Checked = viewModel.UseActiveSkillLevels
        chkLoadMaxAlphaSkills.Checked = viewModel.LoadMaxAlphaSkills

        chkLinksInCopyText.Checked = viewModel.IncludeInGameLinksinCopyText
        chkIncludeShopListInventMats.Checked = viewModel.ShopListIncludeInventMats
        chkIncludeShopListCopyMats.Checked = viewModel.ShopListIncludeCopyMats

        chkDefaultME.Checked = viewModel.UseDefaultME
        txtDefaultME.Enabled = viewModel.UseDefaultME
        txtDefaultME.Text = viewModel.DefaultMEText

        chkDefaultTE.Checked = viewModel.UseDefaultTE
        txtDefaultTE.Enabled = viewModel.UseDefaultTE
        txtDefaultTE.Text = viewModel.DefaultTEText

        chkFuzzworksMarketInterval.Checked = viewModel.UseCustomPriceRefreshInterval
        txtFuzzworksMarketInterval.Enabled = viewModel.UseCustomPriceRefreshInterval
        txtFuzzworksMarketInterval.Text = viewModel.PriceRefreshIntervalText

        chkAutoUpdateSVRBPTab.Checked = viewModel.AutoUpdateSVRonBPTab

        txtProxyAddress.Text = viewModel.ProxyAddress
        txtProxyPort.Text = viewModel.ProxyPortText
    End Sub

    Private Function ReadSettingsViewModel() As SettingsViewModel
        Dim viewModel As New SettingsViewModel

        With viewModel
            .CheckforUpdatesonStart = chkCheckUpdatesStartup.Checked

            If rbtnExportDefault.Checked Then
                .DataExportFormat = rbtnExportDefault.Text
            ElseIf rbtnExportCSV.Checked Then
                .DataExportFormat = rbtnExportCSV.Text
            ElseIf rbtnExportSSV.Checked Then
                .DataExportFormat = rbtnExportSSV.Text
            End If

            .ShowToolTips = chkShowToolTips.Checked
            .DisableSound = chkDisableSound.Checked
            .LoadAssetsonStartup = chkRefreshAssetsonStartup.Checked
            .LoadBPsonStartup = chkRefreshBPsonStartup.Checked
            .LoadESIMarketDataonStartup = chkRefreshMarketDataonStartup.Checked
            .LoadESISystemCostIndiciesDataonStartup = chkRefreshSystemCostIndiciesDataonStartup.Checked
            .LoadESIPublicStructuresonStartup = chkRefreshPublicStructureDataonStartup.Checked
            .SupressESIStatusMessages = chkSupressESImsgs.Checked
            .IncludeInGameLinksinCopyText = chkLinksInCopyText.Checked

            .SaveFacilitiesbyChar = chkSaveFacilitiesbyChar.Checked
            .LoadBPsbyChar = chkLoadBPsbyChar.Checked

            .UseBrokerCorpStanding = chkBrokerCorpStanding.Checked
            .BrokerCorpStandingText = txtBrokerCorpStanding.Text
            .UseBrokerFactionStanding = chkBrokerFactionStanding.Checked
            .BrokerFactionStandingText = txtBrokerFactionStanding.Text

            .UseManufacturingImplant = chkBeanCounterManufacturing.Checked
            .ManufacturingImplantName = cmbBeanCounterManufacturing.Text
            .UseRefiningImplant = chkBeanCounterRefining.Checked
            .RefiningImplantName = cmbBeanCounterRefining.Text
            .UseCopyImplant = chkBeanCounterCopy.Checked
            .CopyImplantName = cmbBeanCounterCopy.Text

            .CheckBuildBuy = chkBuildBuyDefault.Checked
            .SuggestBuildBPNotOwned = chkSuggestBuildwhenBPnotOwned.Checked
            .BuildWhenNotEnoughItemsonMarket = chkBuildWhenNotEnoughItemsonMarket.Checked
            .ManualPriceOverride = chkManualPriceOverride.Checked
            .SaveBPRelicsDecryptors = chkSaveBPRelicsDecryptors.Checked
            .AlwaysBuyFuelBlocks = chkAlwaysBuyFuelBlocks.Checked
            .AlwaysBuyRAMs = chkAlwaysBuyRAMs.Checked
            .SaveBPCCostperBP = chkSaveBPCCostperBP.Checked

            .DisableSVR = chkDisableSVR.Checked
            .DisableGATracking = chkDisableTracking.Checked
            .ShareSavedFacilities = chkShareFacilities.Checked

            .AlphaAccount = chkAlphaAccount.Checked
            .UseActiveSkillLevels = chkUseActiveSkills.Checked
            .LoadMaxAlphaSkills = chkLoadMaxAlphaSkills.Checked

            .ShopListIncludeInventMats = chkIncludeShopListInventMats.Checked
            .ShopListIncludeCopyMats = chkIncludeShopListCopyMats.Checked

            .UseDefaultME = chkDefaultME.Checked
            .DefaultMEText = txtDefaultME.Text
            .UseDefaultTE = chkDefaultTE.Checked
            .DefaultTEText = txtDefaultTE.Text
            .UseCustomPriceRefreshInterval = chkFuzzworksMarketInterval.Checked
            .PriceRefreshIntervalText = txtFuzzworksMarketInterval.Text

            .AutoUpdateSVRonBPTab = chkAutoUpdateSVRBPTab.Checked
            .ProxyAddress = txtProxyAddress.Text
            .ProxyPortText = txtProxyPort.Text
        End With

        Return viewModel
    End Function

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim TempSettings As ApplicationSettings = Nothing
        Dim TempViewModel As SettingsViewModel

        Dim OldMaxAlphaSkillsSetting As Boolean = UserApplicationSettings.LoadMaxAlphaSkills

        Dim Settings As New ProgramSettings
        Dim ReloadFacilties As Boolean = False

        If btnSave.Text = "Save" Then

            ' Make sure accurate data is entered
            If Not CheckEntries() Then
                Exit Sub
            End If

            Me.Cursor = Cursors.WaitCursor
            Me.Enabled = False

            TempViewModel = ReadSettingsViewModel()

            With TempSettings
                ' If they didn't have this checked before, refresh assets
                If SelectedCharacter.ID <> DummyCharacterID Then
                    If UserApplicationSettings.LoadAssetsonStartup = False And TempViewModel.LoadAssetsonStartup Then
                        Call CharacterDataService.RefreshAssets(SelectedCharacter, True)
                    End If

                    ' Same with blueprints
                    If UserApplicationSettings.LoadBPsonStartup = False And TempViewModel.LoadBPsonStartup Then
                        Call CharacterDataService.RefreshBlueprints(SelectedCharacter, True)
                    End If
                End If

                If UserApplicationSettings.SaveFacilitiesbyChar <> TempViewModel.SaveFacilitiesbyChar Then
                    ReloadFacilties = True
                End If

                If UserApplicationSettings.LoadBPsbyChar <> TempViewModel.LoadBPsbyChar Then
                    Dim Response As MsgBoxResult
                    Response = MsgBox("This will reset all Blueprint Data for the program." & Environment.NewLine & "Are you sure you want to do this?", vbYesNo, Application.ProductName)

                    If Response = vbYes Then
                        ' Delete all bps
                        Call EVEDB.ExecuteNonQuerySQL("DELETE FROM OWNED_BLUEPRINTS")
                        ' Also reset all BP cache dates incase they just updated the character or loaded it
                        Call EVEDB.ExecuteNonQuerySQL("UPDATE ESI_CHARACTER_DATA SET BLUEPRINTS_CACHE_DATE = NULL")

                        ' Set the current setting to what they want so the BP's load per the setting
                        UserApplicationSettings.LoadBPsbyChar = TempViewModel.LoadBPsbyChar

                        ' Need to reload the blueprints for all characters
                        Dim rsChar As SQLiteDataReader
                        DBCommand = New SQLiteCommand("SELECT CHARACTER_ID, ACCESS_TOKEN, TOKEN_TYPE, ACCESS_TOKEN_EXPIRE_DATE_TIME, REFRESH_TOKEN, SCOPES FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID <> " & CStr(DummyCharacterID), EVEDB.DBREf)
                        rsChar = DBCommand.ExecuteReader
                        While rsChar.Read
                            Dim TempToken As New SavedTokenData
                            With TempToken
                                .CharacterID = rsChar.GetInt32(0)
                                .AccessToken = rsChar.GetString(1)
                                .TokenType = rsChar.GetString(2)
                                .TokenExpiration = CDate(rsChar.GetString(3))
                                .RefreshToken = rsChar.GetString(4)
                                .Scopes = rsChar.GetString(5)
                                Call CharacterDataService.RefreshPersonalBlueprints(.CharacterID, TempToken, True)
                            End With
                        End While
                        rsChar.Close()
                    Else
                        ' Switch back
                        chkLoadBPsbyChar.Checked = UserApplicationSettings.LoadBPsbyChar
                        TempViewModel.LoadBPsbyChar = UserApplicationSettings.LoadBPsbyChar
                    End If
                End If
            End With

            TempSettings = SettingsService.BuildApplicationSettings(TempViewModel, UserApplicationSettings)

            ' Save the data in the XML file
            Call Settings.SaveApplicationSettings(TempSettings)

            ' Save the data to the local variable
            UserApplicationSettings = TempSettings

            ' If they selected to load max alpha skills for dummy character or reset it, then reload them if it changed
            If SelectedCharacter.ID = DummyCharacterID Then
                If OldMaxAlphaSkillsSetting <> chkLoadMaxAlphaSkills.Checked Then
                    Call SelectedCharacter.LoadDummyCharacter(True, True)
                End If
            End If

            ' They changed the active skill levels, update skills now with new application settings
            If ReloadSkills Then
                ' Set the flag first
                Call SelectedCharacter.Skills.SetActiveSkillFlagValue(UserApplicationSettings.UseActiveSkillLevels)
                Call SelectedCharacter.Skills.LoadCharacterSkills(SelectedCharacter.ID, SelectedCharacter.CharacterTokenData)
            End If

            ' If they changed what the original value was for the shared facilities, reload them
            If ReloadFacilties Then
                ' Load all the forms' facilities 
                Call frmMain.LoadFacilities()

                If ReprocessingPlantOpen Then
                    Call CType(Application.OpenForms.Item("frmReprocessingPlant"), frmReprocessingPlant).InitializeReprocessingFacility()
                End If

                'If IceBeltFlipOpen Then
                '    Call CType(Application.OpenForms.Item("frmIceBeltFlip"), frmIceBeltFlip).InitializeReprocessingFacility()
                'End If

                'If OreBeltFlipOpen Then
                '    Call CType(Application.OpenForms.Item("frmIndustryBeltFlip"), frmIndustryBeltFlip).InitializeReprocessingFacility()
                'End If
            End If

            ' Re-init any tabs that have settings changes before displaying dialog
            Call frmMain.ResetTabs(False)
            Call frmMain.ResetRefresh()

            MsgBox("Settings Saved", vbInformation, Application.ProductName)

            btnSave.Text = "OK"
            Me.Enabled = True
            Me.Cursor = Cursors.Default
        Else
            ' Just exit
            Me.Hide()
        End If

        btnSave.Focus()

    End Sub

    Private Function CheckEntries() As Boolean
        Dim TempTextBox As TextBox = Nothing
        Dim TempCheckBox As CheckBox = Nothing
        Dim TempComboBox As ComboBox = Nothing

        If (Not IsNumeric(txtBrokerCorpStanding.Text) Or Trim(txtBrokerCorpStanding.Text) = "") And chkBrokerCorpStanding.Checked Then
            TempTextBox = txtBrokerCorpStanding
            TempCheckBox = chkBrokerCorpStanding
            GoTo InvalidData
        ElseIf CDbl(txtBrokerCorpStanding.Text) > 10 Then
            txtBrokerCorpStanding.Text = "10.0"
        End If

        If (Not IsNumeric(txtBrokerFactionStanding.Text) Or Trim(txtBrokerFactionStanding.Text) = "") And chkBrokerFactionStanding.Checked Then
            TempTextBox = txtBrokerFactionStanding
            TempCheckBox = chkBrokerFactionStanding
            GoTo InvalidData
        ElseIf CDbl(txtBrokerFactionStanding.Text) > 10 Then
            txtBrokerFactionStanding.Text = "10.0"
        End If

        ' ME/TE
        If (Not IsNumeric(txtDefaultME.Text) Or Trim(txtDefaultME.Text) = "") And chkDefaultME.Checked Then
            TempTextBox = txtDefaultME
            TempCheckBox = chkDefaultME
            GoTo InvalidData
        End If

        If (Not IsNumeric(txtDefaultTE.Text) Or Trim(txtDefaultTE.Text) = "") And chkDefaultTE.Checked Then
            TempTextBox = txtDefaultTE
            TempCheckBox = chkDefaultTE
            GoTo InvalidData
        End If

        If (Not IsNumeric(txtFuzzworksMarketInterval.Text) Or Trim(txtFuzzworksMarketInterval.Text) = "") And chkFuzzworksMarketInterval.Checked Then
            TempTextBox = txtFuzzworksMarketInterval
            TempCheckBox = chkFuzzworksMarketInterval
            GoTo InvalidData
        ElseIf CInt(txtFuzzworksMarketInterval.Text) <= 0 Then
            MsgBox("Cannot set EVE Central Update Interval less than 1 Hour", vbExclamation, Application.ProductName)
            txtFuzzworksMarketInterval.Focus()
            Call txtFuzzworksMarketInterval.SelectAll()
            Return False
        ElseIf CInt(txtFuzzworksMarketInterval.Text) > 99 Then
            MsgBox("Cannot set EVE Central Update Interval greater than 99 hours", vbExclamation, Application.ProductName)
            txtFuzzworksMarketInterval.Focus()
            Call txtFuzzworksMarketInterval.SelectAll()
            Return False
        End If

        Return True

InvalidData:

        If Not IsNothing(TempComboBox) Then
            MsgBox("Invalid " & TempComboBox.Name & " Value", vbExclamation, Application.ProductName)
            TempComboBox.Focus()
            Call TempComboBox.SelectAll()
        Else
            MsgBox("Invalid " & TempCheckBox.Name & " Value", vbExclamation, Application.ProductName)
            TempTextBox.Focus()
            Call TempTextBox.SelectAll()
        End If

        Return False

    End Function

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        If SelectedReset Then
            ' If we hit reset, then we need to get the current list of settings, not just what is loaded (might be defaults)
            ' So just reload the settings
            UserApplicationSettings = AllSettings.LoadApplicationSettings()
        End If
        Me.Hide()
    End Sub

    Private Sub btnReset_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReset.Click
        SelectedReset = True
        ' Load default settings
        UserApplicationSettings = AllSettings.SetDefaultApplicationSettings()
        ' Reload the form
        Call LoadFormSettings()

    End Sub

    Private Sub chkAlphaAccount_CheckedChanged(sender As Object, e As EventArgs) Handles chkAlphaAccount.CheckedChanged
        If chkAlphaAccount.Checked Then
            ' Force them to use active skills in this case
            chkUseActiveSkills.Checked = True
            chkUseActiveSkills.Enabled = False
        Else
            chkUseActiveSkills.Enabled = True
        End If
        MarkDirty()
    End Sub

    Private Sub chkUseActiveSkills_CheckedChanged(sender As Object, e As EventArgs) Handles chkUseActiveSkills.CheckedChanged
        ' They changed active skills, so reload character skills on exit
        ReloadSkills = True
        MarkDirty()
    End Sub

    Private Sub chkSuggestBuildwhenBPnotOwned_CheckedChanged(sender As Object, e As EventArgs) Handles chkSuggestBuildwhenBPnotOwned.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkBuildWhenNotEnoughItemsonMarket_CheckedChanged(sender As Object, e As EventArgs) Handles chkBuildWhenNotEnoughItemsonMarket.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub btnOpenRates_Click(sender As Object, e As EventArgs) Handles btnOpenRates.Click
        Dim f1 As New frmEditDefaultRates
        f1.ShowDialog()
    End Sub

    Private Sub chkManualPriceOverride_CheckedChanged(sender As Object, e As EventArgs) Handles chkManualPriceOverride.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkAutoUpdateSVRBPTab_CheckedChanged(sender As Object, e As EventArgs) Handles chkAutoUpdateSVRBPTab.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkSupressESImsgs_CheckedChanged(sender As Object, e As EventArgs) Handles chkSupressESImsgs.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkRefreshPublicStructureDataonStartup_CheckedChanged(sender As Object, e As EventArgs) Handles chkRefreshPublicStructureDataonStartup.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkRefreshBPsonStartup_CheckedChanged(sender As Object, e As EventArgs) Handles chkRefreshBPsonStartup.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkRefreshAssetsonStartup_CheckedChanged(sender As Object, e As EventArgs) Handles chkRefreshAssetsonStartup.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkDisableTracking_CheckedChanged(sender As Object, e As EventArgs) Handles chkDisableTracking.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkShareFacilities_CheckedChanged(sender As Object, e As EventArgs) Handles chkShareFacilities.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkLinksInCopyText_CheckedChanged(sender As Object, e As EventArgs) Handles chkLinksInCopyText.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkIncludeShopListInventMats_CheckedChanged(sender As Object, e As EventArgs) Handles chkIncludeShopListInventMats.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkIncludeShopListCopyMats_CheckedChanged(sender As Object, e As EventArgs) Handles chkIncludeShopListCopyMats.CheckedChanged
        MarkDirty()
    End Sub

    Private Sub chkLoadMaxAlphaSkills_CheckedChanged(sender As Object, e As EventArgs) Handles chkLoadMaxAlphaSkills.CheckedChanged
        MarkDirty()
    End Sub
End Class
