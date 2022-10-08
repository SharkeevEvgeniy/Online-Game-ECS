using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	[System.Serializable]
	public class SnipeTableCalendarItemStage
	{
		public int id;
		public string name;
		public string stringID;
		public int minHour;
		public int maxHour;
		public string repeatType;
		public int repeatNumber;
		public int[] repeatWeekDays;
		public int repeatMonthDay;
		public SnipeObject data;
	}
}