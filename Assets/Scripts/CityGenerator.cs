using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using System;

public class CityGenerator : MonoBehaviour
{

    public Material land;

	public NavMeshSurface surface;

	/* Parameters for map generation */
	public const int COEF = 100;
    public int NPOINTS = 10;
    public const int MAP_SIZE = 1000;
	private int PLANE_SIZE = 10;

	// Downtown parameters
	public const int DT_SIZE = 300;
	public const int DT_STREETS = 5;

	// River parameters
	public const float RIVER_X = 750;
	public const float RIVER_W = 50;
	public const int NB_BRIDGES = 5;

	// Road dimensions
	public const float ROAD_X = 1.0f * COEF;
	public const float ROAD_Y = 0.001f * COEF;
	public const float ROAD_Z = 0.2f * COEF;

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

	// Lamppost dimensions
	public const float LAMPPOST_X = 0.2f * COEF;
	public const float LAMPPOST_Y = 0.2f * COEF;
	public const float LAMPPOST_Z = 0.2f * COEF;

	// PerlinNoise parameters
	//public float freqx = 0.02f, freqy = 0.018f, offsetx = 0.43f, offsety = 0.22f;
	private float freqx = 0.005f, freqy = 0.005f, offsetx = 0f, offsety = 0f;

	/* Game Objects */
	public GameObject road;
	public GameObject house;
	public GameObject houseBuilding;
	public GameObject workBuilding;
	public GameObject skyscraper;
	public GameObject river;
	public GameObject lamppost;

	private List<GameObject> houseBuildings;
	private List<GameObject> workBuildings;
	private List<GameObject> lampposts;
	private List<GameObject> inhabitants;

	private int nb_inhabitants = 0;
	private int nb_work_places = 0;
	private int nb_buildings_ok = 0;
	private bool build_navmesh = false;

	/* Voronoi Diagram */
    private List<Vector2> m_points;
	private List<LineSegment> m_edges = null;
	private List<LineSegment> m_spanningTree;
	private List<LineSegment> m_delaunayTriangulation;
	private Texture2D tx;


	/* Convert a value at the map scale to the plane scale */
	private float mapToPlaneScale(float x){
		return x / MAP_SIZE * PLANE_SIZE;
	}

	/* Convert a value at the plane scale to the map scale */
	private float planeToMapScale(float x){
		return x * MAP_SIZE / PLANE_SIZE;
	}

	/* Create a map filled with Perlin Noise */
	private float [,] createMap() 
    {
        float [,] map = new float[MAP_SIZE, MAP_SIZE];
		Vector2 center = new Vector2(MAP_SIZE/2, MAP_SIZE/2);
        for (int i = 0; i < MAP_SIZE; i++){
            for (int j = 0; j < MAP_SIZE; j++){
				
				Vector2 coord = new Vector2(i, j);
				float dist_to_center = (center - coord).magnitude/MAP_SIZE; // [0, 1]

				/* Fill the map with Perlin Noise added with a distance-to-the-center factor:
				The density is higher and the PerlinNoise is less important in the center of the map*/
				map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety) * dist_to_center + (1 - dist_to_center);
				if (map[i, j] > 1){
					map[i, j] = 1;
				}
			}
		}
        return map;
    }

	/* Create a map of pixels to be drawn according to Perlin Noise map */
    private Color[] createPerlinPixelMap(float[,] map)
    {
        Color[] pixels = new Color[MAP_SIZE * MAP_SIZE];
        for (int i = 0; i < MAP_SIZE; i++)
            for (int j = 0; j < MAP_SIZE; j++)
            {
                pixels[i * MAP_SIZE + j] = Color.Lerp(Color.white, Color.black, map[i, j]);
            }
        return pixels;
    }

	/* Create a map of pixels to be drawn */
    private Color[] createPixelMap()
    {
        Color[] pixels = new Color[MAP_SIZE * MAP_SIZE];
        for (int i = 0; i < MAP_SIZE; i++)
            for (int j = 0; j < MAP_SIZE; j++)
            {
                if(i < RIVER_X - RIVER_W/4 || i > RIVER_X + RIVER_W/4){
					pixels[i * MAP_SIZE + j] = Color.grey;
				}
            }
        return pixels;
    }

	/* 
	* Creates a list of coordinates that can appear several times according to their Perlin Noise value from map:
	* For coordinates (i, j), the bigger its value in map, the more times the coordinates are added to the list
	*/
	List<float[]> densityList(float[,] map, int x0, int y0, int r){
		List<float[]> newmap = new List<float[]>();
		for (int i = 0; i < MAP_SIZE; i++){
            for (int j = 0; j < MAP_SIZE; j++){

				// Ignore the points in the downtown, in the circle and in the river
				if (((i < MAP_SIZE/2 - DT_SIZE/2 || i > MAP_SIZE/2 + DT_SIZE/2)
					|| (j < MAP_SIZE/2 - DT_SIZE/2 || j > MAP_SIZE/2 + DT_SIZE/2))
					&& Math.Pow(i - x0, 2) + Math.Pow(j - y0, 2) >= Math.Pow(r * 1.5f, 2)
					&& (i < RIVER_X - 2f * RIVER_W || i > RIVER_X + 2f * RIVER_W)){

					// number of occurences of the coordinates (i, j) according to its value in map
					int nb_occ = (int)(map[i, j] * 100);
					for (int k = 0; k < nb_occ; k++){
						newmap.Add(new float[] {i, j});
					}
				}
			}
		}
		return newmap;
	}

	/* Indicate if the point (x, y) is inside the bounds of the map */
	bool isInRange(int x, int y){
		return (x >= 0 && x < MAP_SIZE && y >= 0 && y < MAP_SIZE);
	}


	/* Select a work place that has not been assigned to a person yet */
	public GameObject selectVacantWorkPlace(){
		for(int i = 0; i < workBuildings.Count; i++){
			if(workBuildings[i].gameObject.GetComponent<Building>().getVacantWorkPlaces() > 0){
				return workBuildings[i].gameObject;
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
				if(work_place != null && persons[n].gameObject != null){
					persons[n].gameObject.GetComponent<Person>().setWorkPlace(work_place);
					persons[n].gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
					persons[n].gameObject.GetComponent<Person>().Init();
                    work_place.gameObject.GetComponent<Building>().addPerson(persons[n]);
					inhabitants.Add(persons[n]);
				}
				else if (work_place == null){
					Debug.Log("No work place found!");
				}
			}
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

	/* Count the number of inhabitants and work places available in the city */
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
		surface.BuildNavMesh();
		Debug.Log("navmesh built");

		Debug.Log("nb house buildings = " + houseBuildings.Count);
		Debug.Log("nb inhabitants = " + nb_inhabitants);
		Debug.Log("nb work buildings = " + workBuildings.Count);
		Debug.Log("nb work places = " + nb_work_places);

		assignWorkPlaces();
	}
		

	void Start()
	{
		m_points = new List<Vector2>();
		List<uint> colors = new List<uint>();

		houseBuildings = new List<GameObject>();
		workBuildings = new List<GameObject>();
		lampposts = new List<GameObject>();
		inhabitants = new List<GameObject>();

		/* Rescale the plane */
		this.transform.localScale *= 2 * COEF;
		PLANE_SIZE *= 2 * COEF;

		/* Create points for the downtown grid */
		for (int i = MAP_SIZE/2 - DT_SIZE/2; i < MAP_SIZE/2 + DT_SIZE/2 + 1; i++){
			for (int j = MAP_SIZE/2 - DT_SIZE/2; j < MAP_SIZE/2 + DT_SIZE/2 + 1; j++){

				if(i % (DT_SIZE / DT_STREETS) == 0 && j % (DT_SIZE / DT_STREETS) == 0){
					m_points.Add(new Vector2((float)i, (float)j));
					colors.Add((uint)0);
				}
			}
		}

		/* Create points for the river */
		for(int k = 0; k < NB_BRIDGES + 1; k++){
			m_points.Add(new Vector2(RIVER_X, k * MAP_SIZE * 1.0f / NB_BRIDGES));
			colors.Add((uint)0);
			m_points.Add(new Vector2(RIVER_X + RIVER_W, k * MAP_SIZE * 1.0f / NB_BRIDGES));
			colors.Add((uint)0);
			m_points.Add(new Vector2(RIVER_X - RIVER_W, k * MAP_SIZE * 1.0f / NB_BRIDGES));
			colors.Add((uint)0);
		}
		drawRiver();

		/* Create points for the circle */
		int r = 50;
		int x0 = (int)UnityEngine.Random.Range(0, MAP_SIZE);
		int y0 = (int)UnityEngine.Random.Range(0, MAP_SIZE);

		while((x0 >= MAP_SIZE/2 - DT_SIZE && x0 <= MAP_SIZE/2 + DT_SIZE
			&& y0 >= MAP_SIZE/2 - DT_SIZE && y0 <= MAP_SIZE/2 + DT_SIZE)
			|| (x0 >= RIVER_X - 2 * RIVER_W - r && x0 <= RIVER_X + 2 * RIVER_W + r)){
				x0 = (int)UnityEngine.Random.Range(0, MAP_SIZE);
				y0 = (int)UnityEngine.Random.Range(0, MAP_SIZE);
		}

		m_points.Add(new Vector2((float)x0, (float)y0));
		colors.Add((uint)0);

		// Add N points on the circle
		int N = 8;
		for(int k = 0; k < N; k++){
			double teta = 2 * Math.PI / N * k;
			int x = (int)(x0 + Math.Cos(teta) * r);
			int y = (int)(y0 + Math.Sin(teta) * r);
			m_points.Add(new Vector2((float)x, (float)y));
			colors.Add((uint)0);
		}

		/* Create the maps */

        float [,] map = createMap();
		List<float[]> density_list = densityList(map, x0, y0, r);
        Color[] pixels = createPerlinPixelMap(map);
		//Color[] map_pixels = createPixelMap();
	
        /* Create random points according to density for non-downtown areas*/
		for (int n = 0; n < NPOINTS; n++) {
			
			colors.Add((uint)0);
			// Randomly select coordinates from density list
			float[] rnd_coord = density_list[(int)UnityEngine.Random.Range(0, density_list.Count)];
			m_points.Add(new Vector2(rnd_coord[0], rnd_coord[1]));
		}

		/* Generate Graphs */
		Delaunay.Voronoi v = new Delaunay.Voronoi (m_points, colors, new Rect (0, 0, MAP_SIZE, MAP_SIZE));
		
		/* Display roads and buildings according to Voronoi Diagram */
		m_edges = v.VoronoiDiagram();
		Color color = Color.blue;
		for (int i = 0; i < m_edges.Count; i++) {

			LineSegment seg = m_edges[i];				
			Vector2 left = (Vector2)seg.p0; // left extremity of the edge
			Vector2 right = (Vector2)seg.p1; // right extremity of the edge
			drawRoad(left, right);
			//DrawLine (pixels, left, right, color);
			
			/* Draw buildings at both sides of the roads */
			Vector2 edge_vector = (right - left)/MAP_SIZE * PLANE_SIZE;
			float angle = Vector2.SignedAngle(Vector2.right, edge_vector); // angle between the edge and the axe z = 0
		
			// Compute the number of buildings that can fit into the lenght of the road
			int nb = (int)(edge_vector.magnitude / (2 * BUILDING_WX)) + 1;

			Vector2 ortho_vector = Vector2.Perpendicular(edge_vector);
			ortho_vector.Normalize();

			// Build nb - 1 buildings alongside the road
			for (int k = 1; k < nb; k++){
				
				float x = left.x + edge_vector.x * MAP_SIZE / (PLANE_SIZE * ROAD_X) * (k * 1.0f * COEF/ nb);
				float y = left.y + edge_vector.y * MAP_SIZE / (PLANE_SIZE * ROAD_X) * (k * 1.0f * COEF/ nb);
			
				// Building on one side of the road
				drawBuilding(x + ortho_vector.x * planeToMapScale(ROAD_Z/2 + BUILDING_WX * 3.0f/4),
							 y + ortho_vector.y * planeToMapScale(ROAD_Z/2 + BUILDING_WX * 3.0f/4),
							 map, angle);

				// Building on the other side of the road
				drawBuilding(x - ortho_vector.x * planeToMapScale(ROAD_Z/2 + BUILDING_WX * 3.0f/4),
							 y - ortho_vector.y * planeToMapScale(ROAD_Z/2 + BUILDING_WX * 3.0f/4),
							 map, angle + 180);
			}

			if(nb > 1){
				int n = 2 * nb;
				// Build n lampposts between the buildings
				for (int k = 1; k < n; k += 2){
					float x = left.x + edge_vector.x * MAP_SIZE / (PLANE_SIZE * ROAD_X) * (k * 1.0f * COEF/ n);
					float y = left.y + edge_vector.y * MAP_SIZE / (PLANE_SIZE * ROAD_X) * (k * 1.0f * COEF/ n);

					drawLamppost(x + ortho_vector.x * planeToMapScale(ROAD_Z/2 + BUILDING_WX/4),
								y + ortho_vector.y * planeToMapScale(ROAD_Z/2 + BUILDING_WX/4),
								angle + 90);
					drawLamppost(x - ortho_vector.x * planeToMapScale(ROAD_Z/2 + BUILDING_WX /4),
								y - ortho_vector.y * planeToMapScale(ROAD_Z/2 + BUILDING_WX/4),
								angle + 270);
				}
			}			
			
		}

		/* Apply pixels to texture */
		/*tx = new Texture2D(MAP_SIZE, MAP_SIZE);
        land.SetTexture ("_MainTex", tx);
		tx.SetPixels (map_pixels);
		tx.Apply();*/
	}

	/* Once all the collision of the buildings have been handled, we can assign the work places and build the nawmesh */
	void Update(){
		if(!build_navmesh && nb_buildings_ok == houseBuildings.Count + workBuildings.Count){
			handleBuildingsAndNavMesh();
			build_navmesh = true;
		}
	}


	/* DRAW FUNCTIONS */

    private void DrawPoint (Color [] pixels, Vector2 p, Color c) {
		if (p.x < MAP_SIZE && p.x >= 0 && p.y < MAP_SIZE && p.y >=0) 
		    pixels[(int)p.x * MAP_SIZE + (int)p.y] = c;
	}
	// Bresenham line algorithm
	private void DrawLine(Color [] pixels, Vector2 p0, Vector2 p1, Color c) {
		int x0 = (int)p0.x;
		int y0 = (int)p0.y;
		int x1 = (int)p1.x;
		int y1 = (int)p1.y;

		int dx = Mathf.Abs(x1-x0);
		int dy = Mathf.Abs(y1-y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx-dy;
		while (true) {
            if (x0 >= 0 && x0 < MAP_SIZE && y0 >= 0 && y0 < MAP_SIZE)
    			pixels[x0 * MAP_SIZE + y0] = c;

			if (x0 == x1 && y0 == y1) break;
			int e2 = 2*err;
			if (e2 > -dy) {
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				err += dx;
				y0 += sy;
			}
		}
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

	private void drawBuilding(float x, float y, float[,] map, float angle){

		if(isInRange((int)x, (int)y) && (x < RIVER_X - RIVER_W/2 || x > RIVER_X + RIVER_W/2)){

			float height = map[(int)x, (int)y]; 

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
			}
		}
	}

	private void drawRiver(){
		GameObject go_river = Instantiate(river, new Vector3(0, 0, mapToPlaneScale(RIVER_X) - PLANE_SIZE/2), Quaternion.identity);
		go_river.gameObject.transform.localScale = new Vector3(PLANE_SIZE, ROAD_Y/2, mapToPlaneScale(RIVER_W * 3f/4));
		go_river.gameObject.transform.SetParent(this.gameObject.transform);
	}

	private void drawLamppost(float x, float y, float angle){
		if(isInRange((int)x, (int)y)
			&& (x < RIVER_X - RIVER_W/2 || x > RIVER_X + RIVER_W/2)){
			GameObject go_lamppost = Instantiate(lamppost, new Vector3(mapToPlaneScale(y) - PLANE_SIZE/2, LAMPPOST_Y/2, mapToPlaneScale(x) - PLANE_SIZE/2), Quaternion.Euler(0, angle + 90, 0));
			go_lamppost.gameObject.transform.localScale = new Vector3(LAMPPOST_X, LAMPPOST_Y, LAMPPOST_Z);
			go_lamppost.gameObject.transform.SetParent(this.gameObject.transform);
			lampposts.Add(go_lamppost);
		}
	}

	/* GETTERS AND SETTERS */

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

	public bool getBuildNavMesh(){
		return build_navmesh;
	}

	public void addHouseBuilding(GameObject hb){
		houseBuildings.Add(hb);
	}

	public void addWorkBuilding(GameObject wb){
		workBuildings.Add(wb);
	}

	public void removeFromHouseBuildings(GameObject hb){
		houseBuildings.Remove(hb);
	}

	public void removeFromWorkBuildings(GameObject wb){
		workBuildings.Remove(wb);
	}

	public void increaseNbBuildingsOk(){
		nb_buildings_ok += 1;
	}

	public void addInhabitants(GameObject i){
		inhabitants.Add(i);
	}
}