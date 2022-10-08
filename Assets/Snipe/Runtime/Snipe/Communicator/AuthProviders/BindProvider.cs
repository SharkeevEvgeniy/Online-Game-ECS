using System;
using UnityEngine;

namespace MiniIT.Snipe
{
	public class BindProvider : AuthProvider
	{
		public bool? AccountExists { get; protected set; } = null;

		public delegate void BindResultCallback(BindProvider provider, string error_code);
		public delegate void CheckAuthExistsCallback(BindProvider provider, bool exists, bool is_me, string user_name = null);

		protected BindResultCallback mBindResultCallback;
		protected CheckAuthExistsCallback mCheckAuthExistsCallback;

		public string BindDonePrefsKey
		{
			get { return SnipePrefs.AUTH_BIND_DONE + ProviderId; }
		}

		public bool IsBindDone
		{
			get
			{
				return PlayerPrefs.GetInt(BindDonePrefsKey, 0) == 1;
			}
			internal set
			{
				SetBindDoneFlag(value, true);
			}
		}

		public BindProvider() : base()
		{
			if (IsBindDone)
				OnBindDone();
		}

		public virtual void RequestBind(BindResultCallback bind_callback = null)
		{
			// Override this method.

			mBindResultCallback = bind_callback;

			InvokeBindResultCallback(IsBindDone ? SnipeErrorCodes.OK : SnipeErrorCodes.NOT_INITIALIZED);
		}

		public virtual string GetUserId()
		{
			// Override this method.
			return "";
		}
		
		protected override void OnAuthResetResponse(string error_code, SnipeObject response_data)
		{
			if (error_code == SnipeErrorCodes.NO_SUCH_AUTH)
			{
				AccountExists = false;
			}
			
			base.OnAuthResetResponse(error_code, response_data);
		}

		protected override void OnAuthLoginResponse(string error_code, SnipeObject data)
		{
			if (!string.IsNullOrEmpty(error_code))
			{
				AccountExists = (error_code == SnipeErrorCodes.OK);
				if (AccountExists != true)
				{
					SetBindDoneFlag(false, false);
				}
			}
			
			base.OnAuthLoginResponse(error_code, data);
		}

		public virtual bool CheckAuthExists(CheckAuthExistsCallback callback = null)
		{
			// Override this method.
			return false;
		}

		protected virtual void CheckAuthExists(string user_id, CheckAuthExistsCallback callback)
		{
			mCheckAuthExistsCallback = callback;

			DebugLogger.Log($"[BindProvider] ({ProviderId}) CheckAuthExists {user_id}");

			SnipeObject data = new SnipeObject()
			{
				["ckey"] = SnipeConfig.ClientKey,
				["provider"] = ProviderId,
				["login"] = user_id,
			};

			int login_id = SnipeCommunicator.Instance.Auth.UserID;
			if (login_id != 0)
			{
				data["userID"] = login_id;
			}

			SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_USER_EXISTS)?.RequestAuth(data, OnCheckAuthExistsResponse);
		}

		protected virtual void OnBindResponse(string error_code, SnipeObject data)
		{
			DebugLogger.Log($"[BindProvider] ({ProviderId}) OnBindResponse - {error_code}");

			if (error_code == SnipeErrorCodes.OK)
			{
				AccountExists = true;
				IsBindDone = true;
			}

			InvokeBindResultCallback(error_code);
		}

		protected void OnCheckAuthExistsResponse(string error_code, SnipeObject data)
		{
			if (!string.IsNullOrEmpty(error_code))
				AccountExists = (error_code == SnipeErrorCodes.OK);
			
			bool is_me = data.SafeGetValue("isSame", false);
			if (AccountExists == true && is_me)
				IsBindDone = SnipeCommunicator.Instance.LoggedIn;

			if (mCheckAuthExistsCallback != null)
			{
				mCheckAuthExistsCallback.Invoke(this, AccountExists == true, is_me, data.SafeGetString("name"));
				mCheckAuthExistsCallback = null;
			}

			if (AccountExists.HasValue && SnipeCommunicator.Instance.LoggedIn)
			{
				if (AccountExists == false)
				{
					RequestBind();
				}
				else if (!is_me)
				{
					DebugLogger.Log($"[BindProvider] ({ProviderId}) OnCheckAuthExistsResponse - another account found - InvokeAccountBindingCollisionEvent");
					SnipeCommunicator.Instance.Auth.InvokeAccountBindingCollisionEvent(this, data.SafeGetString("name"));
				}
			}
		}

		protected virtual void InvokeBindResultCallback(string error_code)
		{
			DebugLogger.Log($"[BindProvider] ({ProviderId}) InvokeBindResultCallback - {error_code}");

			if (mBindResultCallback != null)
				mBindResultCallback.Invoke(this, error_code);

			mBindResultCallback = null;
		}
		
		protected void SetBindDoneFlag(bool value, bool invoke_callback)
		{
			bool current_value = PlayerPrefs.GetInt(BindDonePrefsKey, 0) == 1;
			if (value != current_value)
			{
				DebugLogger.Log($"[BindProvider] ({ProviderId}) Set bind done flag to {value}");

				PlayerPrefs.SetInt(BindDonePrefsKey, value ? 1 : 0);

				if (value && invoke_callback)
					OnBindDone();
			}
		}

		protected virtual void OnBindDone()
		{
		}

		public override void DisposeCallbacks()
		{
			mBindResultCallback = null;

			base.DisposeCallbacks();
		}
	}
}