using MiniIT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MiniIT.Snipe
{
	public class SnipeTable
	{
		protected const int MAX_LOADERS_COUNT = 4;
		protected const string VERSION_FILE_NAME = "snipe_tables_version.txt";
		
		public static long VersionFetchingTime { get; protected set; }
		
		protected static string mVersion = null;
		protected static bool mVersionRequested = false;
		protected static List<CancellationTokenSource> mCancellations;
		
		protected static SemaphoreSlim mSemaphore;
		
		protected static readonly object mCacheIOLocker = new object();
		protected static readonly object mParseJSONLocker = new object();
		
		public delegate void LoadingFinishedHandler(bool success);
		
		public bool Loaded { get; protected set; } = false;
		public bool LoadingFailed { get; protected set; } = false;
		
		public enum LoadingLocation
		{
			Network,  // External URL
			Cache,    // Application cache
			BuiltIn,  // StremingAssets
		}
		public LoadingLocation LoadedFrom { get; protected set; } = LoadingLocation.Network;
		
		protected CancellationTokenSource mLoadingCancellation;
		
		public static void Initialize()
		{
			BetterStreamingAssets.Initialize();
		}
		
		public static void ResetVersion()
		{
			DebugLogger.Log("[SnipeTable] ResetVersion");
			
			if (mCancellations != null)
			{
				// clone the list for thread safety
				var cancellations = new List<CancellationTokenSource>(mCancellations);
				foreach (var cancellation in cancellations)
				{
					try
					{
						cancellation?.Cancel();
					}
					catch (ObjectDisposedException)
					{
						// ignore
					}
				}
				mCancellations.Clear();
			}
			
			mVersion = null;
			mVersionRequested = false;
		}
	}
	
	public class SnipeTable<ItemType> : SnipeTable where ItemType : SnipeTableItem, new()
	{
		public event LoadingFinishedHandler LoadingFinished;
		
		public Dictionary<int, ItemType> Items { get; private set; }
		
		public ItemType this[int id]
		{
			get
			{
				TryGetValue(id, out var item);
				return item;
			}
		}
		
		public bool TryGetValue(int id, out ItemType item)
		{
			if (Loaded && Items != null)
			{
				return Items.TryGetValue(id, out item);
			}
			
			item = default;
			return false;
		}
		
		public async Task Load<WrapperType>(string table_name) where WrapperType : class, ISnipeTableItemsListWrapper<ItemType>, new()
		{
			if (string.IsNullOrEmpty(SnipeConfig.GetTablesPath()))
			{
				DebugLogger.LogError("[SnipeTable] Loading failed. Tables path not specified. Make sure that SnipeConfig is initialized.");
				return;
			}
			
			if (mLoadingCancellation != null)
			{
				try
				{
					mLoadingCancellation.Cancel();
				}
				catch (ObjectDisposedException)
				{
					// ignore
				}
				
				if (mCancellations != null)
					mCancellations.Remove(mLoadingCancellation);
			}
			
			mLoadingCancellation = new CancellationTokenSource();
			
			if (mCancellations == null)
				mCancellations = new List<CancellationTokenSource>();
			
			mCancellations.Add(mLoadingCancellation);
			
			try
			{
				await LoadAsync<WrapperType>(table_name, mLoadingCancellation.Token);
			}
			finally
			{
				mCancellations.Remove(mLoadingCancellation);
				mLoadingCancellation.Dispose();
				mLoadingCancellation = null;
			}
		}
		
		private async Task LoadAsync<WrapperType>(string table_name, CancellationToken cancellation_token) where WrapperType : class, ISnipeTableItemsListWrapper<ItemType>, new()
		{
			if (!mVersionRequested)
			{
				mVersionRequested = true;
				
				string version_file_path = Path.Combine(SnipeConfig.PersistentDataPath, VERSION_FILE_NAME);
				
				var stopwatch = Stopwatch.StartNew();
				
				if (SnipeConfig.TablesUpdateEnabled)
				{
					for (int retries_count = 0; retries_count < 3; retries_count++)
					{
						string url = $"{SnipeConfig.GetTablesPath(true)}version.txt";
						DebugLogger.Log($"[SnipeTable] LoadVersion ({retries_count}) " + url);
					
						try
						{
							using (var loader = new HttpClient())
							{
								loader.Timeout = TimeSpan.FromSeconds(1);
								
								var load_task = loader.GetAsync(url); // , cancellation_token);
								if (await Task.WhenAny(load_task, Task.Delay(1000)) == load_task && load_task.Result.IsSuccessStatusCode)
								{
									var content = await load_task.Result.Content.ReadAsStringAsync();
									mVersion = content.Trim();
									
									DebugLogger.Log($"[SnipeTable] LoadVersion done - {mVersion}");
									
									// save to file
									File.WriteAllText(version_file_path, mVersion);
									
									break;
								}
							}
							
							await Task.Delay(100, cancellation_token);
						}
						catch (Exception e)
						{
							if (e is TaskCanceledException || 
								e is AggregateException ae && ae.InnerException is TaskCanceledException)
							{
								DebugLogger.Log($"[SnipeTable] LoadVersion - TaskCanceled");
							}
							else
							{
								DebugLogger.Log($"[SnipeTable] LoadVersion - Exception: {e}");
							}
						}
						
						if (cancellation_token.IsCancellationRequested)
						{
							DebugLogger.Log($"[SnipeTable] LoadVersion task canceled");
							
							stopwatch.Stop();
							VersionFetchingTime = stopwatch.ElapsedMilliseconds;
							
							return;
						}
					}
				}
				
				if (string.IsNullOrEmpty(mVersion))
				{
					DebugLogger.Log($"[SnipeTable] LoadVersion - Failed to load from URL. Trying to read from cache");
					
					long builtin_version = 0;
					long cached_version = 0;
					string builtin_version_string = null;
					string cached_version_string = null;
					
					if (BetterStreamingAssets.FileExists(VERSION_FILE_NAME))
					{
						builtin_version_string = BetterStreamingAssets.ReadAllText(VERSION_FILE_NAME).Trim();
						if (long.TryParse(builtin_version_string, out builtin_version))
						{
							DebugLogger.Log($"[SnipeTable] LoadVersion - built-in value - {builtin_version_string}");
						}
						else
						{
							builtin_version = 0;
						}
					}
					
					
					if (File.Exists(version_file_path))
					{
						cached_version_string = File.ReadAllText(version_file_path).Trim();
						if (long.TryParse(cached_version_string, out cached_version))
						{
							DebugLogger.Log($"[SnipeTable] LoadVersion - cached value - {cached_version_string}");
						}
						else
						{
							cached_version = 0;
						}
					}
					
					if (builtin_version > 0 && builtin_version > cached_version)
					{
						mVersion = builtin_version_string;
					}
					else if (cached_version > 0)
					{
						mVersion = cached_version_string;
					}
				}
				
				stopwatch.Stop();
				VersionFetchingTime = stopwatch.ElapsedMilliseconds;
				
				if (string.IsNullOrEmpty(mVersion))
				{
					DebugLogger.Log($"[SnipeTable] LoadVersion Failed");
					return;
				}
			}
			else
			{
				while (string.IsNullOrEmpty(mVersion))
				{
					await Task.Yield();
					
					if (cancellation_token.IsCancellationRequested)
					{
						DebugLogger.Log($"[SnipeTable] Load {table_name} task canceled");
						return;
					}
				}
			}
			
			if (mSemaphore == null)
			{
				mSemaphore = new SemaphoreSlim(MAX_LOADERS_COUNT);
			}
			
			Loaded = false;
			Items = new Dictionary<int, ItemType>();
			
			try
			{
				await mSemaphore.WaitAsync(cancellation_token);
				await LoadTask<WrapperType>(table_name, cancellation_token);
			}
			catch (TaskCanceledException)
			{
				// ignore
			}
			catch (Exception e)
			{
				DebugLogger.Log($"[SnipeTable] Load {table_name} - Exception: {e}");
			}
			finally
			{
				mSemaphore.Release();
			}
		}

		protected string GetCachePath(string table_name)
		{
			return Path.Combine(SnipeConfig.PersistentDataPath, $"{mVersion}_{table_name}.json.gz");
		}
		
		protected string GetBuiltInPath(string table_name)
		{
			// NOTE: There is a bug - only lowercase works
			// (https://issuetracker.unity3d.com/issues/android-loading-assets-from-assetbundles-takes-significantly-more-time-when-the-project-is-built-as-an-aab)
			return $"{mVersion}_{table_name}.jsongz".ToLower();
		}
		
		protected string GetTableUrl(string table_name)
		{
			return $"{SnipeConfig.GetTablesPath()}{table_name}.json.gz";
		}
		
		private async Task LoadTask<WrapperType>(string table_name, CancellationToken cancellation) where WrapperType : class, ISnipeTableItemsListWrapper<ItemType>, new()
		{
			if (cancellation.IsCancellationRequested)
			{
				DebugLogger.Log($"[SnipeTable] Failed to load table - {table_name}   (task canceled)");
				return;
			}
			
			DebugLogger.Log($"[SnipeTable] LoadTask start - {table_name}");

			// Try to load from cache
			if (!string.IsNullOrEmpty(mVersion))
			{
				string cache_path = GetCachePath(table_name);
				ReadFile<WrapperType>(table_name, cache_path);
				
				// If loading from cache failed
				// try to load built-in file
				if (!this.Loaded)
				{
					ReadFromStramingAssets<WrapperType>(table_name, GetBuiltInPath(table_name));
				}
			}
			
			// If loading from cache failed
			if (!this.Loaded)
			{
				string url = GetTableUrl(table_name);
				DebugLogger.Log("[SnipeTable] Loading table " + url);

				this.LoadingFailed = false;

				int retry = 0;
				while (!this.Loaded && retry <= 2)
				{
					if (cancellation.IsCancellationRequested)
					{
						DebugLogger.Log("[SnipeTable] Failed to load table - " + table_name + "   (task canceled)");
						return;
					}
					
					if (retry > 0)
					{
						await Task.Delay(100, cancellation);
						DebugLogger.Log($"[SnipeTable] Retry #{retry} to load table - {table_name}");
					}

					retry++;

					try
					{
						var loader = new HttpClient();
						var loader_task = loader.GetAsync(url, cancellation);

						await loader_task;

						if (cancellation.IsCancellationRequested)
						{
							DebugLogger.Log("[SnipeTable] Failed to load table - " + table_name + "   (task canceled)");
							return;
						}

						if (loader_task.IsFaulted || loader_task.IsCanceled)
						{
							DebugLogger.Log("[SnipeTable] Failed to load table - " + table_name + "   (loader failed)");
							return;
						}
						
						using (var file_content_stream = await loader_task.Result.Content.ReadAsStreamAsync())
						{
							ReadGZip<WrapperType>(file_content_stream);
						}
							
						if (this.Loaded)
						{
							DebugLogger.Log("[SnipeTable] Table ready - " + table_name);
							
							// "using" block in ReadGZip closes the stream. We need to open it again
							using (var file_content_stream = await loader_task.Result.Content.ReadAsStreamAsync())
							{
								SaveToCache(file_content_stream, table_name);
							}
						}
					}
					catch (Exception e)
					{
						DebugLogger.Log($"[SnipeTable] Failed to load or parse table - {table_name} - {e}");
					}
				}
			}

			this.LoadingFailed = !this.Loaded;
			LoadingFinished?.Invoke(this.Loaded);
		}
		
		private void ReadFile<WrapperType>(string table_name, string file_path) where WrapperType : class, ISnipeTableItemsListWrapper<ItemType>, new()
		{
			lock (mCacheIOLocker)
			{
				if (File.Exists(file_path))
				{
					using (var read_stream = new FileStream(file_path, FileMode.Open))
					{
						try
						{
							ReadGZip<WrapperType>(read_stream);
						}
						catch (Exception)
						{
							DebugLogger.Log($"[SnipeTable] Failed to read file - {table_name}");
						}

						if (this.Loaded)
						{
							this.LoadedFrom = LoadingLocation.Cache;
							DebugLogger.Log($"[SnipeTable] Table ready (from cache) - {table_name}");
						}
					}
				}
			}
		}

		private void ReadFromStramingAssets<WrapperType>(string table_name, string file_path) where WrapperType : class, ISnipeTableItemsListWrapper<ItemType>, new()
		{
			DebugLogger.Log($"[SnipeTable] ReadFromStramingAssets - {file_path}");
			
			if (!BetterStreamingAssets.FileExists(file_path))
			{
				DebugLogger.Log($"[SnipeTable] ReadFromStramingAssets - file not found");
				return;
			}
			
			byte[] data = BetterStreamingAssets.ReadAllBytes(file_path);
			
			if (data != null)
			{
				using (var read_stream = new MemoryStream(data))
				{
					try
					{
						ReadGZip<WrapperType>(read_stream);
					}
					catch (Exception e)
					{
						DebugLogger.Log($"[SnipeTable] Failed to read file - {table_name} - {e}");
					}
				}
				
				if (this.Loaded)
				{
					this.LoadedFrom = LoadingLocation.BuiltIn;
					DebugLogger.Log($"[SnipeTable] Table ready (built-in) - {table_name}");
				}
			}
		}
		
		private void SaveToCache(Stream stream, string table_name)
		{
			string cache_path = GetCachePath(table_name);
			
			lock (mCacheIOLocker)
			{
				try
				{
					if (!File.Exists(cache_path))
					{
						DebugLogger.Log("[SnipeTable] Save to cache " + cache_path);
						
						using (FileStream cache_write_stream = new FileStream(cache_path, FileMode.Create, FileAccess.Write))
						{
							stream.Position = 0;
							stream.CopyTo(cache_write_stream);
						}
					}
				}
				catch (Exception e)
				{
					DebugLogger.Log("[SnipeTable] Failed to save to cache - " + table_name + " - " + e.Message);
				}
			}
		}

		private void ReadGZip<WrapperType>(Stream stream) where WrapperType : class, ISnipeTableItemsListWrapper<ItemType>, new()
		{
			using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
			{
				using (StreamReader reader = new StreamReader(gzip))
				{
					string json_string = reader.ReadToEnd();
					
					WrapperType list_wrapper = default;
					var type_of_wrapper = typeof(WrapperType);

					if (type_of_wrapper == typeof(SnipeTableLogicItemsWrapper))
					{
						DebugLogger.Log("[SnipeTable] SnipeTableLogicItemsWrapper");

						list_wrapper = ParseListWrapper(json_string, SnipeTableLogicItemsWrapper.FromTableData) as WrapperType;
					}
					else if (type_of_wrapper == typeof(SnipeTableCalendarItemsWrapper))
					{
						DebugLogger.Log("[SnipeTable] SnipeTableCalendarItemsWrapper");

						list_wrapper = ParseListWrapper(json_string, SnipeTableCalendarItemsWrapper.FromTableData) as WrapperType;
					}
					else
					{
						lock (mParseJSONLocker)
						{
							list_wrapper = fastJSON.JSON.ToObject<WrapperType>(json_string);
						}
					}
					
					if (list_wrapper?.list != null)
					{
						foreach (ItemType item in list_wrapper.list)
						{
							Items[item.id] = item;
						}
					}
					
					this.Loaded = true;
				}
			}
		}

		private ISnipeTableItemsListWrapper ParseListWrapper(string json_string, Func<Dictionary<string, object>, ISnipeTableItemsListWrapper> parse_func)
		{
			Dictionary<string, object> parsed_data = null;
			lock (mParseJSONLocker)
			{
				parsed_data = SnipeObject.FromJSONString(json_string);
			}

			var list_wrapper = parse_func.Invoke(parsed_data);

			if (list_wrapper == null)
			{
				DebugLogger.Log("[SnipeTable] parsed_data is null");
			}

			return list_wrapper;
		}
	}
}