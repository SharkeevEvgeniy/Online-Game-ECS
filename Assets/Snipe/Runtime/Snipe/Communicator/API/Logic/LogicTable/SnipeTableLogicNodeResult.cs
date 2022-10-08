
namespace MiniIT.Snipe
{
	public class SnipeTableLogicRawNodeResult
	{
		public string type;
		public string name;
		public string stringID;
		public int value;
		public int itemID;
		public int amount;
		public string custom;  // json string
		public int nodeID;
		public bool onFail;
	}
	
	public class SnipeTableLogicNodeResult
	{
		public const string TYPE_ATTR = "attr";
		public const string TYPE_ATTR_SET = "attrSet";
		public const string TYPE_BADGE = "badge";
		public const string TYPE_ITEM = "item";
		public const string TYPE_ACTION = "action";
		public const string TYPE_NODE = "node";
		public const string TYPE_EXIT_TREE = "exitTree";
		
		public string type;
		
		public static implicit operator SnipeTableLogicNodeResult(SnipeTableLogicRawNodeResult raw)
		{
			SnipeTableLogicNodeResult result = null;
			
			switch (raw?.type)
			{
				case TYPE_ATTR:
					result = new SnipeTableLogicNodeResultAttr()
					{
						type = raw.type,
						name = raw.name,
						value = raw.value,
					};
					break;
				
				case TYPE_ATTR_SET:
					result = new SnipeTableLogicNodeResultAttrSet()
					{
						type = raw.type,
						name = raw.name,
						value = raw.value,
					};
					break;
				
				case TYPE_BADGE:
					result = new SnipeTableLogicNodeResultBadge()
					{
						type = raw.type,
						itemID = raw.itemID,
						value = raw.value,
					};
					break;
				
				case TYPE_ITEM:
					result = new SnipeTableLogicNodeResultItem()
					{
						type = raw.type,
						itemID = raw.itemID,
						amount = raw.amount,
					};
					break;
				
				case TYPE_ACTION:
					result = new SnipeTableLogicNodeResultAction()
					{
						type = raw.type,
						stringID = raw.stringID,
						custom = raw.custom,
					};
					break;
				
				case TYPE_NODE:
					result = new SnipeTableLogicNodeResultNode()
					{
						type = raw.type,
						nodeID = raw.nodeID,
						onFail = raw.onFail,
					};
					break;
				
				case TYPE_EXIT_TREE:
					result = new SnipeTableLogicNodeResultNode()
					{
						type = raw.type,
						onFail = raw.onFail,
					};
					break;
			}
			
			return result;
		}
	}
	
	public class SnipeTableLogicNodeResultAttr : SnipeTableLogicNodeResult
	{
		public string name;
		public int value;
	}
	
	public class SnipeTableLogicNodeResultAttrSet : SnipeTableLogicNodeResultAttr
	{
	}
	
	public class SnipeTableLogicNodeResultBadge : SnipeTableLogicNodeResult
	{
		public int itemID;
		public int value;
	}
	
	public class SnipeTableLogicNodeResultItem : SnipeTableLogicNodeResult
	{
		public int itemID;
		public int amount;
	}
	
	public class SnipeTableLogicNodeResultAction : SnipeTableLogicNodeResult
	{
		public string stringID;
		public string custom;  // json string
	}
	
	public class SnipeTableLogicNodeResultExitTree : SnipeTableLogicNodeResult
	{
		public bool onFail;
	}
	
	public class SnipeTableLogicNodeResultNode : SnipeTableLogicNodeResultExitTree
	{
		public int nodeID;
	}
}