using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VadRAnalytics;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SceneExporterManager: EditorWindow {

    string appToken;
    string currentSceneId;
    bool allSceneFlag;
    bool currentSceneFlag;
    bool exportSceneFlag;
    List<UploadSceneObject> scenes;
    int selectedScenes; // No of scenes selected
    int uploadedScenes; // No of scenes uploaded
    int uploadingScenes; // No of scenes uploading
    int errorUploading; // Errors in uploading
    bool uploadComplete;
    double totalProgress;
    string warnings;
    public void ShowWindow()
    {
        GetWindow<SceneExporterManager>("Upload");
    }

    public void Init(string appToken, string currentSceneId, List<UploadSceneObject> scenes)
    {
        this.currentSceneId = currentSceneId;
        this.appToken = appToken;
        this.scenes = scenes;
        this.warnings = "";
        allSceneFlag = false;
        currentSceneFlag = false;
        exportSceneFlag = false;
        uploadComplete = false;
        uploadedScenes = 0;
        uploadingScenes = 0;
        errorUploading = 0;
        selectedScenes = 0;
        if(this.scenes.Count == 0)
        {
            this.currentSceneFlag = true;
        }
    }

    void StartUpload()
    {
        this.exportSceneFlag = false;
        this.uploadComplete = false;
        this.selectedScenes = 0;
        this.uploadingScenes = 0;
        this.errorUploading = 0;
        this.totalProgress = 0;
        string currentPath = SceneManager.GetActiveScene().path;
        if (currentSceneFlag)
        {
            string[] parts = currentPath.Split('.');
            string directory = VadRConfig.BASE_DIRECTORY + parts[0]+"/";
            bool uploadFlag = SceneExporter.ExportScene(currentPath, currentSceneId, appToken);
            if (uploadFlag)
            {
                this.uploadingScenes = 1;
                SceneUploader uploader = new SceneUploader(appToken, currentSceneId, directory, currentPath);
                uploader.intiateUpload(this.uploadProgress, this.uploadSuccess, this.uploadError);
            }
            this.selectedScenes = 1;
        }
        else if (allSceneFlag)
        {
            
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].selected)
                {
                    this.selectedScenes++;
                }
            }
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].selected)
                {
                    bool uploadFlag = SceneExporter.ExportScene(scenes[i].scenePath, scenes[i].sceneId, appToken);
                    string sceneName = scenes[i].scenePath;
                    string[] parts = sceneName.Split('.');
                    string directory = VadRConfig.BASE_DIRECTORY + parts[0]+"/"; 
                    if (uploadFlag)
                    {
                        this.uploadingScenes++;
                        SceneUploader uploader = new SceneUploader(appToken, scenes[i].sceneId, directory, sceneName);
                        uploader.intiateUpload(this.uploadProgress, this.uploadSuccess, this.uploadError);
                    }
                }
            }
        }
        exportSceneFlag = true;
        if (currentPath.Length > 0)
        {
            EditorSceneManager.OpenScene(currentPath);
        }
    }

    // display progress here
    void uploadProgress(string sceneName, double progress)
    {
        this.totalProgress += progress;
    }

    void uploadError(string sceneName, string msg)
    {
        this.errorUploading++;
        if (this.uploadingScenes == (this.uploadedScenes + this.errorUploading))
        {
            this.uploadComplete = true;
        }
    }

    void uploadSuccess(string sceneName)
    {
        this.uploadedScenes++;
        if(this.uploadingScenes == (this.uploadedScenes+ this.errorUploading))
        {
            this.uploadComplete = true;
        }
    }

    void Update()
    {
        if (exportSceneFlag)
        {
            EditorUtility.DisplayProgressBar("Uploading Models", "Uploading scene model files",
                (float)(this.totalProgress / this.uploadingScenes));
        }
        if (this.uploadComplete)
        {
            this.uploadComplete = false;
            this.exportSceneFlag = false;
            EditorUtility.ClearProgressBar();

            if (this.errorUploading > 0)
            {
                EditorUtility.DisplayDialog("Scene Exporter", "Some scenes not uploaded successfully. "+
                    "Please check console for errors.\n" +
                    "Total successfully uploaded scenes: " + this.uploadedScenes, "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Scene Exporter", "All Scenes uploaded successfully", "Ok");
            }
        }
    }

    void OnGUI()
    {
        GUIStyle styleHR = new GUIStyle(GUI.skin.box);
        styleHR.stretchWidth = true;
        styleHR.fixedHeight = 2;
        GUI.skin.label.richText = true;
        GUILayout.Space(12);
        this.appToken = EditorGUILayout.TextField("App Token", this.appToken);

        // Options to select All Scene or Current Scene flag
        if (!this.currentSceneFlag && !this.allSceneFlag)
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Upload Current Scene"))
            {
                this.currentSceneFlag = true;
            }
            if (GUILayout.Button("Upload Multiple Scenes"))
            {
                this.allSceneFlag = true;
            }
            GUILayout.EndHorizontal();
        }
        // If current scene are selected
        if (this.currentSceneFlag)
        {
            GUILayout.Space(5);
            this.currentSceneId = EditorGUILayout.TextField("Current Scene Id",
                this.currentSceneId);
        }
        if (this.allSceneFlag)
        {
            if (this.scenes.Count > 0)
            {
                GUILayout.Space(12);
                GUILayout.Box("", styleHR);
                GUILayout.Label("<size=14><b>Select Scenes To Upload</b></size>");
                GUILayout.Box("", styleHR);
                GUILayout.Space(12);
                // Put a scroll view here
                for(int i=0; i< this.scenes.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    this.scenes[i].selected = EditorGUILayout.ToggleLeft(this.scenes[i].scenePath, 
                        this.scenes[i].selected);
                    this.scenes[i].sceneId = EditorGUILayout.TextField("", 
                        this.scenes[i].sceneId);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("No Scene Found", "Please add scenes in build settings to upload model",
                    "Ok"))
                {
                    this.allSceneFlag = false;
                    this.currentSceneFlag = false;
                }
            }
            
        }
        if (this.warnings.Length > 0)
        {
            GUILayout.Space(15);
            EditorGUILayout.HelpBox(this.warnings, MessageType.Warning);
            GUILayout.Space(15);
        }
        if (this.currentSceneFlag || this.allSceneFlag)
        {
            GUILayout.Space(20);
            GUILayout.Box("", styleHR);
            GUILayout.Space(20);
            if (GUILayout.Button("Export & Upload"))
            {
                this.warnings = "";
                this.validateUpload();
            }
        }
    }

    void validateUpload()
    {
        if (this.currentSceneFlag)
        {
            if(this.appToken.Length == 0 && this.currentSceneId.Length == 0)
            {
                this.warnings = "Please specify App Token and Scene Id";
                return;
            }
            else if (this.appToken.Length == 0)
            {
                this.warnings = "Please specify App Token";
                return;
            }
            else if (this.currentSceneId.Length == 0)
            {
                this.warnings = "Please specify Scene Id.";
                return;
            }
        }
        else if (this.allSceneFlag)
        {
            if (this.appToken.Length == 0)
            {
                this.warnings = "Please specify App Token";
                return;
            }
            for(int i = 0; i < this.scenes.Count; i++)
            {
                if(this.scenes[i].selected && this.scenes[i].sceneId.Length == 0)
                {
                    this.warnings = "Please specify scene ids for all selected scenes";
                    return;
                }
            }
        }
        this.StartUpload();
    }
}

public class UploadSceneObject
{
    public string sceneId;
    public bool selected;
    public string scenePath;

    public UploadSceneObject(string sceneId, bool selected, string scenePath)
    {
        this.sceneId = sceneId;
        this.selected = selected;
        this.scenePath = scenePath;
    }
}