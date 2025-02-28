﻿using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
using JocysCom.VS.AiCompanion.Engine.Companions;
using JocysCom.VS.AiCompanion.Plugins.Core.VsFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace JocysCom.VS.AiCompanion.Engine
{
	public static class Global
	{

		public static ISolutionHelper _SolutionHelper;

		// Get or Set multiple documents.
		public static Action<List<DocItem>> SetSolution = (x) => { };
		public static Func<List<DocItem>> GetActiveProject = () => new List<DocItem>();
		public static Action<List<DocItem>> SetActiveProject = (x) => { };
		public static Action<List<DocItem>> SetSelectedProject = (x) => { };
		public static Func<List<DocItem>> GetSelectedDocuments = () => new List<DocItem>();
		public static Action<List<DocItem>> SetSelectedDocuments = (x) => { };
		public static Func<List<DocItem>> GetOpenDocuments = () => new List<DocItem>();
		public static Action<List<DocItem>> SetOpenDocuments = (x) => { };
		// Get or Set single document.
		public static Func<DocItem> GetClipboard = () => new DocItem();
		public static Action<string> SetClipboard = (x) => { };
		// Get solution properties (macros).
		public static Func<List<PropertyItem>> GetEnvironmentProperties = () => new List<PropertyItem>();
		public static Func<List<PropertyItem>> GetReservedProperties = () => new List<PropertyItem>();
		public static Func<List<PropertyItem>> GetOtherProperties = () => new List<PropertyItem>();

		public static AssemblyInfo Info { get; } = new AssemblyInfo(typeof(Global).Assembly);

		public static AppData AppSettings =>
			AppData.Items.FirstOrDefault();

		public static SettingsData<AppData> AppData =
			new SettingsData<AppData>(null, true, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static SettingsData<PromptItem> PromptItems =
			new SettingsData<PromptItem>($"{nameof(PromptItems)}.xml", true, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static SettingsData<TemplateItem> Templates =
			new SettingsData<TemplateItem>($"{nameof(Templates)}.xml", true, null, System.Reflection.Assembly.GetExecutingAssembly())
			{
				UseSeparateFiles = true,
			};

		public static SettingsData<TemplateItem> Tasks =
			new SettingsData<TemplateItem>($"{nameof(Tasks)}.xml", true, null, System.Reflection.Assembly.GetExecutingAssembly())
			{
				UseSeparateFiles = true,
			};


		public static SettingsData<FineTuningItem> FineTunings =
			new SettingsData<FineTuningItem>($"{nameof(ItemType.FineTuning)}.xml", true, null, System.Reflection.Assembly.GetExecutingAssembly())
			{
				UseSeparateFiles = true,
			};

		public static SettingsData<AssistantItem> Assistants =
			new SettingsData<AssistantItem>($"{nameof(Assistants)}.xml", true, null, System.Reflection.Assembly.GetExecutingAssembly())
			{
				UseSeparateFiles = true,
			};


		public static ISettingsData GetSettings(ItemType type)
		{
			switch (type)
			{
				case ItemType.Task: return Tasks;
				case ItemType.Template: return Templates;
				case ItemType.FineTuning: return FineTunings;
				case ItemType.Assistant: return Assistants;
				default: return new SettingsData<TemplateItem>();
			}
		}

		public static void ShowError(string message)
		{
			var form = new MessageBoxWindow();
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog(message);
		}

		public static void InsertItem(IFileListItem item, ItemType type)
		{
			if (type != ItemType.Task && type != ItemType.Template)
				return;
			AppHelper.FixName(item, Tasks.Items);
			var panel = type == ItemType.Task
				? MainControl.TasksPanel.ListPanel
				: MainControl.TemplatesPanel.ListPanel;
			panel.InsertItem(item);
		}

		public static string FineTuningPath
			=> Path.Combine(AppData.XmlFile.Directory.FullName, nameof(ItemType.FineTuning));

		public static string AssistantsPath
			=> Path.Combine(AppData.XmlFile.Directory.FullName, nameof(Assistants));

		public static string GetPath(AssistantItem item, params string[] args)
		{
			var itemPath = new string[] { AssistantsPath, item.Name };
			var paths = itemPath.Concat(args).ToArray();
			var path = System.IO.Path.Combine(paths);
			return path;
		}

		public static string GetPath(FineTuningItem item, params string[] args)
		{
			var itemPath = new string[] { FineTuningPath, item.Name };
			var paths = itemPath.Concat(args).ToArray();
			var path = System.IO.Path.Combine(paths);
			return path;
		}

		#region Validation

		public static void SetWithTimeout(MessageBoxImage image, string content = null, params object[] args)
		{
			MainControl.InfoPanel.SetWithTimeout(image, content, args);
		}

		public static bool ValidateServiceAndModel(IAiServiceModel am)
		{
			if (am == null)
				return false;
			return ValidateServiceAndModel(am.AiService, am.AiModel);
		}

		public static bool ValidateServiceAndModel(AiService service, string model)
		{
			var messages = new List<string>();
			if (service == null)
				messages.Add("Please select AI Service.");
			if (string.IsNullOrEmpty(model))
				messages.Add("Please select AI Model.");
			if (messages.Any())
				SetWithTimeout(MessageBoxImage.Warning, string.Join(" ", messages));
			return !messages.Any();
		}

		public static bool IsGoodSettings(AiService item, bool redirectToSettings = false)
		{
			var itemsRequired = new List<string>();
			if (string.IsNullOrEmpty(item.BaseUrl))
				itemsRequired.Add("Base URL");
			if (string.IsNullOrEmpty(item.Name))
				itemsRequired.Add("Service Name");
			// If OpenAI service then check for API Key and Organization ID.
			if ((item.BaseUrl ?? "").Contains(".openai.com"))
			{
				if (string.IsNullOrEmpty(item.ApiSecretKey))
					itemsRequired.Add("API Key");
				if (string.IsNullOrEmpty(item.ApiOrganizationId))
					itemsRequired.Add("API Organization ID");
			}
			if (redirectToSettings && itemsRequired.Count > 0)
			{
				MainControl.MainTabControl.SelectedItem = MainControl.OptionsTabItem;
				var s = string.Join(" and ", itemsRequired);
				SetWithTimeout(MessageBoxImage.Warning, $"Please provide the {s}.");
			}
			return itemsRequired.Count == 0;
		}

		#endregion

		#region Events;

		public static event EventHandler OnSaveSettings;
		public static event EventHandler OnFilesUpdaded;
		public static event EventHandler OnFineTuningJobCreated;
		public static event EventHandler OnSourceDataFilesUpdated;
		public static event EventHandler OnTuningDataFilesUpdated;
		public static event EventHandler OnTasksUpdated;

		public static void RaiseOnSaveSettings()
			=> OnSaveSettings?.Invoke(null, EventArgs.Empty);

		public static void RaiseOnFilesUploaded()
			=> OnFilesUpdaded?.Invoke(null, EventArgs.Empty);

		public static void RaiseOnFineTuningJobCreated()
			=> OnFineTuningJobCreated?.Invoke(null, EventArgs.Empty);

		public static void RaiseOnSourceDataFilesUpdated()
			=> OnSourceDataFilesUpdated?.Invoke(null, EventArgs.Empty);

		public static void RaiseOnTuningDataFilesUpdated()
			=> OnTuningDataFilesUpdated?.Invoke(null, EventArgs.Empty);

		public static void RaiseOnTasksUpdated()
			=> OnTasksUpdated?.Invoke(null, EventArgs.Empty);

		#endregion

		public static void SaveSettings()
		{
			RaiseOnSaveSettings();
			AppData.Save();
			PromptItems.Save();
			Templates.Save();
			Tasks.Save();
			FineTunings.Save();
			Assistants.Save();
		}

		/// <summary>
		/// Subscribed by controls that need to refresh when the source data is updated.
		/// </summary>
		public static event EventHandler AiModelsUpdated;

		public static void TriggerAiModelsUpdated()
			=> AiModelsUpdated?.Invoke(null, EventArgs.Empty);

		/// <summary>
		/// Subscribed by controls that need to refresh when the source data is updated.
		/// </summary>
		public static event EventHandler PromptingUpdated;
		public static void TriggerPromptingUpdated()
			=> PromptingUpdated?.Invoke(null, EventArgs.Empty);

		public static void LoadSettings()
		{
			// Load app data.
			AppData.OnValidateData += AppData_OnValidateData;
			AppData.Load();
			if (AppData.IsSavePending)
				AppData.Save();
			// Load Prompt items.
			PromptItems.OnValidateData += PromptItems_OnValidateData;
			PromptItems.Load();
			if (PromptItems.IsSavePending)
				PromptItems.Save();
			// Load templates.
			Templates.OnValidateData += Templates_OnValidateData;
			Templates.Load();
			if (Templates.IsSavePending)
				Templates.Save();
			// Load tasks.
			Tasks.OnValidateData += Tasks_OnValidateData;
			Tasks.Load();
			if (Tasks.IsSavePending)
				Tasks.Save();
			// Load fine tune settings.
			FineTunings.OnValidateData += FineTunings_OnValidateData;
			FineTunings.Load();
			FineTunings.Load();
			if (FineTunings.IsLoadPending)
				FineTunings.Load();
			// Load Assistant settings.
			Assistants.OnValidateData += Assistants_OnValidateData;
			Assistants.Load();
			Assistants.Load();
			if (Assistants.IsLoadPending)
				Assistants.Load();


			// Enable template and task folder monitoring.
			Templates.SetFileMonitoring(true);
			Tasks.SetFileMonitoring(true);
			// If old settings version then reset templates.
			if (AppData.Version < 2)
			{
				AppData.Version = 2;
				ResetTemplates();
			}
		}

		private static void Assistants_OnValidateData(object sender, SettingsData<AssistantItem>.SettingsDataEventArgs e)
		{
			var data = (SettingsData<AssistantItem>)sender;
			if (e.Items.Count != 0)
				return;
		}

		private static void FineTunings_OnValidateData(object sender, SettingsData<FineTuningItem>.SettingsDataEventArgs e)
		{
			var data = (SettingsData<FineTuningItem>)sender;
			if (e.Items.Count != 0)
				return;
			AppHelper.ExtractFiles("FineTuning.zip", FineTuningPath);
			// Trigger reload of data.
			data.IsLoadPending = true;
		}

		class prompt_item
		{
			public string title { get; set; }
			public string pattern { get; set; }
			public string[] options { get; set; }
		}

		private static void PromptItems_OnValidateData(object sender, SettingsData<PromptItem>.SettingsDataEventArgs e)
		{
			var data = (SettingsData<PromptItem>)sender;
			if (e.Items.Count == 0)
			{
				// Find data as embedded resource with the same file name and load.
				data.ResetToDefault();
			}
		}

		public static void ResetTemplates()
		{
			var items = Templates.Items.ToArray();
			foreach (var item in items)
			{
				var error = Templates.DeleteItem(item);
				if (!string.IsNullOrEmpty(error))
					ShowError(error);
			}
			Templates.Load();
			Templates.Save();
		}

		public static void ResetAppSettings()
		{
			// Reset all app settings except list of services and list of models.
			var exclude = new string[] { nameof(AppSettings.AiServices), nameof(AppSettings.AiModels) };
			JocysCom.ClassLibrary.Runtime.Attributes.ResetPropertiesToDefault(AppSettings, false, exclude);
		}

		private static void AppData_OnValidateData(object sender, SettingsData<AppData>.SettingsDataEventArgs e)
		{
			var data = sender as ISettingsData;
			if (e.Items.Count == 0)
			{
				e.Items.Add(new AppData());
				data.IsSavePending = true;
			}
			var appSettings = e.Items.FirstOrDefault();
			// Always refresh plugins.
			var newPluginsList = Engine.AppData.RefreshPlugins(appSettings.Plugins);
			ClassLibrary.Collections.CollectionsHelper.Synchronize(newPluginsList, appSettings.Plugins);
			// Check app services.
			if (appSettings.AiServices == null || appSettings.AiServices.Count == 0)
			{
				appSettings.AiServices = Engine.AppData.GetDefaultAiServices();
				data.IsSavePending = true;
			}
			if (appSettings.AiModels == null || appSettings.AiModels.Count == 0)
			{
				appSettings.AiModels = Engine.AppData.GetDefaultOpenAiModels();
				data.IsSavePending = true;
			}
			if (appSettings.AiServiceData == null)
			{
				var d = new AiServiceSettings();
				d.ListSelection = new List<string> { Engine.AppData.OpenAiName };
				appSettings.AiServiceData = d;
			}
			// Fix OpenAI Id
			var openAiService = appSettings.AiServices.FirstOrDefault(x => x.Name == Engine.AppData.OpenAiName);
			if (openAiService != null)
				openAiService.Id = Engine.AppData.OpenAiServiceId;
			// Fix models.
			var emptyServiceGuid = AppHelper.GetGuid(nameof(AiService), "");
			var models = appSettings.AiModels.Where(x => x.AiServiceId == emptyServiceGuid).ToArray();
			foreach (var model in models)
				model.AiServiceId = Engine.AppData.OpenAiServiceId;
		}

		private static bool FixTempalteItems(IList<TemplateItem> items)
		{
			// Fix items with wrong AI Serivce Id (empty service name).
			var emptyServiceGuid = AppHelper.GetGuid(nameof(AiService), "");
			var tasks = items.Where(x => x.AiServiceId == emptyServiceGuid).ToArray();
			foreach (var task in tasks)
				task.AiServiceId = Engine.AppData.OpenAiServiceId;
			return tasks.Count() > 0;
		}

		private static void Templates_OnValidateData(object sender, SettingsData<TemplateItem>.SettingsDataEventArgs e)
		{
			var data = (SettingsData<TemplateItem>)sender;
			if (e.Items.Count == 0)
			{
				var asm = typeof(Global).Assembly;
				var keys = asm.GetManifestResourceNames()
					.Where(x => x.Contains("Resources.Templates"))
					.ToList();
				foreach (var key in keys)
				{
					var bytes = Helper.GetResource<byte[]>(key, asm);
					var item = data.DeserializeItem(bytes, false);
					e.Items.Add(item);
				}
				data.IsSavePending = true;
				return;
			}
			// Check reserved templates used for some automation.
			var rItem = e.Items.FirstOrDefault(x => x.Name == ClientHelper.GenerateTitleTaskName);
			if (rItem == null)
			{
				var asm = typeof(Global).Assembly;
				var keys = asm.GetManifestResourceNames()
					.Where(x => x.Contains(ClientHelper.GenerateTitleTaskName))
					.ToList();
				foreach (var key in keys)
				{
					var bytes = Helper.GetResource<byte[]>(key, asm);
					var item = data.DeserializeItem(bytes, false);
					e.Items.Add(item);
				}
				data.IsSavePending = true;
			}
			data.IsSavePending |= FixTempalteItems(e.Items);
		}

		private static void Tasks_OnValidateData(object sender, SettingsData<TemplateItem>.SettingsDataEventArgs e)
		{
			var sd = (SettingsData<TemplateItem>)sender;
			if (e.Items.Count == 0)
			{
				var asm = typeof(Global).Assembly;
				var keys = asm.GetManifestResourceNames()
					.Where(x => x.Contains("Resources.Templates"))
					.OrderBy(x => x)
					.ToList();
				foreach (var key in keys)
				{
					// Add chat and grammar templates.
					if (key.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) > -1 ||
						key.IndexOf("Grammar", StringComparison.OrdinalIgnoreCase) > -1)
					{
						var bytes = Helper.GetResource<byte[]>(key, asm);
						var item = sd.DeserializeItem(bytes, false);
						var task = item.Copy(true);
						task.ShowInstructions = false;
						e.Items.Add(task);
					}
				}
				sd.IsSavePending = true;
			}
			sd.IsSavePending |= FixTempalteItems(e.Items);
		}

		public static void ClearItems()
		{
			Templates.Items.Clear();
			Tasks.Items.Clear();
		}

		public static MainControl MainControl;

		public static bool IsVsExtesion { get; set; }
		public static string VsExtensionFeatureMessage = "This feature is only available when the application is run as an extension in Visual Studio.";

	}
}
