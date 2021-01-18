using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class TestNavMesh : MonoBehaviour
{

    /* Parameters for map generation */
	public const int COEF = 100;
    public const int MAP_SIZE = 1000;
	private int PLANE_SIZE = 10;
	public const int DT_SIZE = 300;

	// Road dimensions
	public const float ROAD_X = 1.0f * COEF;
	public const float ROAD_Y = 0.001f * COEF;
	public const float ROAD_Z = 0.1f * COEF;

	// River parameters
	public const float RIVER_X = 750;
	public const float RIVER_W = 50;
	public const int NB_BRIDGES = 5;

	// Building dimensions
	public const float SKYSCRAPER_WX = 0.2f * COEF;
	public const float SKYSCRAPER_WZ = 0.2f * COEF;
	public const float SKYSCRAPER_H = 1f * COEF;

	public const float BUILDING_WX = 0.2f * COEF;
	public const float BUILDING_WZ = 0.2f * COEF;
	public const float BUILDING_H = 1f * COEF;

	public const float HOUSE_WX = 0.2f * COEF;
	public const float HOUSE_WZ = 0.2f * COEF;
	public const float HOUSE_H = 0.2f * COEF;

	// Height thresholds for each type of building
	private const float SKYSCRAPER_THRESHOLD = 0.97f;
	private const float WORK_BUILDING_THRESHOLD = 0.90f;
	private const float HOUSE_BUILDING_THRESHOLD = 0.80f;
	private const float HOUSE_THRESHOLD = 0.70f;


	/* Game Objects */
	public GameObject road;
	public GameObject house;
	public GameObject houseBuilding;
	public GameObject workBuilding;
	public GameObject skyscraper;
	public GameObject river;

	private List<GameObject> houseBuildings;
	private List<GameObject> workBuildings;
	private List<GameObject> lampposts;
	private List<GameObject> inhabitants;

	private int nb_inhabitants = 0;
	private int nb_work_places = 0;
	private int nb_buildings_ok = 0;
	private bool build_navmesh = false;

    public NavMeshSurface surface;

	/* Convert a value at the map scale to the plane scale */
	private float mapToPlaneScale(float x){
		return x / MAP_SIZE * PLANE_SIZE;
	}

	/* Convert a value at the plane scale to the map scale */
	private float planeToMapScale(float x){
		return x * MAP_SIZE / PLANE_SIZE;
	}

    bool isInRange(int x, int y){
		return (x >= 0 && x < MAP_SIZE && y >= 0 && y < MAP_SIZE);
	}

    
    private void drawRoad(Vector2 start, Vector2 end){
		Vector2 edge_vector = end - start;
		float angle = Vector2.SignedAngle(Vector2.right, edge_vector); // angle between the edge and the axe z = 0

		// Instantiate a road beginning at the left extremity of the edge and with the same angle
		GameObject go_road = Instantiate(road, 
										new Vector3((start.y + edge_vector.y / 2)/ MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2, 
													ROAD_Y/2,
		 											(start.x + edge_vector.x / 2)/ MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2),
										Quaternion.Euler(0, angle + 90, 0));

		// Rescale the road according to the norm of the edge vector
		edge_vector = edge_vector / MAP_SIZE * PLANE_SIZE *1/ROAD_X;
		float road_length = ROAD_X * edge_vector.magnitude;
		go_road.transform.localScale = new Vector3(road_length, ROAD_Y, ROAD_Z);
		go_road.gameObject.transform.SetParent(this.gameObject.transform);
	}

	private void drawBuilding(float x, float y, float height, float angle){

		if(isInRange((int)x, (int)y)){

			// Instantiate a skyscraper
			if (height > SKYSCRAPER_THRESHOLD){
				float rnd_h_coef = UnityEngine.Random.Range(0.0f, 1.0f);
				GameObject go_skyscraper = Instantiate(skyscraper,
														new Vector3(y / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2,
																	height/2 * (1 + rnd_h_coef) * SKYSCRAPER_H,
																	x / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2),
														Quaternion.Euler(0, angle, 0));
				go_skyscraper.transform.localScale = new Vector3(SKYSCRAPER_WX, height * (1 + rnd_h_coef) * SKYSCRAPER_H, SKYSCRAPER_WZ);
				
				go_skyscraper.gameObject.GetComponent<Building>().setIsHouse(false);
				go_skyscraper.gameObject.GetComponent<Building>().Init();
				go_skyscraper.gameObject.transform.SetParent(this.gameObject.transform);

				workBuildings.Add(go_skyscraper);
				//int nbWindows = (int)Math.Floor(go_skyscraper.transform.localScale.y/go_skyscraper.transform.localScale.x);
				//nb_work_places += nbWindows;
			}

			// Instantiate a work building
			else if (height > WORK_BUILDING_THRESHOLD){
				float rnd_h_coef = UnityEngine.Random.Range(- 0.5f, 0.5f);
				GameObject go_work_building = Instantiate(workBuilding,
														  new Vector3(y / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2,
														  			  height/4 * (1 + rnd_h_coef) * BUILDING_H,
																	  x / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2),
														  Quaternion.Euler(0, angle, 0));
				go_work_building.transform.localScale = new Vector3(BUILDING_WX, height/2 * (1 + rnd_h_coef) * BUILDING_H, BUILDING_WX);
				
				go_work_building.gameObject.GetComponent<Building>().setIsHouse(false);
				go_work_building.gameObject.GetComponent<Building>().Init();
				go_work_building.gameObject.transform.SetParent(this.gameObject.transform);

				workBuildings.Add(go_work_building);
				//int nbWindows = (int)Math.Floor(go_work_building.transform.localScale.y / go_work_building.transform.localScale.x);
				//nb_work_places += nbWindows;
			}

			// Instantiate a house building
			else if (height > HOUSE_BUILDING_THRESHOLD){
				float rnd_h_coef = UnityEngine.Random.Range(- 0.5f, 0.5f);
				GameObject go_house_building = Instantiate(houseBuilding,
														   new Vector3(y / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2,
																	   height/4 * (1 + rnd_h_coef) * BUILDING_H,
																	   x / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2),
														   Quaternion.Euler(0, angle, 0));
				go_house_building.transform.localScale = new Vector3(BUILDING_WX, height/2 * (1 + rnd_h_coef) * BUILDING_H, BUILDING_WX);
				
				go_house_building.gameObject.GetComponent<Building>().setIsHouse(true);
				go_house_building.gameObject.GetComponent<Building>().Init();
				go_house_building.gameObject.transform.SetParent(this.gameObject.transform);

				houseBuildings.Add(go_house_building);
				//int nbWindows = (int)Math.Floor(go_house_building.transform.localScale.y / go_house_building.transform.localScale.x);
				//nb_inhabitants += nbWindows;
			}

			// Instantiate a house
			else if (height > HOUSE_THRESHOLD){
				GameObject go_house = Instantiate(house,
				 								  new Vector3(y / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2,
				  											  HOUSE_H/2,
				  											  x / MAP_SIZE * PLANE_SIZE - PLANE_SIZE/2),
				   								  Quaternion.Euler(0, angle, 0));
				go_house.transform.localScale = new Vector3(HOUSE_WX, HOUSE_H, HOUSE_WZ);
				
				go_house.gameObject.GetComponent<Building>().setIsHouse(true);
				go_house.gameObject.GetComponent<Building>().Init();
				go_house.gameObject.transform.SetParent(this.gameObject.transform);

				houseBuildings.Add(go_house);
				//nb_inhabitants += 1;
			}
		}
	}

	private void drawRiver(){
		GameObject go_river = Instantiate(river, new Vector3(0, 0, mapToPlaneScale(RIVER_X) - PLANE_SIZE/2), Quaternion.identity);
		go_river.gameObject.transform.localScale = new Vector3(PLANE_SIZE, ROAD_Y/2, mapToPlaneScale(RIVER_W * 3f/4));
		go_river.gameObject.transform.SetParent(this.gameObject.transform);
	}

	/* Select a work place that has not been assigned to a person yet */
	public GameObject selectVacantWorkPlace(){
		for(int i = 0; i < workBuildings.Count; i++){
			Building b = workBuildings[i].gameObject.GetComponent<Building>();
			if(b.getVacantWorkPlaces() > 0){
				return workBuildings[i];
			}
		}
		return null;
	}

	/* Assign the person given to a vacant work place */
	public bool assignPersonToWorkPlace(GameObject person){
		GameObject work_place = selectVacantWorkPlace();
		if(work_place != null){
			person.gameObject.GetComponent<Person>().setWorkPlace(work_place);
			work_place.gameObject.GetComponent<Building>().addPerson(person);
			return true;
		}
		else{
			return false;
		}
	}

	
	/* Assign each person of each house building to a work place */
	public void assignWorkPlaces(){
		for (int i = 0; i < houseBuildings.Count; i++){
			List<GameObject> persons = houseBuildings[i].gameObject.GetComponent<Building>().getGoPersons();
			for (int n = 0; n < persons.Count; n++){
				GameObject work_place = selectVacantWorkPlace();
				if(work_place != null){
					persons[n].gameObject.GetComponent<Person>().setWorkPlace(work_place);
					persons[n].gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
					persons[n].gameObject.GetComponent<Person>().Init();
                    work_place.gameObject.GetComponent<Building>().addPerson(persons[n]);
					inhabitants.Add(persons[n]);
				}
			}
		}
	}


	/* Initialize all the buildings */
	public void initAllBuildings(){
		for(int i = 0; i < houseBuildings.Count; i++){
			houseBuildings[i].gameObject.GetComponent<Building>().Init();
		}
		for(int i = 0; i < workBuildings.Count; i++){
			workBuildings[i].gameObject.GetComponent<Building>().Init();
		}
	}

	/* Destroy houses until there are less inhabitants than work places */
	public void destroyExcessHouses(){
		while(nb_inhabitants > nb_work_places){
			int index = (int)UnityEngine.Random.Range(0, houseBuildings.Count);
			GameObject go_to_destroy = houseBuildings[index];
			int nbPeople = houseBuildings[index].gameObject.GetComponent<Building>().getGoPersons().Count;
			nb_inhabitants -= nbPeople;
			houseBuildings.RemoveAt(index);
			Destroy(go_to_destroy);
			Debug.Log("House building destroyed because of too many houses");
			//go_to_destroy.gameObject.GetComponent<Building>().destroyBuilding(false);
		}
	}

	public void countInhabitantAndWorkPlaces(){
		for(int i = 0; i < houseBuildings.Count; i++){
			nb_inhabitants += houseBuildings[i].gameObject.GetComponent<Building>().getGoPersons().Count;
		}
		for(int i = 0; i < workBuildings.Count; i++){
			nb_work_places += workBuildings[i].gameObject.GetComponent<Building>().getMaxPersons();
		}
	}

	/* Destroy houses in excess, build the NavMesh, initialize the buildings and assign the persons to work places */
	public void handleBuildingsAndNavMesh(){
		countInhabitantAndWorkPlaces();
		destroyExcessHouses();
		//initAllBuildings();
		surface.BuildNavMesh();
		Debug.Log("navmesh built");

		Debug.Log("nb house buildings = " + houseBuildings.Count);
		Debug.Log("nb inhabitants = " + nb_inhabitants);
		Debug.Log("nb work buildings = " + workBuildings.Count);
		Debug.Log("nb work places = " + nb_work_places);

		assignWorkPlaces();
	}

	public void removeFromHouseBuildings(GameObject hb){
		houseBuildings.Remove(hb);
	}
	public void removeFromWorkBuildings(GameObject wb){
		workBuildings.Remove(wb);
	}


    // Start is called before the first frame update
    void Start()
    {
        houseBuildings = new List<GameObject>();
		workBuildings = new List<GameObject>();
		lampposts = new List<GameObject>();
		inhabitants = new List<GameObject>();

		/* Rescale the plane */
		this.transform.localScale *= 2 * COEF;
		PLANE_SIZE *= 2 * COEF;

        //makeTestExample();
		makeCircleExample();
		//makeRandomExample();
    }

	void makeTestExample(){
		drawRoad(new Vector2(400, 500), new Vector2(490, 500));
		drawRoad(new Vector2(510, 500), new Vector2(580, 500));
		drawRoad(new Vector2(400, 400), new Vector2(400, 600));
		drawRoad(new Vector2(600, 400), new Vector2(500, 490));
		drawRoad(new Vector2(600, 600), new Vector2(500, 490));

		// Skyscrapers
        drawBuilding(600, 501, 0.98f, 270);
		drawBuilding(600, 499, 0.99f, 270);
		drawBuilding(601, 500, 0.99f, 270);
		drawBuilding(599, 500, 0.99f, 270);
		drawRoad(new Vector2(575, 490), new Vector2(625, 490));
		drawRoad(new Vector2(575, 510), new Vector2(625, 510));
		drawRoad(new Vector2(590, 475), new Vector2(590, 525));
		drawRoad(new Vector2(610, 475), new Vector2(610, 525));

		// House buildings

		drawBuilding(400, 600, 0.72f, 90);
		drawBuilding(400, 400, 0.85f, 90);
        drawBuilding(400, 500, 0.899f, 90);

		drawBuilding(600, 401, 0.71f, 270);
		drawBuilding(600, 399, 0.71f, 270);
		drawBuilding(601, 400, 0.88f, 270);
		drawBuilding(599, 400, 0.88f, 270);
		drawRoad(new Vector2(575, 390), new Vector2(625, 390));
		drawRoad(new Vector2(575, 410), new Vector2(625, 410));
		drawRoad(new Vector2(590, 375), new Vector2(590, 425));
		drawRoad(new Vector2(610, 375), new Vector2(610, 425));

		drawBuilding(600, 600, 0.88f, 270);
		drawBuilding(600, 620, 0.88f, 270);
		drawBuilding(600, 640, 0.88f, 270);
		drawBuilding(600, 660, 0.88f, 270);

		drawBuilding(500, 500, 0.71f, 90);

		drawRiver();
		drawBuilding(750, 500, 0.85f, 0);
	}

	void makeCircleExample(){
		int r = 50;
		int N = 8;

		for(int k = 0; k < N; k++){
			double teta = 2 * Math.PI / N * k;
			int x = (int)(500 + Math.Cos(teta) * r);
			int y = (int)(500 + Math.Sin(teta) * r);
			drawBuilding(x, y, 0.98f, 0);
		}
		
		for(int k = 0; k < N; k++){
			double teta = 2 * Math.PI / N * k;
			int x = (int)(100 + Math.Cos(teta) * r);
			int y = (int)(500 + Math.Sin(teta) * r);
			drawBuilding(x, y, 0.81f, 0);
		}

		for(int k = 0; k < N; k++){
			double teta = 2 * Math.PI / N * k;
			int x = (int)(900 + Math.Cos(teta) * r);
			int y = (int)(500 + Math.Sin(teta) * r);
			drawBuilding(x, y, 0.81f, 0);
		}

		for(int k = 0; k < N; k++){
			double teta = 2 * Math.PI / N * k;
			int x = (int)(500 + Math.Cos(teta) * r);
			int y = (int)(100 + Math.Sin(teta) * r);
			drawBuilding(x, y, 0.81f, 0);
		}

		for(int k = 0; k < N; k++){
			double teta = 2 * Math.PI / N * k;
			int x = (int)(500 + Math.Cos(teta) * r);
			int y = (int)(900 + Math.Sin(teta) * r);
			drawBuilding(x, y, 0.81f, 0);
		}


		drawRoad(new Vector2(100, 500), new Vector2(500, 500));
		drawRoad(new Vector2(900, 500), new Vector2(500, 500));
		drawRoad(new Vector2(500, 100), new Vector2(500, 500));
		drawRoad(new Vector2(500, 900), new Vector2(500, 500));
	}

	void makeRandomExample(){
		// Work buildings
		for(int i = 0; i < 50; i++){
			float x = UnityEngine.Random.Range(MAP_SIZE * 1f/4, MAP_SIZE * 3f/4);
			float y = UnityEngine.Random.Range(MAP_SIZE * 1f/4, MAP_SIZE * 3f/4);
			float height = UnityEngine.Random.Range(0.90f, 1.0f);
			float angle = UnityEngine.Random.Range(0, 360);
			drawBuilding(x, y, height, 0);
		}

		// House buildings
		for(int i = 0; i < 400; i++){
			float x = UnityEngine.Random.Range(0, MAP_SIZE);
			float y = UnityEngine.Random.Range(0, MAP_SIZE);
			if((x < MAP_SIZE * 1f/4 || x > MAP_SIZE * 3f/4) || (y < MAP_SIZE * 1f/4 || y > MAP_SIZE * 3f/4)){
				float height = UnityEngine.Random.Range(0.70f, 0.89f);
				float angle = UnityEngine.Random.Range(0, 360);
				drawBuilding(x, y, height, 0);
			}
		}
	}

	/* Once all the collision of the buildings have been handled, we can assign the work places and build the nawmesh */
	void Update(){
		if(!build_navmesh && nb_buildings_ok == houseBuildings.Count + workBuildings.Count){
			handleBuildingsAndNavMesh();
			build_navmesh = true;
		}
	}

	public void increaseNbBuildingsOk(){
		nb_buildings_ok += 1;
	}

	public bool getBuildNavMesh(){
		return build_navmesh;
	}

	
	public void addInhabitants(GameObject i){
		inhabitants.Add(i);
	}

	public List<GameObject> getHouseBuildings(){
		return houseBuildings;
	}

	public List<GameObject> getWorkBuildings(){
		return workBuildings;
	}

	public List<GameObject> getLampposts(){
		return lampposts;
	}

	public List<GameObject> getInhabitants(){
		return inhabitants;
	}
}
