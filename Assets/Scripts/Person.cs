using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Person : MonoBehaviour
{
    public NavMeshAgent agent;
    [SerializeField]
    private Vector3 destination;
    [SerializeField]
    private string str_destination;
    private Vector3 init_position;

    private bool is_init = false;

    [SerializeField]
    private GameObject house;
    [SerializeField]
    private GameObject work_place = null;

    // A reference to the DayNightController script
	private DayNightController controller;

    public void Init(){
        if(work_place != null && this.gameObject.activeSelf){
            destination = work_place.gameObject.transform.position;
            destination.y = this.gameObject.transform.position.y;
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(destination);
            str_destination = "work";
        }
        is_init = true;
    }

    void Update(){
        if(is_init && this.gameObject.activeSelf && this.gameObject.transform.parent.gameObject != null && this.gameObject.transform.parent.gameObject.transform.parent.gameObject != null && this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().getBuildNavMesh()){
        //if(is_init && this.gameObject.activeSelf && this.gameObject.transform.parent.gameObject != null && this.gameObject.transform.parent.gameObject.transform.parent.gameObject != null && this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<TestNavMesh>().getBuildNavMesh()){
            controller = this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<DayNightController>();

            if(str_destination != "work" && controller.currentTimeOfDay >= 0.25 && controller.currentTimeOfDay < 0.75){
                setDestination("work");
            }
            else if(str_destination != "house" && (controller.currentTimeOfDay >= 0.75 || controller.currentTimeOfDay < 0.25)){
                setDestination("house");
            }
        }
    }


    /* GETTERS AND SETTERS */

    /* house */
    public void setHouse(GameObject h){
        house = h;
    }
    public GameObject getHouse(){
        return house;
    }

    /* work_place */
    public void setWorkPlace(GameObject w){
        work_place = w;
    }
    public GameObject getWorkPlace(){
        return work_place;
    }

    /* destination */
    public void setDestination(string dest){
        this.gameObject.SetActive(true);
        agent.enabled = true;
        // If the person must go to its work place
        if(dest == "work"){
            
            // If the work_place is not null, the person goes to it
            if(work_place != null){
                //Debug.Log("work_place != null");
                destination = work_place.gameObject.transform.position;
                destination.y = this.gameObject.transform.position.y;
                agent.isStopped = false;
                agent.SetDestination(destination);
                str_destination = dest;
            }
            // If the work_place is null, we try to assign the person to a vacant work_place
            else if(this.gameObject.transform.parent != null && this.gameObject.transform.parent.gameObject.transform.parent != null){
                Debug.Log("work_place == null");
                bool work_place_found = this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().assignPersonToWorkPlace(this.gameObject);
                //bool work_place_found = this.gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<TestNavMesh>().assignPersonToWorkPlace(this.gameObject);
                // If a vacant work_place is found, it is assigned to the person and the person goes to it
                if(work_place_found){
                    Debug.Log("work place found!");
                    destination = work_place.gameObject.transform.position;
                    destination.y = this.gameObject.transform.position.y;
                    //Debug.Log("activeSelf = " + this.gameObject.activeSelf);
                    agent.isStopped = false;
                    agent.SetDestination(destination);
                    str_destination = dest;
                }
                // If no vacant work place is found, the person is destroyed and removed from the list of residents of its house
                else{
                    Debug.Log("agent destroyed because no work place found");
                    this.gameObject.transform.parent.gameObject.GetComponent<Building>().removePerson(this.gameObject);
                    Destroy(this.gameObject);
                }
            }
        }

        // If the person must go to its house
        else if (dest == "house" && house != null){
            destination = house.gameObject.transform.position;
            destination.y = this.gameObject.transform.position.y;
            agent.isStopped = false;
            agent.SetDestination(destination);
            str_destination = dest;
        }
    }

    public Vector3 getDestination(){
        return destination;
    }

    public string getStrDestination(){
        return str_destination;
    }

    public bool isInit(){
        return is_init;
    }

    public void setInitPosition(Vector3 p){
        init_position = p;
    } 

}
