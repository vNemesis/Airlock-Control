using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        bool open = false;
        IMyDoor airlockDoorOuter;
        IMyDoor airlockDoorInner;
        IMySoundBlock airlockSoundBlock;
        IMyLightingBlock airlockLightingBlock;
        IMyAirVent airlockVent;
        bool requestedComplete = false;
        bool setFrequency = false;
        DateTime lastOperationTime;
        int stage = 0;

        public Program()
        {
            if (Storage.Length > 0)
            {
                var parts = Storage.Split(';');
                if (bool.TryParse(parts[0], out open))
                {
                    open = false;
                }
            }
            string[] blockNames = Me.CustomData.Split('\n');

            airlockDoorOuter = (IMyDoor)GridTerminalSystem.GetBlockWithName(blockNames[0]);
            airlockDoorInner = (IMyDoor)GridTerminalSystem.GetBlockWithName(blockNames[1]);
            airlockSoundBlock = (IMySoundBlock)GridTerminalSystem.GetBlockWithName(blockNames[2]);
            airlockLightingBlock = (IMyLightingBlock)GridTerminalSystem.GetBlockWithName(blockNames[3]);
            airlockVent = (IMyAirVent)GridTerminalSystem.GetBlockWithName(blockNames[4]);

            Echo($"Found Outer Airlock: {airlockDoorOuter != null}");
            Echo($"Found Inner Airlock: {airlockDoorInner != null}");
            Echo($"Found Airlock Sound: {airlockSoundBlock != null}");
            Echo($"Found Airlock Light: {airlockLightingBlock != null}");
            Echo($"Found Airlock Vent: {airlockVent != null}");
            Echo("Constructed");
        }

        public void Save()
        {
            Storage = $"{open};";
            Echo("Saved");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (requestedComplete)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                requestedComplete = false;
                setFrequency = false;
                stage = 0;
                string state = open ? "Open" : "CLosed";
                Echo($"Airlock: {state}");
            }
            else
            {
                if (!setFrequency)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    setFrequency = true;
                    lastOperationTime = DateTime.Now;
                }

                if (stage == 0)
                {
                    airlockLightingBlock.Enabled = true;
                    airlockSoundBlock.Play();
                    if (!open)
                    {
                        airlockDoorInner.CloseDoor();
                    }
                    else
                    {
                        airlockDoorOuter.CloseDoor();
                    }
                    lastOperationTime = DateTime.Now;
                    stage = 1;
                }
                else if (stage == 1 && CalculateLastTimeDifference() >= 10)
                {
                    airlockVent.Depressurize = (!open);
                    lastOperationTime = DateTime.Now;
                    stage = 2;
                }
                else if (stage == 2 && CalculateLastTimeDifference() >= 10)
                {
                    if (!open)
                    {
                        airlockDoorOuter.OpenDoor();
                    }
                    else
                    {
                        airlockDoorInner.OpenDoor();
                    }
                    airlockLightingBlock.Enabled = false;
                    airlockSoundBlock.Stop();
                    open = !open;
                    requestedComplete = true;
                }
            }
        }

        private int CalculateLastTimeDifference()
        {
            return (int)Math.Round((DateTime.Now - lastOperationTime).TotalSeconds);
        }
    }
}