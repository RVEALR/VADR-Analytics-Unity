using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VadRAnalytics;
using UnityEngine.SceneManagement;
using System.Net;
using System;
using System.IO;

public class ConfigurationManager : EditorWindow {
   
    
    static List<VadrScene> scenes;
    static List<VadrEventCollector> eventCollectors;
    static string appId;
    static string appToken;
    static bool testMode;
    static bool periodicSendFlag;
    static bool pauseSendFlag;
    static bool pauseOnHeadsetRemoval;
    static string errors;
    static string warnings;
    static string info;
    static ConfigurationManager _instance;
    static DataCollectionManager manager;
    static Vector2 scenesScrollPos = new Vector2();
    static Vector2 eventsScrollPos = new Vector2();

    /// <summary>
    /// Initialize the configuration window
    /// </summary>
    /// <param name="scenes">Currently exisiting scenes</param>
    /// <param name="appId">Current appId if there.</param>
    /// <param name="appToken">Current appId if there.</param>
    /// <param name="testMode">Current test mode if there.</param>
    /// <param name="periodicSendFlag">Whether to send data periodically</param>
    /// <param name="pauseSendFlag">Whether to send data on pause and level load</param>
    /// <param name="pauseOnHeadsetRemoval">Whether to pause data on headset removal</param>
    /// <param name="eventCollectors">List of all default event collector</param>
    public static void Init(List<VadrScene> scenes, List<VadrEventCollector> eventCollectors, 
        string appId, string appToken, bool testMode, bool periodicSendFlag, bool pauseSendFlag, bool pauseOnHeadsetRemoval)
    {
        ConfigurationManager.scenes = scenes;
        ConfigurationManager.appId = appId;
        ConfigurationManager.appToken = appToken;
        ConfigurationManager.testMode = testMode;
        ConfigurationManager.eventCollectors = eventCollectors;
        ConfigurationManager.periodicSendFlag = periodicSendFlag;
        ConfigurationManager.pauseSendFlag = pauseSendFlag;
        ConfigurationManager.pauseOnHeadsetRemoval = pauseOnHeadsetRemoval;
        manager = null;
        errors = "";
        warnings = "";
        info = "";
        scenesScrollPos = new Vector2();
        eventsScrollPos = new Vector2();
    }

    /// <summary>
    /// Initialize the configuration window
    /// </summary>
    /// <param name="manager">DataCollectionManager for which configuration is to be done</param>
    /// <param name="appId">Current appId if there.</param>
    /// <param name="appToken">Current appId if there.</param>
    /// <param name="testMode">Current test mode if there.</param>
    /// <param name="periodicSendFlag">Whether to send data periodically</param>
    /// <param name="pauseSendFlag">Whether to send data on pause and level load</param>
    public static void Init(ref DataCollectionManager manager, List<VadrEventCollector> eventCollectors,
        string appId, string appToken, bool testMode, bool periodicSendFlag, bool pauseSendFlag, bool pauseOnHeadsetRemoval)
    {
        ConfigurationManager.manager = manager;
        ConfigurationManager.appId = appId;
        ConfigurationManager.appToken = appToken;
        ConfigurationManager.testMode = testMode;
        ConfigurationManager.eventCollectors = eventCollectors;
        ConfigurationManager.periodicSendFlag = periodicSendFlag;
        ConfigurationManager.pauseSendFlag = pauseSendFlag;
        ConfigurationManager.scenes = new List<VadrScene>();
        ConfigurationManager.pauseOnHeadsetRemoval = pauseOnHeadsetRemoval;
        errors = "";
        warnings = "";
        info = "";
        scenesScrollPos = new Vector2();
        eventsScrollPos = new Vector2();
    }


    /// <summary>
    /// To Get all default event collectors
    /// </summary>
    /// <returns>List of types of default event collectors</returns>
    public static List<Type> GetAllEventCollectors()
    {
        List<Type> typeList = new List<Type>();
        typeList.Add(typeof(GazeCollector));
        typeList.Add(typeof(GearVrEventCollector));
        typeList.Add(typeof(RiftEventCollector));
        typeList.Add(typeof(OrientationCollector));
        typeList.Add(typeof(PerformanceCollector));
        typeList.Add(typeof(TrackObjects));
        return typeList;
    }

    /// <summary>
    /// Save Configuration of a scene. Adds DataCollectionManager if not present 
    /// </summary>
    /// <param name="vadrScene">Scene whose configuration is to be saved</param>
    static void SaveSceneConfiguration(VadrScene vadrScene)
    {
        EditorSceneManager.OpenScene(vadrScene.scenePath);
        DataCollectionManager dataManager = UnityEngine.Object.FindObjectOfType<DataCollectionManager>();
        if (dataManager != null)
        {
            // Add send data option here
            dataManager.appId = ConfigurationManager.appId;
            dataManager.appToken = ConfigurationManager.appToken;
            dataManager.testMode = ConfigurationManager.testMode;
            dataManager.sceneId = vadrScene.sceneId;
            dataManager.pauseSendFlag = ConfigurationManager.pauseSendFlag;
            dataManager.periodicSendFlag = ConfigurationManager.periodicSendFlag;
            dataManager.pauseOnHeadsetRemoval = ConfigurationManager.pauseOnHeadsetRemoval;
            EditorUtility.SetDirty(dataManager); // Tells unity that it is modified and save it.
            AddEventScripts(dataManager.gameObject);
        }
        else
        {
            // Add send data option here
            PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/VadRManager") as GameObject);
            dataManager = UnityEngine.Object.FindObjectOfType<DataCollectionManager>();
            if (dataManager != null)
            {
                // Add send data option here
                dataManager.appId = ConfigurationManager.appId;
                dataManager.appToken = ConfigurationManager.appToken;
                dataManager.testMode = ConfigurationManager.testMode;
                dataManager.sceneId = vadrScene.sceneId;
                dataManager.pauseSendFlag = ConfigurationManager.pauseSendFlag;
                dataManager.periodicSendFlag = ConfigurationManager.periodicSendFlag;
                dataManager.pauseOnHeadsetRemoval = ConfigurationManager.pauseOnHeadsetRemoval;
                EditorUtility.SetDirty(dataManager); // Tells unity that it is modified and save it.
                AddEventScripts(dataManager.gameObject);
            }
        }
        UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneByPath(vadrScene.scenePath);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Save Configuration of a single datamanager
    /// </summary>
    static void SaveDataManagerConfiguration()
    {
        // Add send data option here
        string currentPath = SceneManager.GetActiveScene().path;
        ConfigurationManager.manager.appId = ConfigurationManager.appId;
        ConfigurationManager.manager.appToken = ConfigurationManager.appToken;
        ConfigurationManager.manager.testMode = ConfigurationManager.testMode;
        ConfigurationManager.manager.pauseSendFlag = ConfigurationManager.pauseSendFlag;
        ConfigurationManager.manager.periodicSendFlag = ConfigurationManager.periodicSendFlag;
        ConfigurationManager.manager.pauseOnHeadsetRemoval = ConfigurationManager.pauseOnHeadsetRemoval;
        AddEventScripts(ConfigurationManager.manager.gameObject);
        EditorUtility.SetDirty(ConfigurationManager.manager); // Tells unity that it is modified and save it
        if (currentPath.Length > 0)
        {
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneByPath(currentPath);
            EditorSceneManager.SaveScene(scene);
        }
    }

    /// <summary>
    /// Add Event related scripts to the DataManager Gameobject 
    /// </summary>
    static void AddEventScripts(GameObject gameobject)
    {
        if(gameobject != null)
        {
            for(int i = 0; i<ConfigurationManager.eventCollectors.Count; i++)
            {
                if (ConfigurationManager.eventCollectors[i].enable)
                {
                    Type type = ConfigurationManager.eventCollectors[i].collector;
                    IEventCollector collector = gameobject.GetComponent(type) as IEventCollector;
                    if (collector == null)
                    {
                        gameobject.AddComponent(type);
                    }
                    collector = gameobject.GetComponent(type) as IEventCollector;
                    collector.setTimeInterval(ConfigurationManager.eventCollectors[i].timeInterval);
                }
                else
                {
                    Type type = ConfigurationManager.eventCollectors[i].collector;
                    UnityEngine.Object collector = gameobject.GetComponent(type) as UnityEngine.Object;
                    if (collector != null)
                    {
                        DestroyImmediate(collector);
                    }
                }
            }
            EditorUtility.SetDirty(gameobject);
        }
    }

    /// <summary>
    /// Save configuration for all scenes or a single DataCollectionManager object
    /// </summary>
    static void SaveConfiguration()
    {
        if (ConfigurationManager.scenes.Count > 0)
        {
            string currentPath = SceneManager.GetActiveScene().path;
            for (int i = 0; i < ConfigurationManager.scenes.Count; i++)
            {
                SaveSceneConfiguration(ConfigurationManager.scenes[i]);
            }
            if(currentPath.Length > 0)
            {
                EditorSceneManager.OpenScene(currentPath);
            }
        }
        else if(ConfigurationManager.manager != null)
        {
            SaveDataManagerConfiguration();
        }
    }

    public static void ShowWindow()
    {
        GetWindow<ConfigurationManager>("Configure");
    }

    private void OnGUI()
    {
        GUIStyle styleHR = new GUIStyle(GUI.skin.box);
        styleHR.stretchWidth = true;
        styleHR.fixedHeight = 2;
        GUI.skin.label.richText = true;
        GUILayout.Space(20);
        EditorGUIUtility.labelWidth = 300;
        
        // App Settings
        GUILayout.Label("<size=16><b>App Settings</b></size>");
        GUILayout.Box("", styleHR);
        GUILayout.Space(12);
        ConfigurationManager.appId = EditorGUILayout.TextField("App Id", ConfigurationManager.appId);
        ConfigurationManager.appToken = EditorGUILayout.TextField("App Token", ConfigurationManager.appToken);
        ConfigurationManager.testMode = EditorGUILayout.Toggle("Dev Mode", ConfigurationManager.testMode);
        //GUILayout.Box("", styleHR);
        GUILayout.Space(10);
        GUILayout.Label("<size=14><b>Sending Data</b></size>");
        ConfigurationManager.pauseOnHeadsetRemoval = EditorGUILayout.Toggle("Pause Default data collection on headset removal",
            ConfigurationManager.pauseOnHeadsetRemoval);
        ConfigurationManager.periodicSendFlag = EditorGUILayout.Toggle("Send Periodically",
            ConfigurationManager.periodicSendFlag);
        ConfigurationManager.pauseSendFlag = EditorGUILayout.Toggle("Send on Pause and Level Load",
            ConfigurationManager.pauseSendFlag);
        // Scene Settings
        if (ConfigurationManager.scenes.Count > 0)
        {
            GUILayout.Space(25);
            GUILayout.Label("<size=16><b>Scene Settings</b></size>");
            GUILayout.Box("", styleHR);
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Scenes</b>");
            GUILayout.Label("<b>Scene Id</b>");
            GUILayout.EndHorizontal();
            GUILayout.Box("", styleHR);
            scenesScrollPos = EditorGUILayout.BeginScrollView(scenesScrollPos, false, false);
            for (int i = 0; i < ConfigurationManager.scenes.Count; i++)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                //GUILayout.Label(ConfigurationManager.scenes[i].scenePath);
                ConfigurationManager.scenes[i].sceneId = EditorGUILayout.TextField(ConfigurationManager.scenes[i].scenePath, ConfigurationManager.scenes[i].sceneId);
                GUILayout.EndHorizontal();
                GUILayout.Box("", styleHR);
            }
            EditorGUILayout.EndScrollView();
        }
        else if (ConfigurationManager.manager != null)
        {
            GUILayout.Box("", styleHR);
            GUILayout.Label("<size=16><b>Scene Settings</b></size>");
            GUILayout.Box("", styleHR);
            ConfigurationManager.manager.sceneId = EditorGUILayout.TextField("Scene Id", ConfigurationManager.manager.sceneId);
        }

        // Event Settings
        if (eventCollectors.Count > 0)
        {
            GUILayout.Space(25);
            GUILayout.Label("<size=16><b>Select Events</b></size>");
            GUILayout.Box("", styleHR);
            GUILayout.Space(12);
            eventsScrollPos = EditorGUILayout.BeginScrollView(eventsScrollPos, false, false);
            for (int i = 0; i < eventCollectors.Count; i++)
            {
                string name = eventCollectors[i].collector.GetMethod("Name").Invoke(null, null).ToString();
                string description = eventCollectors[i].collector.GetMethod("Description").Invoke(null, null).ToString();
                string label = "<size=12><b>" + name + "</b></size>" + "<size=12> (" + description + ")</size>";
                GUILayout.Label(label);
                GUILayout.Space(5);
                eventCollectors[i].enable = EditorGUILayout.Toggle("Enable",
                    eventCollectors[i].enable);
                eventCollectors[i].timeInterval = EditorGUILayout.FloatField("Collection Frequency [in secs]",
                    eventCollectors[i].timeInterval);
                GUILayout.Box("", styleHR);
            }
            EditorGUILayout.EndScrollView();
        }
        GUILayout.Space(25);
        if (errors.Length > 0)
        {
            EditorGUILayout.HelpBox(errors, MessageType.Error);
            GUILayout.Space(10);
        }
        if (warnings.Length > 0)
        {
            EditorGUILayout.HelpBox(warnings, MessageType.Warning);
            GUILayout.Space(10);
        }
        if (info.Length > 0)
        {
            EditorGUILayout.HelpBox(info, MessageType.Info);
            GUILayout.Space(10);
        }
        if (GUILayout.Button("Save"))
        {
            errors = "";
            warnings = "";
            info = "";
            if (ConfigurationManager.appId.Length == 0 || ConfigurationManager.appToken.Length == 0)
            {
                errors = "Please enter valid App Id and App Token";
            }
            ValidateApp();
        }
        GUILayout.Space(20);


    }

    /// <summary>
    /// Validates AppId and AppToken
    /// </summary>
    static void ValidateApp()
    {
        if(!ConfigurationManager.periodicSendFlag && !ConfigurationManager.pauseSendFlag)
        {
            warnings = "Please select when to send data";
            return;
        }
        EditorUtility.DisplayProgressBar("Validating App Id and Token", "", 0.10f);
        HttpRequestHandler.SucessCallback success = validateAppSuccess;
        HttpRequestHandler.ErrorCallback error = validateAppError;
        string url = VadRConfig.getAppUrl + "?id=" + appId + "&token=" + appToken;
        string[] args = { };
        HttpRequestHandler.GetRequestMainThread(url, 10000, success, error, args);
    }


    static void validateAppSuccess(HttpWebResponse response)
    {
        EditorUtility.ClearProgressBar();
        if ((int)response.StatusCode == 200)
        {
            SaveConfiguration();
            info = "Configuration saved successfully";
        }
        else
        {
            errors = "Please enter valid App Id and App Token";
        }
        response.Close();
    }

    /// <summary>
    /// App Validate Error callback. Called when error occurs in app validation from server.
    /// </summary>
    /// <param name="response"></param>
    static void validateAppError(Exception e, string[] args)
    {
        EditorUtility.ClearProgressBar();
        switch (e.GetType().ToString())
        {
            case "System.Net.WebException":
                if (((WebException)e).Response != null)
                {
                    using (var stream = ((WebException)e).Response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            string error = reader.ReadToEnd();
                            var parsedError = JSON.Parse(error);
                            if (parsedError == null)
                            {
                                warnings = "Error validating app with given AppId and AppToken. Please try after sometime";
                                return;
                            }
                            if (parsedError["message"] != null)
                            {
                                errors = parsedError["message"];
                                return;
                            }
                            else
                            {
                                warnings = "Error validating app with given AppId and AppToken. Please try after sometime";
                                return;
                            }
                        }
                    }
                }
                else
                {
                    warnings = "Error validating app with given AppId and AppToken. Please try after sometime";
                    return;
                }
            default:
                warnings = "Error validating app with given AppId and AppToken. Please try after sometime";
                break;
        }
    }
}

public class VadrScene{
    public string scenePath;
    public string sceneId;

    public VadrScene(string scenePath, string sceneId)
    {
        this.scenePath = scenePath;
        this.sceneId = sceneId;
    }
}

public class VadrEventCollector
{
    public Type collector;
    public bool enable;
    public float timeInterval;


    public VadrEventCollector(Type collector, bool enable, float timeInterval)
    {
        this.collector = collector;
        this.enable = enable;
        this.timeInterval = timeInterval;
    }
}