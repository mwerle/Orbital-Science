﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
    //Inherit default science experiment module.
    public class DMMagBoomModule : ModuleScienceExperiment
    {

        [KSPField(isPersistant = false)]
        public string animationName;

        [KSPField(isPersistant = true)]
        bool IsEnabled = false;

        [KSPField(isPersistant = false)]
        bool IsExperimenting = false;

        protected Animation anim;

        //Get first animation name from part. Force module activation and make deployed animation stick.
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.part.force_activate();
            anim = part.FindModelAnimators(animationName)[0];
            if (IsEnabled && anim[animationName] != null)
            {
                anim[animationName].speed = 1f;
                anim[animationName].normalizedTime = 1f;
                anim.Play(animationName);
            }
        }

        //Right click deploy animation. Animation is reversible while playing.
        [KSPEvent(guiActive = true, guiName = "Deploy Magnetometer", active = true)]
        public void DeployEvent()
        {
            if (anim[animationName] != null)
            {
                anim[animationName].speed = 1.5f;

                //Check if animation is stopped, if not animating and undeployed, start deploy animation. If already deployed do nothing.
                if (!anim.IsPlaying(animationName))
                {
                    if (IsEnabled) { return; }
                    else
                    {
                        anim[animationName].normalizedTime = 0f;
                        anim.Play(animationName);
                    }
                }
                
                IsEnabled = true;

                //print("Deploying Magnetometer with " + animationName);
                
                Events["DeployEvent"].active = false;
                Events["RetractEvent"].active = true;
            }
        }

        //Right click retract animation, same as deploy animation.
        [KSPEvent(guiActive = true, guiName = "Retract Magnetometer", active = false)]
        public void RetractEvent()
        {
            if (anim[animationName] != null)
            {
                anim[animationName].speed = -1.5f;
                if (!anim.IsPlaying(animationName))
                {
                    if (!IsEnabled) { return; }
                    else
                    {
                        anim[animationName].normalizedTime = 1f;
                        anim.Play(animationName);
                    }
                }

                IsEnabled = false;

                //print("Retracting Magnetometer with " + animationName);

                Events["DeployEvent"].active = true;
                Events["RetractEvent"].active = false;
            }
        }
        
        //VAB/SPH tweakable deploy/retract toggle.
        [KSPEvent(guiActiveEditor = true, guiName = "Deploy", active = true)]
        public void VABDeploy()
        {
            Events["VABDeploy"].active = false;
            Events["VABRetract"].active = true;
            anim[animationName].speed = 1f;
            anim[animationName].normalizedTime = 1f;
            anim.Play(animationName);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
        public void VABRetract()
        {
            Events["VABDeploy"].active = true;
            Events["VABRetract"].active = false;
            anim[animationName].speed = -1f;
            anim[animationName].normalizedTime = 0f;
            anim.Play(animationName);
        }


        //VAB action group toggle animation.
        [KSPAction("Toggle Magnetometer")]
        public void ToggleEventAction(KSPActionParam param)
        {
            if (IsEnabled) {
                RetractEvent();
            } else {
                DeployEvent();
            }
        }

        //Check craft position and situation.
        public bool VesselSituation()
        {
            //print(vessel.orbit.referenceBody.name + ", " + vessel.situation + ", " + vessel.landedAt + ", " + vessel.altitude + ", " + FlightGlobals.currentMainBody.BiomeMap.GetAtt(vessel.latitude, vessel.longitude).name + ", " + FlightGlobals.currentMainBody.position.x + ", " + FlightGlobals.currentMainBody.position.y + ", " + vessel.latitude + ".");
            if (vessel.situation == Vessel.Situations.FLYING)
            {
                ScreenMessages.PostScreenMessage("The magnetometer is not suitable for use during atmospheric flight, try again on the ground or in space.", 4f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            } else 
            {
                return true;
            }
        }

        //[KSPEvent(guiActive = true, guiName = "Check Position", active = true)]
        //public void CheckPosition()
        //{
        //    VesselSituation();
        //}

        //[KSPAction("Check Position")]
        // public void PositionCheckAction(KSPActionParam param)
        //{ CheckPosition();
        //}

               
       //Replace default science collection mechanism
        //If the part is stowed call the deploy animation and collect science when it completes
        //If the part is in deploy animation, wait until finished and collect science
        //If the part is in the retract animation, reverse the animation and wait until finished to collect science
        //Check that the vessel is in the proper situation for science collection
        //Only allow one data collection operation at a time
        
       IEnumerator WaitForDeploy(float animTime)
       {
        if (anim[animationName] != null)
        {
            yield return new WaitForSeconds(animTime);
            base.DeployExperiment();
            IsExperimenting = false;
        }
       }

        new public void DeployExperiment()
        {
            if (IsExperimenting) { return; }
            else if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        if (anim.IsPlaying(animationName))
                        {
                            ScreenMessages.PostScreenMessage("You can't expect good results while the boom is still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                            StartCoroutine(WaitForDeploy((anim[animationName].length / 1.5f) - (anim[animationName].normalizedTime * (anim[animationName].length / 1.5f))));
                            IsExperimenting = true;
                        }
                        else
                        {
                            base.DeployExperiment();
                            IsExperimenting = false;
                        }
                    }
                }
                else if (VesselSituation() && anim.IsPlaying(animationName) == true)
                {
                    DeployEvent();
                    DeployExperiment();
                }

                else if (VesselSituation())
                {
                    DeployEvent();
                    ScreenMessages.PostScreenMessage("Close proximity to the craft scrambles the magnetometer's sensors, deploying the scanner now.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    StartCoroutine(WaitForDeploy(anim[animationName].length / 1.5f));
                    IsExperimenting = true;
                }
            
        }                   
        //Replace default science collection VAB action group function.
        new public void DeployAction(KSPActionParam p)
        {
            if (IsExperimenting) { return; }
            else if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        if (anim.IsPlaying(animationName))
                        {
                            ScreenMessages.PostScreenMessage("You can't expect good results while the boom is still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                            StartCoroutine(WaitForDeploy((anim[animationName].length / 1.5f) - (anim[animationName].normalizedTime * (anim[animationName].length / 1.5f))));
                            IsExperimenting = true;
                        }
                        else
                        {
                            base.DeployAction(p);
                            IsExperimenting = false;
                        }
                    }
                }
                else if (VesselSituation() && anim.IsPlaying(animationName) == true)
                {
                    DeployEvent();
                    DeployAction(p);
                }
                else if (VesselSituation())
                {
                    DeployEvent();
                    ScreenMessages.PostScreenMessage("Close proximity to the craft scrambles the magnetometer's sensors, deploying the scanner now.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    StartCoroutine(WaitForDeploy(anim[animationName].length / 1.5f));
                    IsExperimenting = true;
                }
        }
        
    }
}