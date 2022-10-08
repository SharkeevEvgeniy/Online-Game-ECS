using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniIT.Snipe
{
	public class SnipePrefs
	{
		public static string PREFIX = "com.miniit.snipe.";

		public static string LOGIN_USER_ID { get { return PREFIX + "LoginUserID"; } }

		public static string AUTH_UID { get { return PREFIX + "AuthUID"; } }
		public static string AUTH_KEY { get { return PREFIX + "AuthKey"; } }

		public static string AUTH_BIND_DONE { get { return PREFIX + "AuthBinded_"; } }
		
		public static string SKIP_UDP { get { return PREFIX + "SkipUDP"; } } // 0 - udp allowed; 1 - skip udp; 2 - both failed
	}
}