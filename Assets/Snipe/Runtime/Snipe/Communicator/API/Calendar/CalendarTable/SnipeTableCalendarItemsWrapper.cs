using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	public class SnipeTableCalendarItemsWrapper : ISnipeTableItemsListWrapper<SnipeTableCalendarItem>
	{
		public List<SnipeTableCalendarItem> list { get; set; }
		
		public static SnipeTableCalendarItemsWrapper FromTableData(Dictionary<string, object> table_data)
		{
			if (table_data != null && table_data.TryGetValue("list", out var table_list_data) && table_list_data is IList table_list)
			{
				var calendar_list_wrapper = new SnipeTableCalendarItemsWrapper();
				calendar_list_wrapper.list = new List<SnipeTableCalendarItem>();
				foreach (Dictionary<string, object> calendar_item_data in table_list)
				{
					var calendar_event = new SnipeTableCalendarItem();
					calendar_list_wrapper.list.Add(calendar_event);

					if (calendar_item_data.TryGetValue("id", out var calendar_event_id))
						calendar_event.id = Convert.ToInt32(calendar_event_id);
					if (calendar_item_data.TryGetValue("name", out var calendar_event_name))
						calendar_event.name = Convert.ToString(calendar_event_name);
					if (calendar_item_data.TryGetValue("stringID", out var calendar_event_stringID))
						calendar_event.stringID = Convert.ToString(calendar_event_stringID);
					if (calendar_item_data.TryGetValue("startDate", out var calendar_event_startDate))
						calendar_event.startDate = Convert.ToInt32(calendar_event_startDate);
					if (calendar_item_data.TryGetValue("endDate", out var calendar_event_endDate))
						calendar_event.endDate = Convert.ToInt32(calendar_event_endDate);
					if (calendar_item_data.TryGetValue("isInfinite", out var calendar_event_isInfinite))
						calendar_event.isInfinite = Convert.ToBoolean(calendar_event_isInfinite);
					if (calendar_item_data.TryGetValue("data", out var calendar_event_data))
						calendar_event.data = calendar_event_data as SnipeObject;
					
					calendar_event.stages = new List<SnipeTableCalendarItemStage>();
					if (calendar_item_data.TryGetValue("stages", out var calendar_event_stages) && calendar_event_stages is IList calendar_event_stages_list)
					{
						foreach (Dictionary<string, object> stage_item_data in calendar_event_stages_list)
						{
							var stage = new SnipeTableCalendarItemStage();
							calendar_event.stages.Add(stage);

							if (stage_item_data.TryGetValue("id", out var stage_id))
								stage.id = Convert.ToInt32(stage_id);
							if (stage_item_data.TryGetValue("name", out var stage_name))
								stage.name = Convert.ToString(stage_name);
							if (stage_item_data.TryGetValue("stringID", out var stage_stringID))
								stage.stringID = Convert.ToString(stage_stringID);
							if (stage_item_data.TryGetValue("repeatType", out var stage_repeatType))
								stage.repeatType = Convert.ToString(stage_repeatType);
							if (stage_item_data.TryGetValue("repeatNumber", out var stage_repeatNumber))
								stage.repeatNumber = Convert.ToInt32(stage_repeatNumber);
							if (stage_item_data.TryGetValue("repeatMonthDay", out var stage_repeatMonthDay))
								stage.repeatMonthDay = Convert.ToInt32(stage_repeatMonthDay);
							if (stage_item_data.TryGetValue("data", out var stage_data))
								stage.data = stage_data as SnipeObject;

							if (stage_item_data.TryGetValue("repeatWeekDays", out var days) && days is IList days_list)
							{
								stage.repeatWeekDays = new int[days_list.Count];

								for (int i = 0; i < days_list.Count; i++)
								{
									stage.repeatWeekDays[i] = Convert.ToInt32(days_list[i]);
								}
							}
							else
							{
								stage.repeatWeekDays = new int[0];
							}
						}
					}
				}
				return calendar_list_wrapper;
			}
			
			return null;
		}
	}
}