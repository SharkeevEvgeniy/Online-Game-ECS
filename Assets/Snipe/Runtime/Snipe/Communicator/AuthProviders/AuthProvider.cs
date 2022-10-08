using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniIT.Snipe
{
	public class AuthProvider
	{
		public virtual string ProviderId { get { return "__"; } }

		public delegate void AuthResultCallback(string error_code, int user_id = 0);
		protected AuthResultCallback mAuthResultCallback;
		
		private string mLogin;
		private string mPassword;
		
		private Stopwatch mLoginStopwatch;

		public virtual void DisposeCallbacks()
		{
			mAuthResultCallback = null;
		}

		public virtual void RequestAuth(AuthResultCallback callback = null, bool reset_auth = false)
		{
			// Override this method.

			//mAuthSuccessCallback = success_callback;
			//mAuthFailCallback = fail_callback;

			InvokeAuthFailCallback(SnipeErrorCodes.NOT_INITIALIZED);
		}

		protected void RequestLogin(string provider, string login, string password, bool reset_auth = false)
		{
			if (reset_auth)
			{
				ResetAuthAndLogin(provider, login, password);
			}
			else
			{
				DoRequestLogin(login, password);
			}
		}
		
		private void ResetAuthAndLogin(string provider, string login, string password)
		{
			SnipeObject data = new SnipeObject()
			{
				["ckey"] = SnipeConfig.ClientKey,
				["provider"] = provider,
				["login"] = login,
				["auth"] = password,
			};
			
			var stopwatch = Stopwatch.StartNew();
			
			SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_RESET)?.RequestAuth(data, 
				(string error_code, SnipeObject response_data) =>
				{
					stopwatch.Stop();
					Analytics.TrackEvent(SnipeMessageTypes.AUTH_RESET, new SnipeObject()
						{
							["request_time"] = stopwatch.ElapsedMilliseconds,
						});
					
					OnAuthResetResponse(error_code, response_data);
				});
		}
		
		protected virtual void OnAuthResetResponse(string error_code, SnipeObject response_data)
		{
			if (error_code == SnipeErrorCodes.OK)
			{
				DoRequestLogin(response_data.SafeGetString("uid"), response_data.SafeGetString("password"));
			}
			// else if (error_code == SnipeErrorCodes.USER_ONLINE)
			// {
				// Task.Run(() => DelayedResetAuth(data));
			// }
			else
			{
				InvokeAuthFailCallback(error_code);
			}
		}
		
		// private async void DelayedResetAuth(SnipeObject data)
		// {
			// await Task.Delay(1000);
			// ResetAuthAndLogin(data);
		// }
		
		protected void DoRequestLogin(string login, string password)
		{
			mLogin = login;
			mPassword = password;
			
			SnipeObject data = SnipeConfig.LoginParameters != null ? new SnipeObject(SnipeConfig.LoginParameters) : new SnipeObject();
			data["login"] = login;
			data["auth"] = password;
			data["loginGame"] = true;
			data["version"] = SnipeClient.SNIPE_VERSION;
			data["appInfo"] = SnipeConfig.AppInfo;
			
			if (SnipeConfig.CompressionEnabled)
			{
				data["flagCanPack"] = true;
			}
			
			SnipeCommunicator.Instance.MessageReceived -= OnMessageReceived;
			SnipeCommunicator.Instance.MessageReceived += OnMessageReceived;
			
			mLoginStopwatch = Stopwatch.StartNew();
			
			SnipeCommunicator.Instance.Client.SendRequest(SnipeMessageTypes.AUTH_USER_LOGIN, data);
		}
		
		private void OnMessageReceived(string message_type, string error_code, SnipeObject response_data, int request_id)
		{
			if (message_type == SnipeMessageTypes.AUTH_USER_LOGIN)
			{
				SnipeCommunicator.Instance.MessageReceived -= OnMessageReceived;
				OnAuthLoginResponse(error_code, response_data);
			}
		}

		protected virtual void OnAuthLoginResponse(string error_code, SnipeObject data)
		{
			mLoginStopwatch?.Stop();
			
			DebugLogger.Log($"[AuthProvider] OnAuthLoginResponse {error_code} {data?.ToJSONString()}");
			
			Analytics.TrackEvent(SnipeMessageTypes.AUTH_USER_LOGIN, new SnipeObject()
				{
					["request_time"] = mLoginStopwatch?.ElapsedMilliseconds,
				});

			if (error_code == SnipeErrorCodes.OK && !string.IsNullOrEmpty(mLogin) && !string.IsNullOrEmpty(mPassword))
			{
				PlayerPrefs.SetString(SnipePrefs.AUTH_UID, mLogin);
				PlayerPrefs.SetString(SnipePrefs.AUTH_KEY, mPassword);
			}
			
			mLogin = "";
			mPassword = "";
		}

		protected virtual void InvokeAuthSuccessCallback(int user_id)
		{
			mAuthResultCallback?.Invoke(SnipeErrorCodes.OK, user_id);
			mAuthResultCallback = null;
		}

		protected virtual void InvokeAuthFailCallback(string error_code)
		{
			mAuthResultCallback?.Invoke(error_code, 0);
			mAuthResultCallback = null;
		}
	}
}