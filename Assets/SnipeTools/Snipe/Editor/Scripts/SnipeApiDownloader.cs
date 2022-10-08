#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace MiniIT.Snipe.Editor
{
	public class SnipeApiDownloader : EditorWindow
	{
		private static readonly string[] SNIPE_VERSIONS = new string[] { "V5", "V6" };

		private int mProjectId = 0;
		private string mDirectoryPath;
		private string mLogin;
		private string mPassword;
		private string mSnipeVersionSuffix = SNIPE_VERSIONS[1]; //"V6";
		private bool mGetTablesList = true;

		private static string mPrefsPrefix;

		private string mToken;
		private string[] mProjectsList;
		private int mSelectedProjectIndex = -1;
		
		private static string mAuthKey;

		public static string RefreshPrefsPrefix()
		{
			if (string.IsNullOrEmpty(mPrefsPrefix))
			{
				var hash = System.Security.Cryptography.MD5.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(Application.dataPath));
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < hash.Length; i++)
				{
					builder.Append(hash[i].ToString("x2"));
				}
				mPrefsPrefix = builder.ToString();
			}

			return mPrefsPrefix;
		}

		[MenuItem("Snipe/Download SnipeApi...")]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow<SnipeApiDownloader>("SnipeApi");
		}

		protected void OnEnable()
		{
			RefreshPrefsPrefix();
			
			LoadAuthKey();
			
			mLogin = GetLogin();
			mPassword = GetPassword();
			
			if (mProjectId <= 0)
				mProjectId = EditorPrefs.GetInt($"{mPrefsPrefix}_SnipeApiDownloader.project_id", mProjectId);
			mSnipeVersionSuffix = EditorPrefs.GetString($"{mPrefsPrefix}_SnipeApiDownloader.snipe_version_suffix", mSnipeVersionSuffix);

			string[] results = AssetDatabase.FindAssets("SnipeApi");
			if (results != null && results.Length > 0)
			{
				string path = AssetDatabase.GUIDToAssetPath(results[0]);
				if (path.EndsWith("SnipeApi.cs"))
				{
					// Application.dataPath edns with "Assets"
					// path starts with "Assets" and ends with "SnipeApi.cs"
					mDirectoryPath = Application.dataPath + path.Substring(6, path.Length - 17);
				}
			}
			
			if (string.IsNullOrEmpty(mDirectoryPath))
			{
				mDirectoryPath = Application.dataPath;
			}
			
			if (SnipeAutoUpdater.AutoUpdateEnabled)
				SnipeAutoUpdater.CheckUpdateAvailable();
		}

		protected void OnDisable()
		{
			SaveLoginAndPassword();
			if (mProjectId > 0)
				EditorPrefs.SetInt($"{mPrefsPrefix}_SnipeApiDownloader.project_id", mProjectId);
			EditorPrefs.SetString($"{mPrefsPrefix}_SnipeApiDownloader.snipe_version_suffix", mSnipeVersionSuffix);
		}
		
		private static string GetLogin()
		{
			return EditorPrefs.GetString($"{mPrefsPrefix}_SnipeApiDownloader.login");
		}
		
		private static string GetPassword()
		{
			return EditorPrefs.GetString($"{mPrefsPrefix}_SnipeApiDownloader.password");
		}
		
		private void SaveLoginAndPassword()
		{
			string key_login = $"{mPrefsPrefix}_SnipeApiDownloader.login";
			string key_password = $"{mPrefsPrefix}_SnipeApiDownloader.password";
			if (!string.IsNullOrEmpty(mLogin) && !string.IsNullOrEmpty(mPassword) || EditorPrefs.HasKey(key_login))
			{
				EditorPrefs.SetString(key_login, mLogin);
				EditorPrefs.SetString(key_password, mPassword);
			}
		}
		
		private string GetAuthKeyFilePath()
		{
			return Path.Combine(Application.dataPath, "..", "snipe_api_key");
		}
		
		private void LoadAuthKey()
		{
			string path = GetAuthKeyFilePath();
			if (File.Exists(path))
			{
				string content = File.ReadAllText(path);
				SetAuthKey(content);
			}
		}
		
		private void SaveAuthKey()
		{
			string path = GetAuthKeyFilePath();
			if (string.IsNullOrEmpty(mAuthKey))
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			else
			{
				File.WriteAllText(path, mAuthKey);
			}
		}
		
		private void SetAuthKey(string value)
		{
			string project_id = null;
			if (!string.IsNullOrEmpty(value))
			{
				string[] parts = value.Split('-');
				if (parts.Length > 3 && parts[0] == "api")
					project_id = parts[1];
			}
			
			if (!string.IsNullOrEmpty(project_id))
			{
				mAuthKey = value;
				int.TryParse(project_id, out mProjectId);
			}
			else
			{
				mAuthKey = null;
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();
			
			EditorGUIUtility.labelWidth = 100;
			
			string auth_key = EditorGUILayout.TextField("API Key", mAuthKey);
			if (auth_key != mAuthKey)
			{
				SetAuthKey(auth_key);
				SaveAuthKey();
			}
			
			if (string.IsNullOrEmpty(mAuthKey))
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("");
				EditorGUILayout.LabelField("OR");
				GUILayout.EndHorizontal();
				
				string login = EditorGUILayout.TextField("Login", mLogin);
				if (login != mLogin)
				{
					mLogin = login;
					SaveLoginAndPassword();
				}
				string password = EditorGUILayout.PasswordField("Password", mPassword);
				if (password != mPassword)
				{
					mPassword = password;
					SaveLoginAndPassword();
				}
			}
			
			EditorGUILayout.Space();
			
			bool auth_valid = (!string.IsNullOrEmpty(mAuthKey) && mProjectId > 0) || (!string.IsNullOrEmpty(mLogin) && !string.IsNullOrEmpty(mPassword));

			EditorGUI.BeginDisabledGroup(!auth_valid);

			if (string.IsNullOrEmpty(mAuthKey))
			{
				GUILayout.BeginHorizontal();

				EditorGUILayout.LabelField($"Project: [{mProjectId}]");

				if (mProjectsList != null)
				{
					int selected_index = EditorGUILayout.Popup(Mathf.Max(0, mSelectedProjectIndex), mProjectsList);

					GUILayout.FlexibleSpace();
					if (selected_index >= 0 && selected_index != mSelectedProjectIndex)
					{
						mSelectedProjectIndex = selected_index;
						string selected_item = mProjectsList[mSelectedProjectIndex];
						if (int.TryParse(selected_item.Substring(0, selected_item.IndexOf("-")).Trim(), out int project_id))
						{
							mProjectId = project_id;
						}
					}
				}

				if (GUILayout.Button("Fetch Projects List"))
				{
					_ = FetchProjectsList();
				}

				GUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.LabelField($"Project: [{mProjectId}] - extracted from the api key");
			}
			
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			GUILayout.BeginHorizontal();
			mDirectoryPath = EditorGUILayout.TextField("Directory", mDirectoryPath);
			if (GUILayout.Button("...", GUILayout.Width(40)))
			{
				string path = EditorUtility.SaveFolderPanel("Choose location of SnipeApi.cs", mDirectoryPath, "");
				if (!string.IsNullOrEmpty(path))
				{
					mDirectoryPath = path;
				}
			}
			GUILayout.EndHorizontal();

			mGetTablesList = EditorGUILayout.Toggle("Get tables list", mGetTablesList);

			EditorGUILayout.BeginHorizontal();

			GUILayout.Label("Snipe Version", GUILayout.Width(EditorGUIUtility.labelWidth));

			int index = Array.IndexOf(SNIPE_VERSIONS, mSnipeVersionSuffix);
			index = EditorGUILayout.Popup(index, SNIPE_VERSIONS);
			mSnipeVersionSuffix = SNIPE_VERSIONS[index];

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Download"))
			{
				DownloadSnipeApiAndClose();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
		}

		private async Task FetchProjectsList()
		{
			mToken = await GetAuthToken();
			
			if (string.IsNullOrEmpty(mToken))
			{
				UnityEngine.Debug.Log("DownloadSnipeApi - FAILED to get token");
				return;
			}

			using (var loader = new HttpClient())
			{
				loader.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mToken);
				var response = await loader.GetAsync($"https://edit.snipe.dev/api/v1/projects");
				
				if (!response.IsSuccessStatusCode)
				{
					UnityEngine.Debug.LogError($"DownloadSnipeApi - failed; HTTP status: {(int)response.StatusCode} - {response.StatusCode}");
					return;
				}

				var content = await response.Content.ReadAsStringAsync();

				var list_wrapper = new ProjectsListResponseListWrapper();
				UnityEditor.EditorJsonUtility.FromJsonOverwrite(content, list_wrapper);
				if (list_wrapper.data is List<ProjectsListResponseListItem> list)
				{
					list.Sort((a, b) => { return a.id - b.id; });

					mProjectsList = new string[list.Count];
					for (int i = 0; i < list.Count; i++)
					{
						var item = list[i];
						mProjectsList[i] = $"{item.id} - {item.stringID} - {item.name} - {(item.isDev ? "DEV" : "LIVE")}";
						if (item.id == mProjectId)
							mSelectedProjectIndex = i;
					}
				}
			}
		}

		private async void DownloadSnipeApiAndClose()
		{
			await DownloadSnipeApi();
			await System.Threading.Tasks.Task.Yield();
			if (mGetTablesList)
			{
				await SnipeTablesPreloadHelper.DownloadTablesList(mToken);
			}
			AssetDatabase.Refresh();
			this.Close();
		}

		public async Task DownloadSnipeApi()
		{
			UnityEngine.Debug.Log("DownloadSnipeApi - start");
			
			mToken = await GetAuthToken();
			
			if (string.IsNullOrEmpty(mToken))
			{
				UnityEngine.Debug.LogError("DownloadSnipeApi - FAILED to get token");
				return;
			}

			using (var loader = new HttpClient())
			{
				loader.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mToken);
				string url = $"https://edit.snipe.dev/api/v1/project/{mProjectId}/code/unityBindings{mSnipeVersionSuffix}";
				var response = await loader.GetAsync(url);

				if (!response.IsSuccessStatusCode)
				{
					UnityEngine.Debug.LogError($"DownloadSnipeApi - FAILED to get token; HTTP status: {(int)response.StatusCode} - {response.StatusCode}");
					return;
				}

				using (StreamWriter sw = File.CreateText(Path.Combine(mDirectoryPath, "SnipeApi.cs")))
				{
					await response.Content.CopyToAsync(sw.BaseStream);
				}
			}

			UnityEngine.Debug.Log("DownloadSnipeApi - done");
		}
		
		public static async Task<string> GetAuthToken()
		{
			if (!string.IsNullOrEmpty(mAuthKey))
				return mAuthKey;
			
			return await RequestAuthToken();
		}

		private static async Task<string> RequestAuthToken()
		{
			RefreshPrefsPrefix();
			string login = GetLogin();
			string password = GetPassword();
			
			if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
			{
				UnityEngine.Debug.LogError($"[SnipeTablesPreloadHelper] Failed to auth. Invalid login or password");
				return null;
			}

			var loader = new HttpClient();
			var request_data = new StringContent($"{{\"login\":\"{login}\",\"password\":\"{password}\"}}", Encoding.UTF8, "application/json");
			var loader_task = loader.PostAsync("https://edit.snipe.dev/api/v1/auth", request_data);
			var loader_response = await loader_task;

			if (loader_task.IsFaulted || loader_task.IsCanceled)
			{
				UnityEngine.Debug.LogError($"[SnipeTablesPreloadHelper] Failed to auth. Task is faulted");
				return null;
			}
			
			if (!loader_response.IsSuccessStatusCode)
			{
				UnityEngine.Debug.LogError($"DownloadSnipeApi - FAILED to auth; HTTP status: {(int)loader_response.StatusCode} - {loader_response.StatusCode}");
				return null;
			}

			string content = loader_response.Content.ReadAsStringAsync().Result;
			UnityEngine.Debug.Log(content);

			var response = new SnipeAuthLoginResponseData();
			UnityEditor.EditorJsonUtility.FromJsonOverwrite(content, response);

			return response.token;
		}
	}

#pragma warning disable 0649

	[System.Serializable]
	internal class ProjectsListResponseListWrapper
	{
		public List<ProjectsListResponseListItem> data;
	}

	[System.Serializable]
	internal class ProjectsListResponseListItem
	{
		public int id;
		public string stringID;
		public string name;
		public bool isDev;
	}

#pragma warning restore 0649

}

#endif // UNITY_EDITOR
