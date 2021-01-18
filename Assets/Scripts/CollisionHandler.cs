using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    private const int MAX_MOVES = 100;
    private int nb_moves = 0;
    private int previous_nb_moves = 0;
    private int nb_same_updates = 0;

    public const int COEF = 100;
    public const int PLANE_SIZE = 10 * 2 * COEF;

    private bool is_well_positioned = false;

    private void OnTriggerStay(Collider other){
        
        if(other.gameObject.tag == "Water"){
            this.gameObject.GetComponent<Building>().destroyBuilding();
        }
        else if (other.gameObject.tag == "Building" || other.gameObject.tag == "Road"){

            if(nb_moves >= MAX_MOVES){
                this.gameObject.GetComponent<Building>().destroyBuilding();
            }

            else{
                Vector3 p = this.gameObject.transform.position;
                Vector3 p1 = other.gameObject.transform.position;
                Vector3 col_axis = p - p1;
                col_axis.y = 0;
                Vector3 new_position = this.gameObject.transform.position + col_axis/30;

                if(new_position.x < - PLANE_SIZE/2 || new_position.z > PLANE_SIZE/2
                    || new_position.z < - PLANE_SIZE/2 || new_position.z > PLANE_SIZE/2){
                        this.gameObject.GetComponent<Building>().destroyBuilding();
                }
                else{
                    nb_moves += 1;
                    this.gameObject.transform.position = new_position;
                }
            }
        }
    }

    void Update(){
        
        if(!is_well_positioned){
            if(nb_moves == previous_nb_moves){
                nb_same_updates += 1;
            }
            else{
                previous_nb_moves = nb_moves;
                nb_same_updates = 0;
            }

            if(nb_same_updates >= 10){
                is_well_positioned = true;
                if(this.gameObject.transform.parent != null){
                    this.gameObject.transform.parent.gameObject.GetComponent<CityGenerator>().increaseNbBuildingsOk();
                    //this.gameObject.transform.parent.gameObject.GetComponent<TestNavMesh>().increaseNbBuildingsOk();
                }
            }
        }
    }
}


