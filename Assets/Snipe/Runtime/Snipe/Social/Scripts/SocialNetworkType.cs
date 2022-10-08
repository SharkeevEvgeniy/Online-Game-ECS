using System;

namespace MiniIT.Social
{
	public class SocialNetworkType
	{
		public const string NONE               = "__";
		public const string VK                 = "VK";
		public const string MAILRU             = "MM";
		public const string ODNOKLASSNIKI      = "OD";
		public const string FACEBOOK           = "FB";
		
		public static string GetCorrectValue(string id = NONE) 
		{
			string value = id.ToUpper();
			if (value != VK &&
				value != MAILRU &&
				value != ODNOKLASSNIKI &&
				value != FACEBOOK)
			{
				value = NONE;
			}
			return value;
		}
	}

}