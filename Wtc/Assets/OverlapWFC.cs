using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
class OverlapWFC : MonoBehaviour{

    //---------prefabs types to delete floating rooms--------
    public GameObject repMur, repMur2, repMur3, repMur4, repMur5, repMur6;

    public Training training = null;
	public int gridsize = 1;
	public int width = 20;
	public int depth = 20;
	public int seed = 0;
	//[HideInInspector]
	public int N = 2;
	public bool periodicInput = false;
	public bool periodicOutput = false;
	public int symmetry = 1;
	public int foundation = 0;
	public int iterations = 0;
	public bool incremental = false;
	public OverlappingModel model = null;
	public GameObject[,,] rendering;
	public GameObject output;
    public Material materialStep;

	private Transform group;
    private bool undrawn = true;

	public static bool IsPrefabRef(UnityEngine.Object o){
		#if UNITY_EDITOR
		return PrefabUtility.GetPrefabParent(o) == null && PrefabUtility.GetPrefabObject(o) != null;
		#endif
		return true;
	}

	static GameObject CreatePrefab(UnityEngine.Object fab, Vector3 pos, Quaternion rot) {
		#if UNITY_EDITOR
		GameObject e = PrefabUtility.InstantiatePrefab(fab as GameObject) as GameObject; 
		e.transform.position = pos;
		e.transform.rotation = rot;
		return e;
		#endif
		GameObject o = GameObject.Instantiate(fab as GameObject) as GameObject; 
		o.transform.position = pos;
		o.transform.rotation = rot;
		return o;
	}

	public void Clear(){
		if (group != null){
			if (Application.isPlaying){Destroy(group.gameObject);} else {
				DestroyImmediate(group.gameObject);
                foreach(Transform child in GameObject.Find("staircases").transform)
                {
                    Destroy(child.gameObject);
                }
            }	
			group = null;
		}
	}

	void Awake(){}

	void Start(){
		Generate(output);
        Generate(GameObject.Find("output-overlap2"));
    }

	void Update(){
		if (incremental){
			Run();
		}
	}

	public void Generate(GameObject outputObj) {
		if (training == null){Debug.Log("Can't Generate: no designated Training component");}
		if (IsPrefabRef(training.gameObject)){
			GameObject o = CreatePrefab(training.gameObject, new Vector3(0,99999f,0f), Quaternion.identity);
			training = o.GetComponent<Training>();
		}
		if (training.sample == null){
			training.Compile();
		}
		if (outputObj == null){
			Transform ot = transform.Find("output-overlap");
			if (ot != null){outputObj = ot.gameObject;}}
		if (outputObj == null){
			outputObj = new GameObject("output-overlap");
			outputObj.transform.parent = transform;
			outputObj.transform.position = this.gameObject.transform.position;
			outputObj.transform.rotation = this.gameObject.transform.rotation;}
		for (int i = 0; i < outputObj.transform.childCount; i++){
			GameObject go = outputObj.transform.GetChild(i).gameObject;
			if (Application.isPlaying){Destroy(go);} else {DestroyImmediate(go);}
		}
		group = new GameObject(training.gameObject.name).transform;
		group.parent = outputObj.transform;
		group.position = outputObj.transform.position;
		group.rotation = outputObj.transform.rotation;
        group.localScale = new Vector3(1f, 1f, 1f);
        rendering = new GameObject[width, depth, 3];
		model = new OverlappingModel(training.sample, N, width, depth, periodicInput, periodicOutput, symmetry, foundation);
        undrawn = true;

        if (model.Run(seed, iterations))
        {
            if (outputObj.name == "output-overlap") Draw();
            else if (outputObj.name == "output-overlap2") Draw(true);
        }
    }

	void OnDrawGizmos(){
		Gizmos.color = Color.cyan;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(new Vector3(width*gridsize/2f-gridsize*0.5f, depth*gridsize/2f-gridsize*0.5f, 0f),
							new Vector3(width*gridsize, depth*gridsize, gridsize));
	}

	public void Run(){
		if (model == null){return;}
        if (undrawn == false) { return; }
        if (model.Run(seed, iterations)){
			Draw();
		}
	}

	public GameObject GetTile(int x, int y, int z)
    {
		return rendering[x,y,z];
	}

	public void Draw(bool staircase = false){
		if (output == null){return;}
		if (group == null){return;}
        undrawn = false;
		try
        {
            for (int z = 0; z < 1; z++)
            {
                for (int y = 0; y < depth; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (rendering[x, y, z] == null)
                        {
                            int v = (int)model.Sample(x, y);
                            if (v != 99 && v < training.tiles.Length)
                            {
                                Vector3 pos = new Vector3(x * gridsize, y * gridsize, z * gridsize);
                                int rot = (int)training.RS[v];
                                GameObject fab = training.tiles[v] as GameObject;
                                if (fab != null)
                                {
                                    GameObject tile = (GameObject)Instantiate(fab, new Vector3(), Quaternion.identity);
                                    Vector3 fscale = tile.transform.localScale;
                                    tile.transform.parent = group;
                                    tile.transform.localPosition = pos;
                                    tile.transform.localEulerAngles = new Vector3(0, 0, 360 - (rot * 90)); //----mark rotation !----
                                    tile.transform.localScale = fscale;
                                    rendering[x, y, z] = tile;

                                    if (staircase && x % 3 == 0 && y % 5 == 0) DrawStaircase(tile);
                                }
                            }
                            else
                            {
                                undrawn = true;
                            }
                        }
                    }
                }
            }

        } catch (IndexOutOfRangeException e) {
            Debug.Log(e);
	  	    model = null;
	  	    return;
	    }

        //---------Sorting and delete plateforms----------
        for (int z = 0; z < 1; z++)
        {
            for (int y = 0; y < depth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (rendering[x, y, z] != null)
                    {

                        //Debug.Log(rendering[x, y, z].name.Replace("(Clone)", "") + " | " + repMur.name);
                        //------
                        if (rendering[x, y, z].name.Replace("(Clone)", "") == repMur.name)
                        {
                            //--the following deletes the 'flying room' if it is one--

                            bool flying = true; //--------------- does the room have a path to it? we suppose it has not and whenever a path is found,
                            int v = (int)model.Sample(x, y);//--- switch flying to false, we then delete the room is flying hasn't been changed by the algorythm
                            Vector3 pos = new Vector3(x * gridsize, y * gridsize, z * gridsize);
                            int rot = (int)training.RS[v];
                            pos = rendering[x, y, 0].transform.position;
                            //pos.x += 1;
                            GameObject currentObj = null; //-> = renderingxyz transform.gameobject
                            while (currentObj == null)
                            {
                                Collider[] hitColliders = Physics.OverlapSphere(rendering[x, y, 0].transform.position, 1f);

                                if (hitColliders.Length != 0)
                                {
                                    for (int i = 0; i < hitColliders.Length; i++)
                                    {
                                        if (hitColliders[i].gameObject.name != null)
                                        {
                                            currentObj = hitColliders[i].gameObject;
                                            if (currentObj.name.Replace("(Clone)", "") == repMur5.name || currentObj.name.Replace("(Clone)", "") == repMur6.name)
                                            {
                                                flying = false;
                                                //Debug.Log("+1 not flying house");
                                            }

                                        }
                                    }
                                }
                                //-----faire une chenille qui est "periodic output frendly"------
                            }
                        }

                        if (rendering[x, y, z].name == "cube3(Clone)" || rendering[x, y, z].name == "cube2(Clone)")
                        {
                            Collider[] hitColliders = Physics.OverlapSphere(rendering[x, y, z].transform.position, 1.0f);
                            if (hitColliders.Length != 0)
                            {
                                for (int i = 0; i < hitColliders.Length; i++)
                                {
                                    if (hitColliders[i].name == "cube1(Clone)") { 
                                        hitColliders[i].name = "porte";
                                    }
                                }
                            }
                        }
                    }
                }
            }            
        }
    }

    void DrawStaircase(GameObject tile)
    {
        if (tile.name == "cube1(Clone)")
        {
            for (int i = 2; i >= 0; i--)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube); ;
                step.GetComponent<MeshRenderer>().material = materialStep;
                step.transform.parent = GameObject.Find("staircases").transform;
                step.name = "my step - " + i;
                step.transform.position = tile.transform.position;
                step.transform.position += new Vector3(-1, i * 1, i * 1);

                if (i == 2)
                {
                    Collider[] hitColliders = Physics.OverlapSphere((step.transform.position + new Vector3(-1, i * 1, i * 1)), 1f);
                    if (hitColliders.Length > 1)
                    {
                        DestroyImmediate(step);
                        i = -1;
                    }
                }
            }
        }
    }

    /*void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.DrawSphere((step.transform.position + new Vector3(-1, i * 1, (i + 1) * 1)), 0.5f);
    }*/
}

 #if UNITY_EDITOR
[CustomEditor (typeof(OverlapWFC))]
public class WFCGeneratorEditor : Editor {
	public override void OnInspectorGUI () {
		OverlapWFC generator = (OverlapWFC)target;
		if (generator.training != null){
			if(GUILayout.Button("generate")){
                /*foreach (Transform child in GameObject.Find("staircases").transform)
                {
                    DestroyImmediate(child.gameObject);
                }*/
                //generator.Clear();
                DestroyImmediate(GameObject.Find("staircases"));
                GameObject staircases = new GameObject();
                staircases.name = "staircases";
                staircases.transform.position.y = -2;
                generator.Generate(generator.output);
                generator.Generate(GameObject.Find("output-overlap2"));
			}
			if (generator.model != null){
				if(GUILayout.Button("RUN")){
					generator.Run();
				}
			}
		}
		DrawDefaultInspector ();
	}
}
#endif