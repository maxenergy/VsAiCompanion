﻿using JocysCom.ClassLibrary.Controls;
using JocysCom.VS.AiCompanion.Engine;
using JocysCom.VS.AiCompanion.Plugins.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JocysCom.VS.AiCompanion.Extension
{
	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// Guid decorating this class must be UNIQUE. If two extensions have the same GUID for a tool window,
	/// Visual Studio might not be able to correctly identify and instantiate the tool windows, leading to conflicts.
	/// </remarks>
	[Guid("9a8e8fad-eb2a-f9f6-abb1-1faa8d7fdec2")]
	public class MainWindow : ToolWindowPane
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		public MainWindow() : base(null)
		{
			// Subscribe to the AssemblyResolve event. This event is triggered when .NET runtime fails to find an assembly,
			// giving you an opportunity to provide the assembly using custom logic.
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			// Set caption.
			var assembly = Assembly.GetExecutingAssembly();
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
			Caption = product;
			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			ControlsHelper.InitInvokeContext();
			Global.LoadSettings();
			Global.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
			// Get or Set multiple documents.
			Global._SolutionHelper = new SolutionHelper();
			VisualStudioHelper.Current = Global._SolutionHelper;
			Global.IsVsExtesion = true;
			Global.GetClipboard = AppHelper.GetClipboard;
			Global.SetClipboard = AppHelper.SetClipboard;
			Global.GetEnvironmentProperties = AppHelper.GetEnvironmentProperties;
			//Global.GetEnvironmentProperties = SolutionHelper.GetEnvironmentProperties;
			//Global.GetReservedProperties = SolutionHelper.GetReservedProperties;
			//Global.GetOtherProperties = SolutionHelper.GetOtherProperties;
			// Create controls.
			var control = new Engine.MainControl();
			control.Unloaded += Control_Unloaded;
			Global.MainControl = control;
			Content = control;
		}

		private void Control_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			Global.SaveSettings();
		}

		/// <summary>
		/// Helps .NET to find assemblies (*.DLLs) from the extension's installed folder.
		/// This method is invoked when .NET runtime fails to find an assembly it's looking for.
		/// </summary>
		/// <param name="sender">The source of the event, in this case, the current AppDomain.</param>
		/// <param name="e">The arguments for the resolve event, containing the name of the assembly that failed to load.</param>
		/// <returns>The resolved assembly, or null if the assembly could not be found.</returns>
		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
		{
			// Extract the simple name of the assembly from the full assembly name.
			// Full assembly names are in the form 'SimpleName, Version, Culture, PublicKeyToken'
			var dllName = new AssemblyName(e.Name).Name + ".dll";
			// Get the path where the extension is installed (i.e., the path of the currently executing assembly).
			var assemblyLocation = Assembly.GetExecutingAssembly().Location;
			var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
			// Construct the full path to the assembly using the directory of the currently executing assembly and the dll name.
			var assemblyPath = Path.Combine(assemblyDirectory, dllName);
			// If the assembly exists at the constructed path, load and return it.
			if (File.Exists(assemblyPath))
				return Assembly.LoadFrom(assemblyPath);
			// If the assembly was not found in the specified directory, return null.
			// This lets the default assembly resolution process continue and .NET runtime will throw FileNotFoundException.
			return null;
		}

		private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
		}

	}
}
