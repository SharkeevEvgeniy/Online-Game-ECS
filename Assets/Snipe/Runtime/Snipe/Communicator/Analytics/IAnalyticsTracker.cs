using System;
using System.Collections;
using System.Collections.Generic;
using MiniIT;

namespace MiniIT.Snipe
{
	public interface IAnalyticsTracker
	{
		bool IsInitialized { get; }
		
		void SetUserId(string uid);
		void SetUserProperty(string name, string value);
		void SetUserProperty(string name, int value);
		void SetUserProperty(string name, float value);
		void SetUserProperty(string name, double value);
		void SetUserProperty(string name, bool value);
		void SetUserProperty<T>(string name, IList<T> value);
		void SetUserProperty(string name, IDictionary<string, object> value);

		void TrackEvent(string name, IDictionary<string, object> properties = null);
		//void TrackEvent(string name, string property_name, object property_value);
		//void TrackEvent(string name, object property_value);
		
		void TrackError(string name, Exception exception = null);
		
		// Used for excluding some messages or error codes from analytics tracking
		bool CheckErrorCodeTracking(string message_type, string error_code);
	}
}