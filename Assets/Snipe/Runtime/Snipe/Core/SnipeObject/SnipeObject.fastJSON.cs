
//  JSON support


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MiniIT
{
	public partial class SnipeObject
	{
		public string ToFastJSONString()
		{
			return fastJSON.JSON.ToJSON(this);
		}

		public static SnipeObject FromFastJSONString(string input_string)
		{
			var decoded = fastJSON.JSON.Parse(input_string);
			if (decoded is Dictionary<string, object> dict)
				return new SnipeObject(dict);
			else
				return new SnipeObject() { ["data"] = decoded };
		}
	}
}