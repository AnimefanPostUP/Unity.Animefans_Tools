
//For Creating and Managing KeyMonitors

namespace AnifansAssetManager.KeyMonitorGroup
{

    using System;
    using System.Collections.Generic;
    using AnifansAssetManager.KeyMonitor;
    using UnityEngine;


    public class KeyMonitorGroup
    {
        public KeyMonitor control;
        public KeyMonitor alt;
        public KeyMonitor shift;
        public KeyMonitor d;
        List<Action> registeredUpdater = new List<Action>();

        //Registered the Update Keys on the Editor Application
        public KeyMonitorGroup()
        {

        }

        //Updates all Loaded Keys
        public void updateMonitors()
        {
            //Call update of all registered functions using thier update function
            foreach (Action update in registeredUpdater)
            {
                update();
            }
        }

        //Init the KeyMonitors, will Clear the Array and Load all Existing Key Monitors into the Array
        public void init()
        {
            registeredUpdater.Clear();

            control = new KeyMonitor(this, KeyCode.LeftControl);
            alt = new KeyMonitor(this, KeyCode.LeftAlt);
            shift = new KeyMonitor(this, KeyCode.LeftShift);
            d = new KeyMonitor(this, KeyCode.D);

        }

        public void registerMonitor(KeyMonitor monitor)
        {
            registeredUpdater.Add(monitor.updateMonitor);
        }

    }
}