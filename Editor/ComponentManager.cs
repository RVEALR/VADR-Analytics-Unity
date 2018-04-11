using UnityEngine;
using UnityEditor;
using VadRAnalytics;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(DataCollectionManager))]
public class ComponentManager : Editor {

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        bool periodicSendFlag = true;
        bool pauseSendFlag = true;
        bool pauseOnHeadsetRemoval = true;
        DataCollectionManager dataManager = (DataCollectionManager)target;
        if(GUILayout.Button("Configure Manager"))
        {
            List<Type> typeList = ConfigurationManager.GetAllEventCollectors();
            List<VadrEventCollector> eventCollectors = new List<VadrEventCollector>();
            for (int i = 0; i < typeList.Count; i++)
            {
                eventCollectors.Add(new VadrEventCollector(typeList[i], false, -1.0f));
            }
            pauseSendFlag = dataManager.pauseSendFlag;
            periodicSendFlag = dataManager.periodicSendFlag;
            pauseOnHeadsetRemoval = dataManager.pauseOnHeadsetRemoval;
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
            for (int i = 0; i < eventCollectors.Count; i++)
            {
                if (eventCollectors[i].timeInterval < 0)
                {
                    eventCollectors[i].timeInterval = 0.2f;
                }
            }
            ConfigurationManager.Init(ref dataManager, eventCollectors,
                dataManager.appId, dataManager.appToken, dataManager.testMode, periodicSendFlag, pauseSendFlag, pauseOnHeadsetRemoval);
            ConfigurationManager.ShowWindow();
        }
    }
}
