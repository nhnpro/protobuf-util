#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ProtobufUnityCompiler : AssetPostprocessor {

    public static readonly string prefProtocEnable = "ProtobufUnity_Enable";
    public static readonly string prefProtocExecutable = "ProtobufUnity_ProtocExecutable";
    public static readonly string prefLogError = "ProtobufUnity_LogError";
    public static readonly string prefLogStandard = "ProtobufUnity_LogStandard";
    public static readonly string prefEnableGenDoc = "ProtobufUnity_Gendoc";
    public static readonly string prefProtocGenDocParam = "ProtobufUnity_Gendoc_Param";

    public static bool enableGenDoc
    {
        get
        {
            return EditorPrefs.GetBool(prefEnableGenDoc, true);
        }
        set
        {
            EditorPrefs.SetBool(prefEnableGenDoc, value);
        }
    }
    
    public static bool enabled
    {
        get
        {
            return EditorPrefs.GetBool(prefProtocEnable, true);
        }
        set
        {
            EditorPrefs.SetBool(prefProtocEnable, value);
        }
    }
    public static bool logError
    {
        get
        {
            return EditorPrefs.GetBool(prefLogError, true);
        }
        set
        {
            EditorPrefs.SetBool(prefLogError, value);
        }
    }

    public static bool logStandard
    {
        get
        {
            return EditorPrefs.GetBool(prefLogStandard, false);
        }
        set
        {
            EditorPrefs.SetBool(prefLogStandard, value);
        }
    }

    public static string ProtocDocPath
    {
        get
        {
            return EditorPrefs.GetString(prefProtocGenDocParam, "protodoc.exe");
        }
        set
        {
            EditorPrefs.SetString(prefProtocGenDocParam, value);
        }
    } 
    
    public static string rawExcPath
    {
        get
        {
            return EditorPrefs.GetString(prefProtocExecutable, "");
        }
        set
        {
            EditorPrefs.SetString(prefProtocExecutable, value);
        }
    }

    public static string excPath
    {
        get
        {
            string ret = EditorPrefs.GetString(prefProtocExecutable, "");
            if (ret.StartsWith(".."))
                return Path.Combine(Application.dataPath, ret);
            else
                return ret;
        }
        set
        {
            EditorPrefs.SetString(prefProtocExecutable, value);
        }
    }

#if UNITY_2018_3_OR_NEWER
    private class ProtobufUnitySettingsProvider : SettingsProvider
    {
        public ProtobufUnitySettingsProvider(string path, SettingsScope scopes = SettingsScope.User) 
        : base(path, scopes)
        { }

        public override void OnGUI(string searchContext)
        {
            ProtobufPreference();
        }
    }

    [SettingsProvider]
    static SettingsProvider ProtobufPreferenceSettingsProvider()
    {
        return new ProtobufUnitySettingsProvider("Preferences/Protobuf");
    }
#else
    [PreferenceItem("Protobuf")]
#endif
    static void ProtobufPreference()
    {
        EditorGUI.BeginChangeCheck();

        enabled = EditorGUILayout.Toggle(new GUIContent("Enable Protobuf Compilation", ""), enabled);

        EditorGUI.BeginDisabledGroup(!enabled);

        EditorGUILayout.HelpBox(@"On Windows put the path to protoc.exe (e.g. C:\My Dir\protoc.exe), on macOS and Linux you can use ""which protoc"" to find its location. (e.g. /usr/local/bin/protoc)", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Path to protoc", GUILayout.Width(100));
        rawExcPath = EditorGUILayout.TextField(rawExcPath, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Select"))
        {
            rawExcPath = EditorUtility.OpenFilePanel("Select Protoc.exe", Application.dataPath, "exe");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        logError = EditorGUILayout.Toggle(new GUIContent("Log Error Output", "Log compilation errors from protoc command."), logError);

        logStandard = EditorGUILayout.Toggle(new GUIContent("Log Standard Output", "Log compilation completion messages."), logStandard);
        enableGenDoc = EditorGUILayout.Toggle(new GUIContent("Generate Docs", "Generate Documents For Proto File."), enableGenDoc);
        if (enableGenDoc)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path to protoc", GUILayout.Width(100));
            ProtocDocPath = EditorGUILayout.TextField(ProtocDocPath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Select"))
            {
                ProtocDocPath = EditorUtility.OpenFilePanel("Select ProtocDoc.exe", Application.dataPath, "exe");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
       

        EditorGUILayout.Space();

        if (GUILayout.Button(new GUIContent("Force Compilation")))
        {
            CompileAllInProject();
        }

        EditorGUI.EndDisabledGroup();

        if (EditorGUI.EndChangeCheck())
        {
        }
    }

    static string[] AllProtoFiles
    {
        get
        {
            string[] protoFiles = Directory.GetFiles(Application.dataPath, "*.proto", SearchOption.AllDirectories);
            return protoFiles;
        }
    }

    static string[] IncludePaths 
    {
        get
        {
            string[] protoFiles = AllProtoFiles;

            string[] includePaths = new string[protoFiles.Length];
            for (int i = 0; i < protoFiles.Length; i++)
            {
                string protoFolder = Path.GetDirectoryName(protoFiles[i]);
                includePaths[i] = protoFolder;
            }
            return includePaths;
        }
    }


    static bool anyChanges = false;
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        anyChanges = false;
        if (enabled == false)
        {
            return;
        }


        foreach (string str in importedAssets)
        {
            if(CompileProtobufAssetPath(str, IncludePaths) == true)
            {
                anyChanges = true;
            }
        }

        /*
        for (int i = 0; i < movedAssets.Length; i++)
        {
            CompileProtobufAssetPath(movedAssets[i]);
        }
        */

        if(anyChanges)
        {
            Debug.Log(nameof(ProtobufUnityCompiler));
            AssetDatabase.Refresh();
        }
    }

    private static void CompileAllInProject()
    {
        if (logStandard)
        {
            Debug.Log("Protobuf Unity : Compiling all .proto files in the project...");
        }


        foreach (string s in AllProtoFiles)
        {
            if (logStandard)
            {
                Debug.Log("Protobuf Unity : Compiling " + s);
            }
            CompileProtobufSystemPath(s, IncludePaths);
        }
        Debug.Log("All Proto Files Compiled !!!");
        AssetDatabase.Refresh();
    }

    private static bool CompileProtobufAssetPath(string assetPath, string[] includePaths)
    {
        string protoFileSystemPath = Directory.GetParent(Application.dataPath) + Path.DirectorySeparatorChar.ToString() + assetPath;
        return CompileProtobufSystemPath(protoFileSystemPath, includePaths);
    }

    private static bool CompileProtobufSystemPath(string protoFileSystemPath, string[] includePaths)
    {
        if (Path.GetExtension(protoFileSystemPath) == ".proto")
        {
            string outputPath = Path.GetDirectoryName(protoFileSystemPath);
            string fileName = Path.GetFileNameWithoutExtension(protoFileSystemPath);

            string options = " --csharp_out \"{0}\" ";
            foreach (string s in includePaths)
            {
                options += $" --proto_path \"{s}\" ";
            }

            if (enableGenDoc)
            {
                options += $" --plugin=protoc-gen-doc={ProtocDocPath}";
                options += " --doc_out=\"{0}\" ";
                options += " --doc_opt=html,{1}.doc.html";
                //options += " --doc_opt=markdown,{1}.readme.md";
            }

            string finalArguments = $"\"{protoFileSystemPath}\"" + string.Format(options, outputPath, fileName);

            // if (logStandard)
            // {
            //     UnityEngine.Debug.Log("Protobuf Unity : Arguments debug : " + finalArguments);
            // }
            
          //  Debug.LogError(finalArguments);

            ProcessStartInfo startInfo = new ProcessStartInfo { FileName = excPath, Arguments =  finalArguments};

            Process proc = new Process { StartInfo = startInfo };
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (logStandard)
            {
                if (output != "")
                {
                    Debug.Log("Protobuf Unity : " + output);
                }
                Debug.Log("Protobuf Unity : Compiled " + Path.GetFileName(protoFileSystemPath));
            }

            if (logError && error != "")
            {
                Debug.LogError("Protobuf Unity : " + error);
            }
            return true;
        }
        return false;
    }


}

#endif
