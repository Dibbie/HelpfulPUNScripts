using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Editor Script: Build And PlayTest
/// Place this script in a Editor folder.
/// This script will automatically enter Play Mode in the Editor, after a build has finished compiling with Ctrl + B
/// </summary>

[InitializeOnLoad]
public class BuildAndPlayTest : Editor {

    private static bool enterPlayModeAfterBuild = true;

    static BuildAndPlayTest()
    {
        EditorApplication.delayCall += WaitForInit;
    }

    [MenuItem("File/Build/Enter PlayMode After Build &#b", false)]
    public static void PlayModeAfterBuild()
    {
        enterPlayModeAfterBuild = !enterPlayModeAfterBuild;
        Menu.SetChecked("File/Build/Enter PlayMode After Build", enterPlayModeAfterBuild);

        Debug.Log("<b>Enter PlayMode After Build</b> is now " + (enterPlayModeAfterBuild ? "Set" : "Unset"));
    }

    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget build, string path)
    {
        EditorApplication.isPlaying = enterPlayModeAfterBuild;
    }
    
    static void WaitForInit()
    {
        Menu.SetChecked("File/Build/Enter PlayMode After Build", enterPlayModeAfterBuild);
    }
}