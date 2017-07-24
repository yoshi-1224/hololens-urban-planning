using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
public class Toolbar :Singleton<Toolbar> {
    
    // hack to disable the gameObject after all the Instance initialization has taken place
	void Start () {
        //gameObject.SetActive(false);
    }
	
    public void hide() {
        gameObject.SetActive(false);
    }
}
