//Create Namespace
namespace AnifansAssetManager.KeyAction
{

    using System;
    using System.Collections.Generic;
    using AnifansAssetManager.KeyMonitorGroup;
    using AnifansAssetManager.KeyActionGroup;
    //Class to Manage Keybinds, Keys, and Actions that are Bind to them
    public class KeyAction
    {

        //enum for status
        public enum Actionstatus
        {
            Active, //Conditions Met
            Executed, //Was Triggered (If Trigger this Prvents a Second Execution until the Conditions arent met anymore)
            Abort, //Was Interupted
            Overwrite, //Conditions Met but low Priority
            Idle //Not Active
        }

        //Status of the Action
        public Actionstatus status = Actionstatus.Idle; //when the action is a callback

        //Bools that are used for Logic

        public string name = ""; //Name of the Action

        public bool isCallback = false;
        public bool disabled = false; //when the action was disabled by another action
        public bool is_trigger = false; //Stops the action from being executed multiple times

        //method to execute when Called
        public Func<bool> action;

        public Func<bool>[] conditions; //Normal Conditions to Execute the Action
        public Func<bool>[] triggerconditions; //If Given the Action will require the Trigger Conditions to be False before it can be executed again

        //internal
        public bool conditionsMet = false; //Normal Conditions
        public bool triggerconditionsMet = false; //


        public KeyAction(KeyActionGroup source, string name, Func<bool>[] conditions, Func<bool>[] triggerconditions, Func<bool> action = null) //Constructor with Callback
        {
            this.name = name;
            this.conditions = conditions;
            this.triggerconditions = triggerconditions;
            this.action = action;

            if (triggerconditions != null) this.is_trigger = true;
            if (action != null) this.isCallback = true;
            source.registerAction(this);
        }


        public bool checkConditions(Func<bool>[] funcs) //Check Only Conditions not other Logic
        {
            if (funcs == null) return true;
            foreach (Func<bool> cond in funcs)
            {
                if (cond == null) continue;
                //callback the conditions
                if (!cond())
                {
                    return false;
                }
            }
            return true;
        }


        public void update()
        { //Updates the Condition, to be fired to Update or at the Start of a GUI Draw

            //Updating the Conditions
            triggerconditionsMet = checkConditions(triggerconditions);
            conditionsMet = checkConditions(conditions) && triggerconditionsMet;

            //Reset the Status if the Conditions arent met anymore (trigger Logic)
            if (is_trigger)
            {
                if (status == Actionstatus.Executed && !triggerconditionsMet)
                {
                    status = Actionstatus.Idle;
                }
            }
            else
            {
                //if its not a trigger just reset the status to idle
                if (!triggerconditionsMet)
                    status = Actionstatus.Idle;
            }
        }


        public bool call_NoExecution() //returns just if the Action would be executed if called
        {
            if (disabled) return false;

            if (!conditionsMet) return false;

            if (is_trigger && status == Actionstatus.Executed)
            {
                return false;
            }

            return true;
        }

        public void set_Status_Executed()
        {
            status = Actionstatus.Executed;
        }

        public bool call() //Function that is called when the action is executed or to be executed
        {

            if (call_NoExecution())
            {
                if (isCallback)
                {
                    if (action.Invoke())
                    {
                        status = Actionstatus.Executed;
                        return true;
                    }
                    else
                    {
                        status = Actionstatus.Abort;
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

    }
}