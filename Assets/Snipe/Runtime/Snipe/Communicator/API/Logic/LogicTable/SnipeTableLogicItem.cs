using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	[System.Serializable]
	public class SnipeTableLogicItem : SnipeTableItem
	{
		public string name;
		public string stringID;
		public List<string> tags;
		public int entryNodeID;
		public int parentID;
		public List<SnipeTableLogicNode> nodes;
	}
}