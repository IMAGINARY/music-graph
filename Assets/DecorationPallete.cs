﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorationPallete : MonoBehaviour
{
    public GameObject decoration_prefab;

    // Create decoration pallete in hexagon form
    void CreateHexagon(float dx)
    {
        float dy = 1.5f * Mathf.Sqrt(3) * dx / 2;
        Vector3 pos = Vector3.zero;


        for (int i = 0; i < 10; ++i)
        {
            GameObject d = DecorationInstancer.GetInstance().Create(Decoration.DecorationType.NODE_STYLE, i);

            d.transform.parent = gameObject.transform;
            d.transform.localPosition = new Vector3(((i & 1) == 0) ? pos.y : (pos.y + dy), pos.x, pos.z - 0.2f);
            d.layer = gameObject.layer;
            pos.x += dx * 0.8f;
        }
    }

    public void SetLayer(int layer)
    {
        gameObject.layer = layer;
        foreach (Transform t in gameObject.GetComponentInChildren<Transform>(true))
        {
            t.gameObject.layer = layer;
        }
    }

    void Start()
    {
        //Create();
        CreateHexagon(0.6f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
