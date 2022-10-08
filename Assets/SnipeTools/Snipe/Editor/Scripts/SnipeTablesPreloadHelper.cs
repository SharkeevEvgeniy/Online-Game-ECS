using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MiniIT.Snipe.Editor
{

public class SnipeTablesPreloadHelper
{
	private static string mPrefsPrefix;

	public static string GetTableListFilePath()
	{
		return Path.Combine(Application.dataPath, "snipe_tables.txt");
	}
	
	public static string GetTablesUrl(string project_string_id)
	{
		return $"https://static-dev.snipe.dev/{project_string_id}/";
	}

	// [MenuItem("Snipe/Download Tables List")]
	public static async Task DownloadTablesList()
	{
		await DownloadTablesList(null);
	}

	public static async Task DownloadTablesList(string token)
	{
		UnityEngine.Debug.Log("[SnipeTablesPreloadHelper] DownloadResponseList - start");

		if (string.IsNullOrEmpty(token))
		{
			UnityEngine.Debug.Log("[SnipeTablesPreloadHelper] DownloadResponseList - request token");

			token = await SnipeApiDownloader.GetAuthToken();
			if (string.IsNullOrEmpty(token))
			{
				UnityEngine.Debug.Log("[SnipeTablesPreloadHelper] - FAILED to get token");
				return;
			}
		}
		
		RefreshPrefsPrefix();
		int project_id = EditorPrefs.GetInt($"{mPrefsPrefix}_SnipeApiDownloader.project_id");
		string project_string_id = "";
		
		UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] project id = {project_id}");
		
		if (project_id > 0)
		{
			UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] Fetching projects list");
			
			using (var project_string_id_client = new HttpClient())
			{
				project_string_id_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var response = await project_string_id_client.GetAsync($"https://edit.snipe.dev/api/v1/project/{project_id}/stringID");
				var content = await response.Content.ReadAsStringAsync();
				
				// UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] {content}");
				
				var response_data = new ProjectStringIdResponseData();
				UnityEditor.EditorJsonUtility.FromJsonOverwrite(content, response_data);
				project_string_id = response_data.stringID;
				UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] Project StringID request errorCode = {response_data.errorCode}");
				UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] Project StringID = {project_string_id}");
			}
			
			UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] Fetching tables list for project {project_string_id}");
			
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var response = await client.GetAsync($"https://edit.snipe.dev/api/v1/project/{project_id}/tableTypes");
				var content = await response.Content.ReadAsStringAsync();
				
				// UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] {content}");
				
				var list_wrapper = new TablesListResponseListWrapper();
				UnityEditor.EditorJsonUtility.FromJsonOverwrite(content, list_wrapper);
				if (list_wrapper.data is List<TablesListResponseListItem> list)
				{
					var file_path = GetTableListFilePath();
					UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] {file_path}");
					if (File.Exists(file_path))
					{
						FileUtil.DeleteFileOrDirectory(file_path);
					}
					
					UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] tables count = {list.Count}");
					
					//if (list.Count > 0)
					{
						using (StreamWriter sw = File.CreateText(file_path))
						{
							var tables = new List<string>(list.Count + 3);
							
							foreach (var item in list)
							{
								if (!item.isPublic)
									continue;

								string table_name = item.stringID;
								if (!string.IsNullOrEmpty(table_name))
								{
									sw.WriteLine(table_name);
									tables.Add(table_name);
								}
							}
							
							// common tables for all projects
							if (!tables.Contains("Items"))
								sw.WriteLine("Items");
							if (!tables.Contains("Logic"))
								sw.WriteLine("Logic");
							if (!tables.Contains("Calendar"))
								sw.WriteLine("Calendar");
							
							if (!string.IsNullOrEmpty(project_string_id))
							{
								sw.WriteLine(GetTablesUrl(project_string_id));
							}
						}
					}
					// else
					// {
						// UnityEngine.Debug.Log($"[SnipeTablesPreloadHelper] No tables found");
					// }
				}
			}
		}
		
		UnityEngine.Debug.Log("[SnipeTablesPreloadHelper] DownloadResponseList - done");
	}
	
	private static void RefreshPrefsPrefix()
	{
		mPrefsPrefix = SnipeApiDownloader.RefreshPrefsPrefix();
	}
}

#pragma warning disable 0649

[System.Serializable]
internal class SnipeAuthLoginResponseData
{
	public string token;
}

[System.Serializable]
internal class TablesListResponseListWrapper
{
	public List<TablesListResponseListItem> data;
}

[System.Serializable]
internal class TablesListResponseListItem
{
	public int id;
	public string stringID;
	public bool isPublic;
}

[System.Serializable]
internal class ProjectStringIdResponseData
{
	public string errorCode;
	public string stringID;
}

#pragma warning restore 0649

}