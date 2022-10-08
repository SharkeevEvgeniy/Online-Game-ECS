using System;
using MiniIT;

namespace MiniIT.Snipe
{
	public static class SnipeApiBase
	{
		public static SnipeCommunicatorRequest CreateRequest(string message_type, SnipeObject data)
		{
			if (SnipeCommunicator.Instance.LoggedIn || SnipeCommunicator.Instance.AllowRequestsToWaitForLogin)
			{
				return SnipeCommunicator.Instance.CreateRequest(message_type, data);
			}
			
			return null;
		}
	}
}