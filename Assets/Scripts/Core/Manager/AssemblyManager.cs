using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

[AttributeUsage(AttributeTargets.Enum)]
public class ExportEventIdAttribute : Attribute
{
    public ExportEventIdAttribute()
    {
    }
}

public class AssemblyManager: ppCore.Common.Singleton<AssemblyManager>
{
    public AssemblyManager()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
#if BATTLE_LOGIC
            if(assembly.FullName.Contains("BattleLogic"))
            {
                AddAssembly(assembly);
            }
#else
            if (assembly.FullName.Contains("Plugins") ||
                assembly.FullName.Contains("Assembly"))
            {
                AddAssembly(assembly);
            }
#endif
        }
    }

    private List<Assembly> m_assemblies = new List<Assembly>();
    public void AddAssembly(Assembly assembly)
    {
        m_assemblies.Add(assembly);
    }

    public Type GetType(string typeName)
    {
        Type rtType = null;
        foreach(var assembly in m_assemblies)
        {
            rtType = assembly.GetType(typeName);
            if (rtType != null)
                return rtType;
        }
        return null;
    }

    public List<Type> GetTypes()
    {
        List<Type> types = new List<Type>();

        foreach (var assembly in m_assemblies)
        {
            types.AddRange(assembly.GetTypes());
        }
        return types;
    }

    public List<Type> GetTypes(string assemplyName)
    {

        List<Type> types = new List<Type>();

        foreach (var assembly in m_assemblies)
        {
            if(assembly.FullName == assemplyName)
                types.AddRange(assembly.GetTypes());
        }
        return types;
    }


    private static Assembly m_assemblyCSharp = null;
    public static Assembly GetAssemblyCSharp()
    {
        if (m_assemblyCSharp != null) return m_assemblyCSharp;
        m_assemblyCSharp = LoadFromReflection();
        if (m_assemblyCSharp == null) m_assemblyCSharp = LoadFromBinary();
        return m_assemblyCSharp;
    }


    public static Assembly LoadFromReflection()
    {
#if UNITY_EDITOR
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            if (assembly == null) continue;
      
            Type gameMain = assembly.GetType("GameMain");
            if ( gameMain == null) continue;

            return assembly;
        }
        return null;
#else
        return null;
#endif
    }


    public static Assembly LoadFromBinary()
    {
#if UNITY_EDITOR && !BATTLE_LOGIC


#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        string strBytesPath = Application.dataPath + "/Plugins/poker.bytes";
        string strBytesPathMdb = Application.dataPath + "/Plugins/poker.sym.mdb";
#else
#if LOAD_ORIGINAL_FILE
        string strBytesPath = Cysharp.Text.ZString.Concat(Application.dataPath,"/Plugins/", ScriptPathConfig.BinaryDllName, "_android_editor_local_res.bytes");
        string strBytesPathMdb = Cysharp.Text.ZString.Concat(Application.dataPath, "/Plugins/", ScriptPathConfig.BinaryDllName, "_android_editor_local_res.dll.mdb");
#else
        string strBytesPath = Application.dataPath + "/Plugins/poker.bytes";
        string strBytesPathMdb = Application.dataPath + "/Plugins/poker.sym.mdb";
#endif
#endif
        byte[] txtAsset = File.ReadAllBytes(strBytesPath);
        byte[] txtAssetMdb = File.ReadAllBytes(strBytesPathMdb);

        return System.Reflection.Assembly.Load(txtAsset, txtAssetMdb);
#else
        return null;
#endif
    }

    public static void Clear()
    {
        m_assemblyCSharp = null;
    }
}