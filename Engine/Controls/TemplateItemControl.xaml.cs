﻿using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Chat;
using JocysCom.VS.AiCompanion.Engine.Companions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JocysCom.VS.AiCompanion.Engine.Controls
{
	/// <summary>
	/// Interaction logic for TemplateItemControl.xaml
	/// </summary>
	public partial class TemplateItemControl : UserControl, INotifyPropertyChanged
	{
		public TemplateItemControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			MarkdownLanguageNameComboBox.ItemsSource = Global.AppSettings.MarkdownLanguageNames.Split(',');
			Global.PromptingUpdated += Global_PromptingUpdated;
			ChatPanel.OnSend += ChatPanel_OnSend;
			ChatPanel.OnStop += ChatPanel_OnStop;
			ChatPanel.MessagesPanel.WebBrowserDataLoaded += MessagesPanel_WebBrowserDataLoaded;
			ChatPanel.MessagesPanel.ScriptingHandler.OnMessageAction += MessagesPanel_ScriptingHandler_OnMessageAction;
			ChatPanel.DataTextBox.GotFocus += ChatPanel_DataTextBox_GotFocus;
			ChatPanel.DataInstructionsTextBox.GotFocus += ChatPanel_DataTextBox_GotFocus;
			//SolutionRadioButton.IsEnabled = Global.GetSolutionDocuments != null;
			//ProjectRadioButton.IsEnabled = Global.GetProjectDocuments != null;
			//FileRadioButton.IsEnabled = Global.GetSelectedDocuments != null;
			//SelectionRadioButton.IsEnabled = Global.GetSelection != null;
			Item = null;
			InitMacros();
			Global.OnSaveSettings += Global_OnSaveSettings;
			ChatPanel.UseEnterToSendMessage = Global.AppSettings.UseEnterToSendMessage;
			PromptsPanel.AddPromptButton.Click += PromptsPanel_AddPromptButton_Click;
			Global.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
			UpdateSpellCheck();
			var checkBoxes = ControlsHelper.GetAll<CheckBox>(this);
			AppHelper.EnableKeepFocusOnMouseClick(checkBoxes);
		}

		private void Global_PromptingUpdated(object sender, EventArgs e)
		{
			PromptsPanel.BindData(_item);
		}

		private void PromptsPanel_AddPromptButton_Click(object sender, RoutedEventArgs e)
		{
			var promptItem = Global.PromptItems.Items.FirstOrDefault(x => x.Name == _item?.PromptName);
			if (promptItem == null)
				return;
			var box = _item.ShowInstructions
				? LastFocusedForCodeTextBox ?? ChatPanel.DataInstructionsTextBox
				: ChatPanel.DataTextBox;
			var promptString = string.Format(promptItem.Pattern, _item?.PromptOption);
			AppHelper.InsertText(box, promptString, false, true);
		}

		private async void MessagesPanel_WebBrowserDataLoaded(object sender, EventArgs e)
		{
			await Helper.Delay(SetZoom, 250);
		}

		private async void MessagesPanel_ScriptingHandler_OnMessageAction(object sender, string[] e)
		{
			var actionString = e[1];
			if (string.IsNullOrEmpty(actionString))
				return;
			var action = (MessageAction)Enum.Parse(typeof(MessageAction), actionString);
			if (action != MessageAction.Use && action != MessageAction.Edit && action != MessageAction.Regenerate)
				return;
			var id = e[0];
			var message = ChatPanel.MessagesPanel.Messages.FirstOrDefault(x => x.Id == id);
			if (message == null)
				return;
			if (action == MessageAction.Use)
			{
				ChatPanel.DataTextBox.Text = message.Body;
				ChatPanel.EditMessageId = null;
				ChatPanel.FocusDataTextBox();
			}
			if (action == MessageAction.Regenerate)
			{
				ChatPanel.EditMessageId = id;
				ChatPanel.FocusDataTextBox();
				await ClientHelper.Send(_item, ChatPanel.ApplyMessageEdit, message.Body);
			}
			else if (action == MessageAction.Edit)
			{
				ChatPanel.DataTextBox.Text = message.Body;
				ChatPanel.EditMessageId = id;
				ChatPanel.FocusDataTextBox();
			}
		}

		private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(AppData.UseEnterToSendMessage):
					ChatPanel.UseEnterToSendMessage = Global.AppSettings.UseEnterToSendMessage;
					break;
				case nameof(AppData.IsSpellCheckEnabled):
					UpdateSpellCheck();
					break;
				default:
					break;
			}
		}

		#region SpellCheck

		void UpdateSpellCheck()
		{
			var isEnabled = Global.AppSettings.IsSpellCheckEnabled;
			UpdateSpellCheckForTextBox(ChatPanel.DataTextBox, isEnabled);
			UpdateSpellCheckForTextBox(ChatPanel.DataInstructionsTextBox, isEnabled);
		}

		private void UpdateSpellCheckForTextBox(TextBox box, bool isEnabled)
		{
			box.PreviewTextInput -= TextBox_PreviewTextInput;
			if (isEnabled)
				box.PreviewTextInput += TextBox_PreviewTextInput;
			SpellCheck.SetIsEnabled(box, isEnabled);
		}

		private async void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var box = (TextBox)sender;
			if (box.SpellCheck.IsEnabled)
				box.SpellCheck.IsEnabled = false;
			if (box == ChatPanel.DataTextBox)
				await Helper.Delay(EnableOnDataTextBox);
			if (box == ChatPanel.DataInstructionsTextBox)
				await Helper.Delay(EnableOnDataInstructionsTextBox);
		}

		void EnableOnDataTextBox()
			=> ChatPanel.DataTextBox.SpellCheck.IsEnabled = true;
		void EnableOnDataInstructionsTextBox()
			=> ChatPanel.DataInstructionsTextBox.SpellCheck.IsEnabled = true;

		#endregion

		private void Global_OnSaveSettings(object sender, EventArgs e)
		{
			// Update from previous settings.
			if (_item != null)
				_item.Settings = ChatPanel.MessagesPanel.GetWebSettings();
		}

		private async void ChatPanel_OnSend(object sender, EventArgs e)
		{
			await ClientHelper.Send(_item, ChatPanel.ApplyMessageEdit);
		}

		private void ChatPanel_OnStop(object sender, EventArgs e)
		{
			_item?.StopClients();
		}

		public string CreativityName
		{
			get
			{
				var v = _item?.Creativity;
				if (v is null)
					return "";
				if (v >= 2 - 0.25)
					return "Very Creative";
				if (v >= 1.5 - 0.25)
					return "Creative";
				if (v >= 1 - 0.25)
					return "Balanced";
				if (v >= 0.5 - 0.25)
					return "Precise";
				return "Very Precise";
			}
		}

		public DataOperation[] AutoOperations
			=> (DataOperation[])Enum.GetValues(typeof(DataOperation));
		public Dictionary<ToolCallApprovalProcess, string> PluginApprovalProcesses
			=> ControlsHelper.GetDictionary((ToolCallApprovalProcess[])Enum.GetValues(typeof(ToolCallApprovalProcess)));
		public Dictionary<string, string> PluginApprovalTemplates
			=> Global.Templates.Items.ToDictionary(x => x.Name, x => x.Name);

		public Dictionary<AttachmentType, string> DataTypes
		{
			get
			{
				if (_DataTypes == null)
				{
					var values = new AttachmentType[] {
						AttachmentType.Clipboard,
						AttachmentType.Selection,
						AttachmentType.ActiveDocument,
						AttachmentType.OpenDocuments,
						AttachmentType.SelectedDocuments,
						AttachmentType.ActiveProject,
						AttachmentType.SelectedProject,
						AttachmentType.Solution,
					};
					_DataTypes = ControlsHelper.GetDictionary(values);
				}
				return _DataTypes;
			}
			set => _DataTypes = value;
		}
		Dictionary<AttachmentType, string> _DataTypes;

		public Dictionary<MessageBoxOperation, string> MessageBoxOperations
		{
			get
			{
				if (_DataTypes == null)
				{
					var values = (MessageBoxOperation[])Enum.GetValues(typeof(MessageBoxOperation));
					_MessageBoxOperations = ControlsHelper.GetDictionary(values);
				}
				return _MessageBoxOperations;
			}
			set => _MessageBoxOperations = value;
		}
		Dictionary<MessageBoxOperation, string> _MessageBoxOperations;

		TemplateItem _item;
		public TemplateItem Item
		{
			get => _item;
			set
			{
				if (Equals(value, _item))
					return;
				var oldItem = _item;
				// Update from previous settings.
				if (_item != null)
				{
					_item.PropertyChanged -= _item_PropertyChanged;
					_item.Settings = ChatPanel.MessagesPanel.GetWebSettings();
				}
				// Make sure that custom AiModel old and new item is available to select.
				AppHelper.UpdateModelCodes(value?.AiService, AiModelBoxPanel.AiModels, value?.AiModel, oldItem?.AiModel);
				// Set new item.
				_item = value ?? AppHelper.GetNewTemplateItem();
				// This will trigger AiCompanionComboBox_SelectionChanged event.
				AiModelBoxPanel.BindData(null);
				DataContext = _item;
				_item.PropertyChanged += _item_PropertyChanged;
				AiModelBoxPanel.BindData(_item);
				OnPropertyChanged(nameof(CreativityName));
				// New item is bound. Make sure that custom AiModel only for the new item is available to select.
				AppHelper.UpdateModelCodes(_item.AiService, AiModelBoxPanel.AiModels, _item?.AiModel);
				IconPanel.BindData(_item);
				PromptsPanel.BindData(_item);
				OnPropertyChanged(nameof(SendChatHistory));
				ChatPanel.MessagesPanel.SetDataItems(_item.Messages, _item.Settings);
				ChatPanel.IsBusy = _item.IsBusy;
				ChatPanel.UpdateButtons();
				System.Diagnostics.Debug.WriteLine($"Bound Item: {_item.Name}");
				// AutoSend once enabled then...
				if (DataType == ItemType.Task && _item.AutoSend)
				{
					// Disable auto-send so that it won't trigger every time item is bound.
					_item.AutoSend = false;
					_ = Dispatcher.BeginInvoke(new Action(() =>
					{
						_ = ClientHelper.Send(_item, ChatPanel.ApplyMessageEdit);
					}));
				}
			}
		}

		// Move to settings later.
		public const string TextToProcess = "Text to process:";

		private void _item_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(TemplateItem.IsBusy):
					ChatPanel.IsBusy = _item.IsBusy;
					ChatPanel.UpdateButtons();
					break;
				case nameof(TemplateItem.Creativity):
					OnPropertyChanged(nameof(CreativityName));
					break;
				case nameof(TemplateItem.IsSystemInstructions):
					var text = _item.TextInstructions.Trim();
					var containsDataHeader = text.Contains(TextToProcess) || text.EndsWith(":");
					if (_item.IsSystemInstructions && text.Contains(TextToProcess))
					{
						var s = text.Replace(TextToProcess, "").TrimEnd();
						AppHelper.SetText(ChatPanel.DataInstructionsTextBox, s);
					}
					else if (!_item.IsSystemInstructions && !containsDataHeader && !string.IsNullOrEmpty(text))
					{
						var s = ClientHelper.JoinMessageParts(text, TextToProcess);
						AppHelper.SetText(ChatPanel.DataInstructionsTextBox, s);
					}
					break;
				default:
					break;
			}
		}

		#region ■ Properties

		[Category("Main"), DefaultValue(ItemType.None)]
		public ItemType DataType
		{
			get => _DataType;
			set
			{
				_DataType = value;
				if (ControlsHelper.IsDesignMode(this))
					return;
				// Update panel settings.
				PanelSettings.PropertyChanged -= PanelSettings_PropertyChanged;
				PanelSettings = Global.AppSettings.GetTaskSettings(value);
				ZoomSlider.Value = PanelSettings.ChatPanelZoom;
				PanelSettings.PropertyChanged += PanelSettings_PropertyChanged;
				// Update the rest.
				PanelSettings.UpdateBarToggleButtonIcon(BarToggleButton);
				PanelSettings.UpdateListToggleButtonIcon(ListToggleButton);
				OnPropertyChanged(nameof(BarPanelVisibility));
				OnPropertyChanged(nameof(TemplateItemVisibility));
			}
		}
		private ItemType _DataType;

		#endregion

		#region Macros

		private void PropertiesRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			InitMacros();
		}

		void InitMacros()
		{
			AddKeys(SelectionComboBox, null, AppHelper.GetReplaceMacrosSelection());
			AddKeys(FileComboBox, null, AppHelper.GetReplaceMacrosDocument());
			AddKeys(DateComboBox, null, AppHelper.GetReplaceMacrosDate());
			AddKeys(VsMacrosComboBox, null, Global.GetEnvironmentProperties());
		}

		int alwaysSelectedIndex = -1;

		void AddKeys(ComboBox cb, string name, IEnumerable<PropertyItem> list)
		{
			var options = new List<PropertyItem>();
			options.AddRange(list);
			cb.ItemsSource = options;
			cb.SelectedIndex = alwaysSelectedIndex;
			cb.SelectionChanged += ComboBox_SelectionChanged;
		}
		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.SelectedIndex <= alwaysSelectedIndex)
				return;
			var item = (PropertyItem)cb.SelectedItem;
			cb.SelectedIndex = alwaysSelectedIndex;
			AppHelper.InsertText(ChatPanel.DataTextBox, "{" + item.Key + "}");
			// Enable use of macros.
			if (!_item.UseMacros)
				_item.UseMacros = true;
		}

		#endregion

		private void This_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			var head = "Caring for Your Sensitive Data";
			var body = "As you share files for AI processing, please remember not to include confidential, proprietary, or sensitive information.";
			Global.MainControl.InfoPanel.HelpProvider.Add(AttachmentEnumComboBox, head, body, MessageBoxImage.Warning);
			Global.MainControl.InfoPanel.HelpProvider.Add(AttachmentIcon, head, body, MessageBoxImage.Warning);
			Global.MainControl.InfoPanel.HelpProvider.Add(ContextTypeLabel, head, body, MessageBoxImage.Warning);
			if (!Global.IsVsExtesion)
			{
				Global.MainControl.InfoPanel.HelpProvider.Add(FileComboBox, UseMacrosCheckBox.Content as string, Global.VsExtensionFeatureMessage);
				Global.MainControl.InfoPanel.HelpProvider.Add(SelectionComboBox, UseMacrosCheckBox.Content as string, Global.VsExtensionFeatureMessage);
				Global.MainControl.InfoPanel.HelpProvider.Add(AutomationVsLabel, AutomationVsLabel.Content as string, Global.VsExtensionFeatureMessage);
				Global.MainControl.InfoPanel.HelpProvider.Add(AutoOperationComboBox, AutomationVsLabel.Content as string, Global.VsExtensionFeatureMessage);
				Global.MainControl.InfoPanel.HelpProvider.Add(AutoFormatCodeCheckBox, AutomationVsLabel.Content as string, Global.VsExtensionFeatureMessage);
			}
			AppHelper.AddHelp(ShowInstructionsCheckBox, "Show instructions that will be included at the start of every message.");
			AppHelper.AddHelp(AutoSendCheckBox, "Automatically send Task for processing to AI when Task is created from the Template.");
			AppHelper.AddHelp(IsPreviewCheckBox, ClientHelper.PreviewModeMessage);
			AppHelper.AddHelp(IsFavoriteCheckBox, "Display the template button in the toolbar for quick task creation.");
			AppHelper.AddHelp(AutoFormatMessageCheckBox, "Use AI to automatically format your message using markdown.");
			//AppHelper.AddHelp(AutoGenerateTitleCheckBox, "Use AI to to automatically generate chat title.");
			AppHelper.AddHelp(ShowPromptingCheckBox, "Guide and shape the AI's output in your desired style." +
				" You can select a 'Prompt' category such as Tone, Format, Context, Role, or Instruction," +
				" and then choose an option within that category to define how the AI should approach the content creation.");
			AppHelper.AddHelp(IsSystemInstructionsCheckBox,
				"If checked, instructions will be sent as a system message." +
				" Otherwise, they will be added to the user's message.");
			var codeButtons = ControlsHelper.GetAll<Button>(CodeButtonsPanel);
			foreach (var codeButton in codeButtons)
			{
				var languageDisplayName = codeButton.ToolTip;
				codeButton.ToolTip = $"Paste {languageDisplayName} code block";
				AppHelper.AddHelp(codeButton,
					$"Wrap selection into `{languageDisplayName}` code block. Hold CTRL to paste from your clipboard as an `{languageDisplayName}` code block."
				);
			}
		}

		private void ClearMessagesButton_Click(object sender, RoutedEventArgs e)
		{
			var text = $"Do you want to clear all messages?";
			var caption = $"{Global.Info.Product} - Clear messages";
			var result = MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
				return;
			_item.Messages.Clear();
			ChatPanel.MessagesPanel.SetDataItems(_item.Messages, _item.Settings);
			ChatPanel.UpdateButtons();
		}

		private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
		{
			ChatPanel.MessagesPanel.InvokeScript("ScrollToBottom()");
		}

		private void GenerateTitleButton_Click(object sender, RoutedEventArgs e)
		{
			var firstMessage = _item.Messages.FirstOrDefault();
			if (firstMessage == null)
				return;
			_ = ClientHelper.GenerateTitle(_item);
		}

		private void HyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			ControlsHelper.OpenUrl(e.Uri.AbsoluteUri);
			e.Handled = true;
		}

		private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			PanelSettings.ChatPanelZoom = (int)ZoomSlider.Value;
		}

		void SetZoom()
		{
			Dispatcher.Invoke(() =>
			{
				ChatPanel.MessagesPanel.SetZoom((int)ZoomSlider.Value);
			});
		}

		public bool SendChatHistory
		{
			get
			{
				return _item?.AttachContext.HasFlag(AttachmentType.ChatHistory) ?? false;
			}
			set
			{
				var item = _item;
				if (item == null)
					return;
				item.AttachContext = item.AttachContext.HasFlag(AttachmentType.ChatHistory)
					? item.AttachContext & ~AttachmentType.ChatHistory
					: item.AttachContext |= AttachmentType.ChatHistory;
			}
		}

		private TextBox LastFocusedForCodeTextBox;

		private void ChatPanel_DataTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			LastFocusedForCodeTextBox = (TextBox)sender;
		}

		private void CodeButton_Click(object sender, RoutedEventArgs e)
		{
			var isCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
			var button = (Button)sender;
			var language = button.Tag as string;
			if (language == "Custom")
				language = MarkdownLanguageNameComboBox.SelectedItem as string ?? "";
			if (string.IsNullOrEmpty(language))
				return;
			var box = _item.ShowInstructions
				? LastFocusedForCodeTextBox ?? ChatPanel.DataTextBox
				: ChatPanel.DataTextBox;
			var caretIndex = box.CaretIndex;
			var clipboardText = isCtrlDown
				? JocysCom.ClassLibrary.Text.Helper.RemoveIdent(Global.GetClipboard()?.Data ?? "")
				: $"{box.SelectedText}";
			var prefix = "";
			// Add new line if caret is not on the new line.
			if (caretIndex > 0 && box.Text[caretIndex - 1] != '\n')
				prefix += $"\r\n";
			prefix += $"```{language}\r\n";
			var suffix = $"\r\n```";
			// Add new line if caret is not at the end of the line.
			if (caretIndex < box.Text.Length && box.Text[caretIndex] != '\r')
				suffix += $"\r\n";
			var text = $"{prefix}{clipboardText}{suffix}";
			AppHelper.InsertText(box, text, true, false);
			var newIndex = string.IsNullOrEmpty(clipboardText)
				? caretIndex + prefix.Length
				: caretIndex + text.Length;
			AppHelper.SetCaret(box, newIndex);
		}

		#region PanelSettings

		TaskSettings PanelSettings { get; set; } = new TaskSettings();

		private async void PanelSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PanelSettings.IsBarPanelVisible))
			{
				PanelSettings.UpdateBarToggleButtonIcon(BarToggleButton);
				OnPropertyChanged(nameof(BarPanelVisibility));
				OnPropertyChanged(nameof(TemplateItemVisibility));
			}
			if (e.PropertyName == nameof(PanelSettings.ChatPanelZoom))
			{
				await Helper.Delay(SetZoom);
			}
		}

		private void ListToggleButton_Click(object sender, RoutedEventArgs e)
		{
			PanelSettings.UpdateListToggleButtonIcon(ListToggleButton, true);
		}

		public Visibility BarPanelVisibility
			=> PanelSettings.IsBarPanelVisible ? Visibility.Visible : Visibility.Collapsed;

		public Visibility TemplateItemVisibility
			=> PanelSettings.IsBarPanelVisible && _DataType == ItemType.Template ? Visibility.Visible : Visibility.Collapsed;

		private void BarToggleButton_Click(object sender, RoutedEventArgs e)
		{
			PanelSettings.UpdateBarToggleButtonIcon(BarToggleButton, true);
		}

		#endregion

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion

	}
}
