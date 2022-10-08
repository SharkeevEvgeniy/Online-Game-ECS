using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Reflection;

using Assembly = System.Reflection.Assembly;
using Debug = UnityEngine.Debug;

namespace MiniIT.Snipe.Editor
{
	public static class EditorUtil
	{
		public static bool NamespaceExists(string namespace_name, string assembly_name = null)
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly asm in assemblies)
            {
                if (!string.IsNullOrEmpty(assembly_name) && !asm.GetName().Name.Equals(assembly_name))
                    continue;

                try
                {
                    System.Type[] types = asm.GetTypes();
                    foreach (System.Type t in types)
                    {
                        if (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.Equals(namespace_name))
                        {
                            return true;
                        }
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var le in e.LoaderExceptions)
					{
                        Debug.LogException(le);
					}
                }
            }

            return false;
        }
	}
}