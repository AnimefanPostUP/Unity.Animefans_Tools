//Class for Monitoring a Specific Key

namespace AnimefanPostUPs_Tools.KeyMonitor
{
    using System.Collections.Generic;
    using UnityEngine;
    using AnimefanPostUPs_Tools.KeyMonitorGroup;

    public class KeyMonitor
    {
        public KeyCode key;
        public int status = 0;

        public bool _down => status == 1;
        public bool _up => status == 3;
        public bool _held_only => status == 2;
        public bool _held => status == 2 || status == 1;

        public int bridgecounter = 0;

        public KeyMonitor(KeyMonitorGroup source, KeyCode key)
        {
            this.key = key;
            source.registerMonitor(this);
        }

        //Update the Key Status, to be called in the Update Function
        public void updateMonitor()
        {

            //Current event:
            Event currentEvent = Event.current;

            bool keydetected = currentEvent.keyCode == key
            || (key == KeyCode.LeftControl || key == KeyCode.RightControl ? currentEvent.control : false)
             || (key == KeyCode.LeftAlt || key == KeyCode.RightAlt ? currentEvent.alt : false)
              || (key == KeyCode.LeftShift || key == KeyCode.RightShift ? currentEvent.shift : false);

            //Skip is event is a repaint
            if (currentEvent.type == EventType.Repaint) return;
            if (currentEvent.type == EventType.KeyDown && keydetected)
            {
                status = 1;
                bridgecounter = 0;
            }
            else if (currentEvent.type == EventType.KeyUp && keydetected)
            {
                status = 3;
            }
            else if (keydetected)
            {
                status = 2;
                bridgecounter = 0;
            }
            else
            {
                if (bridgecounter < 2)
                {
                    status = 2;
                    bridgecounter++;
                }
                else
                {
                    status = 0;
                }

            }

            //Debug.Log("Key: " + key + " Status: " + statusToText());

        }

        public string statusToText()
        {
            switch (status)
            {
                case 0:
                    return "Off";
                case 1:
                    return "WasPressed";
                case 2:
                    return "On";
                case 3:
                    return "Released";
            }
            return "undefined status:" + status;
        }

        //Functions used for Callbacks

        public void reset()
        {
            status = 0;
        }

        public bool down() { return status == 1; }
        public bool held_only() { return status == 2; }
        public bool up() { return status == 3; }
        public bool held() { return status == 2 || status == 1; }
    }
}