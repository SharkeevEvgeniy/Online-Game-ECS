using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	public class LogicNode
	{
		public int id;

		public SnipeTableLogicItem tree { get; private set; }
		public SnipeTableLogicNode node { get; private set; }
		public List<SnipeLogicNodeVar> vars { get; private set; }

		public string name { get => node?.name; }
		public string stringID { get => node?.stringID; }
		public List<SnipeTableLogicRawNodeResult> results { get => node?.results; }

		public int timeleft = -1; // seconds left. (-1) means that the node does not have a timer
		public bool isTimeout { get; private set; }

		public LogicNode(SnipeObject data, SnipeTable<SnipeTableLogicItem> logic_table)
		{
			id = data.SafeGetValue<int>("id");
			
			foreach (var table_tree in logic_table.Items.Values)
			{
				foreach (var table_node in table_tree.nodes)
				{
					if (table_node.id == id)
					{
						tree = table_tree;
						node = table_node;
						break;
					}
				}

				if (node != null)
					break;
			}

			if (node == null)
			{
				DebugLogger.LogError($"[LogicNode] Table node not found. id = {id}");
				return;
			}
			
			foreach (var node_check in node.checks)
			{
				RefreshTimerVar(node_check.type, node_check.value);
			}

			if (data["vars"] is IList data_vars)
			{
				vars = new List<SnipeLogicNodeVar>(Math.Max(data_vars.Count, node.vars.Count));

				foreach (SnipeObject data_var in data_vars)
				{
					bool found = false;

					string var_name = data_var.SafeGetString("name");
					foreach (var node_var in node.vars)
					{
						if (var_name == node_var.name)
						{
							vars.Add(new SnipeLogicNodeVar()
							{
								var = node_var,
								value = data_var?.SafeGetValue<int>("value") ?? default,
								maxValue = data_var?.SafeGetValue<int>("maxValue") ?? default,
							});

							found = true;
							break;
						}
					}

					if (found)
						continue;

					string var_type = data_var.SafeGetString("type");
					if (!string.IsNullOrEmpty(var_type))
					{
						foreach (var node_var in node.checks)
						{
							if (var_type == node_var.type)
							{
								int var_value = data_var?.SafeGetValue<int>("value") ?? default;
								vars.Add(new SnipeLogicNodeVar()
								{
									var = node_var,
									value = var_value,
									maxValue = data_var?.SafeGetValue<int>("maxValue") ?? default,
								});

								RefreshTimerVar(var_type, var_value);

								// found = true;
								break;
							}
						}
					}
				}

			}
		}

		public bool HasCheckType(string check_type)
		{
			if (node == null)
				return false;
			
			foreach (var node_check in node.checks)
			{
				if (node_check.type == check_type)
					return true;
			}
			return false;
		}

		public bool HasCheckName(string check_name)
		{
			if (node == null)
				return false;
			
			foreach (var node_check in node.checks)
			{
				if (node_check.name == check_name)
					return true;
			}
			return false;
		}

		public string GetPurchaseProductSku()
		{
			if (node == null)
				return null;
			
			foreach (var node_check in node.checks)
			{
				if (node_check.type == SnipeTableLogicNodeCheck.TYPE_PAYMENT_ITEM_STRING_ID)
				{
					return node_check.name;
				}
			}
			return null;
		}

		public void CopyVars(LogicNode src_node)
		{
			if (src_node?.vars == null)
				return;

			vars = src_node.vars;

			foreach (var node_var in vars)
			{
				RefreshTimerVar(node_var.var.type, node_var.value);
			}
		}

		private bool RefreshTimerVar(string var_type, int var_value)
		{
			bool is_timeout = (var_type == SnipeTableLogicNodeCheck.TYPE_TIMEOUT);
			if (is_timeout || var_type == SnipeTableLogicNodeCheck.TYPE_TIMER)
			{
				this.timeleft = var_value;
				this.isTimeout = is_timeout;

				return true;
			}

			return false;
		}
	}

	public class SnipeLogicNodeVar
	{
		public SnipeTableLogicNodeVar @var;
		public int value;
		public int maxValue;

		public string name { get => var?.name; }
		public float condValue { get => var is SnipeTableLogicNodeVarCondCounter cc_var ? cc_var.condValue : default; }
	}
}