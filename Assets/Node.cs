﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Node : MonoBehaviour
{
    static int next_id = 0;
    public int id;
    public int note = 0;
    public float duration = 1;

    void Awake()
    {
        id = next_id++;
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}