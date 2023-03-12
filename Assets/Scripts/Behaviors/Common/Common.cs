using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Common : MonoBehaviour
{
    public static Common main;

    public LoadingBar LoadingBar;
    public Storage Storage;

    public void Awake()
    {
        main = this;

        Storage = new Storage("save");
        Storage.Set("Count", Storage.Get("Count", 0) + 1);
        Debug.Log(Storage.Get("Count", 0));
        Storage.Save();

        Application.targetFrameRate = 60;

        CommonScene.LoadAlt("Song Select");
        
    }

    void OnDestroy()
    {
        main = main == this ? null : main;
    }
}
