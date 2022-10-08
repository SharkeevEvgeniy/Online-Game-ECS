using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	[System.Serializable]
	public class SnipeTableCalendarItem : SnipeTableItem
	{
		public string name;
		public string stringID;
		public int startDate;
		public int endDate;
		public bool isInfinite;
		public SnipeObject data;
		public List<SnipeTableCalendarItemStage> stages;
	}
}