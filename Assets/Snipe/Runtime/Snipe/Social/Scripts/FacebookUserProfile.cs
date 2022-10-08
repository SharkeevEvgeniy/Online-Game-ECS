#if SNIPE_FACEBOOK

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniIT;
using MiniIT.Utils;


namespace MiniIT.Social
{
	[System.Serializable]
	public class FacebookUserProfile : SocialUserProfile
	{
		public FacebookUserProfile(string id = "", string network_type = "__") : base(id, network_type)
		{
		}

		public override SnipeObject ToObject(bool full = true)
		{
			SnipeObject profile = new SnipeObject();
			profile["id"]   = this.Id;
			profile["networktype"]  = this.NetworkType;
			profile["first_name"]   = this.FirstName;
			profile["last_name"]    = this.LastName;
			if (full)
			{
				profile["photo_small"]  = this.PhotoSmallURL;
				profile["photo_medium"] = this.PhotoMediumURL;
				profile["link"] = this.Link;
				profile["gender"] = this.Gender;
				profile["online"] = this.Online;
				profile["invitable"] = this.Invitable;
			}
			return profile;
		}

		public static new SocialUserProfile FromObject(object raw_data)
		{
			var profile = SocialUserProfile.FromObject(raw_data);

			//if (string.IsNullOrEmpty(profile.PhotoSmallURL))
				profile.PhotoSmallURL = "https://graph.facebook.com/" + profile.Id + "/picture/?width=50&height=50";
			//if (string.IsNullOrEmpty(profile.PhotoMediumURL))
				profile.PhotoMediumURL = "https://graph.facebook.com/" + profile.Id + "/picture/?width=100&height=100";

			return profile;
		}
	}
}

#endif