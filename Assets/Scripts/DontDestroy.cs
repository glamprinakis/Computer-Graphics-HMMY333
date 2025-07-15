using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    private static DontDestroy instance = null;

    void Awake()
    {
        if (instance == null) // if no instance exists
        {
            instance = this; // set the instance to the current object
            DontDestroyOnLoad(gameObject); // mark it to not be destroyed
        }
        else
        {
            Destroy(gameObject); // destroy the new object
        }
    }
}