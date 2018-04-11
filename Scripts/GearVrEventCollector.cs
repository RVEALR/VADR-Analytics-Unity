using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VadRAnalytics;

public class GearVrEventCollector : MonoBehaviour, IEventCollector {

    Camera userCamera;
    public float timeInterval = 0.2f;
    Quaternion orientation;
    Vector3 controllerPosition;
    Vector3 controllerVelocity;
    Vector3 controllerAcceleration;
    Vector3 controllerAngularVelocity;
    Vector3 controllerAngularAcceleration;
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
        headsetFilter.Add("Headset", "Gear VR");
        isHeadsetMounted = true;
        isRemovalRegistered = false;
#if VADR_GEARVR
        // Initializing all in the starting so no need to do it again & again. GC collection
        initializeFilters();
        initializePositions();
        initializeClickFlag();
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


    public void initializeFilters()
    {
        filterDictionary = new Dictionary<string, Filters>();
        Filters filter = new Filters(1);
        filter.Add("Controller", "Right Controller");
        filterDictionary.Add("RController", filter);
        filter = new Filters();
        filter.Add("Controller", "Left Controller");
        filterDictionary.Add("LController", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Touchpad");
        filter.Add("Button", "Up");
        filterDictionary.Add("TouchpadUp", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Touchpad");
        filter.Add("Button", "Down");
        filterDictionary.Add("TouchpadDown", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Touchpad");
        filter.Add("Button", "Left");
        filterDictionary.Add("TouchpadLeft", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Touchpad");
        filter.Add("Button", "Right");
        filterDictionary.Add("TouchpadRight", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Touchpad");
        filter.Add("Button", "Tap");
        filterDictionary.Add("TouchpadTap", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Touchpad");
        filter.Add("Button", "Back");
        filterDictionary.Add("TouchpadBack", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Right Controller");
        filter.Add("Button", "Trigger");
        filterDictionary.Add("RControllerTrigger", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Right Controller");
        filter.Add("Button", "Back");
        filterDictionary.Add("RControllerBack", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Right Controller");
        filter.Add("Button", "Touchpad");
        filterDictionary.Add("RControllerTouchpad", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Left Controller");
        filter.Add("Button", "Trigger");
        filterDictionary.Add("LControllerTrigger", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Left Controller");
        filter.Add("Button", "Back");
        filterDictionary.Add("LControllerBack", filter);
        filter = new Filters(2);
        filter.Add("Controller", "Left Controller");
        filter.Add("Button", "Touchpad");
        filterDictionary.Add("LControllerTouchpad", filter);
    }

    public void initializePositions()
    {
        clickPosition = new Dictionary<string, Vector3>
        {
            { "TouchpadUp", userCamera.transform.position },
            { "TouchpadDown", userCamera.transform.position },
            { "TouchpadLeft", userCamera.transform.position },
            { "TouchpadRight", userCamera.transform.position },
            { "TouchpadTap", userCamera.transform.position },
            { "TouchpadBack", userCamera.transform.position },
            { "RControllerTrigger", userCamera.transform.position },
            { "RControllerBack", userCamera.transform.position },
            { "RControllerTouchpad", userCamera.transform.position },
            { "LControllerTrigger", userCamera.transform.position },
            { "LControllerBack", userCamera.transform.position },
            { "LControllerTouchpad", userCamera.transform.position }
        };
    }

    public void initializeClickFlag()
    {
        clickTime = new Dictionary<string, float>
        {
            { "TouchpadUp", 0.0f },
            { "TouchpadDown", 0.0f },
            { "TouchpadLeft", 0.0f },
            { "TouchpadRight", 0.0f },
            { "TouchpadTap", 0.0f },
            { "TouchpadBack", 0.0f },
            { "RControllerTrigger", 0.0f },
            { "RControllerBack", 0.0f },
            { "RControllerTouchpad", 0.0f },
            { "LControllerTrigger", 0.0f },
            { "LControllerBack", 0.0f },
            { "LControllerTouchpad", 0.0f }
        };
    }

    public void loop()
    {
#if VADR_GEARVR
        if(OVRInput.GetActiveController() == OVRInput.Controller.RTrackedRemote)
        {
            orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
            controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTrackedRemote);
            controllerVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTrackedRemote);
            controllerAcceleration = OVRInput.GetLocalControllerAcceleration(OVRInput.Controller.RTrackedRemote);
            controllerAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.RTrackedRemote);
            controllerAngularAcceleration = OVRInput.GetLocalControllerAngularAcceleration(OVRInput.Controller.RTrackedRemote);
            Infos info = new Infos(19);
            // Changing between left handed and right handed system. 
            // Refer https://gamedev.stackexchange.com/questions/129204/switch-axes-and-handedness-of-a-quaternion
            info.Add("Orientation X", orientation.x); 
            info.Add("Orientation Y", -1*orientation.y);
            info.Add("Orientation Z", -1 * orientation.z);
            info.Add("Orientation W",  orientation.w);
            info.Add("Position X", -1*controllerPosition.x);
            info.Add("Position Y", controllerPosition.y);
            info.Add("Position Z", controllerPosition.z);
            info.Add("Velocity X", -1 * controllerVelocity.x);
            info.Add("Velocity Y", controllerVelocity.y);
            info.Add("Velocity Z", controllerVelocity.z);
            info.Add("Acceleration X", -1 * controllerAcceleration.x);
            info.Add("Acceleration Y", controllerAcceleration.y);
            info.Add("Acceleration Z", controllerAcceleration.z);
            info.Add("Ang Velocity X", controllerAngularVelocity.x);
            info.Add("Ang Velocity Y", -1*controllerAngularVelocity.y);
            info.Add("Ang Velocity Z", -1 * controllerAngularVelocity.z);
            info.Add("Ang Acceleration X", controllerAngularAcceleration.x);
            info.Add("Ang Acceleration Y", -1 * controllerAngularAcceleration.y);
            info.Add("Ang Acceleration Z", -1 * controllerAngularAcceleration.z);
            VadRAnalyticsManager.RegisterEvent("vadrGearVR Controller Orientation", userCamera.transform.position, filterDictionary["RController"], info);
        }
        if (OVRInput.GetActiveController() == OVRInput.Controller.LTrackedRemote)
        {
            orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTrackedRemote);
            controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTrackedRemote);
            controllerVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTrackedRemote);
            controllerAcceleration = OVRInput.GetLocalControllerAcceleration(OVRInput.Controller.LTrackedRemote);
            controllerAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.LTrackedRemote);
            controllerAngularAcceleration = OVRInput.GetLocalControllerAngularAcceleration(OVRInput.Controller.LTrackedRemote);
            Infos info = new Infos(19);
            // Changing between left handed and right handed system. 
            // Refer https://gamedev.stackexchange.com/questions/129204/switch-axes-and-handedness-of-a-quaternion
            info.Add("Orientation X", orientation.x);
            info.Add("Orientation Y", -1 * orientation.y);
            info.Add("Orientation Z", -1 * orientation.z);
            info.Add("Orientation W", orientation.w);
            info.Add("Position X", -1 * controllerPosition.x);
            info.Add("Position Y", controllerPosition.y);
            info.Add("Position Z", controllerPosition.z);
            info.Add("Velocity X", -1 * controllerVelocity.x);
            info.Add("Velocity Y", controllerVelocity.y);
            info.Add("Velocity Z", controllerVelocity.z);
            info.Add("Acceleration X", -1 * controllerAcceleration.x);
            info.Add("Acceleration Y", controllerAcceleration.y);
            info.Add("Acceleration Z", controllerAcceleration.z);
            info.Add("Ang Velocity X", controllerAngularVelocity.x);
            info.Add("Ang Velocity Y", -1 * controllerAngularVelocity.y);
            info.Add("Ang Velocity Z", -1 * controllerAngularVelocity.z);
            info.Add("Ang Acceleration X", controllerAngularAcceleration.x);
            info.Add("Ang Acceleration Y", -1 * controllerAngularAcceleration.y);
            info.Add("Ang Acceleration Z", -1 * controllerAngularAcceleration.z);
            VadRAnalyticsManager.RegisterEvent("vadrGearVR Controller Orientation", userCamera.transform.position, filterDictionary["LController"], info);
        }
#endif
    }

    private void Update()
    {
#if VADR_GEARVR
        if(!VadRAnalyticsManager.IsDefaultCollectionPause())
            checkAllButtons();
#endif
    }

#if VADR_GEARVR
    private void OnEnable()
    {
        OVRManager.HMDMounted += headsetMounted;
        OVRManager.HMDUnmounted += headsetUnmounted;
    }

    private void OnDisable()
    {
        OVRManager.HMDMounted -= headsetMounted;
        OVRManager.HMDUnmounted -= headsetUnmounted;
    }

    private void checkAllButtons()
    {
        checkClick(OVRInput.Button.DpadUp, OVRInput.Controller.Touchpad, "TouchpadUp", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.DpadDown, OVRInput.Controller.Touchpad, "TouchpadDown", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.DpadLeft, OVRInput.Controller.Touchpad, "TouchpadLeft", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.DpadRight, OVRInput.Controller.Touchpad, "TouchpadRight", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.One, OVRInput.Controller.Touchpad, "TouchpadTap", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.Two, OVRInput.Controller.Touchpad, "TouchpadBack", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTrackedRemote, "RControllerTrigger", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.Back, OVRInput.Controller.RTrackedRemote, "RControllerBack", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.PrimaryTouchpad, OVRInput.Controller.RTrackedRemote, "RControllerTouchpad", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTrackedRemote, "LControllerTrigger", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.Back, OVRInput.Controller.LTrackedRemote, "LControllerBack", "vadrGearVR Button Clicked");
        checkClick(OVRInput.Button.PrimaryTouchpad, OVRInput.Controller.LTrackedRemote, "LControllerTouchpad", "vadrGearVR Button Clicked");
    }

    private ButtonState checkClick(OVRInput.Button button, OVRInput.Controller controller, string key, string eventName)
    {
        if(OVRInput.Get(button, controller))
        {
            if(clickTime[key] > 0)
            {
                return ButtonState.Pressed; // Pressed
            }
            clickTime[key]  = Time.timeSinceLevelLoad;
            clickPosition[key] = userCamera.transform.position;
            return ButtonState.Down; // Down
        }
        else if (clickTime[key] > 0)
        {
            Infos info = new Infos(1);
            info.Add("Duration", Time.timeSinceLevelLoad - clickTime[key]);
            VadRAnalyticsManager.RegisterEvent(eventName, clickPosition[key], filterDictionary[key], info, clickTime[key]);
            VadRLogger.debug("Event Registered: " + eventName);
            clickTime[key] = 0.0f;
            return ButtonState.Up; // Up
        }
        else
        {
            clickTime[key] = 0.0f;
            return ButtonState.None;
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
        return "Collects data related to Gear VR events. Requires Oculus utilities for Unity";
    }

    public void setTimeInterval(float timeInterval)
    {
        this.timeInterval = timeInterval;
    }

    public static string Name()
    {
        return "Gear VR Controller Metrics";
    }
}
