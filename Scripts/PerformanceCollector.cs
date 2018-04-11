using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using VadRAnalytics;

public class PerformanceCollector : MonoBehaviour, IEventCollector
{
    public float timeInterval = 0.2f;
    Camera userCamera;
    float fpsSum;
    int totalFrames;
#if UNITY_ANDROID 
    bool cpuFlag = false;
    bool memFlag = false;
    float cpuUsage = 0;
    float previousTotalCpu = 0;
    float previousProcessCpu = 0;
    float residentStorage = -1.0f;
    float swapStorage = -1.0f;
#endif
    public void setup()
    {
        userCamera = DataCollectionManager.Instance.userCamera;
        totalFrames = 0;
        fpsSum = 0;
        VadRLogger.info("PerformanceCollector initialized");
        calculateCpuUsage();
        calculateRamUsage();
    }

    public float getTimeInterval()
    {
        return timeInterval;
    }

    public void loop()
    {
        if (totalFrames > 0)
        {
            Vector3 position = userCamera.transform.position;
            float fps = fpsSum / totalFrames;
            Infos info = new Infos();
            info.Add("FPS", fps);
#if UNITY_ANDROID
            if (cpuUsage > 0)
            {
                info.Add("Cpu Usage", cpuUsage);
            }
            if (residentStorage >= 0 && swapStorage >= 0)
            {
                info.Add("Memory ResidentUsage", residentStorage);
                info.Add("Memory SwapUsage", swapStorage);
            }

#endif
            VadRAnalyticsManager.RegisterEvent("vadrPerformance", position, info);
            fpsSum = 0;
            totalFrames = 0;
        }
    }

    public void Update()
    {
        fpsSum += 1 / Time.deltaTime;
        totalFrames++;
    }

    public static string Description()
    {
        return "Collects Performance metrics at regular interval. Like Frames per seconds, CPU usage, Memory Usage, etc.";
    }

    public void setTimeInterval(float timeInterval)
    {
        this.timeInterval = timeInterval;
    }

    public static string Name()
    {
        return "Performance Metrics";
    }

    void calculateCpuUsage()
    {
#if UNITY_ANDROID
        cpuFlag = true;
        if (Application.platform == RuntimePlatform.Android)
        {
            Thread oThread = new Thread(() =>
            {
                while (cpuFlag)
                {
                    float currentTotalCpu = 0.0f, currentProcessCpu = 0.0f;
                    try
                    {
                        System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                        string[] linesCpu = File.ReadAllLines("/proc/stat");
                        string[] linesCpuProcess = File.ReadAllLines("/proc/" + currentProcess.Id + "/stat");
                        if (linesCpu.Length > 0)
                        {
                            string[] cpuParts = linesCpu[0].Split(' ');
                            if (cpuParts.Length > 8)
                            {
                                for (int i = 2; i < 9; i++)
                                {
                                    currentTotalCpu += float.Parse(cpuParts[i]);
                                }
                            }
                        }
                        if (linesCpuProcess.Length > 0)
                        {
                            string[] cpuParts = linesCpuProcess[0].Split(' ');
                            if (cpuParts.Length >= 15)
                            {
                                currentProcessCpu = float.Parse(cpuParts[13]) + float.Parse(cpuParts[14]); //13 is for user time and 14 is for kernal time
                        }
                        }

                        if (previousTotalCpu > 0 && previousProcessCpu > 0)
                        {
                            cpuUsage = 100 * ((currentProcessCpu - previousProcessCpu) / (currentTotalCpu - previousTotalCpu));
                        }
                        previousTotalCpu = currentTotalCpu;
                        previousProcessCpu = currentProcessCpu;
                        Thread.Sleep((int)(timeInterval * 1000));
                    }
                    catch (Exception e)
                    {
                        VadRLogger.warning("Error in collecting cpu data: " + e.Message);
                    }
                }
                VadRLogger.debug("Cpu Calculation stopped");
            });
            oThread.Start();
        }
#endif
    }

    void calculateRamUsage()
    {
#if UNITY_ANDROID
        memFlag = true;
        if (Application.platform == RuntimePlatform.Android)
        {
            Thread oThread = new Thread(() =>
            {
                while (memFlag)
                {
                    try
                    {
                        System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                        string[] linesCpuProcess = File.ReadAllLines("/proc/" + currentProcess.Id + "/status");
                        for (int i = 0; i < linesCpuProcess.Length; i++)
                        {
                            string line = linesCpuProcess[i];
                            string[] parts = line.Split(':');
                            if (parts.Length > 1)
                            {
                                if (parts[0].Trim().ToLower() == "vmrss")
                                {
                                    residentStorage = float.Parse(parts[1].Trim().Split(' ')[0]) / 1000.0f;
                                }
                                else if (parts[0].Trim().ToLower() == "vmswap")
                                {
                                    swapStorage = float.Parse(parts[1].Trim().Split(' ')[0]) / 1000.0f;
                                }
                            }
                        }
                        Thread.Sleep((int)(timeInterval * 1000));
                    }
                    catch (Exception e)
                    {
                        VadRLogger.warning("Error in collecting Memory Data: " + e.Message);
                    }
                }
                VadRLogger.debug("Memory data collection stopped");
            });
            oThread.Start();
        }
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID
        cpuFlag = false;
        memFlag = false;
#endif
    }
}
