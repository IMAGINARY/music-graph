﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

[RequireComponent(typeof(ChuckSubInstance))]
public class GameController : MonoBehaviour
{
    public GameObject node_prefab;
    public GameObject edge_prefab;


    string[] scale = new string[] { "A3", "B4", "C4", "D4", "E4", "F4", "G4" };
    int[] midis = new int[] { 69, 71, 72, 74, 76, 77, 79 };
    Graph graph;
    Scheduler scheduler;

    Node current;


    private void Awake()
    {
        graph = new Graph();
        scheduler = new Scheduler(GetComponent<ChuckSubInstance>());
        current = CreateNode(new Vector3(-2, 2, 0)).GetComponent<Node>();
    }

    // Start is called before the first frame update
    void Start()
    {
        scheduler.Start(() => {
            var edges = graph.GetEdges(current.id);
            Node ret;
            if (edges.Count == 0)
                ret = graph.GetRoot();
            else
            {
                var en = edges.GetEnumerator();
                en.MoveNext();
                ret = graph.GetNode(en.Current);
            }
            current = ret;
            
            return ret;
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject CreateNode(Vector3 position)
    {
        GameObject o = Instantiate(node_prefab, position, Quaternion.identity);
        graph.AddNode(o.GetComponent<Node>());

        return o;
    }

    public GameObject CreateEdge(GameObject from, GameObject to)
    {
        GameObject edge = Instantiate(edge_prefab, Vector3.zero, Quaternion.identity);

        Edge e = edge.GetComponent<Edge>();
        Node a = from.GetComponent<Node>();
        Node b = to.GetComponent<Node>();

        e.origin = a;
        e.dest = b;

        graph.AddEdge(e);

        return edge;
    }

    public GameObject CreateEdge(GameObject from, Vector3 position)
    {
        GameObject to = CreateNode(position);
        return CreateEdge(from, to);
    }
}


public class Graph
{
    Node root;
    Dictionary<int, Node> nodes;
    Dictionary<int, SortedSet<int>> edges;
    Dictionary<int, SortedSet<int>> reverse_edges;

    public Graph()
    {
        Clear();
    }

    public void Clear()
    {
        root = null;
        nodes = new Dictionary<int, Node>();
        edges = new Dictionary<int, SortedSet<int>>();
        reverse_edges = new Dictionary<int, SortedSet<int>>();
    }

    public Node GetRoot() { return root; }
    public SortedSet<int> GetEdges(int id) { return edges[id]; }
    public Node GetNode(int id)
    {
        if (nodes.ContainsKey(id))
            return nodes[id];

        Debug.Log(String.Format("Graph does not contains node {0}", id));
        return root;
    }

    public void AddNode(Node n)
    {
        nodes[n.id] = n;
        edges[n.id] = new SortedSet<int>();
        reverse_edges[n.id] = new SortedSet<int>();

        if (nodes.Count == 1)
            root = n;
    }

    public void RemoveNode(Node n)
    {
        if (n == root) return;

        nodes.Remove(n.id);
        foreach (var id in edges[n.id])
            reverse_edges[id].Remove(n.id);

        edges.Remove(n.id);
    }

    public void AddEdge(Edge e)
    {
        Node origin = e.origin;
        Node dest = e.dest;

        if (!nodes.ContainsKey(origin.id) || !nodes.ContainsKey(dest.id))
        {
            Debug.Log("Bad graph link!");
            return;
        }

        edges[origin.id].Add(dest.id);
        reverse_edges[dest.id].Add(origin.id); 
    }

    public void RemoveEdge(Edge e)
    {
        Node origin = e.origin;
        Node dest = e.dest;

        if (!nodes.ContainsKey(origin.id) || !nodes.ContainsKey(dest.id))
        {
            Debug.Log("Edge nodes does not exists!");
            return;
        }

        edges[origin.id].Remove(dest.id);
        reverse_edges[dest.id].Remove(origin.id);
    }
}




public class Scheduler
{
    int bpm = 120;
    int measure = 4;
    int subdiv = 2;

    int ticks = 0;
    int next = 0;

    ChuckSubInstance instance;
    Chuck.VoidCallback callback;

    //hack to use duplicate keys
    //.Remove method will not work! 
    public class DupComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);
            return result == 0 ? 1 : result;
         
        }
        #endregion
    }
    // <tick, midi> queue
    SortedList<int, int> queue = new SortedList<int, int>(new DupComparer<int>());

    public delegate Node NextAction();
    NextAction GetNext;

    int[] midis = new int[] { 69, 71, 72, 74, 76, 77, 79 };

    public float dt
    {
        get { return 1.0f / (bpm * measure * subdiv); }
    }


    public Scheduler(ChuckSubInstance inst)
    {
        instance = inst;
        if (!instance.RunFile("runner.ck"))
            Debug.Log("Could not load runner.ck!");

        callback = instance.CreateVoidCallback(Tick);
        instance.StartListeningForChuckEvent("notifier", callback);

        instance.SetFloat("dt", dt);
    }

    ~Scheduler()
    {
        instance.StopListeningForChuckEvent("notifier", callback);
    }

    public void Start(NextAction fn)
    {
        GetNext = fn;
        instance.BroadcastEvent("start");
    }

    public void Enqueue()
    {
        if (next == ticks)
        {
            Node n = GetNext();
            //Debug.Log(n == null ? "null node" : "good node");
            int note = midis[n.note];
            //Debug.Log(note);
            queue.Add(ticks, note);
            //queue.Add(ticks+2, note);
            //queue.Add(ticks+4, note);
            //queue.Add(ticks+6, note);
            //queue.Add(ticks + 4, note + 7);

            next += (int)(subdiv * n.duration);
        }
    }

    void Play(int[] notes)
    {
        var data = Array.ConvertAll(notes, val => (long)val);
        instance.SetIntArray("midi", data);
        instance.SetInt("play", 1);
    }

    void Tick()
    {
        Enqueue();

        List<int> notes = new List<int>();
        int cnt = 0;
        foreach (var i in queue)
        {
            if (i.Key > ticks) break;

            notes.Add(i.Value);
            ++cnt;
        }

        while (cnt-- > 0)
            queue.RemoveAt(0);

        Play(notes.ToArray());
        ++ticks;
    }
}