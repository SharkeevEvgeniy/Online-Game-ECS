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

public class PreloadSnipeTables : IPreprocessBuildWithReport
{
	private static string mTablesUrl;
	private static List<string> mTableNames;
	
	private static string mVersion;
	private static string mStreamingAssetsPath;
	
	public int callbackOrder { get { return 10; } }
	public void OnPreprocessBuild(BuildReport report)
	{
		StaticOnPreprocessBuild();
	}
	
	// [MenuItem ("Snipe/Preload TEST")]
	public static void StaticOnPreprocessBuild()
	{
		Load();
		
		Debug.Log("[PreloadSnipeTables] - OnPreprocessBuild finished");
	}
	
	private static string GetTableListFilePath()
	{
		return Path.Combine(Application.dataPath, "snipe_tables.txt");
	}
	
	private static string GetTablesVersionFilePath()
	{
		return Path.Combine(Application.streamingAssetsPath, "snipe_tables_version.txt");
	}

	[MenuItem ("Snipe/Preload Tables")]
	public static void Load()
	{
		Debug.Log("[PreloadSnipeTables] - started");
		
		mStreamingAssetsPath = Application.streamingAssetsPath;
		
		var filepath = GetTableListFilePath();
		if (File.Exists(filepath))
		{
			mTableNames = new List<string>();
			using (StreamReader sr = File.OpenText(filepath))
			{
				string s = "";
				while (!string.IsNullOrEmpty(s = sr.ReadLine()))
				{
					if (s.ToLower().StartsWith("http") && s.Contains("://"))
						mTablesUrl = s;
					else
						mTableNames.Add(s);
				}
			}
		}
		else
		{
			mTableNames = null;
		}
		
		if (mTableNames == null || mTableNames.Count == 0)
		{
			Debug.Log("[PreloadSnipeTables] - Tables list is empty");
			return;
		}
		
		Debug.Log("[PreloadSnipeTables] Tables count = " + mTableNames.Count);
		
		var files = Directory.EnumerateFiles(mStreamingAssetsPath, "*.jsongz*");
		foreach (string filename in files)
		{
			foreach (string tablename in mTableNames)
			{
				if (filename.Contains($"_{tablename}."))
				{
					Debug.Log("Delete " + tablename);
					FileUtil.DeleteFileOrDirectory(filename);
					break;
				}
			}
		}
		
		string version_file_path = GetTablesVersionFilePath();
		if (File.Exists(version_file_path))
			File.Delete(version_file_path);
		
		Task.Run(async () => { await LoadVersion(); }).Wait(10000);
		
		if (!string.IsNullOrWhiteSpace(mVersion))
		{
			foreach (string tablename in mTableNames)
			{
				Task.Run(async () => { await LoadTable(tablename); }).Wait(10000);
			}
		}
		
        AssetDatabase.Refresh();
		
		Debug.Log("[PreloadSnipeTables] - done");
	}
	
	protected static async Task LoadVersion()
	{
		mVersion = "";
		string url = $"{mTablesUrl}/version.txt";
		
		Debug.Log($"[PreloadSnipeTables] LoadVersion from {url}");
		
		try
		{
			var loader = new HttpClient();
			var loader_task = loader.GetAsync(url);

			await loader_task;

			if (loader_task.IsFaulted || loader_task.IsCanceled)
			{
				Debug.LogError("[PreloadSnipeTables] LoadVersion - Failed to load tables version - (loader failed)");
				return;
			}
			
			HttpResponseMessage response = loader_task.Result;
			if (!response.IsSuccessStatusCode)
			{
				Debug.LogError($"[PreloadSnipeTables] LoadVersion - Failed - http error: {response.StatusCode}");
				return;
			}
			
			using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
			{
				mVersion = reader.ReadLine().Trim();
			}
			
			Debug.Log($"[PreloadSnipeTables] Version = {mVersion}");
			
			if (!string.IsNullOrWhiteSpace(mVersion))
			{
				// save to file
				File.WriteAllText(GetTablesVersionFilePath(), mVersion);
			}
		}
		catch (Exception)
		{
			mVersion = "";
			Debug.LogError("[PreloadSnipeTables] LoadVersion - Failed to read tables version");
		}
	}
	
	protected static async Task LoadTable(string table_name)
	{
		string url = $"{mTablesUrl}/{table_name}.json.gz";
		string filename = $"{mVersion}_{table_name}.jsongz";

		// NOTE: There is a bug - only lowercase works
		// (https://issuetracker.unity3d.com/issues/android-loading-assets-from-assetbundles-takes-significantly-more-time-when-the-project-is-built-as-an-aab)
		filename = filename.ToLower();
		
		Debug.Log(filename);
		string cache_path = Path.Combine(mStreamingAssetsPath, filename);
		
		Debug.Log("[PreloadSnipeTables] Loading table " + url);
		
		try
		{
			var loader = new HttpClient();
			var loader_task = loader.GetAsync(url);

			await loader_task;
			
			if (loader_task.IsFaulted || loader_task.IsCanceled || !loader_task.Result.IsSuccessStatusCode)
			{
				Debug.LogError($"[PreloadSnipeTables] Failed to load table - {table_name}   (loader failed)");
				return;
			}
			
			HttpResponseMessage response = loader_task.Result;
			if (!response.IsSuccessStatusCode)
			{
				Debug.LogError($"[PreloadSnipeTables] LoadTable {table_name} - Failed - http error: {response.StatusCode}");
				return;
			}
			
			using (var file_content_stream = await response.Content.ReadAsStreamAsync())
			{
				using (FileStream cache_write_stream = new FileStream(cache_path, FileMode.Create, FileAccess.Write))
				{
					try
					{
						file_content_stream.Position = 0;
						file_content_stream.CopyTo(cache_write_stream);
					}
					catch (Exception ex)
					{
						Debug.LogError("[PreloadSnipeTables] Failed to save - " + table_name + " - " + ex.Message);
					}
				}
			}
		}
		catch (Exception)
		{
			Debug.LogError("[PreloadSnipeTables] Failed to load table - " + table_name);
		}
	}
	
}