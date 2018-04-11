using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VadRAnalytics;

public class RiftEventCollector : MonoBehaviour, IEventCollector
{

    Camera userCamera;
    public float timeInterval = 0.2f;
    Quaternion rOrientation;
    Vector3 rControllerPosition;
    Vector3 rControllerVelocity;
    Vector3 rControllerAcceleration;
    Vector3 rControllerAngularVelocity;
    Vector3 rControllerAngularAcceleration;
    Quaternion lOrientation;
    Vector3 lControllerPosition;
    Vector3 lControllerVelocity;
    Vector3 lControllerAcceleration;
    Vector3 lControllerAngularVelocity;
    Vector3 lControllerAngularAcceleration;
    Vector3 trackingLostPos;
    float trackingLostTime;
    Dictionary<string, Filters> filterDictionary;
    Dictionary<string, Vector3> clickPosition;
    Dictionary<string, float> clickTime;
    Filters headsetFilter;
    bool isHeadsetMounted;
    bool isRemovalRegistered;
    public void setup()
    {
        userCamera = DataCollectionManager.Instance.userCamera;
        headsetFilter = new Filters(1);
        headsetFilter.Add("Headset", "Oculus Rift");
        trackingLostTime = -1.0f;
        isHeadsetMounted = true;
        isRemovalRegistered = false;
        trackingLostPos = userCamera.transform.position;
#if VADR_RIFT
        // Initializing all in the starting so no need to do it again & again. GC collection
        initializeFilters();
        initializePositions();
        initializeClickFlag();
        StartCoroutine(GetRiftData());
#endif
    }


    private enum ButtonState
    {
        None = 1,
        Down = 2,
        Pressed = 3,
        Up = 4,
    }

    public float getTimeInterval()
    {
        return timeInterval;
    }

    public void loop()
    {
#if VADR_RIFT
        Infos info = new Infos(38);
        bool activeFlag = false;
        if (OVRInput.GetActiveController() == OVRInput.Controller.RTouch || OVRInput.GetActiveController() == OVRInput.Controller.Touch)
        {
            activeFlag = true;
            rOrientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            rControllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            rControllerVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            rControllerAcceleration = OVRInput.GetLocalControllerAcceleration(OVRInput.Controller.RTouch);
            rControllerAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.RTouch);
            rControllerAngularAcceleration = OVRInput.GetLocalControllerAngularAcceleration(OVRInput.Controller.RTouch);
            // Changing between left handed and right handed system. 
            // Refer https://gamedev.stackexchange.com/questions/129204/switch-axes-and-handedness-of-a-quaternion
            info.Add("Right Orientation X", rOrientation.x);
            info.Add("Right Orientation Y", -1 * rOrientation.y);
            info.Add("Right Orientation Z", -1 * rOrientation.z);
            info.Add("Right Orientation W", rOrientation.w);
            info.Add("Right Position X", -1 * rControllerPosition.x);
            info.Add("Right Position Y", rControllerPosition.y);
            info.Add("Right Position Z", rControllerPosition.z);
            info.Add("Right Velocity X", -1 * rControllerVelocity.x);
            info.Add("Right Velocity Y", rControllerVelocity.y);
            info.Add("Right Velocity Z", rControllerVelocity.z);
            info.Add("Right Acceleration X", -1 * rControllerAcceleration.x);
            info.Add("Right Acceleration Y", rControllerAcceleration.y);
            info.Add("Right Acceleration Z", rControllerAcceleration.z);
            info.Add("Right Ang Velocity X", rControllerAngularVelocity.x);
            info.Add("Right Ang Velocity Y", -1 * rControllerAngularVelocity.y);
            info.Add("Right Ang Velocity Z", -1 * rControllerAngularVelocity.z);
            info.Add("Right Ang Acceleration X", rControllerAngularAcceleration.x);
            info.Add("Right Ang Acceleration Y", -1 * rControllerAngularAcceleration.y);
            info.Add("Right Ang Acceleration Z", -1 * rControllerAngularAcceleration.z);
        }
        if (OVRInput.GetActiveController() == OVRInput.Controller.LTouch || OVRInput.GetActiveController() == OVRInput.Controller.Touch)
        {
            activeFlag = true;
            lOrientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            lControllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            lControllerVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            lControllerAcceleration = OVRInput.GetLocalControllerAcceleration(OVRInput.Controller.LTouch);
            lControllerAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.LTouch);
            lControllerAngularAcceleration = OVRInput.GetLocalControllerAngularAcceleration(OVRInput.Controller.LTouch);
            // Changing between left handed and right handed system. 
            // Refer https://gamedev.stackexchange.com/questions/129204/switch-axes-and-handedness-of-a-quaternion
            info.Add("Left Orientation X", lOrientation.x);
            info.Add("Left Orientation Y", -1 * lOrientation.y);
            info.Add("Left Orientation Z", -1 * lOrientation.z);
            info.Add("Left Orientation W", lOrientation.w);
            info.Add("Left Position X", -1 * lControllerPosition.x);
            info.Add("Left Position Y", lControllerPosition.y);
            info.Add("Left Position Z", lControllerPosition.z);
            info.Add("Left Velocity X", -1 * lControllerVelocity.x);
            info.Add("Left Velocity Y", lControllerVelocity.y);
            info.Add("Left Velocity Z", lControllerVelocity.z);
            info.Add("Left Acceleration X", -1 * lControllerAcceleration.x);
            info.Add("Left Acceleration Y", lControllerAcceleration.y);
            info.Add("Left Acceleration Z", lControllerAcceleration.z);
            info.Add("Left Ang Velocity X", lControllerAngularVelocity.x);
            info.Add("Left Ang Velocity Y", -1 * lControllerAngularVelocity.y);
            info.Add("Left Ang Velocity Z", -1 * lControllerAngularVelocity.z);
            info.Add("Left Ang Acceleration X", lControllerAngularAcceleration.x);
            info.Add("Left Ang Acceleration Y", -1 * lControllerAngularAcceleration.y);
            info.Add("Left Ang Acceleration Z", -1 * lControllerAngularAcceleration.z);
        }
        if (activeFlag)
        {
            VadRAnalyticsManager.RegisterEvent("vadrRift Controller Orientation", userCamera.transform.position, info);
        }
#endif
    }

#if VADR_RIFT
    /// <summary>
    /// Gets data like Playarea, Player Height, No. of Trackers
    /// </summary>
    /// <returns></returns>
    IEnumerator GetRiftData()
    {
        yield return new WaitForSeconds(1.0f);
        float height = OVRManager.profile.eyeHeight;
        Vector3 dimension = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
        float area = dimension.x * dimension.z;
        float trackerCount = OVRManager.tracker.count;
        Dictionary<string, float> riftMetadata = new Dictionary<string, float>
        {
            { "User Height", height },
            { "PlayArea", area },
            { "Trackers", trackerCount }
        };
        VadRAnalyticsManager.AddSessionMetadata(riftMetadata);
    }

    private void OnEnable()
    {
        OVRManager.TrackingLost += trackingLost;
        OVRManager.TrackingAcquired += trackingAcquired;
        OVRManager.HMDMounted += headsetMounted;
        OVRManager.HMDUnmounted += headsetUnmounted;
    }

    private void OnDisable()
    {
        OVRManager.TrackingLost -= trackingLost;
        OVRManager.TrackingAcquired -= trackingAcquired;
        OVRManager.HMDMounted -= headsetMounted;
        OVRManager.HMDUnmounted -= headsetUnmounted;
    }

    private void initializeFilters()
    {
        filterDictionary = new Dictionary<string, Filters>();
        Filters filter = new Filters(1);
        filter.Add("Button", "X");
        filterDictionary.Add("ButtonX", filter);
        filter = new Filters(1);
        filter.Add("Button", "Y");
        filterDictionary.Add("ButtonY", filter);
        filter = new Filters(1);
        filter.Add("Button", "Start");
        filterDictionary.Add("ButtonStart", filter);
        filter = new Filters(1);
        filter.Add("Button", "Left Index Trigger");
        filterDictionary.Add("LIndexTrigger", filter);
        filter = new Filters(1);
        filter.Add("Button", "Left Hand Trigger");
        filterDictionary.Add("LHandTrigger", filter);
        filter = new Filters(1);
        filter.Add("Button", "Left Stick");
        filterDictionary.Add("LStick", filter);
        filter = new Filters(1);
        filter.Add("Button", "A");
        filterDictionary.Add("ButtonA", filter);
        filter = new Filters(1);
        filter.Add("Button", "B");
        filterDictionary.Add("ButtonB", filter);
        filter = new Filters(1);
        filter.Add("Button", "Reserved");
        filterDictionary.Add("ButtonReserved", filter);
        filter = new Filters(1);
        filter.Add("Button", "Right Index Trigger");
        filterDictionary.Add("RIndexTrigger", filter);
        filter = new Filters(1);
        filter.Add("Button", "Right Hand Trigger");
        filterDictionary.Add("RHandTrigger", filter);
        filter = new Filters(1);
        filter.Add("Button", "Right Stick");
        filterDictionary.Add("RStick", filter);
    }

    private void initializePositions()
    {
        clickPosition = new Dictionary<string, Vector3>
        {
            { "ButtonX", userCamera.transform.position },
            { "ButtonY", userCamera.transform.position },
            { "ButtonStart", userCamera.transform.position },
            { "LIndexTrigger", userCamera.transform.position },
            { "LHandTrigger", userCamera.transform.position },
            { "LStick", userCamera.transform.position },
            { "ButtonA", userCamera.transform.position },
            { "ButtonB", userCamera.transform.position },
            { "ButtonReserved", userCamera.transform.position },
            { "RIndexTrigger", userCamera.transform.position },
            { "RHandTrigger", userCamera.transform.position },
            { "RStick", userCamera.transform.position }
        };
    }

    private void initializeClickFlag()
    {
        clickTime = new Dictionary<string, float>
        {
            { "ButtonX", 0.0f },
            { "ButtonY", 0.0f },
            { "ButtonStart", 0.0f },
            { "LIndexTrigger", 0.0f },
            { "LHandTrigger", 0.0f },
            { "LStick", 0.0f },
            { "ButtonA", 0.0f },
            { "ButtonB", 0.0f },
            { "ButtonReserved", 0.0f },
            { "RIndexTrigger", 0.0f },
            { "RHandTrigger", 0.0f },
            { "RStick", 0.0f }
        };
    }

    private void Update()
    {
        if(!VadRAnalyticsManager.IsDefaultCollectionPause())
            checkAllButtons();
    }

    private void checkAllButtons()
    {
        checkClick(OVRInput.Button.One, OVRInput.Controller.LTouch, "ButtonX", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.Two, OVRInput.Controller.LTouch, "ButtonY", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.Start, OVRInput.Controller.LTouch, "ButtonStart", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch, "LIndexTrigger", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch, "LHandTrigger", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch, "LStick", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.One, OVRInput.Controller.RTouch, "ButtonA", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.Two, OVRInput.Controller.RTouch, "ButtonB", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.Start, OVRInput.Controller.RTouch, "ButtonReserved", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch, "RIndexTrigger", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch, "RHandTrigger", "vadrRift Button Clicked");
        checkClick(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch, "RStick", "vadrRift Button Clicked");

    }

    private ButtonState checkClick(OVRInput.Button button, OVRInput.Controller controller, string key, string eventName)
    {
        if (OVRInput.Get(button, controller))
        {
            if (clickTime[key] > 0)
            {
                return ButtonState.Pressed; // Pressed
            }
            clickTime[key] = Time.timeSinceLevelLoad;
            clickPosition[key] = userCamera.transform.position;
            return ButtonState.Down; // Down
        }
        else if (clickTime[key] > 0)
        {
            Infos info = new Infos(1);
            info.Add("Duration", Time.timeSinceLevelLoad - clickTime[key]);
            VadRAnalyticsManager.RegisterEvent(eventName, clickPosition[key], filterDictionary[key], info, clickTime[key]);
            clickTime[key] = 0.0f;
            return ButtonState.Up; // Up
        }
        else
        {
            clickTime[key] = 0.0f;
            return ButtonState.None;
        }
    }

    private void trackingLost()
    {
        trackingLostPos = userCamera.transform.position;
        trackingLostTime = Time.timeSinceLevelLoad;
    }

    private void trackingAcquired()
    {
        if (trackingLostTime >= 0 && !VadRAnalyticsManager.IsDefaultCollectionPause())
        {
            Infos info = new Infos(1);
            info.Add("Time", Time.timeSinceLevelLoad - trackingLostTime);
            VadRAnalyticsManager.RegisterEvent("vadrRift Tracking Lost", trackingLostPos, headsetFilter, info, trackingLostTime);
            trackingLostTime = -1.0f;
        }
    }

    private void headsetUnmounted()
    {
        isHeadsetMounted = false;
        isRemovalRegistered = false;
        StartCoroutine(RegisterHeadsetRemovalEvent(Time.timeSinceLevelLoad, userCamera.transform.position));
        if (DataCollectionManager.Instance.pauseOnHeadsetRemoval)
        {
            VadRAnalyticsManager.PauseDefaultEventCollection(true);
            VadRAnalyticsManager.PauseTime(true);
        }
    }

    private void headsetMounted()
    {
        isHeadsetMounted = true;
        if (DataCollectionManager.Instance.pauseOnHeadsetRemoval)
        {
            VadRAnalyticsManager.PauseDefaultEventCollection(false);
            VadRAnalyticsManager.PauseTime(false);
        }
    }

    /// <summary>
    /// Checks whether headset is removed even after 5 seconds. There could be false alarms
    /// </summary>
    /// <returns></returns>
    IEnumerator RegisterHeadsetRemovalEvent(float removalTime, Vector3 position)
    {
        yield return new WaitForSeconds(5.0f);
        if (!isHeadsetMounted && !isRemovalRegistered)
        {
            isRemovalRegistered = true;
            VadRAnalyticsManager.RegisterEvent("vadrHeadset Removed", position, headsetFilter, removalTime);
        }
    }
#endif


    public static string Description()
    {
        return "Collects data related to Rift Controller events. Requires Oculus Utilities for Unity";
    }

    public void setTimeInterval(float timeInterval)
    {
        this.timeInterval = timeInterval;
    }

    public static string Name()
    {
        return "Rift Controller Metrics";
    }
}
