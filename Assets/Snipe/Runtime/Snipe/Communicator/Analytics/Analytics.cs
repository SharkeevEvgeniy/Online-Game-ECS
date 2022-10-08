using System;
using System.Collections;
using System.Collections.Generic;
using MiniIT;
using UnityEngine;

namespace MiniIT.Snipe
{
	public class Analytics : MonoBehaviour
	{
		public static bool IsEnabled = true;
		
		#region AnalyticsTracker
		
		public static long PingTime { get; internal set; }
		public static long ConnectionEstablishmentTime { get; internal set; }
		public static double WebSocketTcpClientConnectionTime { get; internal set; }
		public static double WebSocketSslAuthenticateTime { get; internal set; }
		public static double WebSocketHandshakeTime { get; internal set; }
		public static double WebSocketMiscTime { get; internal set; }
		
		private static IAnalyticsTracker mTracker;
		
		private static string mUserId = null;
		
		public static void SetTracker(IAnalyticsTracker tracker)
		{
			mTracker = tracker;
			
			if (!string.IsNullOrEmpty(mUserId))
			{
				CheckReady();
			}
		}
		
		private static bool CheckReady()
		{
			bool ready = mTracker != null && mTracker.IsInitialized && IsEnabled;
			
			if (ready && !string.IsNullOrEmpty(mUserId))
			{
				mTracker.SetUserId(mUserId);
				mUserId = null;
			}
			
			return ready;
		}
		
		public static void SetUserId(string uid)
		{
			mUserId = uid;
			CheckReady();
		}

		public static void SetUserProperty(string name, string value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}
		public static void SetUserProperty(string name, int value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}
		public static void SetUserProperty(string name, float value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}
		public static void SetUserProperty(string name, double value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}
		public static void SetUserProperty(string name, bool value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}
		public static void SetUserProperty<T>(string name, IList<T> value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}
		public static void SetUserProperty(string name, IDictionary<string, object> value)
		{
			if (CheckReady())
			{
				mTracker.SetUserProperty(name, value);
			}
		}

		public static void TrackEvent(string name, IDictionary<string, object> properties = null)
		{
			if (CheckReady())
			{
				// Some trackers (for example Amplitude) may crash if used not in the main Unity thread.
				// We'll put events into a queue and call mTracker.TrackEvent in the MonoBehaviour's coroutine.
				
				if (properties == null)
					properties = new Dictionary<string, object>(2);
				if (PingTime > 0)
					properties["ping_time"] = PingTime;
				
				if (SnipeCommunicator.InstanceInitialized && SnipeCommunicator.Instance.Connected)
				{	
					properties["server_reaction"] = SnipeCommunicator.Instance.ServerReaction.TotalMilliseconds;
				}
				
				GetInstance().EnqueueEvent(name, properties);
			}
		}
		public static void TrackEvent(string name, string property_name, object property_value)
		{
			if (CheckReady())
			{
				Dictionary<string, object> properties = new Dictionary<string, object>(3);
				properties[property_name] = property_value;
				TrackEvent(name, properties);
			}
		}
		public static void TrackEvent(string name, object property_value)
		{
			if (CheckReady())
			{
				Dictionary<string, object> properties = new Dictionary<string, object>(3);
				properties["value"] = property_value;
				TrackEvent(name, properties);
			}
		}
		
		public static void TrackErrorCodeNotOk(string message_type, string error_code, SnipeObject data)
		{
			if (CheckReady() && mTracker.CheckErrorCodeTracking(message_type, error_code))
			{
				Dictionary<string, object> properties = new Dictionary<string, object>(5);
				properties["message_type"] = message_type;
				properties["error_code"] = error_code;
				properties["data"] = data?.ToJSONString();
				TrackEvent(EVENT_ERROR_CODE_NOT_OK, properties);
			}
		}
		
		public static void TrackError(string name, Exception exception = null)
		{
			if (CheckReady())
			{
				GetInstance().EnqueueError(name, exception);
			}
		}
		
		#endregion AnalyticsTracker
		
		#region Constants
		
		private const string EVENT_NAME = "Snipe Event";
		public const string EVENT_COMMUNICATOR_START_CONNECTION = "Communicator Start Connection";
		public const string EVENT_COMMUNICATOR_CONNECTED = "Communicator Connected";
		public const string EVENT_COMMUNICATOR_DISCONNECTED = "Communicator Disconnected";
		public const string EVENT_ROOM_COMMUNICATOR_CONNECTED = "Room Communicator Connected";
		public const string EVENT_ROOM_COMMUNICATOR_DISCONNECTED = "Room Communicator Disconnected";
		public const string EVENT_ACCOUNT_REGISTERED = "Account registered";
		public const string EVENT_ACCOUNT_REGISTERATION_FAILED = "Account registeration failed";
		public const string EVENT_LOGIN_REQUEST_SENT = "Login request sent";
		public const string EVENT_LOGIN_RESPONSE_RECEIVED = "Login response received";
		public const string EVENT_AUTH_LOGIN_REQUEST_SENT = "Auth Login request sent";
		public const string EVENT_AUTH_LOGIN_RESPONSE_RECEIVED = "Auth Login response received";
		public const string EVENT_SINGLE_REQUEST_CLIENT_CONNECTED = "SingleRequestClient Connected";
		public const string EVENT_SINGLE_REQUEST_CLIENT_DISCONNECTED = "SingleRequestClient Disconnected";
		public const string EVENT_SINGLE_REQUEST_RESPONSE = "SingleRequestClient Response";
		
		private const string EVENT_ERROR_CODE_NOT_OK = "ErrorCode not ok";
		
		#endregion Constants
		
		#region MonoBehaviour
		
		private static Analytics mInstance;
		private static Analytics GetInstance()
		{
			if (mInstance == null)
			{
				mInstance = new GameObject("SnipeAnalyticsTracker").AddComponent<Analytics>();
			}
			return mInstance;
		}
		
		private void Awake()
		{
			if (mInstance != null && mInstance != this)
			{
				Destroy(this.gameObject);
				return;
			}
			
			DontDestroyOnLoad(this.gameObject);
			StartCoroutine(ProcessEventsQueue());
		}
		
		#region EventsQueue
		
		enum EventsQueueItemType
		{
			Event,
			Error,
		}
		
		class EventsQueueItem
		{
			internal EventsQueueItemType type;
			internal string name;
			internal IDictionary<string, object> properties;
			internal Exception exception;
		}
		
		private List<EventsQueueItem> mEventsQueue;
		
		private void EnqueueEvent(string name, IDictionary<string, object> properties = null)
		{
			if (mEventsQueue == null)
				mEventsQueue = new List<EventsQueueItem>();
			lock (mEventsQueue)
			{
				mEventsQueue.Add(new EventsQueueItem()
				{
					type = EventsQueueItemType.Event,
					name = name,
					properties = properties,
				});
			}
		}
		
		private void EnqueueError(string name, Exception exception = null)
		{
			if (mEventsQueue == null)
				mEventsQueue = new List<EventsQueueItem>();
			lock (mEventsQueue)
			{
				mEventsQueue.Add(new EventsQueueItem()
				{
					type = EventsQueueItemType.Error,
					name = name,
					exception = exception,
				});
			}
		}
		
		private IEnumerator ProcessEventsQueue()
		{
			while (true)
			{
				if (mEventsQueue != null && mEventsQueue.Count > 0)
				{
					lock (mEventsQueue)
					{
						foreach (var item in mEventsQueue)
						{
							if (item.type == EventsQueueItemType.Error)
							{
								mTracker.TrackError(item.name, item.exception);
							}
							else
							{
								var event_properties = item.properties;
								if (event_properties == null)
									event_properties = new Dictionary<string, object>() { ["event_type"] = item.name };
								else
									event_properties["event_type"] = item.name;
								
								mTracker.TrackEvent(EVENT_NAME, event_properties);
							}
						}
						mEventsQueue.Clear();
					}
				}
				
				yield return null;
			}
		}
		
		#endregion EventsQueue
		
		#endregion MonoBehaviour
	}
}