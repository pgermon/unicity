using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    // Building associated to the script
    private bool is_house = false;
    private bool is_init = false;

    // Window parameters
    public GameObject window;
    private List<GameObject> go_windows;
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

    // Person parameters
    public GameObject person;
    private List<GameObject> go_persons;
    private List<GameObject> go_persons_inside;

    private int max_persons = 0;
    private Vector3 person_init_position;
    public const float PERSON_X = 0.15f;
    public const float PERSON_Y = 0.25f;
    public const float PERSON_Z = 0.15f;

    // A reference to the DayNightController script
	private DayNightController controller;

    public const int COEF = 100;

    /* Building dimensions */
	public const float SKYSCRAPER_WX = 0.2f * COEF;
	public const float SKYSCRAPER_WZ = 0.2f * COEF;
	public const float SKYSCRAPER_H = 1f * COEF;

	public const float BUILDING_WX = 0.2f * COEF;
	public const float BUILDING_WZ = 0.2f * COEF;
	public const float BUILDING_H = 1f * COEF;

	public const float HOUSE_WX = 0.2f * COEF;
	public const float HOUSE_WZ = 0.2f * COEF;
	public const float HOUSE_H = 0.2f * COEF;

    /* Height thresholds for each type of building */
	private const float SKYSCRAPER_THRESHOLD = 0.97f;
	private const float WORK_BUILDING_THRESHOLD = 0.90f;
	private const float HOUSE_BUILDING_THRESHOLD = 0.80f;
	private const float HOUSE_THRESHOLD = 0.70f;


    /* Create as much windows as possible according to the building height */
    void createWindows(){

        Vector3 building_position = this.transform.position;
        Vector3 building_rotation = this.transform.rotation.eulerAngles;
        Vector3 building_scale = this.transform.localScale;

        float cos_angle = (float)Math.Cos(- Math.PI / 180 * building_rotation.y);
        float sin_angle = (float)Math.Sin(- Math.PI / 180 * building_rotation.y);

        
        for(int n = 0; n < Math.Floor(building_scale.y/building_scale.x); n++){
            Vector3 window_position = building_position;
            Vector3 window_scale = building_scale;

            // Compute the right scale and position of the window
            window_scale.x = WINDOW_X * building_scale.x;
            window_scale.z = WINDOW_Z * building_scale.x;
            window_scale.y = WINDOW_Y * building_scale.x;

            window_position.x += cos_angle * building_scale.x/2;
            window_position.z += sin_angle * building_scale.x/2;
            window_position.y = n * building_scale.x + building_scale.x/2;

            GameObject go_window = Instantiate(window, window_position, Quaternion.Euler(0, building_rotation.y, 0));

            go_window.transform.localScale = window_scale;
            go_window.transform.SetParent(this.gameObject.transform);

            win_mat = go_window.gameObject.GetComponent<Renderer>().material;
            win_mat.SetColor("_EmissionColor", Color.yellow);
            go_windows.Add(go_window);
        }
        max_persons = go_windows.Count;
    }


    /* Create a door */
    void createDoor(){

        Vector3 building_position = this.transform.position;
        Vector3 building_rotation = this.transform.rotation.eulerAngles;
        Vector3 building_scale = this.transform.localScale;

        float cos_angle = (float)Math.Cos(- Math.PI / 180 * building_rotation.y);
        float sin_angle = (float)Math.Sin(- Math.PI / 180 * building_rotation.y);

        // Compute the right scale and position of the door
        Vector3 door_position = building_position;
        Vector3 door_scale = new Vector3(DOOR_X * building_scale.x, DOOR_Y * building_scale.x, DOOR_Z * building_scale.x);

        door_position.x -= cos_angle * building_scale.x / 2;
        door_position.y = door_scale.y / 2;
        door_position.z -= sin_angle * building_scale.x / 2;
        go_door = Instantiate(door, door_position, Quaternion.Euler(0, building_rotation.y, 0));

        go_door.transform.localScale = door_scale;
        go_door.transform.SetParent(this.gameObject.transform);
    }

    /* Create the persons of the building */
    void createPersons(){

        Vector3 building_rotation = this.transform.rotation.eulerAngles;
        Vector3 building_scale = this.transform.localScale;

        // Compute the right scale for the persons
        Vector3 person_scale = new Vector3(building_scale.x * PERSON_X, building_scale.x * PERSON_Y, building_scale.x * PERSON_Z);

        float cos_angle = (float)Math.Cos(- Math.PI / 180 * building_rotation.y);
        float sin_angle = (float)Math.Sin(- Math.PI / 180 * building_rotation.y);

        person_init_position = this.gameObject.transform.position;
        person_init_position.x -= cos_angle * building_scale.x * 4f/5;
        person_init_position.y = person_scale.y;
        person_init_position.z -= sin_angle * building_scale.x * 4f/5;

        if(is_house){
            for (int i = 0; i < max_persons; i++){
                // Instantiate the persons inside the house
                GameObject go_person = Instantiate(person, this.transform.position, Quaternion.Euler(0, building_rotation.y, 0));
                go_person.transform.localScale = person_scale;
                go_person.transform.SetParent(this.gameObject.transform);
                go_person.gameObject.GetComponent<Person>().setHouse(this.gameObject);
                go_person.gameObject.GetComponent<Person>().setInitPosition(person_init_position);
                go_persons.Add(go_person);
            }
        }
        
    }

    /* Make all the persons inside leave the building and set their destination according the type of the building */
    public void leaveBuilding(){

        // Compute the initial position to spawn the persons
        /*if(go_persons_inside.Count > 0){
            Vector3 building_rotation = this.transform.rotation.eulerAngles;
            Vector3 building_scale = this.transform.localScale;
            Vector3 person_scale = new Vector3(building_scale.x * PERSON_X, building_scale.x * PERSON_Y, building_scale.x * PERSON_Z);

            float cos_angle = (float)Math.Cos(- Math.PI / 180 * building_rotation.y);
            float sin_angle = (float)Math.Sin(- Math.PI / 180 * building_rotation.y);

            person_init_position = this.gameObject.transform.position;
            person_init_position.x -= cos_angle * building_scale.x * 4f/5;
            person_init_position.y = person_scale.y;
            person_init_position.z -= sin_angle * building_scale.x * 4f/5;
        }*/

        for(int i = 0; i < go_persons_inside.Count; i++){
            if(go_persons_inside[i].gameObject != null && go_persons_inside[i].gameObject.GetComponent<Person>().isInit()){
                go_persons_inside[i].gameObject.transform.position = person_init_position;
                go_persons_inside[i].gameObject.SetActive(true);
                go_persons_inside[i].gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
                go_persons_inside[i].gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = false;
                
                if(is_house){
                    go_persons_inside[i].gameObject.GetComponent<Person>().setDestination("work");
                }
                else{
                    go_persons_inside[i].gameObject.GetComponent<Person>().setDestination("house");
                }
            }
            go_persons_inside.RemoveAt(i);
            go_windows[go_persons_inside.Count].gameObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        }
    }

    public void Init(){
        go_windows = new List<GameObject>();
        go_persons = new List<GameObject>();
        go_persons_inside = new List<GameObject>();

        createWindows();
        createDoor();
        createPersons(); 

        is_init = true;
    }

    // Start is called before the first frame update
    /*void Start()
    {
        if (!is_init) Init();  
    }*/

    // Set a person inside the building if the person is allowed (resident or worker) and it collides the building
    private void OnTriggerStay(Collider other){
        
        if(other.gameObject.tag == "Person" && is_init && this.gameObject.transform.parent != null && this.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().getBuildNavMesh()){

            controller = this.gameObject.transform.parent.gameObject.GetComponent<DayNightController>();

            if((is_house && (controller.currentTimeOfDay >= 0.75 || controller.currentTimeOfDay < 0.25) && go_persons.Contains(other.gameObject) && go_persons_inside.Count < max_persons)
            || (!is_house && controller.currentTimeOfDay >= 0.25 && controller.currentTimeOfDay < 0.75 && go_persons.Contains(other.gameObject) && go_persons_inside.Count < max_persons)){
                other.gameObject.SetActive(false);
                go_persons_inside.Add(other.gameObject);
                go_windows[go_persons_inside.Count - 1].gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            }

            // If the person is not allowed in the building, it is pushed away
            else{
                Vector3 col_axis = other.gameObject.transform.position - this.gameObject.transform.position;
                col_axis.y = 0;
                Vector3 new_position = other.gameObject.transform.position + col_axis/2;
                other.gameObject.transform.position = new_position;
            }
        }
    }

    /* Actions to do before destroying a building */
    public void destroyBuilding(){
    
        if(is_house){
            this.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().removeFromHouseBuildings(this.gameObject);
            //this.gameObject.transform.parent.gameObject.GetComponent<TestNavMesh>().removeFromHouseBuildings(this.gameObject);
        }
        else{
            this.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().removeFromWorkBuildings(this.gameObject);
            //this.gameObject.transform.parent.gameObject.GetComponent<TestNavMesh>().removeFromWorkBuildings(this.gameObject);
        }

        Destroy(this.gameObject);
        Debug.Log("Building destroyed because of collision conflict");
    }


    // Update is called once per frame
    void Update()
    {
        if(is_init && this.gameObject.transform.parent != null && this.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().getBuildNavMesh()){
            controller = this.gameObject.transform.parent.gameObject.GetComponent<DayNightController>();

            // All the persons inside leave the house building in the morning
            if(is_house && go_persons_inside.Count > 0 && controller.currentTimeOfDay >= 0.25 && controller.currentTimeOfDay < 0.75){
                leaveBuilding();
            }
            // All the persons inside leave the work building in the evening
            else if(!is_house && go_persons_inside.Count > 0 && (controller.currentTimeOfDay >= 0.75 || controller.currentTimeOfDay < 0.25)){
                leaveBuilding();
            }
        }       
    }
    
    /* GETTERS AND SETTERS */

    /* isHouse */
    public bool isHouse(){
        return is_house;
    }
    public void setIsHouse(bool h){
        is_house = h;
    }

    /* go_persons */
    public List<GameObject> getGoPersons(){
        return go_persons;
    }

    /* Add a person to the list of persons */
    public void addPerson(GameObject person){
        go_persons.Add(person);
    }
    /* Remove a person to the list of persons */
    public void removePerson(GameObject person){
        go_persons.Remove(person);
    }

    /* max_persons */
    public int getMaxPersons(){
        return max_persons;
    }   
    /* vacant_work_places */
    public int getVacantWorkPlaces(){
        return max_persons - go_persons.Count;
    }
}
