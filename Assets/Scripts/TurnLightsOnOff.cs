using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnLightsOnOff : MonoBehaviour
{
    // A reference to the DayNightController script
    private DayNightController controller;
    
    // Update is called once per frame
    void Update()
    {
        if(this.gameObject.transform.parent.gameObject.transform.parent.gameObject != null && this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().getBuildNavMesh()){

            controller = this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<DayNightController>();
            Light bulb = this.gameObject.GetComponent<Light>();
            if(bulb.enabled && controller.currentTimeOfDay >= 0.25 && controller.currentTimeOfDay < 0.75){
                bulb.enabled = false;
            }
            else if(!bulb.enabled && controller.currentTimeOfDay >= 0.75){
                bulb.enabled = true;
            }
        }
    }
}
