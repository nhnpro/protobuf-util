using UnityEditor;
using UnityEngine;

public class ProtobufUnityWindow : EditorWindow
{
    private const string BASE_MENU_PATH = "NHN_UTILS/";
    
    [MenuItem(BASE_MENU_PATH + "Protobuf Utility %F11")]
    public static void Open()
    {
        var window = GetWindow(typeof(ProtobufUnityWindow)
            , false, "Protobuf Utility", true);
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        
    }

    private void OnGUI()
    {
        
    }
}
