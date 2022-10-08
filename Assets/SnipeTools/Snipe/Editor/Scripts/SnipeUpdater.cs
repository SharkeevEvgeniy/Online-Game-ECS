#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MiniIT.Snipe.Editor
{

public class SnipeUpdater : EditorWindow
{
	internal const string GIT_API_BASE_URL = "https://api.github.com/repos/Mini-IT/SnipeUnityPackage/";
	
	internal const string SNIPE_PACKAGE_NAME = "com.miniit.snipe.client";
	internal const string SNIPE_PACKAGE_BASE_URL = "https://github.com/Mini-IT/SnipeUnityPackage.git";
	
	internal const string TOOLS_PACKAGE_NAME = "com.miniit.snipe.tools";
	internal const string TOOLS_PACKAGE_BASE_URL = "https://github.com/Mini-IT/SnipeToolsUnityPackage.git";

	private static ListRequest mPackageListRequest;
	private static AddRequest mPackageAddRequest;

	private static GitHubBranchesListWrapper mBranches;
	private static GitHubTagsListWrapper mTags;

	public static string[] SnipePackageVersions { get; private set; }
	public static int CurrentSnipePackageVersionIndex { get; private set; } = -1;
	private static int mSelectedSnipePackageVersionIndex;

	[MenuItem("Snipe/Updater...")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(SnipeUpdater));
	}
	
	private void OnEnable()
	{
		if (mBranches == null || mTags == null)
		{
			_ = FetchVersionsList();
		}
	}

	void OnGUI()
	{
		if (mPackageListRequest != null || mBranches == null || mTags == null || SnipePackageVersions == null)
		{
			EditorGUILayout.LabelField("Fetching... please wait...");
		}
		else if (mPackageAddRequest != null)
		{
			EditorGUILayout.LabelField("Installing... please wait...");
		}
		else
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Update Snipe Tools Package"))
			{
				var request = InstallSnipeToolsPackage();
				while (!request.IsCompleted)
				{
				}
				if (request.Status == StatusCode.Success)
				{
					this.Close();
				}
			}
			
			if (GUILayout.Button("Fetch Versions"))
			{
				_ = FetchVersionsList();
			}
			GUILayout.EndHorizontal();

			if (SnipePackageVersions != null)
			{
				string current_version_name = CurrentSnipePackageVersionIndex >= 0 ? SnipePackageVersions[CurrentSnipePackageVersionIndex] : "unknown";
				EditorGUILayout.LabelField($"Current version (detected): {current_version_name}");
				
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Version: ");
				mSelectedSnipePackageVersionIndex = EditorGUILayout.Popup(mSelectedSnipePackageVersionIndex, SnipePackageVersions);
				
				GUILayout.FlexibleSpace();
				if (mSelectedSnipePackageVersionIndex >= 0 && GUILayout.Button("Switch / Update"))
				{
					string selected_version = SnipePackageVersions[mSelectedSnipePackageVersionIndex];
					string version_id = (selected_version == "master") ? "" : $"{selected_version}";

					InstallSnipePackage(version_id);
				}
				GUILayout.EndHorizontal();
			}
		}
	}
	
	internal static void InstallSnipePackage(string version)
	{
		string version_suffix = string.IsNullOrEmpty(version) ? "" : $"#{version}";
		mPackageAddRequest = Client.Add($"{SNIPE_PACKAGE_BASE_URL}{version_suffix}");
		EditorApplication.update -= OnEditorUpdate;
		EditorApplication.update += OnEditorUpdate;
	}
	
	public static AddRequest InstallSnipeToolsPackage()
	{
		return Client.Add($"{TOOLS_PACKAGE_BASE_URL}");
	}

	public static async Task FetchVersionsList()
	{
		mBranches = null;
		mTags = null;

		UnityEngine.Debug.Log("[SnipeUpdater] GetBranchesList - start");
		
		// UnityEngine.Debug.Log($"[SnipeUpdater] Fetching brunches list");
		
		mBranches = await RequestList<GitHubBranchesListWrapper>(GIT_API_BASE_URL, "branches");
		mTags = await RequestList<GitHubTagsListWrapper>(GIT_API_BASE_URL, "tags");

		int items_count = (mBranches?.items?.Count ?? 0) + (mTags?.items?.Count ?? 0);
		SnipePackageVersions = new string[items_count];
		mSelectedSnipePackageVersionIndex = 0;

		int i = 0;
		if (mBranches?.items != null)
		{
			foreach (var item in mBranches.items)
			{
				// UnityEngine.Debug.Log($"[SnipeUpdater] {item.name}");
				SnipePackageVersions[i++] = item.name;
			}
		}
		
		// UnityEngine.Debug.Log($"[SnipeUpdater] Fetching tags list");
		
		if (mTags?.items != null)
		{
			foreach (var item in mTags.items)
			{
				// UnityEngine.Debug.Log($"[SnipeUpdater] {item.name}");
				SnipePackageVersions[i++] = item.name;
			}
		}
		
		UnityEngine.Debug.Log("[SnipeUpdater] GetBranchesList - done");

		UnityEngine.Debug.Log("[SnipeUpdater] Check installed packages");

		mPackageListRequest = UnityEditor.PackageManager.Client.List(false, false);
		EditorApplication.update -= OnEditorUpdate;
		EditorApplication.update += OnEditorUpdate;
		
		while (mPackageListRequest != null)
		{
			await Task.Delay(10);
		}
	}
	
	internal static async Task<WrapperType> RequestList<WrapperType>(string git_base_url, string url_suffix) where WrapperType : new()
	{
		UnityEngine.Debug.Log("[SnipeUpdater] RequestList - start - " + url_suffix);
		
		var list_wrapper = new WrapperType();
		
		using (var web_client = new HttpClient())
		{
			web_client.DefaultRequestHeaders.UserAgent.ParseAdd("SnipeUpdater");
			var response = await web_client.GetAsync($"{git_base_url}{url_suffix}");
			var content = await response.Content.ReadAsStringAsync();
			
			// UnityEngine.Debug.Log($"[SnipeUpdater] {content}");
			
			UnityEditor.EditorJsonUtility.FromJsonOverwrite("{\"items\":" + content + "}", list_wrapper);
		}
		
		UnityEngine.Debug.Log("[SnipeUpdater] RequestList - done");
		return list_wrapper;
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
						if (item.name == SNIPE_PACKAGE_NAME)
						{
							UnityEngine.Debug.Log($"[SnipeUpdater] found package: {item.name} {item.version} {item.packageId}");

							int index = item.packageId.LastIndexOf(".git#");
							if (index > 0)
							{
								string package_version = item.packageId.Substring(index + ".git#".Length);
								CurrentSnipePackageVersionIndex = mSelectedSnipePackageVersionIndex = Array.IndexOf(SnipePackageVersions, package_version);
							}
							else
							{
								CurrentSnipePackageVersionIndex = mSelectedSnipePackageVersionIndex = Array.IndexOf(SnipePackageVersions, item.version);
							}
							break;
						}
					}
				}
				else if (mPackageListRequest.Status >= StatusCode.Failure)
				{
					Debug.Log($"[SnipeUpdater] Search failed : {mPackageListRequest.Error.message}");
				}

				mPackageListRequest = null;
				EditorApplication.update -= OnEditorUpdate;
			}
		}
		else if (mPackageAddRequest != null)
		{
			if (mPackageAddRequest.IsCompleted)
			{
				if (mPackageAddRequest.Status == StatusCode.Success)
					Debug.Log("[SnipeUpdater] Installed: " + mPackageAddRequest.Result.packageId);
				else if (mPackageAddRequest.Status >= StatusCode.Failure)
					Debug.Log($"[SnipeUpdater] Installed error: {mPackageAddRequest.Error.message}");

				mPackageAddRequest = null;
				EditorApplication.update -= OnEditorUpdate;
			}
		}
		else
		{
			EditorApplication.update -= OnEditorUpdate;
		}
	}
}

#pragma warning disable 0649

[System.Serializable]
internal class GitHubTagsListWrapper
{
	public List<GitHubTagsListItem> items;
}

[System.Serializable]
internal class GitHubBranchesListWrapper
{
	public List<GitHubBranchesListItem> items;
}

[System.Serializable]
internal class GitHubTagsListItem
{
	public string name;
	public string node_id;
	// public GitHubCommitData commit;
}

[System.Serializable]
internal class GitHubBranchesListItem
{
	public string name;
	public bool @projected;
	// public GitHubCommitData commit;
}

//[System.Serializable]
//internal class GitHubCommitData
//{
//	public int sha;
//	public string url;
//}

#pragma warning restore 0649

}

#endif // UNITY_EDITOR