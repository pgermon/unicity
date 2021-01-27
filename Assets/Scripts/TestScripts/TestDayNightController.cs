using UnityEngine;
using System.Collections.Generic;

public class TestDayNightController : MonoBehaviour {

    // The directional light which we manipulate as our sun.
    public Light sun;
    public Light moon;

    // The number of real-world seconds in one full game day.
    // Set this to 86400 for a 24-hour realtime day.
    public float secondsInFullDay = 120f;

    // The value we use to calculate the current time of day.
    // Goes from 0 (midnight) through 0.25 (sunrise), 0.5 (midday), 0.75 (sunset) to 1 (midnight).
    // We define ourself what value the sunrise sunrise should be etc., but I thought these 
    // values fit well. And now much of the script are hardcoded to these values.
    //[Range(0,1)]
    public float currentTimeOfDay = 0.26f;

    // A multiplier other scripts can use to speed up and slow down the passing of time.
    [HideInInspector]
    public float timeMultiplier = 1f;

    // Get the initial intensity of the sun so we remember it.
    float sunInitialIntensity;
    float moonInitialIntensity;

    void Start(){
        sunInitialIntensity = sun.intensity;
        moonInitialIntensity = moon.intensity;
    }

    private bool day = true;

    void Update() {

        // Update the sceen once all the elements have been initialized
        if(this.gameObject.GetComponent<TestNavMesh>().getBuildNavMesh()){
            
            // This makes currentTimeOfDay go from 0 to 1 in the number of seconds we've specified.
            currentTimeOfDay += (Time.deltaTime / secondsInFullDay) * timeMultiplier;

            // If currentTimeOfDay is 1 (midnight) set it to 0 again so we start a new day.
            if (currentTimeOfDay >= 1) {
                currentTimeOfDay = 0;
            }

            // Updates the sun's rotation and intensity according to the current time of day.
            UpdateSunMoon();

            // Update the elements of the city according to the current time of day
            //UpdateCityObjects();
        }
    }

    void UpdateSunMoon() {
        // Rotate the sun 360 degrees around the x-axis according to the current time of day.
        // We subtract 90 degrees from this to make the sun rise at 0.25 instead of 0.
        // I just found that easier to work with.
        // The y-axis determines where on the horizon the sun will rise and set.
        // The z-axis does nothing.
        sun.transform.localRotation = Quaternion.Euler((currentTimeOfDay * 360f) - 90, 170, 0);
        moon.transform.localRotation = Quaternion.Euler((currentTimeOfDay * 360f) + 90, 170, 0);

        // The following determines the sun's intensity according to current time of day.
        // You'll notice I have hardcoded a bunch of values here. They were just the values
        // I felt worked best. This can obviously be made to be user configurable.
        // Also with some more clever code you can have different lengths for the day and
        // night as well.

        // The sun is full intensity during the day.
        float sunIntensityMultiplier = 1;
        float moonIntensityMultiplier = 1;
        // Set intensity to 0 during the night.
        if (currentTimeOfDay <= 0.23f || currentTimeOfDay >= 0.75f) {
            sunIntensityMultiplier = 0;
        }
        // Fade in the sun when it rises.
        else if (currentTimeOfDay <= 0.25f) {
            // 0.02 is the amount of time between sunrise and the time we start fading out
            // the intensity (0.25 - 0.23). By dividing 1 by that value we we get get 50.
            // This tells us that we have to fade in the intensity 50 times faster than the
            // time is passing to be able to go from 0 to 1 intensity in the same amount of
            // time as the currentTimeOfDay variable goes from 0.23 to 0.25. That way we get
            // a perfect fade.
            sunIntensityMultiplier = Mathf.Clamp01((currentTimeOfDay - 0.23f) * (1 / 0.02f));
            moonIntensityMultiplier = Mathf.Clamp01(1 - ((currentTimeOfDay - 0.73f) * (1 / 0.02f)));
        }
        // And fade it out when it sets.
        else if (currentTimeOfDay >= 0.73f) {
            sunIntensityMultiplier = Mathf.Clamp01(1 - ((currentTimeOfDay - 0.73f) * (1 / 0.02f)));
            moonIntensityMultiplier = Mathf.Clamp01((currentTimeOfDay - 0.23f) * (1 / 0.02f));
        }
        // Set moon intensity to 0 during the day
        else{
            moonIntensityMultiplier = 0;
        }

        // Multiply the intensity of the sun according to the time of day.
        sun.intensity = sunInitialIntensity * sunIntensityMultiplier;
        moon.intensity = moonInitialIntensity * moonIntensityMultiplier;
    }

    void UpdateCityObjects(){
        TestNavMesh city = this.gameObject.GetComponent<TestNavMesh>();
        
        List<GameObject> lampposts = city.getLampposts();

        // Day time
        if(currentTimeOfDay >= 0.25 && currentTimeOfDay < 0.75){

            if(!day){
                // Turn off the lampposts in the morning
                for(int i = 0; i < lampposts.Count; i++){
                    lampposts[i].gameObject.transform.Find("Lightbulb").gameObject.GetComponent<Light>().enabled = false;
                }
                day = !day;
            }

            // The house buildings push the persons outside
            List<GameObject> house_buildings = city.getHouseBuildings();
            for(int i = 0; i < house_buildings.Count; i++){
                house_buildings[i].gameObject.GetComponent<TestBuilding>().leaveBuilding();
            }

            // The inhabitants go to work
            List<GameObject> inhabitants = city.getInhabitants();
            for(int i = 0; i < inhabitants.Count; i++){
                TestPerson p = inhabitants[i].gameObject.GetComponent<TestPerson>();
                if(inhabitants[i].gameObject.activeSelf && p.getStrDestination() != "work"){
                    p.setDestination("work");
                }
            }
        }

        // Night time
        else if(currentTimeOfDay >= 0.75 || currentTimeOfDay < 0.25){

            if(day){
                // Turn on the lampposts in the evening
                for(int i = 0; i < lampposts.Count; i++){
                    lampposts[i].gameObject.transform.Find("Lightbulb").gameObject.GetComponent<Light>().enabled = true;
                }
                day = !day;
            }

            // The work buildings push the persons outside
            List<GameObject> work_buildings = city.getWorkBuildings();
            for(int i = 0; i < work_buildings.Count; i++){
                work_buildings[i].gameObject.GetComponent<TestBuilding>().leaveBuilding();
            }

            // The inhabitants go to their house
            List<GameObject> inhabitants = city.getInhabitants();
            for(int i = 0; i < inhabitants.Count; i++){
                TestPerson p = inhabitants[i].gameObject.GetComponent<TestPerson>();
                if(inhabitants[i].gameObject.activeSelf && p.getStrDestination() != "house"){
                    p.setDestination("house");
                }
            }
        }
    }
}