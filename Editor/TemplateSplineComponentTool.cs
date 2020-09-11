using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.EditorTools;


enum SplineToolStatus
{
    Init,
    AddControlPoints,
    EditControlPoint,
    InsertControlPoint,
    BreakControlPoint
}


[EditorTool("Template Spline Tool")]
public class TemplateSplineComponentTool : EditorTool
{
    SplineToolStatus state;

    [SerializeField]
    public Texture2D m_ToolIcon;

    string ToolName = "SplineTool";

    GUIContent m_IconContent;

    GameObject targetTool;

    LineRenderer lineRendererComponent;
    TemplateSplineComponent splineComponent;


    string MakeUniqueGameObjectName(string inString)
    {
        string outString = inString;

        int counter = 0;

        while (GameObject.Find(outString) != null)
        {
            outString = inString + counter.ToString();
            counter++;
        }

        return outString;
    }


    void OnActiveToolChanged()
    {
        if (EditorTools.IsActiveTool(this))
        {
            m_IconContent = new GUIContent()
            {
                image = m_ToolIcon,
                text = "Spline Tool",
                tooltip = "Spline Tool"
            };

            if (SceneView.lastActiveSceneView.drawGizmos == false)
                SceneView.lastActiveSceneView.drawGizmos = true;

            //targetTool = GameObject.Find(ToolName);

            bool foundExistingSpline = false;

            
            if (Selection.activeGameObject != null)
            {
                TemplateSplineComponent foundComp = Selection.activeGameObject.GetComponent<TemplateSplineComponent>();

                if (foundComp == null)
                {
                    foundComp = Selection.activeGameObject.GetComponentInParent<TemplateSplineComponent>();

                    if (foundComp != null)
                    {
                        foundExistingSpline = true;
                        targetTool = foundComp.gameObject;
                    }
                }
                else
                {
                    foundExistingSpline = true;
                    targetTool = foundComp.gameObject;
                }
            }
            

            if (foundExistingSpline == false)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Click positions for the Spline points"), 2f);
                state = SplineToolStatus.Init;

            }
            else
            {
                //Selection.activeGameObject = targetTool;
                state = SplineToolStatus.AddControlPoints;

                if (targetTool.GetComponent<TemplateSplineComponent>() != null)
                    splineComponent = targetTool.GetComponent<TemplateSplineComponent>();
            }

            

            //SceneView.lastActiveSceneView.FrameSelected();
        }

    }

    void OnCheckCurrentTool()
    {

    }



    private void OnEnable()
    {
        EditorTools.activeToolChanged += OnActiveToolChanged;
        Selection.selectionChanged += OnCheckCurrentTool;
    }

    private void OnDisable()
    {
        EditorTools.activeToolChanged -= OnActiveToolChanged;
        Selection.selectionChanged -= OnCheckCurrentTool;
    }

    public override GUIContent toolbarIcon
    {
        get { return m_IconContent; }
    }


    GameObject SpawnControlPointAt(Vector3 position)
    {
        //GameObject newGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject newGo = new GameObject();

        newGo.transform.position = position;
        newGo.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        //newGo.GetComponent<Renderer>().sharedMaterial = splineComponent.controllerMaterial;
        

        return newGo;
    }

    

    public override void OnToolGUI(EditorWindow window)
    {
        /// Temp
        //state = SplineToolStatus.InsertControlPoint;


        if (Event.current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(1);

        
        RaycastHit cHit;

        Ray cRay = SceneView.lastActiveSceneView.camera.ScreenPointToRay(HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition));

        Vector3 contactPoint = new Vector3(0f, 0f, 0f);



        
        if (state == SplineToolStatus.Init)
        {
            if (Event.current.type == EventType.MouseUp)
            {
                if (Physics.Raycast(cRay, out cHit, Mathf.Infinity))
                {
                    contactPoint = cHit.point;
                    

                    GameObject newGo = new GameObject(MakeUniqueGameObjectName(ToolName));
                    newGo.transform.SetPositionAndRotation(contactPoint, Quaternion.identity);

                    targetTool = newGo;

                    //lineRendererComponent = newGo.AddComponent<LineRenderer>();
                    splineComponent = newGo.AddComponent<TemplateSplineComponent>();

                    GameObject centerGo = SpawnControlPointAt(contactPoint);
                    centerGo.gameObject.name = "SplinePoint";
                    centerGo.transform.up = cHit.normal;
                    centerGo.transform.SetParent(targetTool.transform);

                    

                    // Set Default control points

                    GameObject controlPoint1 = SpawnControlPointAt(contactPoint);
                    GameObject controlPoint2 = SpawnControlPointAt(contactPoint);

                    controlPoint1.transform.SetParent(centerGo.transform);
                    controlPoint2.transform.SetParent(centerGo.transform);

                    splineComponent.controlPoints.Add(controlPoint1);
                    splineComponent.controlPoints.Add(controlPoint2);
                    //splineComponent.controlPoints.Add(SpawnControlPointAt(contactPoint));
                    //splineComponent.controlPoints.Add(SpawnControlPointAt(contactPoint));

                    centerGo.AddComponent<TemplateSplinePoint>();

                    state = SplineToolStatus.AddControlPoints;
                    Event.current.Use();
                }

            }
        }
        
        if (state == SplineToolStatus.AddControlPoints)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {

                if (Event.current.modifiers == EventModifiers.Control)
                {
                    if (Physics.Raycast(cRay, out cHit, Mathf.Infinity))
                    {
                        contactPoint = cHit.point;

                        //Vector3 offset = (controlPoints[controlPoints.Count - 1].transform.position - controlPoints[controlPoints.Count - 2].transform.position) / 2f;

                        Vector3 previousCenter = (splineComponent.controlPoints[splineComponent.controlPoints.Count - 1].transform.position + splineComponent.controlPoints[splineComponent.controlPoints.Count - 2].transform.position) / 2f;

                        //Transform centerPoint = cHit.transform;
                        //centerPoint.LookAt(previousCenter);

                        GameObject centerGo = SpawnControlPointAt(contactPoint);

                        centerGo.gameObject.name = "SplinePoint";

                        centerGo.transform.up = cHit.normal;

                        //centerGo.transform.forward = previousCenter.normalized;
                        centerGo.transform.LookAt(previousCenter);


                        centerGo.transform.localScale = centerGo.transform.localScale * 2f;

                        centerGo.transform.SetParent(targetTool.transform);

                        GameObject controlPoint1 = SpawnControlPointAt(contactPoint + centerGo.transform.right);
                        GameObject controlPoint2 = SpawnControlPointAt(contactPoint - centerGo.transform.right);

                        //DestroyImmediate(centerGo);

                        controlPoint1.transform.up = cHit.normal;
                        controlPoint2.transform.up = cHit.normal;

                        controlPoint1.transform.localRotation = centerGo.transform.localRotation;
                        controlPoint2.transform.localRotation = centerGo.transform.localRotation;

                        //controlPoint1.transform.SetParent(targetTool.transform);
                        //controlPoint2.transform.SetParent(targetTool.transform);

                        controlPoint1.transform.SetParent(centerGo.transform);
                        controlPoint2.transform.SetParent(centerGo.transform);

                        splineComponent.controlPoints.Add(controlPoint1);
                        splineComponent.controlPoints.Add(controlPoint2);

                        centerGo.AddComponent<TemplateSplinePoint>();

                        MaterialPropertyBlock matBlock = new MaterialPropertyBlock();
                        matBlock.SetColor("_BaseColor", Color.magenta);


                        //centerGo.GetComponent<Renderer>().SetPropertyBlock(matBlock);

                        //centerGo.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.magenta);
                        

                        Event.current.Use();
                    }
                }
            }
        }



        if (state == SplineToolStatus.InsertControlPoint)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (Physics.Raycast(cRay, out cHit, Mathf.Infinity))
                {
                    contactPoint = cHit.point;

                    Debug.Log(contactPoint);

                    GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.SetPositionAndRotation(contactPoint, Quaternion.identity);

                }

                Debug.Log("Origin : " + cRay.origin);
                Debug.Log("Direction : " + cRay.direction);
                

                //Debug.Log(Vector3.Cross(new Vector3(-1, 0, 0), new Vector3(3, 0, 0)).normalized);
            }

            if (state == SplineToolStatus.BreakControlPoint)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                }
            }


            // Draw Controller points
            /*
            if (Event.current.type == EventType.Repaint)
            {
                if (splineComponent.controlPoints != null)
                {
                    for (int i = 0; i < splineComponent.controlPoints.Count; i++)
                    {
                        Handles.SphereHandleCap(0, splineComponent.controlPoints[i].transform.position, Quaternion.identity, 1f, EventType.Repaint);
                    }
                }
            }
            */
        }

    }


    public Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
    {
        lineDir.Normalize();//this needs to be a unit vector
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
    }


}