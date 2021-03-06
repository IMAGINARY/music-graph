﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    public class InputState
    {
        public enum State { NONE, NODE_SELECT, NODE_DRAG, EDGE_DRAG, SLIDER_DRAG, EDGE_CUT, DECORATION_DRAG };
        public State state = State.NONE;

        public void SetDecorationDrag()
        {
            state = State.DECORATION_DRAG;
        }

        public void SetSliderDrag()
        {
            state = State.SLIDER_DRAG;
        }

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

        public void SetEdgeCut()
        {
            state = State.EDGE_CUT;
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
    GameObject dummy_decor;

    InputState input_state = new InputState();
    GameObject selection;
    GameObject drag;


    //track move to discard unintentional small moves.
    Vector2 opos;
    float dist = 0;

    // we should track touch to properly handle multi-touch
    bool touch_began = false;
    Touch last;

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
                if (touch_began) { drag = null; OnEnd(last); }
                touch_began = true;

                Ray ray = ScreenToRay(t.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    selection = hit.transform.gameObject;
                else
                    selection = null;
                //if (selection) Debug.Log(string.Format("Got {0}", selection.name));

                drag = selection;

                opos = t.position;
                dist = 0;

                OnBegin(t, selection);
                break;
            case TouchPhase.Moved:
                dist += (t.position - opos).magnitude;
                opos = t.position;
                if (dist > 5)
                {
                    if (drag != null)
                        OnDrag(t, drag);
                    else
                        OnMove(t);
                }
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                touch_began = false;
                drag = null;
                OnEnd(t);
                break;
        }
        last = t;
    }

    void OnBegin(Touch t, GameObject obj)
    {
        if (obj != null)
        {
            if (obj.name.Contains("EdgeLink"))
            {
                input_state.SetEdgeDrag();

                Vector3 position = ScreenToRay(t.position).origin;
                position.z = 0;

                dummy_edge.GetComponent<Edge>().origin = obj.transform.parent.GetComponent<Node>();
                dummy_node.transform.position = position;
                dummy_node.SetActive(true);
                dummy_edge.SetActive(true);
            }
            else if (obj.name.Contains("TimeSlider"))
            {
                input_state.SetSliderDrag();
            }
            else if (obj.name.Contains("Decoration"))
            {
                input_state.SetDecorationDrag();
                Decoration dec = obj.GetComponent<Decoration>();
                dummy_decor = DecorationInstancer.GetInstance().Create(dec.type, dec.id);
                dummy_decor.transform.position = obj.transform.position;
                dummy_decor.layer = 8;

                obj.transform.parent.GetComponent<DecorationPallete>().SetLayer(8);
                EffectController.GetInstance().OnDecorationDrag();
            }
            else if (obj.name.Contains("BPM"))
            {
                obj.GetComponent<BpmController>().Click();
            }
            else if (obj.name.Contains("Play"))
            {
                obj.GetComponent<PlayController>().Click();
            }
            else if (obj.name.Contains("Reload"))
            {
                GameController.GetInstance().Reload();
            }
            else if (obj.name.Equals("Help"))
            {
                obj.GetComponent<HelpController>().Enable();
            }
            else if (obj.name.Contains("HelpPlane"))
            {
                obj.transform.parent.gameObject.GetComponent<HelpController>().Next();
            }
            else if (isNode(obj))
            {
                input_state.SetNodeSelect();
                GetComponent<InputManager>().menu.GetComponent<SliderMenu>().Centralized(obj.GetComponent<Node>().note);
            }
            else input_state.Clear();
        }
        else
            input_state.Clear();
    }

    void OnDrag(Touch t, GameObject obj)
    {
        Vector3 position = ScreenToRay(t.position).origin;
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
                obj.GetComponent<ArrowMove>().MoveTo(position);
                break;
            case InputState.State.SLIDER_DRAG:
                obj.GetComponent<TimeSlider>().Move(t.position);
                break;
            case InputState.State.DECORATION_DRAG:
                dummy_decor.transform.position = position;
                break;
        }

    }

    void OnMove(Touch t)
    {
        //check if our move cuts an edge
        input_state.SetEdgeCut();
        Vector2 p0 = ScreenToRay(t.position).origin;
        Vector2 p1 = ScreenToRay(t.position - t.deltaPosition).origin;

        var edges = GetComponent<GameController>().GetEdges();
        //Must use reverse iteration for removing elements
        for (int i = edges.Count-1; i >= 0; --i)
        {
            var edge = edges[i];
            if (edge.GetComponent<Curve>().Intersect(p0, p1))
            {
                GetComponent<GameController>().RemoveEdge(edge);
            }
        }
    }

    void OnEnd(Touch t)
    {
        //Debug.Log(input_state.state);
        Ray ray = ScreenToRay(t.position);
        RaycastHit hit;

        switch (input_state.state)
        {
            case InputState.State.NODE_SELECT:
                //Node selected : go to menu
                GetComponent<InputManager>().SetState(InputManager.State.MENU_INPUT);
                break;
            case InputState.State.NODE_DRAG:
                //delete node dragged to border
                Vector3 pos = ClampPosition(selection.transform.position);

                bool is_root = (selection == GetComponent<GameController>().GetRootNode());
                bool drag_to_border = (selection.transform.position - pos).sqrMagnitude > 1e-3;
                if (drag_to_border && !is_root)
                {
                    GetComponent<GameController>().RemoveNode(selection);
                    selection = null;
                } else
                {
                    selection.transform.position = pos;
                }
                break;
            case InputState.State.EDGE_DRAG:
                dummy_node.SetActive(false);
                dummy_edge.SetActive(false);

                // avoid raycast hit itself
                selection.GetComponent<ArrowMove>().gameObject.SetActive(false);
                

                if (Physics.Raycast(ray, out hit))
                {
                    GameObject from = dummy_edge.GetComponent<Edge>().origin.gameObject;
                    GameObject to = hit.transform.gameObject;

                    if (from != to && isNode(to))
                    {
                        GetComponent<GameController>().CreateEdge(from, to);
                    }
                }
                else
                {
                    GameObject from = dummy_edge.GetComponent<Edge>().origin.gameObject;
                    GetComponent<GameController>().CreateEdge(from, dummy_node.transform.position);
                }

                selection.GetComponent<ArrowMove>().MoveToOrigin();
                selection.GetComponent<ArrowMove>().gameObject.SetActive(true);
                dummy_edge.GetComponent<Edge>().origin = null;
                break;
            case InputState.State.SLIDER_DRAG:
                break;
            case InputState.State.DECORATION_DRAG:
                selection.transform.parent.GetComponent<DecorationPallete>().SetLayer(0);
                //Needed to avoid raycast on itself
                dummy_decor.SetActive(false);

                if (Physics.Raycast(ray, out hit))
                {
                    GameObject o = hit.transform.gameObject;
                    var socket = o.GetComponent<DecorationSocket>();
                    var decor = dummy_decor.GetComponent<Decoration>();
                    dummy_decor.layer = 0;
                    if (socket != null)
                    {
                        if (socket.Set(decor))
                        {
                            dummy_decor.SetActive(true);
                            //Debug.Log(string.Format("Decoration placed({0})!", decor.id));
                        } else
                        {
                            Destroy(dummy_decor);
                        }
                    }
                    else
                    {
                        Destroy(dummy_decor);
                    }
                } else
                {
                    Destroy(dummy_decor);
                }

                //selection.SetActive(true);
                EffectController.GetInstance().Clear();
                break;
        }
        input_state.Clear();
    }

    bool isNode(GameObject o)
    {
        return o.GetComponent<Node>() != null;
    }

    bool isEdge(GameObject o)
    {
        return o.GetComponent<Edge>() != null;
    }

    //Dont let position go outside viewport
    Vector3 ClampPosition(Vector3 pos)
    {
        float border = 15;
        Vector2 p = Camera.main.WorldToScreenPoint(pos);
        p.x = p.x < border ? border : p.x;
        p.y = p.y < border ? border : p.y;

        float w = Camera.main.pixelWidth;
        float h = Camera.main.pixelHeight;

        p.x = p.x > (w - border) ? (w - border) : p.x;
        p.y = p.y > (h - border) ? (h - border) : p.y;

        Ray ray = Camera.main.ScreenPointToRay(p);

        Vector3 ret = ray.origin;
        ret.z = pos.z;

        return ret;
    }

    public static Ray ScreenToRay(Vector2 pos)
    {
        Ray r = Camera.main.ScreenPointToRay(pos);

        if (r.direction.z < 0)
            r.direction = -r.direction;

        float dz = r.direction.z;
        float oz = r.origin.z;

        float s = -(oz + 100) / dz;
        r.origin = r.origin + s * r.direction;

        return r;
    }
}
