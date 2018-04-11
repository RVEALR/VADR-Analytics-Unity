using UnityEngine;
using UnityEditor;
using VadRAnalytics;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;

public class MenuManager {

    [MenuItem("VadR/Configure Manager")]
    static void Configure()
    {
        ShowConfigurationWindow();
    }


    [MenuItem("VadR/Export Scene Model")]
    static void ExportSceneModels()
    {
        ShowExportWindow();
    }

    [MenuItem("VadR/Check Update")]
    static void CheckUpdate()
    {
        if (SdkUpdateCheck.version.Length > 0 && SdkUpdateCheck.version != VadRConfig.VERSION)
        {
            if (!UpdateWindow.Init(SdkUpdateCheck.version, SdkUpdateCheck.htmlUrl))
            {
                UpdateWindow.ShowWindow();
            }
        }
        else
        {
            EditorUtility.DisplayDialog("VadR VR Analytics", "Your VadR VR Analytics Plugin is up to date.", "Ok");
        }
    }

    /// <summary>
    /// Setting the configuration window from current parameters
    /// </summary>
    static void ShowConfigurationWindow()
    {
        string appId = "";
        string appToken = "";
        bool testMode = true;
        bool pauseSendFlag = true;
        bool periodicSendFlag = true;
        bool pauseOnHeadsetRemoval = true;
        List<VadrScene> vadrScenes = new List<VadrScene>();
        List<Type> typeList = ConfigurationManager.GetAllEventCollectors();
        List<VadrEventCollector> eventCollectors = new List<VadrEventCollector>();
        for(int i = 0; i<typeList.Count; i++)
        {
            eventCollectors.Add(new VadrEventCollector(typeList[i], false, -1.0f));
        }
        UnityEngine.SceneManagement.Scene currentScene = EditorSceneManager.GetActiveScene();
        string currentPath = currentScene.path;    
        if (currentPath.Length > 0 || EditorUtility.DisplayDialog("", "You can lose unsaved changes", "Continue", "Cancel"))
        {
            float totalScenes = EditorBuildSettings.scenes.Length;
            float completedScenes = 0;
            foreach (EditorBuildSettingsScene S in EditorBuildSettings.scenes)
            {
                EditorUtility.DisplayProgressBar("Initializing", "Initializing Configuration Manager",
                    completedScenes / totalScenes);
                completedScenes++;
                if (S.enabled)
                {
                    string sceneId = "";
                    EditorSceneManager.OpenScene(S.path);
                    DataCollectionManager dataManager = UnityEngine.Object.FindObjectOfType<DataCollectionManager>();
                    if (dataManager != null)
                    {
                        // Prefill App Id and App Token of already present.
                        if (dataManager.sceneId != null)
                        {
                            sceneId = dataManager.sceneId;
                        }
                        if (appId.Length == 0 && dataManager.appId != null)
                        {
                            appId = dataManager.appId;
                        }
                        if (appToken.Length == 0 && dataManager.appToken != null)
                        {
                            appToken = dataManager.appToken;
                        }
                        if (testMode)
                        {
                            testMode = dataManager.testMode;
                        }
                        if (!dataManager.periodicSendFlag)
                        {
                            periodicSendFlag = dataManager.periodicSendFlag;
                        }
                        if (!dataManager.pauseSendFlag)
                        {
                            pauseSendFlag = dataManager.pauseSendFlag;
                        }
                        if (!dataManager.pauseOnHeadsetRemoval)
                        {
                            pauseOnHeadsetRemoval = dataManager.pauseOnHeadsetRemoval;
                        }
                        // Getting already attached event scripts data.
                        GameObject gameobject = dataManager.gameObject;
                        if (gameobject != null)
                        {
                            for (int i = 0; i < eventCollectors.Count; i++)
                            {
                                IEventCollector collector = gameobject.GetComponent(eventCollectors[i].collector) as IEventCollector;
                                if (collector != null)
                                {
                                    eventCollectors[i].enable = true;
                                    if ((collector.getTimeInterval() < eventCollectors[i].timeInterval) || (eventCollectors[i].timeInterval < 0))
                                    {
                                        eventCollectors[i].timeInterval = collector.getTimeInterval();
                                    }
                                }
                            }
                        }

                    }
                    vadrScenes.Add(new VadrScene(S.path, sceneId));
                }
            }
            for (int i = 0; i < eventCollectors.Count; i++)
            {
                if (eventCollectors[i].timeInterval < 0)
                {
                    eventCollectors[i].timeInterval = 0.2f;
                }
            }
            if (currentPath.Length > 0)
            {
                EditorSceneManager.OpenScene(currentPath);
            }
            EditorUtility.ClearProgressBar();
            ConfigurationManager.Init(vadrScenes, eventCollectors, appId, appToken, testMode, 
                periodicSendFlag, pauseSendFlag, pauseOnHeadsetRemoval);
            ConfigurationManager.ShowWindow();
        }
    }

    static void ShowExportWindow()
    {
        string appToken = "";
        string currentSceneId = "";
        List<UploadSceneObject> vadrScenes = new List<UploadSceneObject>();

        UnityEngine.SceneManagement.Scene currentScene = EditorSceneManager.GetActiveScene();
        string currentPath = currentScene.path;
        if(currentPath.Length == 0)
        {
            if (EditorUtility.DisplayDialog("Save Scene", "Please save scene first to Upload model.", "Ok", ""))
            {
                return;
            }
        }
        else
        {
            float totalScenes = EditorBuildSettings.scenes.Length;
            float completedScenes = 0;
            foreach (EditorBuildSettingsScene S in EditorBuildSettings.scenes)
            {
                EditorUtility.DisplayProgressBar("Initializing", "Initializing Configuration Manager", 
                    completedScenes/totalScenes);
                completedScenes++;
                if (S.enabled)
                {
                    string sceneId = "";
                    EditorSceneManager.OpenScene(S.path);
                    DataCollectionManager dataManager = UnityEngine.Object.FindObjectOfType<DataCollectionManager>();
                    if (dataManager != null)
                    {
                        // Prefill Scene Id and App Token of already present.
                        if (dataManager.sceneId != null)
                        {
                            sceneId = dataManager.sceneId;
                        }
                        if (S.path == currentPath)
                        {
                            currentSceneId = dataManager.sceneId;
                        }
                        if (appToken.Length == 0 && dataManager.appToken != null)
                        {
                            appToken = dataManager.appToken;
                        }
                    }
                    vadrScenes.Add(new UploadSceneObject(sceneId, false, S.path));
                }
            }
            if (currentPath.Length > 0)
            {
                EditorSceneManager.OpenScene(currentPath);
            }
            EditorUtility.ClearProgressBar();
            SceneExporterManager sceneExporter = ScriptableObject.CreateInstance<SceneExporterManager>();
            sceneExporter.Init(appToken, currentSceneId, vadrScenes);
            sceneExporter.ShowWindow();
        }
    }
}
