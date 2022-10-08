
namespace MiniIT.Snipe
{
	public class SnipeTableLogicRawNodeVar
	{
		public string type;
		public string name;
		public string @operator;
		public int value;
		public float condValue;
	}
	
	public class SnipeTableLogicNodeVar
	{
		public const string TYPE_ATTR = "attr";
		public const string TYPE_CLIENT = "client";
		public const string TYPE_COUNTER = "counter";
		public const string TYPE_COND_COUNTER = "condCounter";

		// checks
		public const string TYPE_RELATIVE = "relative";
		public const string TYPE_TIMER = "timer";
		public const string TYPE_TIMEOUT = "timeout";
		public const string TYPE_PAYMENT_ITEM_STRING_ID = "paymentItemStringID";

		public string type;
		public string name;

		public static implicit operator SnipeTableLogicNodeVar(SnipeTableLogicRawNodeVar raw)
		{
			SnipeTableLogicNodeVar result = null;

			switch (raw?.type)
			{
				case TYPE_ATTR:
					result = new SnipeTableLogicNodeVarAttr()
					{
						type = raw.type,
						name = raw.name,
					};
					break;

				case TYPE_CLIENT:
					result = new SnipeTableLogicNodeVarClient()
					{
						type = raw.type,
						name = raw.name,
						value = raw.value,
					};
					break;

				case TYPE_COUNTER:
					result = new SnipeTableLogicNodeVarCounter()
					{
						type = raw.type,
						name = raw.name,
					};
					break;

				case TYPE_COND_COUNTER:
					result = new SnipeTableLogicNodeVarCondCounter()
					{
						type = raw.type,
						name = raw.name,
						@operator = raw.@operator,
						condValue = raw.condValue,
					};
					break;

			}

			return result;
		}

		public static implicit operator SnipeTableLogicNodeVar(SnipeTableLogicRawNodeCheck raw)
		{
			SnipeTableLogicNodeVar result = null;

			switch (raw?.type)
			{
				case TYPE_RELATIVE:
					result = new SnipeTableLogicNodeVarCheckRelative()
					{
						type = raw.type,
						@operator = raw.@operator,
						value = raw.value,
						name = raw.name,
					};
					break;

				case TYPE_TIMER:
					result = new SnipeTableLogicNodeVarTimer()
					{
						type = raw.type,
						value = raw.value,
					};
					break;

				case TYPE_TIMEOUT:
					result = new SnipeTableLogicNodeVarTimeout()
					{
						type = raw.type,
						value = raw.value,
					};
					break;

				case TYPE_PAYMENT_ITEM_STRING_ID:
					result = new SnipeTableLogicNodeVarPaymentItemStringID()
					{
						type = raw.type,
						name = raw.name,
					};
					break;
			}
			
			return result;
		}
	}
	
	public class SnipeTableLogicNodeVarAttr : SnipeTableLogicNodeVar
	{
	}
	
	public class SnipeTableLogicNodeVarClient : SnipeTableLogicNodeVar
	{
		public int value;
	}
	
	public class SnipeTableLogicNodeVarCounter : SnipeTableLogicNodeVar
	{
	}
	
	public class SnipeTableLogicNodeVarCondCounter : SnipeTableLogicNodeVar
	{
		public string @operator;
		public float condValue;
	}

	// checks

	public class SnipeTableLogicNodeVarCheckRelative : SnipeTableLogicNodeVar
	{
		public string @operator;
		public int value;
	}

	public class SnipeTableLogicNodeVarTimer : SnipeTableLogicNodeVar
	{
		public int value;
	}

	public class SnipeTableLogicNodeVarTimeout : SnipeTableLogicNodeVarTimer
	{
	}

	public class SnipeTableLogicNodeVarPaymentItemStringID : SnipeTableLogicNodeVar
	{
	}
}