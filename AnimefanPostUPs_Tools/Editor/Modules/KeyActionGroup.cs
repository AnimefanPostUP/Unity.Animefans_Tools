namespace AnifansAssetManager.KeyActionGroup
{

    using System;
    using System.Collections.Generic;
    using AnifansAssetManager.KeyMonitorGroup;
    using AnifansAssetManager.KeyAction;


    //Class to Manage multiple KeyActions, their Execution and their Priority
    public class KeyActionGroup
    {

        //Store all Actions in a List
        public List<KeyAction> registeredUpdaters = new List<KeyAction>();

        public KeyAction mode_Focus;
        public KeyAction mode_Explorer;
        public KeyAction mode_Hidden;
        public KeyAction action_Update;
        public KeyAction action_Duplicate;


        public void updateKeyActions()
        {
            foreach (KeyAction action in registeredUpdaters)
            {
                action.update();
            }
        }

        public void init(KeyMonitorGroup keys)
        {
            mode_Focus = new KeyAction(this, "Mode_Focus", new Func<bool>[] { keys.control.held }, null, null);
            mode_Explorer = new KeyAction(this, "Mode_Explorer", new Func<bool>[] { keys.alt.held }, null, null);
            mode_Hidden = new KeyAction(this, "Mode_Hidden", new Func<bool>[] { keys.control.held, keys.alt.held, keys.shift.held }, null, null);
            action_Update = new KeyAction(this, "Mode_Update", new Func<bool>[] { keys.shift.held }, null, null);
            action_Duplicate = new KeyAction(this, "Action_Duplicate", new Func<bool>[] { keys.shift.held }, new Func<bool>[] { keys.d.down }, null);
        }

        public void registerAction(KeyAction action)
        {
            registeredUpdaters.Add(action);
        }



    }
}