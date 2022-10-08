using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniIT;

public static class SnipeConfig
{
	public static string ClientKey;
	public static string AppInfo;

	public static List<string> ServerUrls = new List<string>();
	public static List<string> TablesUrls = new List<string>();
	
	public static string ServerUdpAddress;
	public static ushort ServerUdpPort;
	
	public static bool CompressionEnabled = false;
	public static int MinMessageSizeToCompress = 1024; // bytes
	
	public static SnipeObject LoginParameters;
	public static bool TablesUpdateEnabled = true;
	
	public static string PersistentDataPath { get; private set; }
	public static string StreamingAssetsPath { get; private set; }
	
	private static int mServerUrlIndex = 0;
	private static int mTablesUrlIndex = 0;

	/// <summary>
	/// Should be called from the main Unity thread
	/// </summary>
	public static void InitFromJSON(string json_string)
	{
		Init(SnipeObject.FromJSONString(json_string));
	}

	/// <summary>
	/// Should be called from the main Unity thread
	/// </summary>
	public static void Init(SnipeObject data)
	{
		ClientKey = data.SafeGetString("client_key");
		
		ServerUdpAddress = data.SafeGetString("server_udp_address");
		ServerUdpPort = data.SafeGetValue<ushort>("server_udp_port");
		
		if (ServerUrls == null)
			ServerUrls = new List<string>();
		else
			ServerUrls.Clear();
		
		if (data["server_urls"] is IList server_ulrs_list)
		{
			foreach (string url in server_ulrs_list)
			{
				if (!string.IsNullOrEmpty(url))
				{
					ServerUrls.Add(url);
				}
			}
		}
		
		if (ServerUrls.Count < 1)
		{
			// "service_websocket" field for backward compatibility
			var service_url = data.SafeGetString("service_websocket");
			if (!string.IsNullOrEmpty(service_url))
				ServerUrls.Add(service_url);
		}
		
 		if (TablesUrls == null)
			TablesUrls = new List<string>();
		else
			TablesUrls.Clear();
		
		if (data["tables_path"] is IList tables_ulrs_list)
		{
			foreach (string path in tables_ulrs_list)
			{
				var corrected_path = path.Trim();
				if (!corrected_path.EndsWith("/"))
					corrected_path += "/";
				
				TablesUrls.Add(corrected_path);
			}
		}
		
		mServerUrlIndex = 0;
		mTablesUrlIndex = -1;

		PersistentDataPath = Application.persistentDataPath;
		StreamingAssetsPath = Application.streamingAssetsPath;

		InitAppInfo();
	}
	
	private static void InitAppInfo()
	{
		AppInfo = new SnipeObject()
		{
			["identifier"] = Application.identifier,
			["version"] = Application.version,
			["platform"] = Application.platform.ToString(),
		}.ToJSONString();
	}
	
	public static string GetServerUrl()
	{
		mServerUrlIndex = GetValidIndex(ServerUrls, mServerUrlIndex, false);
		if (mServerUrlIndex >= 0)
		{
			return ServerUrls[mServerUrlIndex];
		}

		return null;
	}
	
	public static void NextServerUrl()
	{
		mServerUrlIndex = GetValidIndex(ServerUrls, mServerUrlIndex, true);
	}

	public static string GetTablesPath(bool next = false)
	{
		mTablesUrlIndex = GetValidIndex(TablesUrls, mTablesUrlIndex, next);
		if (mTablesUrlIndex >= 0)
		{
			return TablesUrls[mTablesUrlIndex];
		}

		return null;
	}
	
	public static bool CheckUdpAvailable()
	{
		return ServerUdpPort > 0 && !string.IsNullOrEmpty(ServerUdpAddress);
	}
	
	private static int GetValidIndex(IList list, int index, bool next = false)
	{
		if (list != null && list.Count > 0)
		{
			if (next)
			{
				if (index < list.Count - 1)
					index++;
				else
					index = 0;
			}
			
			if (index < 0)
			{
				index = 0;
			}
			
			return index;
		}

		return -1;
	}
}

