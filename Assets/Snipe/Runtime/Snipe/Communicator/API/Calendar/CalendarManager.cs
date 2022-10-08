using System;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	public class CalendarManager
	{
		public TimeZoneInfo ServerTimeZone = TimeZoneInfo.CreateCustomTimeZone("server time", TimeSpan.FromHours(3), "server time", "server time");

		private SnipeTable<SnipeTableCalendarItem> mCalendarTable = null;

		public void Init(SnipeTable<SnipeTableCalendarItem> calendar_table)
		{
			mCalendarTable = calendar_table;
		}

		~CalendarManager()
		{
			Dispose();
		}

		public void Dispose()
		{
			mCalendarTable = null;
		}

		public bool IsEventActive(string eventID)
		{
			SnipeTableCalendarItem item = GetItem(eventID);
			if (item != null)
			{
				var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				return now > item.startDate && (item.isInfinite ||  now < item.endDate);
			}

			return false;
		}

		public bool IsEventStageActive(string eventID, string stageID)
		{
			SnipeTableCalendarItem item = GetItem(eventID);
			if (item != null)
			{
				var now_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				if (now_ts > item.startDate && (item.isInfinite || now_ts < item.endDate))
				{
					SnipeTableCalendarItemStage stage = null;
					foreach (var s in item.stages)
					{
						if (s.stringID == stageID)
						{
							stage = s;
							break;
						}
					}

					return IsEventStageActive(item, stage);
				}
			}

			return false;
		}

		public List<SnipeTableCalendarItem> GetActiveEvents()
		{
			var result = new List<SnipeTableCalendarItem>();

			if (mCalendarTable?.Items != null)
			{
				foreach (var item in mCalendarTable.Items.Values)
				{
					if (IsEventActive(item.stringID))
					{
						result.Add(item);
					}
				}
			}

			return result;
		}

		public SnipeTableCalendarItemStage GetActiveEventStage(string eventID)
		{
			SnipeTableCalendarItem item = GetItem(eventID);
			if (item != null)
			{
				var now_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				if (now_ts > item.startDate && (item.isInfinite || now_ts < item.endDate))
				{
					foreach (var stage in item.stages)
					{
						if (IsEventStageActive(item, stage))
						{
							return stage;
						}
					}
				}
			}

			return null;
		}

		private bool IsEventStageActive(SnipeTableCalendarItem item, SnipeTableCalendarItemStage stage)
		{
			if (item == null || stage == null)
				return false;

			var now_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var server_time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServerTimeZone);

			int maxHour = stage.maxHour > 0 ? stage.maxHour : 24;
			if (server_time.Hour < stage.minHour || server_time.Hour >= maxHour)
				return false;

			switch (stage.repeatType)
			{
				case "day":
					int day_number = (int)TimeSpan.FromSeconds(now_ts - item.startDate).TotalDays + 1;
					return (stage.repeatNumber < 2 || day_number % stage.repeatNumber == 1);

				case "week":
					int week_number = (int)(TimeSpan.FromSeconds(now_ts - item.startDate).TotalDays / 7) + 1;
					if ((stage.repeatNumber < 2 || week_number % stage.repeatNumber == 1) && stage.repeatWeekDays != null)
					{
						int today_week_day = (server_time.DayOfWeek == DayOfWeek.Sunday) ? 7 : (int)server_time.DayOfWeek;

						foreach (int week_day in stage.repeatWeekDays)
						{
							if (week_day == today_week_day)
								return true;
						}
					}
					break;

				case "month":
					var server_start_time = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(item.startDate).DateTime, ServerTimeZone);
					int month_number = (server_time.Month > server_start_time.Month ? server_time.Month : server_time.Month + 12) - server_start_time.Month + 1;
					if (stage.repeatNumber < 2 || month_number % stage.repeatNumber == 1)
					{
						return (server_time.Day == stage.repeatMonthDay);
					}
					break;

				//case "none":
				default:
					return true;
			}

			return false;
		}

		public TimeSpan GetEvetTimeLeft(string eventID)
		{
			SnipeTableCalendarItem item = GetItem(eventID);
			if (item != null)
			{
				var now_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				if (now_ts > item.startDate)
				{
					if (item.isInfinite)
						return TimeSpan.MaxValue;

					return TimeSpan.FromSeconds(item.endDate - now_ts);
				}
			}

			return TimeSpan.Zero;
		}

		public TimeSpan GetEvetStageTimeLeft(string eventID, string stageID)
		{
			SnipeTableCalendarItem item = GetItem(eventID);
			if (item != null)
			{
				var now_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				if (now_ts > item.startDate && (item.isInfinite || now_ts < item.endDate))
				{
					SnipeTableCalendarItemStage stage = null;
					foreach (var s in item.stages)
					{
						if (s.stringID == stageID)
						{
							stage = s;
							break;
						}
					}

					if (stage != null)
					{
						switch (stage.repeatType)
						{
							//case "day":
							//	break;

							case "week":
								var server_time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServerTimeZone);
								int week_number = (int)(TimeSpan.FromSeconds(now_ts - item.startDate).TotalDays / 7) + 1;
								if ((stage.repeatNumber < 2 || week_number % stage.repeatNumber == 1) && stage.repeatWeekDays != null)
								{
									int today_week_day = (server_time.DayOfWeek == DayOfWeek.Sunday) ? 7 : (int)server_time.DayOfWeek;
									int end_day = -1;

									for (int i = 0; i < stage.repeatWeekDays.Length; i++)
									{
										if (stage.repeatWeekDays[i] == today_week_day)
										{
											end_day = today_week_day;
											for (int j = i; j < stage.repeatWeekDays.Length; j++)
											{
												if (stage.repeatWeekDays[j] == end_day + 1)
													end_day++;
												else
													break;
											}
											break;
										}
									}

									if (end_day >= today_week_day)
									{
										var end_date = new DateTime(server_time.Year, server_time.Month, server_time.Day).AddDays(end_day - today_week_day + 1);
										return (end_date - server_time);
									}
								}
								break;

							//case "month":
							//	break;

							//case "none":
							default:
								return TimeSpan.FromSeconds(item.endDate - now_ts);
						}
					}
				}
			}

			return TimeSpan.Zero;
		}

		private SnipeTableCalendarItem GetItem(string eventID)
		{
			if (mCalendarTable?.Items == null)
				return null;

			foreach (var item in mCalendarTable.Items.Values)
			{
				if (item.stringID == eventID)
				{
					return item;
				}
			}

			return null;
		}
	}
}