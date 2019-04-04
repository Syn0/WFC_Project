using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class filterTransparency : MonoBehaviour
{
  public GameObject overlap2;
  public GameObject tiles;
  public Material m;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
  
  private void OnTriggerEnter(Collider other)
  {
    var color = new Vector4(255, 255, 255, 0.4f);
    if (other.tag== "Player") {
      //overlap2.transform.position += Vector3.up * 50.0f;
      overlap2.SetActive(false);
      //m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
      /*
      foreach (Transform child in tiles.transform ) {
       // child.gameObject.GetComponent<Renderer>().material.shader.;
        foreach (Material m in child.GetComponent<Renderer>().materials) {
          Debug.Log(m);
         
          m.shader = Shader.Find("_Color");
          m.SetColor("_Color", color);

        }
        //color = Color.cyan; //new Vector4(color.r, color.g, color.b, 0.4f);
      }*/

      Debug.Log("Player Trigger");
    }
  }
  private void OnTriggerExit(Collider other)
  {
    if (other.tag == "Player") {
     // overlap2.transform.position -= Vector3.up * 50.0f;
      overlap2.SetActive(true);
      Debug.Log("Player Trigger Exit");
    }
  }
}
