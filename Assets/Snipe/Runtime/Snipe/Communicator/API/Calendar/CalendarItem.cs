using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	public class CalendarItem
	{
		public int id;
		
		public SnipeTableCalendarItem node { get; private set; }

		public string name { get => node?.name; }
		public string stringID { get => node?.stringID; }
		public List<SnipeTableCalendarItemStage> stages { get => node?.stages; }

		public int timeleft = -1; // seconds left. (-1) means that the node does not have a timer
		public bool isTimeout { get; private set; }

		public CalendarItem(SnipeObject data, SnipeTable<SnipeTableCalendarItem> logic_table)
		{
			id = data.SafeGetValue<int>("id");
			
			
		}
	}
}