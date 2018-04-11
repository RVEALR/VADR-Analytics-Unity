using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using VadRAnalytics;

public class TrackObjects : MonoBehaviour, IEventCollector {

    public float timeInterval = 0.5f;
    public List<GameObject> trackObjects;
    private List<ObjectTrackMetadata> trackObjectsMetadata;
    Camera userCamera;
    Plane[] planes;
    Infos info;
    Filters filter;
    Infos infoGaze;
    Filters filterGaze;
    Vector3 cameraForward;
    RaycastHit rayCastHit;
    bool objectHit;
    float objectDistance;
    float focusIndex;
    float objectAngle;
    string fileName;
    bool flagAddedFromFile; // Whether tracking objects are added from file
    public void setup()
    {
        flagAddedFromFile = false;
        trackObjectsMetadata = new List<ObjectTrackMetadata>();
        userCamera = DataCollectionManager.Instance.userCamera;
        if(trackObjects == null)
        {
            trackObjects = new List<GameObject>();
        }
        for(int i = 0; i < trackObjects.Count; i++)
        {
            trackObjectsMetadata.Add(new ObjectTrackMetadata(trackObjects[i], 0, 
                calculateSize(trackObjects[i]))); // Assuming the size of object doesn't changes 
        }
        fileName = Application.persistentDataPath + "/" + VadRConfig.TRACKER_OBJ_FILE;
        getTrackerObjects();
        getTrackerObjectsFromServer();
    }


    /// <summary>
    /// Add GameObject to the list of track objectss
    /// </summary>
    /// <param name="trackObject">Object to track</param>
    public void AddTrackObject(GameObject trackObject)
    {
        VadRLogger.debug("Adding tracked object: "+trackObject.name);
        for(int i = 0; i < trackObjects.Count; i++)
        {
            if(trackObjects[i] == trackObject)
            {
                return;
            }
        }
        VadRLogger.debug("Added tracked object: " + trackObject.name);
        trackObjects.Add(trackObject);
        trackObjectsMetadata.Add(new ObjectTrackMetadata(trackObject, 0, calculateSize(trackObject)));
    }

    /// <summary>
    /// Runs after timeInterval seconds
    /// </summary>
    public void loop()
    {
        StartCoroutine(CheckObjectTracking());
    }

    // Checking one object in a frame for better performance.
    IEnumerator CheckObjectTracking()
    {
        rayCastHit = new RaycastHit();
        objectHit = Physics.Raycast(userCamera.transform.position, cameraForward,
            out rayCastHit, Mathf.Infinity);
        planes = GeometryUtility.CalculateFrustumPlanes(userCamera);
        for (int i = 0; i < trackObjectsMetadata.Count; i++)
        {
            if (trackObjectsMetadata[i].gameObject != null)
            {
                if (isInView(trackObjectsMetadata[i], planes) || 
                    (objectHit && isParent(rayCastHit.transform, trackObjectsMetadata[i].gameObject.transform)))
                {
                    if (isInSight(trackObjectsMetadata[i].gameObject))
                    {
                        // Assuming object is visible for more than one second for observable
                        if (Time.time - trackObjectsMetadata[i].timeVisible > 1.0f)
                        {
                            cameraForward = userCamera.transform.forward;
                            // Ray Cast in camera froward direction to see if it hit the object.
                            // If hit the object angle = 0.
                            objectAngle = 0;
                            if (objectHit && isParent(rayCastHit.transform, trackObjectsMetadata[i].gameObject.transform))
                            {
                                objectAngle = 0;
                                infoGaze = new Infos(1);
                                filterGaze = new Filters(1);
                                filterGaze.Add("Object", trackObjectsMetadata[i].gameObject.name);
                                infoGaze.Add("Time", Time.time - trackObjectsMetadata[i].lastCheckTime);
                                VadRLogger.debug("Object Gazed: "+ trackObjectsMetadata[i].gameObject.name+(Time.time - trackObjectsMetadata[i].lastCheckTime));
                                VadRAnalyticsManager.RegisterEvent("vadrObject Gaze", rayCastHit.transform.position,
                                    filterGaze, infoGaze);
                            }
                            else
                            {
                                objectAngle = angleBetweenVectors(cameraForward, 
                                    trackObjectsMetadata[i].gameObject.transform.position-userCamera.transform.position);
                            }

                            objectDistance = Vector3.Distance(userCamera.transform.position,
                                trackObjectsMetadata[i].gameObject.transform.position);
                            focusIndex = calculateFocus(objectAngle, objectDistance, trackObjectsMetadata[i].size);
                            if (focusIndex > 0)
                            {
                                filter = new Filters(1);
                                filter.Add("Object", trackObjectsMetadata[i].gameObject.name);
                                info = new Infos(1);
                                info.Add("Focus", focusIndex);
                                VadRAnalyticsManager.RegisterEvent("vadrObject Focus", userCamera.transform.position,
                                    filter, info);
                            }
                        }
                    }
                    else
                    {
                        trackObjectsMetadata[i].timeVisible = Time.time; // Resetting time if not visible
                    }
                }
                else
                {
                    trackObjectsMetadata[i].timeVisible = Time.time; // Resetting time if not visible
                }
            }
            trackObjectsMetadata[i].lastCheckTime = Time.time;
            yield return null;
        }
    }

    private bool isInView(ObjectTrackMetadata trackObject, Plane[] planes)
    {
        if(trackObject.gameObject.transform.position != trackObject.position || 
            trackObject.gameObject.transform.rotation.eulerAngles != trackObject.rotation)
        {
            trackObject.position = trackObject.gameObject.transform.position;
            trackObject.rotation = trackObject.gameObject.transform.rotation.eulerAngles;
            trackObject.collider = trackObject.gameObject.GetComponent<Collider>();
        }
        if(trackObject.collider != null)
        {
            return GeometryUtility.TestPlanesAABB(planes, trackObject.collider.bounds);
        }
        Vector3 onScreen = userCamera.WorldToViewportPoint(trackObject.position);
        if (onScreen[2] > 0 && new Rect(0, 0, 1, 1).Contains(onScreen))
        {
            return true;
        }
        return false;
    }

    private bool isInSight(GameObject trackObject)
    {
        RaycastHit rayCastHit = new RaycastHit();
        bool objectHit = Physics.Raycast(userCamera.transform.position,
            trackObject.transform.position - userCamera.transform.position, out rayCastHit);
        // comparison using instanceId of tranforms of game object
        if (objectHit && isParent(rayCastHit.transform, trackObject.transform))
        {
            return true;
        }
        return false;
    }

    private bool isParent(Transform g1, Transform parent)
    {
        while(g1 != null)
        {
            if(g1.GetInstanceID() == parent.GetInstanceID())
            {
                return true;
            }
            g1 = g1.transform.parent;
        }
        return false;
    }

    
    private float angleBetweenVectors(Vector3 from, Vector3 to)
    {
        Vector3 angles = Quaternion.FromToRotation(to, from).eulerAngles;
        float angle = angles.y; // Taking only longitude angle now
        // Converting angle to -180 to 180.
        angle = angle % 360;
        if (angle > 180)
        {
            angle = angle - 360;
        }
        else if (angle < -180)
        {
            angle = angle + 360;
        }
        // Returning mod of angle. Both eyes are symmetric
        return Mathf.Abs(angle); 
    }

    // From http://www.sciencedirect.com/science/article/pii/0002939447923118
    // 0 to 10 deg = 1 to 0.5
    // 10 to 20 deg = 0.5 to 0.2
    // 20 deg to 45 deg = 0.2 to 0
    private float calculateFocus(float angle, float distance, double size)
    {
        float focus = 0;
        // The factor of 100 is obtained from experiment.
        // The apparent size of the object would depend inversly on distance^2.
        double distanceFactor = 100 * size / (distance * distance);
        distanceFactor = Math.Min(distanceFactor, 1);
        if(angle>= 0 && angle <= 10)
        {
            focus = 1 - (0.05f * angle);
        }
        else if(angle>10 && angle <= 20)
        {
            focus = 0.8f - (0.03f * angle);
        }
        else
        {
            focus = 0.36f - (0.008f*angle);// Assuming acuity 0 at foveal angle 45 deg.
        }
        focus = Mathf.Max(0, focus);
        return (float)(focus * distanceFactor);
    }

    private double calculateSize(GameObject trackObject)
    {
        //Size is the biggest diagnol
        double size = Math.Sqrt((trackObject.transform.lossyScale[0]) * (trackObject.transform.lossyScale[0]) +
                 (trackObject.transform.lossyScale[1]) * (trackObject.transform.lossyScale[1]) +
                 (trackObject.transform.lossyScale[2]) * (trackObject.transform.lossyScale[2]));
        return size;
    }


    private void getTrackerObjects()
    {
        
        Thread oThread = new Thread(() =>
        {
            if (File.Exists(fileName))
            {
                string[] fileLines = File.ReadAllLines(fileName);
                if (fileLines.Length > 0)
                {
                    flagAddedFromFile = false;
                    for (int i = 0; i < fileLines.Length; i++)
                    {
                        addTrackerObjectFromResult(fileLines[i]);
                    }
                }
            }
        });
        oThread.Start();
    }

    private void getTrackerObjectsFromServer()
    {
        if (DataCollectionManager.Instance.sceneId.Length > 0)
        {
            string url = VadRConfig.GET_TRACKERS + DataCollectionManager.Instance.sceneId + "/";
            HttpRequestHandler.GetRequest(url, 10000, success, error, new string[] { });
        }
    }

    private void addTrackerObjectFromResult(string result)
    {
        var parsed = JSON.Parse(result);//[{"name": "abc", "position": [10.2, 12.3, 123.2]}]
        if(parsed["objects"] != null)
        {
            JSONArray array = parsed["objects"].AsArray;
            VadRThreadManager.getInstance().Enqueue(AddTrackerObjectsCoroutine(array));
        }
    }

    IEnumerator AddTrackerObjectsCoroutine(JSONArray resultObjects)
    {
        GameObject[] allObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        GameObject matchObject;
        bool positionMatch = false;
        int multiple = 0;
        for(int i = 0; i < resultObjects.Count; i++)
        {
            multiple = 0;
            matchObject = null;
            positionMatch = false;
            if (resultObjects[i]["name"] != null)
            {
                for (int j = 0; j < allObjects.Length; j++)
                {
                    if (allObjects[j].name.Replace(' ', '_').Equals(resultObjects[i]["name"]))
                    {
                        multiple++;
                        matchObject = allObjects[j];
                        if (resultObjects[i]["position"] != null)
                        {
                            // -1 since position x is inversed
                            if (Mathf.Abs((-1*resultObjects[i]["position"][0].AsFloat) - allObjects[j].transform.position.x) < 0.001f 
                                && Mathf.Abs(resultObjects[i]["position"][1].AsFloat - allObjects[j].transform.position.y) < 0.001f
                                && Mathf.Abs(resultObjects[i]["position"][2].AsFloat - allObjects[j].transform.position.z) < 0.001f)
                            {
                                positionMatch = true;
                                break;
                            } 
                        }
                        else
                        {
                            positionMatch = false;
                        } 
                    }
                }
            }
            if(matchObject != null)
            {
                if (positionMatch || multiple == 1)
                {
                    AddTrackObject(matchObject);
                }
            }
            yield return null;
        }
    }

    private void success(HttpWebResponse response)
    {
        if ((int)response.StatusCode == 200)
        {
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string result = reader.ReadToEnd();
                if(result.Length > 0)
                {
                    if (!flagAddedFromFile)
                    {
                        addTrackerObjectFromResult(result);
                    }
                    File.WriteAllText(fileName, result); // Updating tracker file
                }
            }
        }
   }

    private void error(Exception e, string[] args)
    {

    }
    /// <summary>
    /// Get time interval of collecting events
    /// </summary>
    /// <returns>Time interval of collecting events (in sec.)</returns>
    public float getTimeInterval()
    {
        return this.timeInterval;
    }

    /// <summary>
    /// Sets time interval of collecting event
    /// </summary>
    /// <param name="timeInterval">Time Interval (in sec.)</param>
    public void setTimeInterval(float timeInterval)
    {
        this.timeInterval = timeInterval;
    }

    public static string Description()
    {
        return "Collects data on whether users observed a particular object."+
            " The object to be observed must have a collider.";
    }

    public static string Name()
    {
        return "Object Tracker";
    }

    class ObjectTrackMetadata
    {
        public GameObject gameObject;
        public float timeVisible;
        public float lastCheckTime;
        public double size;
        public Vector3 position;
        public Vector3 rotation;
        public Collider collider;

        public ObjectTrackMetadata(GameObject gameObject, float timeVisible, double size)
        {
            this.gameObject = gameObject;
            this.timeVisible = timeVisible;
            this.size = size;
            this.position = gameObject.transform.position;
            this.rotation = gameObject.transform.rotation.eulerAngles;
            this.collider = gameObject.GetComponent<Collider>();
            this.lastCheckTime = Time.time;
        } 
    }
}
 