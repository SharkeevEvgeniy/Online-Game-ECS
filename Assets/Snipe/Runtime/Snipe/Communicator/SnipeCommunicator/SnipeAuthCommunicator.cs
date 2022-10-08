using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MiniIT;

namespace MiniIT.Snipe
{
	using AuthResultCallback = AuthProvider.AuthResultCallback;
	using AccountRegisterResponseHandler = AuthProvider.AuthResultCallback;

	public class SnipeAuthCommunicator
	{
		public delegate void AccountBindingCollisionHandler(BindProvider provider, string user_name = null);

		public event AccountRegisterResponseHandler AccountRegisterResponse;
		public event AccountBindingCollisionHandler AccountBindingCollision;
		
		public delegate void GetUserAttributeCallback(string error_code, string user_name, string key, object value);

		private int mUserID = 0;
		public int UserID
		{
			get
			{
				if (mUserID <= 0)
				{
					mUserID = Convert.ToInt32(PlayerPrefs.GetString(SnipePrefs.LOGIN_USER_ID, "0"));

					if (mUserID != 0)
					{
						Analytics.SetUserId(mUserID.ToString());
					}
				}
				return mUserID;
			}
			private set
			{
				mUserID = value;
				PlayerPrefs.SetString(SnipePrefs.LOGIN_USER_ID, mUserID.ToString());
				
				Analytics.SetUserId(mUserID.ToString());
			}
		}
		
		public bool JustRegistered { get; private set; } = false;

		private static List<AuthProvider> mAuthProviders;
		private static AuthProvider mCurrentProvider;

		private AuthResultCallback mAuthResultCallback;

		private static bool mRebindAllProviders = false;

		public ProviderType AddAuthProvider<ProviderType>() where ProviderType : AuthProvider, new()
		{
			ProviderType auth_provider = GetAuthProvider<ProviderType>();
			if (auth_provider == null)
			{
				auth_provider = new ProviderType();
				
				if (mAuthProviders == null)
					mAuthProviders = new List<AuthProvider>();
				
				mAuthProviders.Add(auth_provider);
			}

			return auth_provider;
		}

		public List<AuthProvider> GetAuthProviders()
		{
			return mAuthProviders;
		}

		public ProviderType GetAuthProvider<ProviderType>() where ProviderType : AuthProvider
		{
			if (mAuthProviders != null)
			{
				foreach (AuthProvider provider in mAuthProviders)
				{
					if (provider != null && provider is ProviderType)
					{
						return provider as ProviderType;
					}
				}
			}

			return null;
		}

		public AuthProvider GetAuthProvider(string provider_id)
		{
			if (mAuthProviders != null)
			{
				foreach (AuthProvider provider in mAuthProviders)
				{
					if (provider != null && provider.ProviderId == provider_id)
					{
						return provider;
					}
				}
			}

			return null;
		}

		public bool SetCurrentProvider(AuthProvider provider)
		{
			DebugLogger.Log($"[SnipeAuthCommunicator] SetCurrentProvider - {provider?.ProviderId}");

			if (provider == null)
			{
				if (mCurrentProvider != null)
				{
					mCurrentProvider.DisposeCallbacks();
					mCurrentProvider = null;
				}
				return false;
			}

			if (mCurrentProvider == provider || mCurrentProvider?.ProviderId == provider?.ProviderId)
				return true;

			if (mAuthProviders != null)
			{
				if (mAuthProviders.IndexOf(provider) >= 0)
				{
					if (mCurrentProvider != null)
						mCurrentProvider.DisposeCallbacks();

					mCurrentProvider = provider;
					return true;
				}
				else
				{
					var added_provider = GetAuthProvider(provider.ProviderId);
					if (added_provider != null)
					{
						if (mCurrentProvider != null)
							mCurrentProvider.DisposeCallbacks();

						mCurrentProvider = added_provider;
						return true;
					}
				}
			}

			return false;
		}

		public void SwitchToDefaultProvider()
		{
			SwitchToDefaultAuthProvider();
		}

		public void BindAllProviders(bool force_all = false, BindProvider.BindResultCallback single_bind_callback = null)
		{
			if (mAuthProviders != null)
			{
				foreach (var auth_provider in mAuthProviders)
				{
					if (auth_provider is BindProvider provider && (force_all || provider.AccountExists == false))
					{
						provider.RequestBind(single_bind_callback);
					}
				}
			}
		}

		private void ClearAllBindings()
		{
			if (mAuthProviders != null)
			{
				foreach (var auth_provider in mAuthProviders)
				{
					if (auth_provider is BindProvider provider)
					{
						PlayerPrefs.DeleteKey(provider.BindDonePrefsKey);
					}
				}
			}
		}

		public void Authorize<ProviderType>(AuthResultCallback callback = null) where ProviderType : AuthProvider
		{
			mCurrentProvider = GetAuthProvider<ProviderType>();

			if (mCurrentProvider == null)
			{
				DebugLogger.Log("[SnipeAuthCommunicator] Authorize<ProviderType> - provider not found");

				callback?.Invoke(SnipeErrorCodes.NOT_INITIALIZED, 0);

				return;
			}

			AuthorizeWithCurrentProvider(callback);
		}

		public void Authorize(AuthResultCallback callback = null)
		{
			if (mCurrentProvider == null)
			{
				if (!string.IsNullOrEmpty(PlayerPrefs.GetString(SnipePrefs.AUTH_KEY)))
					SwitchToDefaultProvider();
				else
					SwitchToNextAuthProvider();
			}

			AuthorizeWithCurrentProvider(callback);
		}

		public void Authorize(bool reset, AuthResultCallback callback = null)
		{
			if (reset) // forget previous provider and start again from the beginning
			{
				AuthProvider prev_provider = mCurrentProvider;

				mCurrentProvider = null; 
				SwitchToNextAuthProvider();

				if (prev_provider != mCurrentProvider)
					prev_provider.DisposeCallbacks();
			}

			Authorize(callback);
		}

		/// <summary>
		/// Clear all auth data and authorize using specified <c>AuthProvider</c>.
		/// </summary>
		public void ClearAuthDataAndSetCurrentProvider(AuthProvider provider)
		{
			PlayerPrefs.DeleteKey(SnipePrefs.LOGIN_USER_ID);
			PlayerPrefs.DeleteKey(SnipePrefs.AUTH_UID);
			PlayerPrefs.DeleteKey(SnipePrefs.AUTH_KEY);
			
			foreach (var auth_provider in mAuthProviders)
			{
				if (auth_provider is BindProvider bind_provider)
				{
					bind_provider.IsBindDone = false;
				}
			}
			
			SetCurrentProvider(provider);
		}

		/// <summary>
		/// After successful authorization with current provider <c>BindAllProviders(true)</c> will be called
		/// </summary>
		public void RebindAllProvidersAfterAuthorization()
		{
			mRebindAllProviders = true;
		}

		public void ClaimRestoreToken(string token, Action<bool> callback)
		{
			SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_RESTORE)?.RequestAuth(
				new SnipeObject()
				{
					["ckey"] = SnipeConfig.ClientKey,
					["token"] = token,
				},
				(error_code, response) =>
				{
					if (error_code == "ok")
					{
						ClearAllBindings();
						UserID = 0;
						PlayerPrefs.SetString(SnipePrefs.AUTH_UID, response.SafeGetString("uid"));
						PlayerPrefs.SetString(SnipePrefs.AUTH_KEY, response.SafeGetString("password"));
						PlayerPrefs.Save();
						callback?.Invoke(true);
					}
					else
					{
						callback?.Invoke(false);
					}
				});
		}
		
		public void GetUserAttribute(string provider_id, string user_id, string key, GetUserAttributeCallback callback)
		{
			SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_ATTR_GET)?.RequestAuth(
				new SnipeObject()
				{
					["provider"] = provider_id,
					["login"] = user_id,
					["key"] = key,
				},
				(error_code, response) =>
				{
					if (callback != null)
					{
						if (response != null)
						{
							callback.Invoke(error_code, response?.SafeGetString("name"), response?.SafeGetString("key"), response?["val"]);
						}
						else
						{
							callback.Invoke("error", "", key, null);
						}
					}
				});
		}

		internal void InvokeAccountBindingCollisionEvent(BindProvider provider, string user_name = null)
		{
			AccountBindingCollision?.Invoke(provider, user_name);
		}

		private void AuthorizeWithCurrentProvider(AuthResultCallback callback = null)
		{
			JustRegistered = false;
			mAuthResultCallback = callback;
			CurrentProviderRequestAuth();
		}

		private void CurrentProviderRequestAuth()
		{
			bool reset_auth = !(mCurrentProvider is DefaultAuthProvider) || string.IsNullOrEmpty(PlayerPrefs.GetString(SnipePrefs.AUTH_KEY));
			mCurrentProvider.RequestAuth(OnCurrentProviderAuthResult, reset_auth);
		}

		private void SwitchToNextAuthProvider(bool create_default = true)
		{
			AuthProvider prev_provider = mCurrentProvider;
			mCurrentProvider = null;

			if (mAuthProviders != null && mAuthProviders.Count > 0)
			{
				int next_index = 0;
				if (prev_provider != null)
				{
					next_index = mAuthProviders.IndexOf(prev_provider) + 1;
				}

				if (mAuthProviders.Count > next_index)
				{
					mCurrentProvider = mAuthProviders[next_index];
				}
			}

			if (mCurrentProvider == null && create_default)
			{
				mCurrentProvider = new DefaultAuthProvider();
			}
		}

		private void SwitchToDefaultAuthProvider()
		{
			if (mCurrentProvider != null && !(mCurrentProvider is DefaultAuthProvider))
			{
				mCurrentProvider.DisposeCallbacks();
				mCurrentProvider = null;
			}
			if (mCurrentProvider == null)
				mCurrentProvider = new DefaultAuthProvider();
		}

		private void OnCurrentProviderAuthResult(string error_code, int user_id = 0)
		{
			if (user_id != 0)
			{
				UserID = user_id;

				InvokeAuthSuccessCallback(user_id);

				mCurrentProvider?.DisposeCallbacks();
				mCurrentProvider = null;

				BindAllProviders(mRebindAllProviders);
				mRebindAllProviders = false;
			}
			else
			{
				DebugLogger.Log("[SnipeAuthCommunicator] OnCurrentProviderAuthFail (" + (mCurrentProvider != null ? mCurrentProvider.ProviderId : "null") + ") error_code: " + error_code);
				
				mRebindAllProviders = false;
				
				if (error_code == SnipeErrorCodes.NO_SUCH_USER ||
					error_code == SnipeErrorCodes.NO_SUCH_AUTH ||
					error_code == SnipeErrorCodes.NOT_INITIALIZED)
				{
					if (mAuthProviders != null && mAuthProviders.Count > mAuthProviders.IndexOf(mCurrentProvider) + 1)
					{
						// try next provider
						mCurrentProvider?.DisposeCallbacks();

						SwitchToNextAuthProvider();
						CurrentProviderRequestAuth();
					}
					else // all providers failed
					{
						RequestRegister();
					}
				}
				else
				{
					InvokeAuthFailCallback(error_code);
				}
			}
		}

		private void InvokeAuthSuccessCallback(int user_id)
		{
			mAuthResultCallback?.Invoke(SnipeErrorCodes.OK, user_id);
			mAuthResultCallback = null;
		}

		private void InvokeAuthFailCallback(string error_code)
		{
			mAuthResultCallback?.Invoke(error_code, 0);
			mAuthResultCallback = null;

			mCurrentProvider?.DisposeCallbacks();
			mCurrentProvider = null;
		}

		private void RequestRegister()
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			
			SnipeCommunicator.Instance.CreateRequest(SnipeMessageTypes.AUTH_USER_REGISTER)?.RequestAuth(null,
				(error_code, response) =>
				{
					stopwatch.Stop();
					
					int user_id = 0;
					
					if (error_code == "ok")
					{
						JustRegistered = true;

						string auth_login = response.SafeGetString("uid");
						string auth_token = response.SafeGetString("password");

						PlayerPrefs.SetString(SnipePrefs.AUTH_UID, auth_login);
						PlayerPrefs.SetString(SnipePrefs.AUTH_KEY, auth_token);

						user_id = response.SafeGetValue<int>("id");
						
						Analytics.SetUserId(user_id.ToString());
						Analytics.TrackEvent(Analytics.EVENT_ACCOUNT_REGISTERED, new SnipeObject()
						{
							["user_id"] = user_id,
							["request_time"] = stopwatch.ElapsedMilliseconds,
						});

						SwitchToDefaultAuthProvider();
						mCurrentProvider.RequestAuth(OnCurrentProviderAuthResult);

						BindAllProviders(false);
					}
					else
					{
						Analytics.TrackEvent(Analytics.EVENT_ACCOUNT_REGISTERATION_FAILED, new SnipeObject()
						{
							["error_code"] = error_code,
							["request_time"] = stopwatch.ElapsedMilliseconds,
						});
						
						InvokeAuthFailCallback(error_code);
					}

					AccountRegisterResponse?.Invoke(error_code, user_id);
				});
		}
	}
}