using System;
using System.Text.RegularExpressions;
using UnityEngine;
using MiniIT;
using MiniIT.Snipe;
using MiniIT.Social;

public class DeviceIdAuthProvider : BindProvider
{
	public const string PROVIDER_ID = "dvid";
	public override string ProviderId { get { return PROVIDER_ID; } }

	public override void RequestAuth(AuthResultCallback callback = null, bool reset_auth = false)
	{
		DebugLogger.Log("[DeviceIdAuthProvider] RequestAuth");
		
		mAuthResultCallback = callback;
		
		if (SystemInfo.unsupportedIdentifier != SystemInfo.deviceUniqueIdentifier)
		{
			RequestLogin(ProviderId, GetUserId(), "", reset_auth);
		}
		else
		{
			InvokeAuthFailCallback(SnipeErrorCodes.NOT_INITIALIZED);
		}
	}

	public override void RequestBind(BindResultCallback bind_callback = null)
	{
		DebugLogger.Log("[DeviceIdAuthProvider] RequestBind");

		mBindResultCallback = bind_callback;
		
		if (IsBindDone)
		{
			InvokeBindResultCallback(SnipeErrorCodes.OK);
			return;
		}
		
		if (SystemInfo.unsupportedIdentifier != SystemInfo.deviceUniqueIdentifier)
		{
			string auth_login = PlayerPrefs.GetString(SnipePrefs.AUTH_UID);
			string auth_token = PlayerPrefs.GetString(SnipePrefs.AUTH_KEY);

			if (string.IsNullOrEmpty(auth_login) || string.IsNullOrEmpty(auth_token))
			{
				DebugLogger.Log("[DeviceIdAuthProvider] internal uid or token is invalid");

				InvokeBindResultCallback(SnipeErrorCodes.PARAMS_WRONG);
			}
			else
			{
				SnipeObject data = new SnipeObject()
				{
					["ckey"] = SnipeConfig.ClientKey,
					["provider"] = ProviderId,
					["login"] = GetUserId(),
					["loginInt"] = auth_login,
					["authInt"] = auth_token,
				};

				DebugLogger.Log("[DeviceIdAuthProvider] send user.bind " + data.ToJSONString());
				SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_USER_BIND)?.RequestAuth(data, OnBindResponse);
			}
		}
		else
		{
			InvokeAuthFailCallback(SnipeErrorCodes.NOT_INITIALIZED);
		}
	}

	protected override void OnAuthLoginResponse(string error_code, SnipeObject data)
	{
		base.OnAuthLoginResponse(error_code, data);

		DebugLogger.Log($"[DeviceIdAuthProvider] {error_code}");

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

	public override string GetUserId()
	{
		return SystemInfo.deviceUniqueIdentifier;
	}

	public override bool CheckAuthExists(CheckAuthExistsCallback callback = null)
	{
		if (SystemInfo.unsupportedIdentifier != SystemInfo.deviceUniqueIdentifier)
		{
			CheckAuthExists(GetUserId(), callback);
			return true;
		}

		return false;
	}
}
