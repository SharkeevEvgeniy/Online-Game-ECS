using MiniIT;
using System;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	public class SnipeTableItem
	{
		public int id;
	}

	public interface ISnipeTableItemsListWrapper
	{
	}

	public interface ISnipeTableItemsListWrapper<ItemType> : ISnipeTableItemsListWrapper
	{
		List<ItemType> list { get; set; }
	}
}