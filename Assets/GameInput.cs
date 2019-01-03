﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    public class InputState
    {
        public enum State { NONE, NODE_SELECT, NODE_DRAG, EDGE_DRAG };
        public State state = State.NONE;

        public void SetNodeSelect()
        {
            state = State.NODE_SELECT;
        }

        public void SetNodeDrag()
        {
            state = State.NODE_DRAG;
        }

        public void SetEdgeDrag()
        {
            state = State.EDGE_DRAG;
        }

        public void Clear()
        {
            state = State.NONE;
        }
    }

    public GameObject node_prefab;
    public GameObject edge_prefab;

    GameObject dummy_node;
    GameObject dummy_edge;

    InputState input_state = new InputState();
    GameObject selection;
    GameObject drag;

    void Awake()
    {
        dummy_node = new GameObject();
        dummy_node.AddComponent<Node>();
        dummy_node.SetActive(false);

        dummy_edge = Instantiate(edge_prefab);
        dummy_edge.SetActive(false);
        dummy_edge.GetComponent<Edge>().dest = dummy_node.GetComponent<Node>();
    }

    public GameObject GetSelection()
    {
        return selection;
    }

    public void DispatchEvent(Touch t)
    {
        switch (t.phase)
        {
            case TouchPhase.Began:
                Ray ray = Camera.main.ScreenPointToRay(t.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    selection = hit.transform.gameObject;
                else
                    selection = null;
                //if (selection != null) Debug.Log(string.Format("Got {0}", selection.name));

                drag = selection;

                OnBegin(t, selection);
                break;
            case TouchPhase.Moved:
                if (drag != null)
                    OnDrag(t, drag);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                drag = null;
                OnEnd(t);
                break;
        }
    }

    void OnBegin(Touch t, GameObject obj)
    {
        if (obj != null)
        {
            if (obj.name.Contains("EdgeLink"))
            {
                input_state.SetEdgeDrag();

                Vector3 position = Camera.main.ScreenPointToRay(t.position).origin;
                position.z = 0;

                dummy_edge.GetComponent<Edge>().origin = obj.transform.parent.GetComponent<Node>();
                dummy_node.transform.position = position;
                dummy_node.SetActive(true);
                dummy_edge.SetActive(true);
            }
            else if (isNode(obj)) input_state.SetNodeSelect();
            else input_state.Clear();
        }
        else
            input_state.Clear();
    }

    void OnDrag(Touch t, GameObject obj)
    {
        Vector3 position = Camera.main.ScreenPointToRay(t.position).origin;
        position.z = 0;

        switch (input_state.state)
        {
            case InputState.State.NODE_SELECT:
                input_state.SetNodeDrag();
                //Fall through
                goto case InputState.State.NODE_DRAG;
            case InputState.State.NODE_DRAG:
                obj.transform.position = position;
                break;
            case InputState.State.EDGE_DRAG:
                dummy_node.transform.position = position;
                break;
        }

    }

    void OnEnd(Touch t)
    {
        //Debug.Log(input_state.state);

        switch (input_state.state)
        {
            case InputState.State.NODE_SELECT:
                //Node selected : go to menu
                GetComponent<InputManager>().SetState(InputManager.State.MENU_INPUT);
                break;
            case InputState.State.NODE_DRAG:
                break;
            case InputState.State.EDGE_DRAG:
                dummy_node.SetActive(false);
                dummy_edge.SetActive(false);


                Ray ray = Camera.main.ScreenPointToRay(t.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    GameObject from = dummy_edge.GetComponent<Edge>().origin.gameObject;
                    GameObject to = hit.transform.gameObject;

                    if (from != to && isNode(to))
                    {
                        GameObject edge = Instantiate(edge_prefab, from.transform.position, Quaternion.identity);
                        MakeLink(from, to, edge);
                    }
                }
                else
                {
                    GameObject from = dummy_edge.GetComponent<Edge>().origin.gameObject;
                    GameObject to = Instantiate(node_prefab, dummy_node.transform.position, Quaternion.identity);
                    GameObject edge = Instantiate(edge_prefab, from.transform.position, Quaternion.identity);

                    MakeLink(from, to, edge);
                }

                dummy_edge.GetComponent<Edge>().origin = null;
                break;
        }
    }

    void MakeLink(GameObject from, GameObject to, GameObject edge)
    {
        Edge e = edge.GetComponent<Edge>();
        Node a = from.GetComponent<Node>();
        Node b = to.GetComponent<Node>();

        e.origin = a;
        e.dest = b;
    }

    bool isNode(GameObject o)
    {
        return o.GetComponent<Node>() != null;
    }

    bool isEdge(GameObject o)
    {
        return o.GetComponent<Edge>() != null;
    }
}
