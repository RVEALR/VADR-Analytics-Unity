using UnityEngine;
using VadRAnalytics;

public class OrientationCollector : MonoBehaviour, IEventCollector {

    public float timeInterval = 0.2f;
    Vector3 lastPosition;
    float lastTime;
    Camera userCamera;
    Vector3 velocity;
    Vector3 position;
    Quaternion orientation;
    public void setup()
    {
        userCamera = DataCollectionManager.Instance.userCamera;
        lastTime = Time.time;
        lastPosition = userCamera.transform.position;
        VadRLogger.info("PositionCollector initialized");
    }

    public float getTimeInterval()
    {
        return timeInterval;
    }

    //ToDo add rotation info
    public void loop()
    {
        float time = Time.time - lastTime;
        if (time > 0)
        {
            position = userCamera.transform.position;
            velocity = (position - lastPosition) / time;
            orientation = userCamera.transform.localRotation;
            Infos info = new Infos(8);
            info.Add("Time", time);
            info.Add("Velocity X", velocity.x);
            info.Add("Velocity Y", velocity.y);
            info.Add("Velocity Z", velocity.z);
            info.Add("Orientation X", orientation.x);
            info.Add("Orientation Y", -1 * orientation.y);
            info.Add("Orientation Z", -1 * orientation.z);
            info.Add("Orientation W", orientation.w);
            if (VadRAnalyticsManager.IsMediaActive())
            {
                VadRAnalyticsManager.RegisterEvent("vadrMedia Position", position, info);
            }
            else
            {
                VadRAnalyticsManager.RegisterEvent("vadrPosition", position, info);
            }
            lastPosition = position;
            lastTime = Time.time;
        }
    }

    public static string Description()
    {
        return "Collects User Orientation metrics at regular interval."+
            " This includes user position, user head rotation, etc.";
    }

    public void setTimeInterval(float timeInterval)
    {
        this.timeInterval = timeInterval;
    }

    public static string Name()
    {
        return "Orientation Metrics";
    }
}
