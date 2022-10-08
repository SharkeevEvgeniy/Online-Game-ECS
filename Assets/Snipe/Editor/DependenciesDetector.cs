#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor.Compilation;
using UnityEditor;

using Debug = UnityEngine.Debug;

namespace MiniIT.Snipe.Editor
{
	[InitializeOnLoad]
	public static class DependenciesDetector
	{
		private static readonly string[][] NAMESPACE_DEFINES = new []
		{
			new string[] { "Facebook.Unity", "SNIPE_FACEBOOK" },
		};
		
		static DependenciesDetector()
        {
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			
			Debug.Log($"[Snipe DependenciesDetector] start");
			
			//CompilationPipeline.compilationFinished += (context) =>
            //{
			//	Debug.Log($"[Snipe DependenciesDetector] compilationFinished");
				DelayedRun();
			//};
		}
		
		private static async void DelayedRun()
		{
			await Task.Delay(200);
			Debug.Log($"[Snipe DependenciesDetector] delay finished");
			
			Run();
		}
		
		[MenuItem("Snipe/Detect Dependencies")]
		public static void Run()
		{
			Debug.Log($"[Snipe DependenciesDetector] Run");
			
			// var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			// if (buildTargetGroup == BuildTargetGroup.Unknown)
			// {
				// var propertyInfo = typeof(EditorUserBuildSettings).GetProperty("activeBuildTargetGroup", BindingFlags.Static | BindingFlags.NonPublic);
				// if (propertyInfo != null)
					// buildTargetGroup = (BuildTargetGroup)propertyInfo.GetValue(null, null);
			// }
			
			foreach (BuildTargetGroup buildTargetGroup in (BuildTargetGroup[]) Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (!IsValidBuildTargetGroup(buildTargetGroup))
					continue;
				
				Debug.Log($"[Snipe DependenciesDetector] buildTargetGroup = {buildTargetGroup}");
				
				EditorApplication.LockReloadAssemblies();
				
				var previousProjectDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
				var projectDefines = previousProjectDefines.Split(';').ToList();
				
				foreach (var item in NAMESPACE_DEFINES)
				{
					RefreshDefineSymbolForNamespace(buildTargetGroup, projectDefines, item[0], item[1]);
				}
				
				var newProjectDefines = string.Join(";", projectDefines.ToArray());
				if (newProjectDefines != previousProjectDefines)
				{
					Debug.Log($"[Snipe DependenciesDetector] define symbols changed - applying");
					
					//EditorApplication.LockReloadAssemblies();
					
					PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newProjectDefines);
					
					// Let other systems execute before reloading assemblies
					Thread.Sleep(1000);
				}
				
				EditorApplication.UnlockReloadAssemblies();
			}
        }
		
		private static void RefreshDefineSymbolForNamespace(BuildTargetGroup buildTargetGroup, IList<string> projectDefines, string namespace_name, string define_symbol)
		{
			Debug.Log($"[Snipe DependenciesDetector] RefreshDefineSymbolForNamespace {buildTargetGroup} - {define_symbol} - {namespace_name}");
			
			if (EditorUtil.NamespaceExists(namespace_name))
			{
				Debug.Log($"[Snipe DependenciesDetector] -- namespace exists: {namespace_name}");
				if (!projectDefines.Contains(define_symbol, StringComparer.OrdinalIgnoreCase))
				{
					Debug.Log($"[Snipe DependenciesDetector] Add define symbol: {define_symbol}");
					projectDefines.Add(define_symbol);
				}
				else
				{
					Debug.Log($"[Snipe DependenciesDetector] -- define symbol exists: {define_symbol}");
				}
			}
			else
			{
				Debug.Log($"[Snipe DependenciesDetector] -- namespace does not exist: {namespace_name}");
				
				if (projectDefines.Remove(define_symbol))
				{
					Debug.Log("[Snipe DependenciesDetector] Remove define symbol: {define_symbol}");
				}
			}
		}
		
		private static bool IsValidBuildTargetGroup(BuildTargetGroup group)
        {
			if (group == BuildTargetGroup.Unknown)
				return false;
			Type unityEditorModuleManagerType = Type.GetType("UnityEditor.Modules.ModuleManager, UnityEditor.dll");
			if (unityEditorModuleManagerType == null)
				return true;
			
			MethodInfo method1 = unityEditorModuleManagerType.GetMethod("GetTargetStringFromBuildTargetGroup", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo method2 = typeof(PlayerSettings).GetMethod("GetPlatformName", BindingFlags.Static | BindingFlags.NonPublic);
			if (method1 == null || method2 == null)
				return true;
			string str1 = (string) method1.Invoke(null, new object[] {group});
			string str2 = (string) method2.Invoke(null, new object[] {group});
			if (string.IsNullOrEmpty(str1))
				return !string.IsNullOrEmpty(str2);
			return true;
        }
	}
}

#endif