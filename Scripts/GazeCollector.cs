using UnityEngine;
using VadRAnalytics;

public class GazeCollector : MonoBehaviour, IEventCollector {

    Camera userCamera; 
    public float timeInterval = 0.2f;
    public LayerMask ignoreLayer;
    int mask;
    Vector3 gazePosition;
    Filters pauseFilter;
    public void setup()
    {
        mask = ~ignoreLayer.value;
        gazePosition = new Vector3(0, 0, 0);
        userCamera = DataCollectionManager.Instance.userCamera;
        pauseFilter = new Filters(1);
        pauseFilter.Add("Status","Paused");
    }

    public float getTimeInterval()
    {
        return timeInterval;
    }

    public void loop()
    {
        if (!VadRAnalyticsManager.IsMediaActive())
        {
            if (gazeDataInModel())
            {
                VadRAnalyticsManager.RegisterEvent("vadrGaze", gazePosition);
            }
        }
        else
        {
            gazeDataInMedia();
            if (VadRAnalyticsManager.IsVideoPaused())
            {
                VadRAnalyticsManager.RegisterEvent("vadrMedia Gaze", gazePosition, pauseFilter);    
            }
            else
            {
                VadRAnalyticsManager.RegisterEvent("vadrMedia Gaze", gazePosition);
            }
        }
    }

    private bool gazeDataInModel()
    {
        RaycastHit rayCastHit = new RaycastHit();
        bool objectHit = Physics.Raycast(userCamera.transform.position, userCamera.transform.forward, 
            out rayCastHit, Mathf.Infinity, mask);
        // See if we can remove the transparent objects and include non transparent objects.
        if (objectHit)
        {
            gazePosition = rayCastHit.point;
        }
        return objectHit;
    }

    private void gazeDataInMedia()
    {

        Vector3 rotation = getNormalizedRotation(userCamera.transform.rotation.eulerAngles + 
            VadRAnalyticsManager.GetMediaCameraOrientation());
        if(rotation.x >= -90 && rotation.x <= 90)
        {
            gazePosition.x = rotation.x; // Since registerData would multiply by -1, Not multiplying here
            gazePosition.y = rotation.y - 90;
            gazePosition.z = rotation.z;
        }
        else if (rotation.x >= 90 && rotation.x <= 180)
        {
            gazePosition.x = 180 - rotation.x; // Since registerData would multiply by -1, Inverting here
            gazePosition.y = rotation.y - 90;
            gazePosition.z = rotation.z;
        }
        else if (rotation.x >= -180 && rotation.x <= -90)
        {
            gazePosition.x = -1*(180 + rotation.x); // Since registerData would multiply by -1, Inverting here
            gazePosition.y = rotation.y + 90;
            gazePosition.z = rotation.z;
        }
        gazePosition = getNormalizedRotation(gazePosition);
    }

    private Vector3 getNormalizedRotation(Vector3 rotation) // return rotation in (-180, 180) and (-180,180)
    {
        rotation.x = rotation.x % 360;
        rotation.y = rotation.y % 360;
        rotation.z = rotation.z % 360;
        if (rotation.x > 180)
        {
            rotation.x = rotation.x - 360;
        }
        else if (rotation.x < -180)
        {
            rotation.x = rotation.x + 360;
        }
        if (rotation.y > 180)
        {
            rotation.y = rotation.y - 360;
        }
        else if (rotation.y < -180)
        {
            rotation.y = rotation.y + 360;
        }
        if (rotation.z > 180)
        {
            rotation.z = rotation.z - 360;
        }
        else if (rotation.z < -180)
        {
            rotation.z = rotation.z + 360;
        }
        return rotation;
    }

    public static string Description()
    {
        return "Collects User gaze (where user is looking) at regular interval.";
    }

    public void setTimeInterval(float timeInterval)
    {
        this.timeInterval = timeInterval;
    }

    public static string Name()
    {
        return "Gaze Metrics";
    }
}
