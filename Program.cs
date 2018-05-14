using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WoWCacheCleaner
{
	static class Program
	{
		private static readonly String APPLICATION_NAME = "WoWCacheCleaner";

		private static bool debugMode = false;
		private static NotifyIcon notifyIcon;
		private static RegistryKey startupRegistry;

		[STAThread]
		static void Main()
		{
			// If the application is already running display an error message and terminate.
			if(Process.GetProcessesByName(APPLICATION_NAME).Length > 1)
			{
				MessageBox.Show(APPLICATION_NAME + " is already running.", APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			
			startupRegistry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			notifyIcon = new NotifyIcon();
			notifyIcon.Text = APPLICATION_NAME;
			notifyIcon.Icon = Properties.Resources.WoW_icon;
			notifyIcon.Visible = true;

			ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.ShowImageMargin = false;
			contextMenuStrip.ShowCheckMargin = true;
			ToolStripMenuItem loadOnStartupButton = new ToolStripMenuItem();
			loadOnStartupButton.Text = "Load on Startup";
			loadOnStartupButton.Checked = GetLoadOnStartup();
			loadOnStartupButton.CheckOnClick = true;
			loadOnStartupButton.CheckedChanged += HandleStartupCheckChanged;
			contextMenuStrip.Items.Add(loadOnStartupButton);
			ToolStripMenuItem debugModeButton = new ToolStripMenuItem();
			debugModeButton.Text = "Debug Mode";
			debugModeButton.CheckOnClick = true;
			debugModeButton.CheckedChanged += HandleDebugCheckChanged;
			contextMenuStrip.Items.Add(debugModeButton);
			ToolStripMenuItem menuItem = new ToolStripMenuItem();
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(menuItem);
			menuItem.Text = "Exit";
			menuItem.Click += Exit;

			notifyIcon.ContextMenuStrip = contextMenuStrip;

			BackgroundWorker wowProcessListener = new BackgroundWorker();
			wowProcessListener.DoWork += CheckWowProcess;
			wowProcessListener.RunWorkerAsync();

			Application.Run();
		}

		static void Exit(object Sender, EventArgs e)
		{
			notifyIcon.Visible = false;
			Application.Exit();
		}

		static void HandleStartupCheckChanged(object sender, EventArgs e)
		{
			ToolStripMenuItem button = sender as ToolStripMenuItem;
			SetLoadOnStartup(button.Checked);
		}

		static void HandleDebugCheckChanged(object sender, EventArgs e)
		{
			ToolStripMenuItem button = sender as ToolStripMenuItem;
			debugMode = button.Checked;
		}

		static bool GetLoadOnStartup()
		{
			return startupRegistry.GetValue(APPLICATION_NAME) != null;
		}

		static void SetLoadOnStartup(bool loadOnStartup)
		{
			if(loadOnStartup)
			{
				startupRegistry.SetValue(APPLICATION_NAME, "\"" + Application.ExecutablePath + "\"");
				DebugMessage("Load on startup enabled. Set to " + Application.ExecutablePath);
			}
			else
			{
				startupRegistry.DeleteValue(APPLICATION_NAME);
				DebugMessage("Load on startup disabled.");
			}
		}

		/// <summary>
		/// Checks for a running WoW process. If one is found the thread blocks until the process exits, at which point it deletes the associated WoW installation's cache folder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void CheckWowProcess(object sender, DoWorkEventArgs e)
		{
			while(true)
			{
				Process[] wow32Processes = Process.GetProcessesByName("Wow");
				Process[] wow64Processes = Process.GetProcessesByName("Wow-64");
				List<Process> wowProcesses = new List<Process>();
				wowProcesses.AddRange(wow32Processes);
				wowProcesses.AddRange(wow64Processes);

				if(wowProcesses.Count > 0)
				{
					Process wowProcess = wowProcesses[0];
					FileInfo info = new FileInfo(wowProcesses[0].MainModule.FileName);
					DirectoryInfo cacheDirectory = new DirectoryInfo(info.Directory.FullName + "\\Cache");
					DebugMessage("Detected WoW process.");

					wowProcess.WaitForExit();
					if(cacheDirectory.Exists)
					{
						cacheDirectory.Delete(true);
						DebugMessage("Deleted cache directory: " + cacheDirectory.FullName);
					}
				}

				Thread.Sleep(60 * 1000);
			}
		}

		/// <summary>
		/// Create a notification icon toast containing a debug message if debug mode is enabled.
		/// </summary>
		/// <param name="message"></param>
		static void DebugMessage(String message)
		{
			if(debugMode)
			{
				notifyIcon.ShowBalloonTip(1000, APPLICATION_NAME + " Debug", message, ToolTipIcon.Info);
			}
		}
	}
}
