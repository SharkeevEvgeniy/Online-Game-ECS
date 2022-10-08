using System;
using UnityEngine;
using MiniIT;
using MiniIT.Snipe;
using MiniIT.Social;

public class AmazonAuthProvider : BindProvider
{
	public const string PROVIDER_ID = "amzn";
	public override string ProviderId { get { return PROVIDER_ID; } }
	
	private string mUserId;
	
	public AmazonAuthProvider() : base()
	{
	}

	public override void RequestAuth(AuthResultCallback callback = null, bool reset_auth = false)
	{
		mAuthResultCallback = callback;

		if (CheckUserIdValid())
		{
			RequestLogin(ProviderId, mUserId, "", reset_auth);
			return;
		}

		InvokeAuthFailCallback(SnipeErrorCodes.NOT_INITIALIZED);
	}

	public override void RequestBind(BindResultCallback bind_callback = null)
	{
		DebugLogger.Log("[AmazonAuthProvider] RequestBind");

		mBindResultCallback = bind_callback;
		
		if (IsBindDone)
		{
			InvokeBindResultCallback(SnipeErrorCodes.OK);
			return;
		}
		
		if (CheckUserIdValid())
		{
			string auth_login = PlayerPrefs.GetString(SnipePrefs.AUTH_UID);
			string auth_token = PlayerPrefs.GetString(SnipePrefs.AUTH_KEY);

			if (!string.IsNullOrEmpty(auth_login) && !string.IsNullOrEmpty(auth_token))
			{				
				SnipeObject data = new SnipeObject()
				{
					["ckey"] = SnipeConfig.ClientKey,
					["provider"] = ProviderId,
					["login"] = mUserId,
					["loginInt"] = auth_login,
					["authInt"] = auth_token,
				};

				DebugLogger.Log("[AmazonAuthProvider] send user.bind " + data.ToJSONString());
				SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_USER_BIND)?.RequestAuth(data, OnBindResponse);

				return;
			}
		}

		InvokeBindResultCallback(SnipeErrorCodes.NOT_INITIALIZED);
	}

	protected override void OnAuthLoginResponse(string error_code, SnipeObject data)
	{
		base.OnAuthLoginResponse(error_code, data);

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
		return mUserId;
	}
	
	public void SetUserId(string user_id)
	{
		if (mUserId == user_id)
			return;
		
		mUserId = user_id;
		
		if (CheckUserIdValid())
		{
			AccountExists = null;
			
			if (SnipeCommunicator.Instance.LoggedIn)
			{
				CheckAuthExists(null);
			}
		}
	}
	
	public bool CheckUserIdValid()
	{
		return CheckUserIdValid(mUserId);
	}
	
	public static bool CheckUserIdValid(string user_id)
	{
		return !string.IsNullOrEmpty(user_id) && user_id.ToLower() != "fakeid";
	}

	public override bool CheckAuthExists(CheckAuthExistsCallback callback = null)
	{
		if (string.IsNullOrEmpty(mUserId))
			return false;
		
		CheckAuthExists(mUserId, callback);
		return true;
	}
}
