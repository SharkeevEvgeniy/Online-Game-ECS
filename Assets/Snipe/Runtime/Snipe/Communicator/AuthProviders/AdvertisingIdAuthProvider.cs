using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using MiniIT;
using MiniIT.Snipe;
using MiniIT.Social;

public class AdvertisingIdAuthProvider : BindProvider
{
	public const string PROVIDER_ID = "adid";
	public override string ProviderId { get { return PROVIDER_ID; } }

	public static string AdvertisingId { get; private set; } = null;
	public static TimeSpan AdvertisingIdFetchTime { get; private set; }
	
	private SnipeObject mBindRequestData = null;
	private Queue<Action> mAdvertisingIdReadyActions;
	
	public AdvertisingIdAuthProvider() : base()
	{
		if (AdvertisingId != null)
			return;
		
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		
		void advertising_id_callback(string advertising_id, bool tracking_enabled, string error)
		{
			stopwatch.Stop();
			AdvertisingIdFetchTime = stopwatch.Elapsed;
			
			var error_string = string.IsNullOrEmpty(error) ? "" : ", error: " + error;
			DebugLogger.Log($"[AdvertisingIdAuthProvider] advertising_id : {advertising_id} {error_string}");
			
			Analytics.TrackEvent("RequestAdvertisingId", new SnipeObject()
				{
					["request_time"] = stopwatch.ElapsedMilliseconds,
				});
			
			AdvertisingId = advertising_id ?? "";
			InvokeAdvertisingIdReadyActions();
		}
		
#if MINI_IT_ADVERTISING_ID
		MiniIT.Utils.AdvertisingIdFetcher.RequestAdvertisingId((adid) => advertising_id_callback(adid, true, ""));
#else
		if (!Application.RequestAdvertisingIdentifierAsync(advertising_id_callback))
		{
			DebugLogger.Log("[AdvertisingIdAuthProvider] advertising id is not supported on this platform");
			
			AdvertisingId = "";
			InvokeAdvertisingIdReadyActions();
		}
#endif
	}
	
	private void EnqueueAdvertisingIdReadyAction(Action action)
	{
		if (mAdvertisingIdReadyActions == null)
			mAdvertisingIdReadyActions = new Queue<Action>();
		mAdvertisingIdReadyActions.Enqueue(action);
	}
	
	private void InvokeAdvertisingIdReadyActions()
	{
		if (mAdvertisingIdReadyActions != null)
		{
			while (mAdvertisingIdReadyActions.Count > 0)
			{
				var action = mAdvertisingIdReadyActions.Dequeue();
				try
				{
					action?.Invoke();
				}
				catch(Exception e)
				{
					DebugLogger.Log("[AdvertisingIdAuthProvider] InvokeAdvertisingIdReadyActions error: " + e.Message);
				}
			}
			
			mAdvertisingIdReadyActions = null;
		}
	}

	public override void RequestAuth(AuthResultCallback callback = null, bool reset_auth = false)
	{
		DebugLogger.Log("[AdvertisingIdAuthProvider] RequestAuth");
		
		mAuthResultCallback = callback;
		
		void on_advertising_id_ready()
		{
			DebugLogger.Log("[AdvertisingIdAuthProvider] RequestAuth - on_advertising_id_ready");
			
			if (CheckAdvertisingId(AdvertisingId))
			{
				RequestLogin(ProviderId, AdvertisingId, "", reset_auth);
			}
			else
			{
				DebugLogger.Log("[AdvertisingIdAuthProvider] advertising id is invalid");
				
				InvokeAuthFailCallback(SnipeErrorCodes.NOT_INITIALIZED);
			}
		}
		
		if (AdvertisingId == null)  // not initialized yet
		{
			EnqueueAdvertisingIdReadyAction(on_advertising_id_ready);
		}
		else
		{
			on_advertising_id_ready();
		}
	}

	private bool CheckAdvertisingId(string advertising_id)
	{
		if (string.IsNullOrEmpty(advertising_id))
			return false;

		// on IOS value may be "00000000-0000-0000-0000-000000000000"
		return Regex.IsMatch(advertising_id, @"[^0\W]");
	}

	public override void RequestBind(BindResultCallback bind_callback = null)
	{
		DebugLogger.Log("[AdvertisingIdAuthProvider] RequestBind");
		
		if (mBindResultCallback != null && mBindResultCallback != bind_callback)
		{
			DebugLogger.LogWarning("[AdvertisingIdAuthProvider] Bind callback is not null. Previous callback will not be called.");
		}

		mBindResultCallback = bind_callback;
		
		if (IsBindDone)
		{
			InvokeBindResultCallback(SnipeErrorCodes.OK);
			return;
		}
		
		void on_advertising_id_ready()
		{
			DebugLogger.Log("[AdvertisingIdAuthProvider] RequestBind - on_advertising_id_ready");
			
			if (IsBindDone)
			{
				InvokeBindResultCallback(SnipeErrorCodes.OK);
				return;
			}
			
			string auth_login = PlayerPrefs.GetString(SnipePrefs.AUTH_UID);
			string auth_token = PlayerPrefs.GetString(SnipePrefs.AUTH_KEY);

			if (string.IsNullOrEmpty(auth_login) || string.IsNullOrEmpty(auth_token))
			{
				DebugLogger.Log("[AdvertisingIdAuthProvider] internal uid or token is invalid");

				InvokeBindResultCallback(SnipeErrorCodes.PARAMS_WRONG);
			}
			else
			{
				if (CheckAdvertisingId(AdvertisingId))
				{
					SnipeObject data = new SnipeObject()
					{
						["ckey"] = SnipeConfig.ClientKey,
						["provider"] = ProviderId,
						["login"] = AdvertisingId,
						["loginInt"] = auth_login,
						["authInt"] = auth_token,
					};
					
					if (mBindRequestData != null && SnipeObject.ContentEquals(mBindRequestData, data))
					{
						DebugLogger.LogWarning("[AdvertisingIdAuthProvider] Bind is already requested. This request will not be performed.");
					}
					else
					{
						mBindRequestData = data;

						DebugLogger.Log("[AdvertisingIdAuthProvider] send user.bind " + data.ToJSONString());
						SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_USER_BIND)?.RequestAuth(data, OnBindResponse);
					}
				}
				else
				{
					DebugLogger.Log("[AdvertisingIdAuthProvider] advertising_id is invalid");

					InvokeBindResultCallback(SnipeErrorCodes.NOT_INITIALIZED);
				}
			}
		}
		
		if (AdvertisingId == null)  // not initialized yet
		{
			EnqueueAdvertisingIdReadyAction(on_advertising_id_ready);
		}
		else
		{
			on_advertising_id_ready();
		}
	}

	protected override void OnAuthLoginResponse(string error_code, SnipeObject data)
	{
		base.OnAuthLoginResponse(error_code, data);

		DebugLogger.Log($"[AdvertisingIdAuthProvider] {error_code}");

		if (error_code == SnipeErrorCodes.OK)
		{
			int user_id = data.SafeGetValue<int>("id");

			IsBindDone = true;

			InvokeAuthSuccessCallback(user_id);
		}
		else
		{
			InvokeAuthFailCallback(error_code);
		}
	}
	
	protected override void OnBindResponse(string error_code, SnipeObject data)
	{
		mBindRequestData = null;
		
		base.OnBindResponse(error_code, data);
	}

	public override string GetUserId()
	{
		return AdvertisingId;
	}

	public override bool CheckAuthExists(CheckAuthExistsCallback callback = null)
	{
		void on_advertising_id_ready()
		{
			DebugLogger.Log("[AdvertisingIdAuthProvider] CheckAuthExists - on_advertising_id_ready");

			if (CheckAdvertisingId(AdvertisingId))
			{
				CheckAuthExists(AdvertisingId, callback);
			}
			else
			{
				DebugLogger.Log("[AdvertisingIdAuthProvider] CheckAuthExists - advertising_id is invalid");

				if (callback != null)
					callback.Invoke(this, false, false);
			}
		}
		
		if (AdvertisingId == null)  // not initialized yet
		{
			EnqueueAdvertisingIdReadyAction(on_advertising_id_ready);
		}
		else
		{
			on_advertising_id_ready();
		}

		return true;
	}
}
