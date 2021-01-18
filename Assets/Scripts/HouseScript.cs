using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseScript : MonoBehaviour
{

    // Person parameters
    public GameObject person;
    private GameObject go_person;
    public const float PERSON_X = 0.15f;
    public const float PERSON_Y = 0.25f;
    public const float PERSON_Z = 0.15f;
    private Vector3 person_init_position;
    private bool isPersonInside;

    // Window parameters
    public GameObject window;
    private GameObject go_window;
    private Material win_mat;
    public const float WINDOW_X = 0.01f;
    public const float WINDOW_Y = 0.4f;
    public const float WINDOW_Z = 0.4f;

    // Door parameters
    public GameObject door;
    private GameObject go_door;
    public const float DOOR_X = 0.01f;
    public const float DOOR_Y = 0.7f;
    public const float DOOR_Z = 0.3f;


    // The number of real-world seconds in one full game day.
    // Set this to 86400 for a 24-hour realtime day.
    private float secondsInFullDay = 120f;

    // The value we use to calculate the current time of day.
    // Goes from 0 (midnight) through 0.25 (sunrise), 0.5 (midday), 0.75 (sunset) to 1 (midnight).
    // We define ourself what value the sunrise sunrise should be etc., but I thought these 
    // values fit well. And now much of the script are hardcoded to these values.
    private float currentTimeOfDay = 0.5f;

    // A multiplier other scripts can use to speed up and slow down the passing of time.
    [HideInInspector]
    private float timeMultiplier = 1f;


    // Start is called before the first frame update
    void Start()
    {
        Vector3 house_position = this.gameObject.transform.position;
        Vector3 house_rotation = this.gameObject.transform.rotation.eulerAngles;
        Vector3 house_scale = this.gameObject.transform.localScale;

        float cos_angle = (float)Math.Cos(- Math.PI / 180 * house_rotation.y);
        float sin_angle = (float)Math.Sin(- Math.PI / 180 * house_rotation.y);

        /* Create a window */
        Vector3 window_position = house_position;
        Vector3 window_scale = house_scale;

        // Compute the right position of the window
        window_position.x += cos_angle * house_scale.x/2;
        window_position.z += sin_angle * house_scale.x/2;
        go_window = Instantiate(window, window_position, Quaternion.Euler(0, house_rotation.y, 0));

        window_scale.x *= WINDOW_X;
        window_scale.y *= WINDOW_Y;
        window_scale.z *= WINDOW_Z;

        go_window.transform.localScale = window_scale;
        go_window.transform.SetParent(this.gameObject.transform);

        win_mat = go_window.gameObject.GetComponent<Renderer>().material;
        win_mat.SetColor("_EmissionColor", Color.yellow);

        /* Create a door */
        Vector3 door_position = house_position;
        Vector3 door_scale = house_scale;

        // Compute the right position of the door
        door_scale.y *= DOOR_Y;
        door_position.y = door_scale.y/2;
        door_position.x -= cos_angle * house_scale.x/2;
        door_position.z -= sin_angle * house_scale.x/2;
        go_door = Instantiate(door, door_position, Quaternion.Euler(0, house_rotation.y, 0));

        door_scale.x *= DOOR_X;
        door_scale.z *= DOOR_Z;

        go_door.transform.localScale = door_scale;
        go_door.transform.SetParent(this.gameObject.transform);


        /* Create a person */
        person_init_position = house_position;
        Vector3 person_scale = house_scale;

        // Compute the right initial position of the person
        person_scale.y *= PERSON_Y;
        person_init_position.y = person_scale.y;

        person_init_position.x -= cos_angle * house_scale.x * 4f/5;
        person_init_position.z -= sin_angle * house_scale.x * 4f/5;
        go_person = Instantiate(person, person_init_position, Quaternion.Euler(0, house_rotation.y, 0));

        person_scale.x *= PERSON_X;
        person_scale.z *= PERSON_Z;

        go_person.transform.localScale = person_scale;
        go_person.transform.SetParent(this.gameObject.transform);
        isPersonInside = false;

    }

    // Set the person inside if it collides the house
    private void OnTriggerStay(Collider other){
        
        if(other.gameObject == go_person){
            isPersonInside = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // This makes currentTimeOfDay go from 0 to 1 in the number of seconds we've specified.
        currentTimeOfDay += (Time.deltaTime / secondsInFullDay) * timeMultiplier;

        // If currentTimeOfDay is 1 (midnight) set it to 0 again so we start a new day.
        if (currentTimeOfDay >= 1) {
            currentTimeOfDay = 0;
        }

        // The person leaves the house in the morning (can't be at home during the day)
        if(isPersonInside && currentTimeOfDay >= 0.25 && currentTimeOfDay < 0.75){
            isPersonInside = false;
            go_person.transform.position = person_init_position;
        }

        // Deactivate the person if it is inside the house and turn the light on
        if(isPersonInside && go_person.activeSelf){
            win_mat.EnableKeyword("_EMISSION");
            go_person.SetActive(false);
        }
        // Activate the person if it is outside the house and turn the ligh off
        else if(!isPersonInside && !go_person.activeSelf){
            go_person.transform.position = person_init_position;
            win_mat.DisableKeyword("_EMISSION");
            go_person.SetActive(true);
        }
    }
}
