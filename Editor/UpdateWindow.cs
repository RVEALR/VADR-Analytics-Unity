using UnityEngine;
using UnityEditor;
using VadRAnalytics;
using System;

public class UpdateWindow : EditorWindow
{
    static string url;
    static string version;
	public static bool Init(string version, string url)
    {
        UpdateWindow.url = url;
        UpdateWindow.version = version;
        string skipSdk = EditorPrefs.GetString(VadRConfig.SDK_SKIP_VERSION, "");
        if (skipSdk == version)
        {
            return false;
        }
        ShowWindow();
        return true;
    }

    public static void ShowWindow()
    {
        int option = EditorUtility.DisplayDialogComplex("Update VadR VR Analytics Plugin",
               "New version of VadR VR Analytics Plugin is available. \n"+
               " It is highly recommended to update to newest version. \n \n" +
               "Current Version : "+VadRConfig.VERSION + "\n"+
               "Available Version: "+UpdateWindow.version+ "\n",
               "Yes, Download",
               "No, Don't Ask Again",
               "No");

        switch (option)
        {
            // Download
            case 0:
                Application.OpenURL(UpdateWindow.url);
                break;

            // Don't ask again
            case 1:
                EditorPrefs.SetString(VadRConfig.SDK_SKIP_VERSION, UpdateWindow.version);
                EditorUtility.DisplayDialog("VadR VR Analytics", 
                    "Your can always manually check for plugin update from \"VadR -> Check Update\".", "Ok");
                break;

            // No.
            case 2:
                break;

            default:
                break;
        }
    }

}
