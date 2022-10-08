#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace MiniIT.Snipe.Editor
{
#if !UNITY_CLOUD_BUILD
	[InitializeOnLoad]
#endif
	public static class SnipeToolsAutoUpdater
	{
		internal const string GIT_API_BASE_URL = "https://api.github.com/repos/Mini-IT/SnipeToolsUnityPackage/";
		
		private static bool mProcessing = false;
		private static List<string> mPackageVersions;
		private static int mCurrentPackageVersionIndex = -1;
		private static ListRequest mPackageListRequest;
		
		//[MenuItem("Snipe/Check for SnipeTools Update")]
		public static async void CheckUpdateAvailable()
		{
#if UNITY_CLOUD_BUILD
			return;
#endif
			
			if (mProcessing)
				return;
			mProcessing = true;
			
			Debug.Log("[SnipeToolsAutoUpdater] CheckUpdateAvailable");
			
			await FetchVersionsList();
			
			if (mPackageVersions != null && mPackageVersions.Count > 0)
			{
				string current_version_code = mCurrentPackageVersionIndex >= 0 ?
					mPackageVersions[mCurrentPackageVersionIndex] :
					"unknown";
					
				Debug.Log($"[SnipeToolsAutoUpdater] Current version (detected): {current_version_code}");
				
				if (SnipeAutoUpdater.TryParseVersion(current_version_code, out int[] version))
				{
					string newer_version_code = null;
					int[] newer_version = null;
					
					for (int i = 0; i < mPackageVersions.Count; i++)
					{
						if (i == mCurrentPackageVersionIndex)
							continue;
						
						string ver_name = mPackageVersions[i];
						if (SnipeAutoUpdater.TryParseVersion(ver_name, out int[] ver) && SnipeAutoUpdater.CheckVersionGreater(newer_version ?? version, ver))
						{
							newer_version_code = ver_name;
							newer_version = ver;
						}
					}
					
					if (!string.IsNullOrEmpty(newer_version_code))
					{
						Debug.Log($"[SnipeToolsAutoUpdater] A newer version found: {newer_version_code}");
						
						if (EditorUtility.DisplayDialog("Snipe Tools Auto Updater",
							$"Snipe Tools {newer_version_code}\n\nNewer version of Snipe Tools found\n(Installed version is {current_version_code})",
							"Update now", "Dismiss"))
						{
							SnipeUpdater.InstallSnipeToolsPackage();
						}
					}
				}
			}
			
			mProcessing = false;
		}
		
		public static async Task FetchVersionsList()
		{
			UnityEngine.Debug.Log("[SnipeToolsAutoUpdater] FetchVersionsList - GetBranchesList - start");
			
			var branches = await SnipeUpdater.RequestList<GitHubBranchesListWrapper>(GIT_API_BASE_URL, "branches");
			var tags = await SnipeUpdater.RequestList<GitHubTagsListWrapper>(GIT_API_BASE_URL, "tags");

			int items_count = (branches?.items?.Count ?? 0) + (tags?.items?.Count ?? 0);
			mPackageVersions = new List<string>(items_count);

			if (branches?.items != null)
			{
				foreach (var item in branches.items)
				{
					mPackageVersions.Add(item.name);
				}
			}
			
			if (tags?.items != null)
			{
				foreach (var item in tags.items)
				{
					mPackageVersions.Add(item.name);
				}
			}
			
			UnityEngine.Debug.Log("SnipeToolsAutoUpdater] FetchVersionsList - GetBranchesList - done");

			UnityEngine.Debug.Log("SnipeToolsAutoUpdater] FetchVersionsList - Check installed packages");

			mPackageListRequest = UnityEditor.PackageManager.Client.List(false, false);
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.update += OnEditorUpdate;
			
			while (mPackageListRequest != null)
			{
				await Task.Delay(10);
			}
		}
		
		private static void OnEditorUpdate()
		{
			if (mPackageListRequest != null)
			{
				if (mPackageListRequest.IsCompleted)
				{
					if (mPackageListRequest.Status == StatusCode.Success)
					{
						foreach (var item in mPackageListRequest.Result)
						{
							if (item.name == SnipeUpdater.TOOLS_PACKAGE_NAME)
							{
								UnityEngine.Debug.Log($"[SnipeToolsAutoUpdater] found package: {item.name} {item.version} {item.packageId}");

								int index = item.packageId.LastIndexOf(".git#");
								if (index > 0)
								{
									string package_version = item.packageId.Substring(index + ".git#".Length);
									mCurrentPackageVersionIndex = mPackageVersions.IndexOf(package_version);
								}
								else
								{
									mCurrentPackageVersionIndex = mPackageVersions.IndexOf(item.version);
								}
								break;
							}
						}
					}
					else if (mPackageListRequest.Status >= StatusCode.Failure)
					{
						Debug.Log($"[SnipeToolsAutoUpdater] Search failed : {mPackageListRequest.Error.message}");
					}

					mPackageListRequest = null;
					EditorApplication.update -= OnEditorUpdate;
				}
			}
			else
			{
				EditorApplication.update -= OnEditorUpdate;
			}
		}
	}

}
#endif