using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using VadRAnalytics;

public class SceneUploader {

    string token = "";
    string sceneId = "";
    string folder = "";
    string sceneName = "";
    int totalFiles = 0;
    double totalBytes = 0;
    int totalFilesUploaded = 0;
    double totalBytesUploaded = 0;
    Model model;
    private object _lock;
    bool errorFlag;
    ModelUploadSucessCallback success;
    ModelUploadErrorCallback error;
    ModelUploadProgressCallback progress;
    public SceneUploader(string token, string sceneId, string folder, string sceneName)
    {
        this.token = token;
        this.sceneId = sceneId;
        this.folder = folder;
        this.totalFilesUploaded = 0;
        this.totalBytesUploaded = 0;
        this._lock = new object();
        this.sceneName = sceneName;
    }
    public void intiateUpload(ModelUploadProgressCallback progress, 
        ModelUploadSucessCallback success, ModelUploadErrorCallback error)
    {
        this.errorFlag = false;
        this.progress = progress;
        this.success = success;
        this.error = error;
        bool flagDrc = false; 
        DirectoryInfo d = new DirectoryInfo(this.folder);
        FileInfo[] files = d.GetFiles("*"); //Getting Text files
        string request = "{";
        request += "\"sceneId\": \"" + this.sceneId + "\",";
        request += "\"versionName\": \"" + Application.version + "\",";
        request += "\"token\": \"" + this.token + "\",";
        request += "\"files\": [";
        foreach (FileInfo file in files)
        {
            if(file.Extension == ".drc")
            {
                flagDrc = true;
            }
        }
        foreach (FileInfo file in files)
        {
            if (flagDrc && file.Extension == ".obj") //don't upload the .obj if .drc is there
                continue;
            if (file.Name.Length > 0)
            {
                this.totalFiles++;
                this.totalBytes += file.Length;
                request += "\"" + file.Name + "\",";
            }
        }
        if (request[request.Length - 1] == ',')
        {
            request = request.Substring(0, request.Length - 1);
        }
        request += "]}";
        this.regiterFiles(request);
    }

    private void regiterFiles(string request)
    {
        HttpRequestHandler.SucessCallback success = this.registerFileSuccess;
        HttpRequestHandler.ErrorCallback error = this.registerFileError;
        HttpRequestHandler.PostRequest(VadRConfig.REGISTER_FILE_URL, request, 10000, success, error, new string[0]);
    }

    private void registerFileSuccess(HttpWebResponse response)
    {
        if ((int)response.StatusCode == 200) //status code 200 should be only when success
        {
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseString = reader.ReadToEnd();
                var parsed = JSON.Parse(responseString);
                if (parsed["baseUrl"] != null && parsed["token"] != null)
                {
                    string baseUrl = parsed["baseUrl"];
                    string azureToken = parsed["token"];
                    if (parsed["model"] != null)
                    {
                        this.model = new Model(parsed["model"]["id"], this.sceneId,
                            parsed["model"]["version_id"], parsed["model"]["version"]);
                        if(parsed["files"] != null)
                        {
                            var files = parsed["files"].AsArray;
                            for(int i=0;i<files.Count; i++)
                            {
                                ModelFile file = new ModelFile(files[i]["actualName"], files[i]["blobName"], 
                                    files[i]["id"]);
                                this.model.addFile(file);
                            }
                        }
                        for(int i=0; i < this.model.files.Count; i++)
                        {
                            FileUploadProgressCallback progress = this.uploadFileProgressCallback;
                            FileUploadSuccessCallback success = this.uploadFileSuccessCallback;
                            FileUploadErrorCallback error = this.uploadFileErrorCallback;
                            string url = baseUrl + this.model.files[i].blobName + "?" + azureToken;
                            string filepath = this.folder + this.model.files[i].actualName;
                            AzureUploader uploader = new AzureUploader(url, filepath, success, progress, error);
                            uploader.startUpload();
                        }

                    }
                }
            }
        }
        response.Close();
    }

    private void registerFileError(Exception e, string[] args)
    {
        if (!this.errorFlag)
        {
            this.errorFlag = true;
            string msg = "Error Uploading model for scene " + this.sceneName +
                                        ". Please try after sometime";
            switch (e.GetType().ToString())
            {
                case "System.Net.WebException":
                    if (((WebException)e).Response != null)
                    {
                        var stream = ((WebException)e).Response.GetResponseStream();
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                string error = reader.ReadToEnd();
                                var parsedError = JSON.Parse(error);
                                if (parsedError == null || parsedError["message"] == null)
                                {
                                    VadRLogger.error(msg);
                                    if (this.error != null)
                                        this.error(this.sceneName, msg);
                                }
                                else
                                {
                                    VadRLogger.error("Error in uploading model for scene "
                                        + this.sceneName + " Error: " + parsedError["message"]);
                                    if (this.error != null)
                                        this.error(this.sceneName, "Error in uploading model for scene "
                                        +this.sceneName+ " Error: "+parsedError["message"]);
                                }
                            }
                        }
                        else
                        {
                            VadRLogger.error(msg);
                            if (this.error != null)
                                this.error(this.sceneName, msg);
                        }
                    }
                    else
                    {
                        if (this.error != null)
                            this.error(this.sceneName, msg);
                        VadRLogger.error(msg);
                    }
                    break;
                default:
                    if (this.error != null)
                        this.error(this.sceneName, msg);
                    VadRLogger.error(msg);
                    break;
            }
        }
    }

    private void updateUploadStatus()
    {
        if(this.model != null)
        {
            string request = "{\"modelId\": \""+this.model.modelId + "\",";
            request += "\"sceneId\": \"" + this.model.sceneId + "\",";
            request += "\"token\": \"" + this.token + "\",";
            request += "\"files\": [";
            for(int i = 0; i < this.model.files.Count; i++)
            {
                request += "\"" + this.model.files[i].id + "\",";
            }
            if(request[request.Length-1] == ',')
            {
                request = request.Substring(0, request.Length - 1);
            }
            request += "]}";
            HttpRequestHandler.SucessCallback success = this.updateStatusSuccess;
            HttpRequestHandler.ErrorCallback error = this.updateStatusError;
            HttpRequestHandler.PostRequest(VadRConfig.UPDATE_FILE_STATUS_URL, request, 10000, success, error, new string[0]);
        }
    }

    private void updateStatusSuccess(HttpWebResponse response)
    {
        if((int)response.StatusCode == 200)
        {
            if(this.success != null)
            {
                this.success(this.sceneName);
            }
        }
    }

    private void updateStatusError(Exception e, string[] args)
    {
        if (!this.errorFlag)
        {
            this.errorFlag = true;
            string msg = "Error Uploading model for scene " + this.sceneName +
                                        ". Please try after sometime";
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
                                if (parsedError == null || parsedError["message"] == null)
                                {
                                    VadRLogger.error(msg);
                                    if(this.error != null)
                                        this.error(this.sceneName, msg);
                                }
                                else
                                {
                                    VadRLogger.error("Error in uploading model for scene "
                                        + this.sceneName + " Error: " + parsedError["message"]);
                                    if (this.error != null)
                                        this.error(this.sceneName, "Error in uploading model for scene "
                                        + this.sceneName + " Error: " + parsedError["message"]);
                                }
                            }
                        }
                    }
                    else
                    {
                        VadRLogger.error(msg);
                        if (this.error != null)
                            this.error(this.sceneName, msg);
                    }
                    break;
                default:
                    VadRLogger.error(msg);
                    if (this.error != null)
                        this.error(this.sceneName, msg);
                    break;
            }
        }
    }

    void uploadFileProgressCallback(int bytesUploaded)
    {
        lock (this._lock)
        {
            this.totalBytesUploaded += bytesUploaded;
            if(this.progress != null)
            {
                this.progress(this.sceneName, (bytesUploaded / this.totalBytes));
            }
        }
    }

    void uploadFileSuccessCallback(string filepath)
    {
        lock (this._lock)
        {
            this.totalFilesUploaded++;
            if(this.totalFilesUploaded == this.totalFiles)
            {
                this.updateUploadStatus();
            }
        }
    }

    void uploadFileErrorCallback(string filepath, string msg)
    {
        if (!this.errorFlag)
        {
            string message = "Error in uploading model for scene: " + this.sceneName + ". Error: " + msg;
            VadRLogger.error(message);
            this.errorFlag = true;
            if(this.error != null)
            {
                this.error(this.sceneName, message);
            }
        }
    }

    public delegate void ModelUploadSucessCallback(string sceneName);
    public delegate void ModelUploadErrorCallback(string sceneName, string msg);
    public delegate void ModelUploadProgressCallback(string sceneName, double progress);
}

#pragma warning disable 0414

class Model
{
    public string modelId;
    string versionId;
    string version;
    public string sceneId;
    public List<ModelFile> files;

    public Model(string modelId, string sceneId, string versionId, string version)
    {
        this.modelId = modelId;
        this.sceneId = sceneId;
        this.versionId = versionId;
        this.version = version;
        this.files = new List<ModelFile>();
    }

    public void addFile(ModelFile file)
    {
        this.files.Add(file);
    }
    
}

class ModelFile
{
    public string actualName;
    public string blobName;
    public string id;

    public ModelFile(string actualName, string blobName, string id)
    {
        this.actualName = actualName;
        this.blobName = blobName;
        this.id = id;
    }
}