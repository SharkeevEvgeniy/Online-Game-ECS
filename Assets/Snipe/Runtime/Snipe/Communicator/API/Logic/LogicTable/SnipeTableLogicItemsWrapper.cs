using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniIT.Snipe
{
	public class SnipeTableLogicItemsWrapper : ISnipeTableItemsListWrapper<SnipeTableLogicItem>
	{
		public List<SnipeTableLogicItem> list { get; set; }
		
		public static SnipeTableLogicItemsWrapper FromTableData(Dictionary<string, object> table_data)
		{
			if (table_data != null && table_data.TryGetValue("list", out var table_list_data) && table_list_data is IList table_list)
			{
				var logic_list_wrapper = new SnipeTableLogicItemsWrapper();
				logic_list_wrapper.list = new List<SnipeTableLogicItem>();
				foreach (Dictionary<string, object> tree_data in table_list)
				{
					var tree = new SnipeTableLogicItem();
					logic_list_wrapper.list.Add(tree);

					if (tree_data.TryGetValue("id", out var tree_id))
						tree.id = Convert.ToInt32(tree_id);
					if (tree_data.TryGetValue("name", out var tree_name))
						tree.name = Convert.ToString(tree_name);
					if (tree_data.TryGetValue("stringID", out var tree_stringID))
						tree.stringID = Convert.ToString(tree_stringID);
					if (tree_data.TryGetValue("entryNodeID", out var tree_entryNodeID))
						tree.entryNodeID = Convert.ToInt32(tree_entryNodeID);
					if (tree_data.TryGetValue("parentID", out var tree_parentID))
						tree.parentID = Convert.ToInt32(tree_parentID);
					
					tree.tags = new List<string>();
					if (tree_data.TryGetValue("tags", out var tree_tags) && tree_tags is IList tree_tags_list)
					{
						foreach (var tag in tree_tags_list)
						{
							string tag_string = Convert.ToString(tag);
							if (!string.IsNullOrEmpty(tag_string))
							{
								tree.tags.Add(tag_string);
							}
						}
					}
					
					tree.nodes = new List<SnipeTableLogicNode>();
					if (tree_data.TryGetValue("nodes", out var tree_nodes) && tree_nodes is IList tree_nodes_list)
					{
						foreach (Dictionary<string, object> node_data in tree_nodes_list)
						{
							var node = new SnipeTableLogicNode();
							tree.nodes.Add(node);

							if (node_data.TryGetValue("id", out var node_id))
								node.id = Convert.ToInt32(node_id);
							if (node_data.TryGetValue("name", out var node_name))
								node.name = Convert.ToString(node_name);
							if (node_data.TryGetValue("stringID", out var node_stringID))
								node.stringID = Convert.ToString(node_stringID);
							if (node_data.TryGetValue("note", out var node_note))
								node.note = Convert.ToString(node_note);
							if (node_data.TryGetValue("hasConfirm", out var node_hasConfirm))
								node.hasConfirm = Convert.ToBoolean(node_hasConfirm);
							if (node_data.TryGetValue("canClientFail", out var node_canClientFail))
								node.canClientFail = Convert.ToBoolean(node_canClientFail);
							if (node_data.TryGetValue("sendProgress", out var node_sendProgress))
								node.sendProgress = Convert.ToBoolean(node_sendProgress);
							
							node.requires = new List<SnipeTableLogicRawNodeRequire>();
							if (node_data.TryGetValue("requires", out var node_requires) && node_requires is IList node_requires_list)
							{
								foreach (Dictionary<string, object> node_require_data in node_requires_list)
								{
									var node_require = new SnipeTableLogicRawNodeRequire();
									node.requires.Add(node_require);

									if (node_require_data.TryGetValue("type", out var require_type))
										node_require.type = Convert.ToString(require_type);
									if (node_require_data.TryGetValue("name", out var require_name))
										node_require.name = Convert.ToString(require_name);
									if (node_require_data.TryGetValue("itemID", out var require_itemID))
										node_require.itemID = Convert.ToInt32(require_itemID);
									if (node_require_data.TryGetValue("operator", out var require_operator))
										node_require.@operator = Convert.ToString(require_operator);
									if (node_require_data.TryGetValue("value", out var require_value))
										node_require.value = Convert.ToInt32(require_value);
								}
							}

							node.vars = new List<SnipeTableLogicRawNodeVar>();
							if (node_data.TryGetValue("vars", out var node_vars) && node_vars is IList node_vars_list)
							{
								foreach (Dictionary<string, object> node_var_data in node_vars_list)
								{
									var node_var = new SnipeTableLogicRawNodeVar();
									node.vars.Add(node_var);

									if (node_var_data.TryGetValue("type", out var var_type))
										node_var.type = Convert.ToString(var_type);
									if (node_var_data.TryGetValue("name", out var var_name))
										node_var.name = Convert.ToString(var_name);
									if (node_var_data.TryGetValue("operator", out var var_operator))
										node_var.@operator = Convert.ToString(var_operator);
									if (node_var_data.TryGetValue("value", out var var_value))
										node_var.value = Convert.ToInt32(var_value);
									if (node_var_data.TryGetValue("condValue", out var var_condValue))
										node_var.condValue = Convert.ToSingle(var_condValue);
								}
							}

							node.checks = new List<SnipeTableLogicRawNodeCheck>();
							if (node_data.TryGetValue("checks", out var node_checks) && node_checks is IList node_checks_list)
							{
								foreach (Dictionary<string, object> node_check_data in node_checks_list)
								{
									var node_check = new SnipeTableLogicRawNodeCheck();
									node.checks.Add(node_check);

									if (node_check_data.TryGetValue("type", out var check_type))
										node_check.type = Convert.ToString(check_type);
									if (node_check_data.TryGetValue("name", out var check_name))
										node_check.name = Convert.ToString(check_name);
									if (node_check_data.TryGetValue("operator", out var check_operator))
										node_check.@operator = Convert.ToString(check_operator);
									if (node_check_data.TryGetValue("value", out var check_value))
										node_check.value = Convert.ToInt32(check_value);
								}
							}

							node.results = new List<SnipeTableLogicRawNodeResult>();
							if (node_data.TryGetValue("results", out var node_results) && node_results is IList node_results_list)
							{
								foreach (Dictionary<string, object> node_result_data in node_results_list)
								{
									var node_result = new SnipeTableLogicRawNodeResult();
									node.results.Add(node_result);

									if (node_result_data.TryGetValue("type", out var result_type))
										node_result.type = Convert.ToString(result_type);
									if (node_result_data.TryGetValue("name", out var result_name))
										node_result.name = Convert.ToString(result_name);
									if (node_result_data.TryGetValue("stringID", out var result_stringID))
										node_result.stringID = Convert.ToString(result_stringID);
									if (node_result_data.TryGetValue("value", out var result_value))
										node_result.value = Convert.ToInt32(result_value);
									if (node_result_data.TryGetValue("itemID", out var result_itemID))
										node_result.itemID = Convert.ToInt32(result_itemID);
									if (node_result_data.TryGetValue("amount", out var result_amount))
										node_result.amount = Convert.ToInt32(result_amount);
									if (node_result_data.TryGetValue("nodeID", out var result_nodeID))
										node_result.nodeID = Convert.ToInt32(result_nodeID);
									if (node_result_data.TryGetValue("custom", out var result_custom))
										node_result.custom = Convert.ToString(result_custom);
									if (node_result_data.TryGetValue("onFail", out var result_onFail))
										node_result.onFail = Convert.ToBoolean(result_onFail);
								}
							}
						}
					}
				}
				return logic_list_wrapper;
			}
			
			return null;
		}
	}
}