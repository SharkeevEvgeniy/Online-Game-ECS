#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniIT.Snipe.Editor
{
#if !UNITY_CLOUD_BUILD
	[InitializeOnLoad]
#endif
	public static class SnipeAutoUpdater
	{
		private const string PREF_AUTO_UPDATE_ENABLED = "Snipe.AutoUpdateEnabled";
		private const string PREF_LAST_UPDATE_CHECK_ID = "Snipe.LastUpdateCheckId";
		private const string PREF_LAST_UPDATE_CHECK_TS = "Snipe.LastUpdateCheckTS";
		
		private const string MENU_AUTO_UPDATE_ENABLED = "Snipe/Check for Updates Automatically";
		
		private static bool mProcessing = false;

		public static bool AutoUpdateEnabled
		{
			get => EditorPrefs.GetBool(PREF_AUTO_UPDATE_ENABLED, true);
			set => EditorPrefs.SetBool(PREF_AUTO_UPDATE_ENABLED, value);
		}

		[MenuItem(MENU_AUTO_UPDATE_ENABLED, false)]
		static void SnipeAutoUpdaterCheckMenu()
		{
			AutoUpdateEnabled = !AutoUpdateEnabled;
			Menu.SetChecked(MENU_AUTO_UPDATE_ENABLED, AutoUpdateEnabled);

			ShowNotificationOrLog(AutoUpdateEnabled ? "Snipe auto update enabled" : "Snipe auto update disabled");
			
			if (AutoUpdateEnabled)
			{
				CheckUpdateAvailable();
			}
		}

		// The menu won't be gray out, we use this validate method for update check state
		[MenuItem(MENU_AUTO_UPDATE_ENABLED, true)]
		static bool SnipeAutoUpdaterCheckMenuValidate()
		{
			Menu.SetChecked(MENU_AUTO_UPDATE_ENABLED, AutoUpdateEnabled);
			return true;
		}
		
		static SnipeAutoUpdater()
		{
#if !UNITY_CLOUD_BUILD
			Run();
#endif
		}
		
		//[MenuItem("Snipe/Run Autoupdater")]
		public static void Run()
		{
			if (AutoUpdateEnabled)
			{
				bool check_needed = EditorPrefs.GetInt(PREF_LAST_UPDATE_CHECK_ID, 0) != (int)EditorAnalyticsSessionInfo.id;
				if (!check_needed)
				{
					var check_ts = EditorPrefs.GetInt(PREF_LAST_UPDATE_CHECK_TS, 0);
					var passed = DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(check_ts).UtcDateTime;
					check_needed = passed.TotalHours >= 12;
				}
				
				if (check_needed)
				{
					CheckUpdateAvailable();
				}
			}
		}

		private static void ShowNotificationOrLog(string msg)
		{
			if (Resources.FindObjectsOfTypeAll<SceneView>().Length > 0)
				EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent(msg));
			else
				Debug.Log($"[SnipeAutoUpdater] {msg}"); // When there's no scene view opened, we just print a log
		}
		
		public static async void CheckUpdateAvailable()
		{
			if (mProcessing)
				return;
			mProcessing = true;
			
			Debug.Log("[SnipeAutoUpdater] CheckUpdateAvailable");
			
			await SnipeUpdater.FetchVersionsList();
			
			if (SnipeUpdater.SnipePackageVersions != null && SnipeUpdater.SnipePackageVersions.Length > 0)
			{
				string current_version_code = SnipeUpdater.CurrentSnipePackageVersionIndex >= 0 ?
					SnipeUpdater.SnipePackageVersions[SnipeUpdater.CurrentSnipePackageVersionIndex] :
					"unknown";
					
				Debug.Log($"[SnipeAutoUpdater] Current version (detected): {current_version_code}");
				
				if (TryParseVersion(current_version_code, out int[] version))
				{
					string newer_version_code = null;
					int[] newer_version = null;
					
					for (int i = 0; i < SnipeUpdater.SnipePackageVersions.Length; i++)
					{
						if (i == SnipeUpdater.CurrentSnipePackageVersionIndex)
							continue;
						
						string ver_name = SnipeUpdater.SnipePackageVersions[i];
						if (TryParseVersion(ver_name, out int[] ver) && CheckVersionGreater(newer_version ?? version, ver))
						{
							newer_version_code = ver_name;
							newer_version = ver;
						}
					}
					
					if (!string.IsNullOrEmpty(newer_version_code))
					{
						Debug.Log($"[SnipeAutoUpdater] A newer version found: {newer_version_code}");
						
						if (EditorUtility.DisplayDialog("Snipe Auto Updater",
							$"Snipe {newer_version_code}\n\nNewer version found.\n(Installed version is {current_version_code})",
							"Update now", "Dismiss"))
						{
							SnipeUpdater.InstallSnipePackage(newer_version_code);
						}
					}
				}
			}
			
			EditorPrefs.SetInt(PREF_LAST_UPDATE_CHECK_ID, (int)EditorAnalyticsSessionInfo.id);
			EditorPrefs.SetInt(PREF_LAST_UPDATE_CHECK_TS, (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
			
			mProcessing = false;
			
			SnipeToolsAutoUpdater.CheckUpdateAvailable();
		}
		
		internal static bool TryParseVersion(string version_string, out int[] version)
		{
			string[] version_code = version_string.Split('.');
			if (version_code != null && version_code.Length == 3)
			{
				version = new int[version_code.Length];
				bool parsing_failed = false;
				for (int i = 0; i < version_code.Length; i++)
				{
					if (!int.TryParse(version_code[i], out version[i]))
					{
						parsing_failed = true;
						break;
					}
				}
				
				if (!parsing_failed)
				{
					return true;
				}
			}
			
			version = null;
			return false;
		}
		
		internal static bool CheckVersionGreater(int[] current_version, int[] check_version)
		{
			if (check_version[0] < current_version[0])
				return false;
			if (check_version[0] > current_version[0])
				return true;
			if (check_version[1] < current_version[1])
				return false;
			if (check_version[1] > current_version[1])
				return true;
			if (check_version[2] < current_version[2])
				return false;
			return (check_version[2] > current_version[2]);
		}
	}

}
#endif