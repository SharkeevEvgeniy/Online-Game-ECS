
namespace MiniIT.Snipe
{
	public class SnipeTableLogicRawNodeRequire
	{
		public string type;
		public string name;
		public int itemID;
		public string @operator;
		public int value;
	}
	
	public class SnipeTableLogicNodeRequire
	{
		public const string TYPE_ATTR = "attr";
		public const string TYPE_ITEM = "item";
		
		public string type;
		public string @operator;
		public int value;
		
		public static implicit operator SnipeTableLogicNodeRequire(SnipeTableLogicRawNodeRequire raw)
		{
			SnipeTableLogicNodeRequire result = null;
			
			switch (raw?.type)
			{
				case TYPE_ATTR:
					result = new SnipeTableLogicNodeRequireAttr()
					{
						type = raw.type,
						@operator = raw.@operator,
						value = raw.value,
						name = raw.name,
					};
					break;
				
				case TYPE_ITEM:
					result = new SnipeTableLogicNodeRequireItem()
					{
						type = raw.type,
						@operator = raw.@operator,
						value = raw.value,
						itemID = raw.itemID,
					};
					break;
			}
			
			return result;
		}
	}
	
	public class SnipeTableLogicNodeRequireAttr : SnipeTableLogicNodeRequire
	{
		public string name;
	}
	
	public class SnipeTableLogicNodeRequireItem : SnipeTableLogicNodeRequire
	{
		public int itemID;
	}
}