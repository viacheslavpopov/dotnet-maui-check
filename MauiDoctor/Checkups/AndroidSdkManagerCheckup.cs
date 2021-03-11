﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;

namespace MauiDoctor.Checkups
{
	public class AndroidSdkManagerCheckup : Checkup
	{
		public AndroidSdkManagerCheckup(string overrideSdkRoot = null)
		{
			OverrideSdkRoot = overrideSdkRoot;
		}

		public override string Id => "androidsdk";

		public override string Title => "Android SDK";

		public DirectoryInfo SelectedHome { get; private set; }

		public string OverrideSdkRoot { get; }

		public override Task<Diagonosis> Examine()
		{
			try
			{
				var homes = string.IsNullOrEmpty(OverrideSdkRoot)
					? Android.GetHomes()
					: new[] { new DirectoryInfo(OverrideSdkRoot) };

				foreach (var home in homes)
				{
					try
					{
						var android = new Android(home);
						var v = android.GetSdkManagerVersion();
						if (v != default)
						{
							if (SelectedHome == default)
							{
								SelectedHome = home;
								ReportStatus($":check_mark: [bold darkgreen]{home.FullName} ({v}) found.[/]");
							}
							else
							{
								ReportStatus($":check_mark: {home.FullName} ({v}) also found.");
							}
						}
						else
						{
							ReportStatus($":warning: {home.FullName} invalid.");
						}
					}
					catch
					{
						ReportStatus($":warning: {home.FullName} invalid.");
					}
				}

				if (SelectedHome != default)
					return Task.FromResult(Diagonosis.Ok(this));
			} catch { }

			return Task.FromResult(new Diagonosis(Status.Error, this, "Failed to find Android SDK.", new Prescription("Install Android SDK Manager",
				new ActionRemedy((r, ct) =>
				{
					if (SelectedHome != null)
					{
						if (SelectedHome.Exists)
						{
							try { SelectedHome.Delete(true); }
							catch (UnauthorizedAccessException)
							{
								throw new Exception("Fix requires running with adminstrator privileges.  Try opening a terminal as administrator and running maui-doctor again.");
							}
							catch (Exception ex)
							{
								throw new Exception("Failed to delete existing Android SDK: " + ex.Message);
							}

							try { SelectedHome.Create(); }
							catch { }
						}
					}
					else
					{
						SelectedHome = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"));
						try { SelectedHome.Create(); }
						catch { }
					}

					var sdk = new AndroidSdk.AndroidSdkManager(SelectedHome);

					sdk.Acquire();

					return Task.CompletedTask;
				}))));
		}
	}
}
