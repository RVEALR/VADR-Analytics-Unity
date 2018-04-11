using UnityEngine;
using UnityEditor;
using System.IO;
using VadRAnalytics;
using System.Net;
using System;

[InitializeOnLoad]
public class SdkUpdateCheck
{
    public static int latestVersionId = 0;
    public static string htmlUrl = "";
    public static string version = "";
    static bool flag;
    static bool runFlag = true;
    static object _lock = new object();

    static SdkUpdateCheck()
    {
        if (runFlag)
        {
            runFlag = false;
            lock (_lock)
            {
                flag = false;
            }
            HttpRequestHandler.GetRequest(VadRConfig.VERSION_CHECK_URL, 5000, success, error, new string[] { });
            EditorApplication.update += Update;
        }
    }

    static void Update()
    {
        if (flag)
        {
            lock (_lock)
            {
                flag = false;
            }
            if (version.Length > 0 && version != VadRConfig.VERSION && htmlUrl.Length > 0)
            {
                UpdateWindow.Init(version, htmlUrl);
            }
        }
    }

    static void success(HttpWebResponse response)
    {
        if ((int)response.StatusCode == 200)
        {
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseString = reader.ReadToEnd();
                var result = JSON.Parse(responseString);
                if (result["id"] != null)
                {
                    latestVersionId = result["id"].AsInt;
                    version = result["tag_name"];
                    if (result["html_url"] != null)
                    {
                        htmlUrl = result["html_url"];
                        lock (_lock)
                        {
                            flag = true;
                        }
                    }
                }
            }
        }
        response.Close();
    }

    static void error(Exception e, string[] args)
    {

    }
}