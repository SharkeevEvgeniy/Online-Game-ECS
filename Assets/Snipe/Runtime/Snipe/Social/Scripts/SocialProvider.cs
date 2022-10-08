using System;
using System.Collections.Generic;

namespace MiniIT.Social
{
	public abstract class SocialProvider
	{
		public event Action LoggedOut;

		public bool Initialized { get; protected set; }

		public string NetworkType { get; protected set; }
		public SocialUserProfile PlayerProfile { get; protected set; }

		public SocialProvider(string network_type)
		{
			this.NetworkType = network_type;
		}

		public abstract void Init(Action callback = null, Action fail_callback = null);

		public virtual void Logout()
		{
			LoggedOut?.Invoke();
		}

		public virtual string GetPlayerUserID()
		{
			if (PlayerProfile != null)
				return PlayerProfile.Id;
			return "";
		}

		#region Profiles cache

		private static Dictionary<string, SocialUserProfile> mProfilesCache = new Dictionary<string, SocialUserProfile>();

		protected void AddProfileToCache(SocialUserProfile profile)
		{
			if (profile != null &&
				 !string.IsNullOrEmpty(profile.Id) &&
				 !string.IsNullOrEmpty(profile.FirstName))
			{
				mProfilesCache[profile.CombinedUserID] = profile;
			}
		}

		public bool HasProfileInCache(string id)
		{
			return mProfilesCache.ContainsKey(NetworkType + id);
		}

		public bool HasProfileData(string id, string nt)
		{
			return mProfilesCache.ContainsKey(nt + id);
		}

		public SocialUserProfile GetCachedProfile(string id, string nt = null)
		{
			if (string.IsNullOrEmpty(nt))
				nt = this.NetworkType;

			SocialUserProfile profile = null;
			if (mProfilesCache.TryGetValue(nt + id, out profile))
					return profile;

			return null;
		}

		protected List<SocialUserProfile> ProfilesCached(IList<string> userids)
		{
			// если все запрашиваемые профили уже в кэше, вернем их сразу
			List<SocialUserProfile> profiles = new List<SocialUserProfile>();
			foreach (string uid in userids)
			{
				SocialUserProfile profile;
				mProfilesCache.TryGetValue(NetworkType + uid, out profile);
				if (profile != null)
				{
					profiles.Add(profile);
				}
				else // если не найден хоть один профиль, выходим из цикла
				{
					break;
				}
			}

			// если найдены все, сгенерируем  положительный ответ
			if (profiles.Count == userids.Count)
				return profiles;

			return null;
		}

		#endregion

		public bool ForceRequestCachedProfiles { get; set; }

		public void RequestProfiles(string userids, Action<IList<SocialUserProfile>> callback = null)
		{
			if (!string.IsNullOrEmpty(userids))
				RequestProfiles(userids.Replace(" ", "").Split(','), callback);
		}

		public void RequestProfiles(IList<string> userids, Action<IList<SocialUserProfile>> callback = null)
		{
			if (userids == null && userids.Count == 0)
				return;

			// если все запрашиваемые профили уже в кэше, вернем их сразу
			List<SocialUserProfile> profiles = ProfilesCached(userids);
			if (profiles != null)
			{
				if (callback != null)
					callback.Invoke(profiles);

				DispatchEventProfilesReceived(profiles);
			}

			// но, если необходимо, запрос все равно выполним для актуализации данных (например может измениться поле online)
			if (profiles == null || ForceRequestCachedProfiles)
			{
				DoRequestProfiles(userids,
					(IList<SocialUserProfile> profiles_data) =>
					{
						if (callback != null)
							callback.Invoke(profiles_data);

						OnProfiles(profiles_data);
					}
				);
			}
		}

		protected virtual void DoRequestProfiles(IList<string> userids, Action<IList<SocialUserProfile>> callback)
		{
			// Override in child class
		}

		protected virtual void OnProfiles(IList<SocialUserProfile> profiles)
		{
			DispatchEventProfilesReceived(profiles);
		}

		#region Events

		public event Action InitializationComplete;
		public event Action InitializationFailed;
		public event Action<IList<SocialUserProfile>> ProfilesReceived;

		protected void DispatchEventInitializationComplete()
		{
			if (InitializationComplete != null)
				InitializationComplete.Invoke();
		}

		protected void DispatchEventInitializationFailed()
		{
			if (InitializationFailed != null)
				InitializationFailed.Invoke();
		}

		protected void DispatchEventProfilesReceived(IList<SocialUserProfile> profiles)
		{
			if (ProfilesReceived != null)
				ProfilesReceived.Invoke(profiles);
		}

		#endregion

	}
}