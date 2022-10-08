using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniIT;
using MiniIT.Utils;


namespace MiniIT.Social
{
	[System.Serializable]
	public class SocialUserProfile
	{
		protected string mID;                  // id пользователя в соцсети
		protected string mSocialNetworkType;

		protected SnipeObject mRawData;  // данные профиля в том виде, котором они получены от соцсети
		
		public string FirstName;          // имя пользователя
		public string LastName;           // фамилия пользователя
		public string PhotoSmallURL;         // url маленькой аватарки (обычно 50x50px)
		public string PhotoMediumURL;        // url средней аватарки (обычно от 100x100px до 200x200px)
		public string Link;                // url сраницы профиля
		public int Gender;                 // 1-мужской, 2-женский;
		public bool Online;                // может содержать не всегда достоверное значение т.к. не все соцсети предоставляют данные об онлайне пользователя
		public bool Invitable;

		public Texture2D LoadedPhotoSmall;         // аватарка (обычно 50x50px)
		public Texture2D LoadedPhotoMedium;        // аватарка (обычно от 100x100px до 200x200px)
		
		private string mCombinedID = "";   // кобминированный ID пользователя, включающий ID соцсети и ID пользователя в этой соцсети

		protected SimpleImageLoader mSmallPhotoLoader;
		protected SimpleImageLoader mMediumPhotoLoader;

		public SocialUserProfile(string id = "", string network_type = "__")
		{
			this.Id = id;
			this.mSocialNetworkType = network_type;
			UpdateCombinedID();
		}
		
		public string Id
		{
			get { return mID; }
			set
			{
				mID = value;
				UpdateCombinedID();
			}
		}
		
		public string NetworkType
		{
			get { return mSocialNetworkType; }
			set
			{
				mSocialNetworkType = SocialNetworkType.GetCorrectValue(value);
				UpdateCombinedID();
			}
		}
		
		public string CombinedUserID
		{
			get { return mCombinedID; }
		}
		
		private void UpdateCombinedID()
		{
			mCombinedID = mSocialNetworkType + mID;
		}
		
		public bool Equals(string id, string network_type)
		{
			return (this.Id == id && mSocialNetworkType == network_type);
		}
		
		public new string ToString()
		{
			return CombinedUserID;
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SocialUserProfile))
				return false;

			SocialUserProfile p = (obj as SocialUserProfile);
			return this.Equals(p.Id, p.NetworkType); //????
		}
		
		public virtual SnipeObject ToObject(bool full = true)
		{
			SnipeObject profile = new SnipeObject();
			profile["id"]   = this.Id;
			profile["networktype"]  = this.NetworkType;
			profile["first_name"]   = this.FirstName;
			profile["last_name"]    = this.LastName;
			profile["photo_small"]  = this.PhotoSmallURL;
			profile["photo_medium"] = this.PhotoMediumURL;
			if (full)
			{
				profile["link"] = this.Link;
				profile["gender"] = this.Gender;
				profile["online"] = this.Online;
				profile["invitable"] = this.Invitable;
			}
			return profile;
		}

		public virtual string ToJSONString(bool full = true)
		{
			return this.ToObject(full).ToJSONString();
		}
		
		public SocialUserProfile Clone()
		{
			SocialUserProfile profile = new SocialUserProfile(this.Id, this.NetworkType);
			profile.FirstName   = this.FirstName;
			profile.LastName    = this.LastName;
			profile.PhotoSmallURL  = this.PhotoSmallURL;
			profile.PhotoMediumURL = this.PhotoMediumURL;
			profile.Link         = this.Link;
			profile.Gender       = this.Gender;
			profile.Online       = this.Online;
			profile.Invitable    = this.Invitable;
			return profile;
		}

		public static SocialUserProfile FromJSON(string json)
		{
			return FromObject(SnipeObject.FromJSONString(json));
		}

		public static SocialUserProfile FromObject(object raw_data)
		{
			if (raw_data is SocialUserProfile)
				return (raw_data as SocialUserProfile).Clone();
			
			if (!(raw_data is SnipeObject))
				return new SocialUserProfile();
			
			SnipeObject data = (SnipeObject)raw_data;
			
			SocialUserProfile profile = new SocialUserProfile();
			if (data.ContainsKey("id"))
				profile.Id = data.SafeGetString("id");
			else if (data.ContainsKey("networkid"))             // в таком формате данные могут прийти от игрового сервера
				profile.Id = data.SafeGetString("networkid");
			
			if (data.ContainsKey("network_type"))
				profile.NetworkType = data.SafeGetString("network_type");
			else if (data.ContainsKey("networktype"))           // в таком формате данные могут прийти от игрового сервера
				profile.NetworkType = data.SafeGetString("networktype");
			else if (data.ContainsKey("nt"))                    // в таком формате данные могут прийти от игрового сервера
				profile.NetworkType = data.SafeGetString("nt");
			else if (data.ContainsKey("netid"))                 // в таком формате данные могут прийти от игрового сервера
				profile.NetworkType = data.SafeGetString("netid");
			
			if (data.ContainsKey("first_name"))
				profile.FirstName = data.SafeGetString("first_name");
			if (data.ContainsKey("last_name"))
				profile.LastName = data.SafeGetString("last_name");
			if (data.ContainsKey("photo_small"))
				profile.PhotoSmallURL = data.SafeGetString("photo_small");
			if (data.ContainsKey("photo_medium"))
				profile.PhotoMediumURL = data.SafeGetString("photo_medium");
			if (data.ContainsKey("link"))
				profile.Link = data.SafeGetString("link");
			if (data.ContainsKey("gender"))
				profile.Gender = Convert.ToInt32( data["gender"] );
			if (data.ContainsKey("online"))
				profile.Online = Convert.ToBoolean( data["online"] );

			if (data.ContainsKey("invitable"))
				profile.Invitable = Convert.ToBoolean( data["invitable"] );

			profile.UpdateCombinedID();

			return profile;
		}
		
		public string Name
		{
			get { return FirstName + " " + LastName; }
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					string name_value = value.Replace("\t", " ");
					int space_index = name_value.LastIndexOf(" ");
					if (space_index > 0)
					{
						FirstName = name_value.Substring(0, space_index);
						LastName = name_value.Substring(space_index + 1);
					}
					else
					{
						FirstName = name_value;
						LastName = "";
					}
				}
				else
				{
					FirstName = "";
					LastName = "";
				}
			}
		}

		public string GetFullName(string separator = " ")
		{
			return FirstName + separator + LastName;
		}
		
		public SnipeObject Raw
		{
			get { return mRawData; }
			internal set { mRawData = value; }
		}
		
		internal void SetRawData(SnipeObject data)
		{
			mRawData = data;
		}

		//-------------------------------------------

		public SimpleImageLoader LoadPhotoSmall(Action<Texture2D> callback = null)
		{
			if (this.LoadedPhotoSmall != null)
			{
				if (callback != null)
					callback.Invoke(this.LoadedPhotoSmall);

				return null;
			}
			else
			{
				if (mSmallPhotoLoader != null && mSmallPhotoLoader.Url != this.PhotoSmallURL)
					mSmallPhotoLoader.Cancel();

				mSmallPhotoLoader = SimpleImageLoader.Load(this.PhotoSmallURL,
					(Texture2D texture) =>
					{
						LoadedPhotoSmall = texture;
						if (callback != null)
							callback.Invoke(texture);

						mSmallPhotoLoader = null;
					},
					true
				);
			}

			return mSmallPhotoLoader;
		}

		public SimpleImageLoader LoadPhotoMedium(Action<Texture2D> callback = null)
		{
			if (this.LoadedPhotoMedium != null)
			{
				if (callback != null)
					callback.Invoke(this.LoadedPhotoMedium);

				return null;
			}
			else
			{
				if (mMediumPhotoLoader != null && mMediumPhotoLoader.Url != this.PhotoMediumURL)
					mMediumPhotoLoader.Cancel();

				mMediumPhotoLoader = SimpleImageLoader.Load(this.PhotoMediumURL,
					(Texture2D texture) =>
					{
						LoadedPhotoMedium = texture;
						if (callback != null)
							callback.Invoke(texture);

						mMediumPhotoLoader = null;
					},
					true
				);
			}

			return mMediumPhotoLoader;
		}
	}
}

