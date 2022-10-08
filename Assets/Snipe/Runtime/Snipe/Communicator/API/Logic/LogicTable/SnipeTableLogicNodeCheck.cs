
namespace MiniIT.Snipe
{
	public class SnipeTableLogicRawNodeCheck
	{
		public string type;
		public string name;
		public string @operator;
		public int value;
	}

	public class SnipeTableLogicNodeCheck
	{
		public const string TYPE_ATTR = "attr";
		public const string TYPE_CLIENT = "client";
		public const string TYPE_RELATIVE = "relative";
		public const string TYPE_COUNTER = "counter";
		public const string TYPE_COND_COUNTER = "condCounter";
		public const string TYPE_TIMER = "timer";
		public const string TYPE_TIMEOUT = "timeout";
		public const string TYPE_PAYMENT_ITEM_STRING_ID = "paymentItemStringID";

		public string type;

		public static implicit operator SnipeTableLogicNodeCheck(SnipeTableLogicRawNodeCheck raw)
		{
			SnipeTableLogicNodeCheck result = null;
			
			switch (raw?.type)
			{
				case TYPE_ATTR:
					result = new SnipeTableLogicNodeCheckAttr()
					{
						type = raw.type,
						@operator = raw.@operator,
						value = raw.value,
						name = raw.name,
					};
					break;
				
				case TYPE_CLIENT:
					result = new SnipeTableLogicNodeCheckClient()
					{
						type = raw.type,
						@operator = raw.@operator,
						value = raw.value,
						name = raw.name,
					};
					break;
				
				case TYPE_RELATIVE:
					result = new SnipeTableLogicNodeCheckRelative()
					{
						type = raw.type,
						@operator = raw.@operator,
						value = raw.value,
						name = raw.name,
					};
					break;
				
				case TYPE_COUNTER:
					result = new SnipeTableLogicNodeCheckCounter()
					{
						type = raw.type,
						value = raw.value,
						name = raw.name,
					};
					break;
				
				case TYPE_COND_COUNTER:
					result = new SnipeTableLogicNodeCheckCondCounter()
					{
						type = raw.type,
						value = raw.value,
						name = raw.name,
					};
					break;
				
				case TYPE_TIMER:
					result = new SnipeTableLogicNodeCheckTimer()
					{
						type = raw.type,
						value = raw.value,
					};
					break;
				
				case TYPE_TIMEOUT:
					result = new SnipeTableLogicNodeCheckTimeout()
					{
						type = raw.type,
						value = raw.value,
					};
					break;
				
				case TYPE_PAYMENT_ITEM_STRING_ID:
					result = new SnipeTableLogicNodeCheckPaymentItemStringID()
					{
						type = raw.type,
						name = raw.name,
					};
					break;
			}
			
			return result;
		}
	}
	
	public class SnipeTableLogicNodeCheckCounter : SnipeTableLogicNodeCheck
	{
		public string name;
		public int value;
	}
	
	public class SnipeTableLogicNodeCheckCondCounter : SnipeTableLogicNodeCheckCounter
	{
	}
	
	public class SnipeTableLogicNodeCheckRelative : SnipeTableLogicNodeCheckCounter
	{
		public string @operator;
	}
	
	public class SnipeTableLogicNodeCheckClient : SnipeTableLogicNodeCheckRelative
	{
	}
	
	public class SnipeTableLogicNodeCheckTimer : SnipeTableLogicNodeCheck
	{
		public int value;
	}
	
	public class SnipeTableLogicNodeCheckTimeout : SnipeTableLogicNodeCheckTimer
	{
	}
	
	public class SnipeTableLogicNodeCheckAttr : SnipeTableLogicNodeCheckRelative
	{
	}
	
	public class SnipeTableLogicNodeCheckPaymentItemStringID : SnipeTableLogicNodeCheck
	{
		public string name;
	}
}