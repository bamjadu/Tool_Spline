using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;


enum MenuContext
{
    SplineMenu,
    PointMenu
}


[CanEditMultipleObjects]
[CustomEditor(typeof(TemplateSplinePoint))]
public class TemplateSplinePointEditor : Editor
{

    MenuContext currentMenuContext;
    bool isUIvisible = false;
    bool isSnapUIvisible = false;

    Vector2 lastMousePosition = Vector2.zero;

    int targetPointIndex;
    List<GameObject> selectedGoToBeSnapped = new List<GameObject>();

    private void OnEnable()
    {
        currentMenuContext = MenuContext.SplineMenu;
        isUIvisible = false;
        isSnapUIvisible = false;
        lastMousePosition = Vector2.zero;

        selectedGoToBeSnapped.Clear();
        selectedGoToBeSnapped = new List<GameObject>();
    }

    private void OnSceneGUI()
    {
        

        TemplateSplinePoint pointComp = (TemplateSplinePoint)target;
        
        EditorGUI.BeginChangeCheck();

        EditorGUI.BeginChangeCheck();
        float s = Handles.ScaleSlider(pointComp.scale, pointComp.transform.position, pointComp.transform.right, pointComp.transform.rotation, 1f, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            pointComp.scale = s;
        }

        EditorGUI.BeginChangeCheck();
        float s1 = Handles.ScaleSlider(pointComp.scale, pointComp.transform.position, -pointComp.transform.right, pointComp.transform.rotation, 1f, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            pointComp.scale = s1;
        }

        if (EditorGUI.EndChangeCheck())
        {
            //pointComp.scale = s;
            pointComp.UpdateControlPointPosition();
        }


        
        
        int controlID = GUIUtility.GetControlID(FocusType.Passive);


        switch(Event.current.GetTypeForControl(controlID))
        {
            
            case EventType.MouseDown :

                if (Event.current.button == 1)
                {
                    /*
                    switch(currentMenuContext)
                    {
                        case MenuContext.SplineMenu:
                            
                            CloseSplineMenu();
                            break;
                        case MenuContext.PointMenu:
                            
                            CloseSnapPointMenu();
                            break;
                    }
                    */

                    GUIUtility.hotControl = controlID;

                    Vector2 pos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

                    lastMousePosition.x = pos.x - SceneView.currentDrawingSceneView.position.x;
                    lastMousePosition.y = pos.y - SceneView.currentDrawingSceneView.position.y;

                    
                    Event.current.Use();
                }
                break;
            
            case EventType.MouseUp:

                if (Event.current.button == 1)
                {
                    GUIUtility.hotControl = 0;


                    switch(currentMenuContext)
                    {
                        case MenuContext.SplineMenu:
                            isUIvisible = !isUIvisible;
                            break;
                        case MenuContext.PointMenu:
                            selectedGoToBeSnapped.Clear();

                            

                            isSnapUIvisible = !isSnapUIvisible;
                            break;
                    }

                    Event.current.Use();
                }
                break;
        }



        Handles.BeginGUI();
        if (isUIvisible)
        {

            


            GUILayout.Window(8282, new Rect(lastMousePosition, new Vector2(230, 180)), FuncSplinePointMenu, "Spline Point Menu");
            
            
            
        }
        

        if (isSnapUIvisible)
        {
            //Handles.BeginGUI();
            GUILayout.Window(8283, new Rect(lastMousePosition, new Vector2(200, 140)), FuncSnpaPointMenu, "Snap Point Menu");
            //Handles.EndGUI();
        }
        Handles.EndGUI();



    }

    void Unlinks()
    {
        int count = 0;

        foreach(Object obj in targets)
        {
            if (obj.GetType() == typeof(TemplateSplinePoint))
            {
                ((TemplateSplinePoint)obj).linkedTransform = null;
                count++;
            }
        }

        CloseSplineMenu();

        SceneView.lastActiveSceneView.ShowNotification(new GUIContent(string.Format("{0} Points are just unlinked.", count)));
    }

    void CloseSplineMenu()
    {
        isUIvisible = false;
    }

    void SnapPoints()
    {
        currentMenuContext = MenuContext.PointMenu;
        isSnapUIvisible = true;
    }

    void CloseSnapPointMenu()
    {
        currentMenuContext = MenuContext.SplineMenu;
        isSnapUIvisible = false;
        selectedGoToBeSnapped.Clear();
    }

    void OnGotoParentSpline()
    {
        TemplateSplineComponent splineComp = (target as TemplateSplinePoint).GetComponentInParent<TemplateSplineComponent>();

        if (splineComp != null)
        {
            //Debug.Log("Selected : " + splineComp.gameObject);
            Selection.activeGameObject = splineComp.gameObject;
        }

    }

    void OnGotoParentSplineGroup()
    {
        TemplateSplineComponent splineComp = (target as TemplateSplinePoint).GetComponentInParent<TemplateSplineComponent>();

        if (splineComp != null)
        {
            TemplateSplineComponentGroup grpComp = splineComp.GetComponentInParent<TemplateSplineComponentGroup>();

            if (grpComp != null)
            {
                Selection.activeGameObject = grpComp.gameObject;
            }
            else
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Could not find any SplineGroup"));
            }

        }
    }

    void OnGotoPreviousPoint()
    {
        int currentSiblingIndex = (target as TemplateSplinePoint).transform.GetSiblingIndex();

        Transform targetTransform = null;

        if (currentSiblingIndex != 0)
        {
            targetTransform = (target as TemplateSplinePoint).transform.parent.GetChild(currentSiblingIndex-1);
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(string.Format("Select the previous spline point")));
        }
        else
        {
            targetTransform = (target as TemplateSplinePoint).transform;
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(string.Format("Selection is the initial spline point.")));
        }

        Selection.activeGameObject = targetTransform.gameObject;

    }

    void OnGotoNextPoint()
    {
        int currentSiblingIndex = (target as TemplateSplinePoint).transform.GetSiblingIndex();

        Transform targetTransform = null;

        if (currentSiblingIndex != (target as TemplateSplinePoint).transform.parent.GetComponentsInChildren<TemplateSplinePoint>().Length-1)
        {
            targetTransform = (target as TemplateSplinePoint).transform.parent.GetChild(currentSiblingIndex + 1);
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(string.Format("Select the next spline point")));
        }
        else
        {
            targetTransform = (target as TemplateSplinePoint).transform;
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(string.Format("Selection is the last spline point.")));
        }

        Selection.activeGameObject = targetTransform.gameObject;
    }


    void FuncSplinePointMenu(int windowID)
    {
        GUILayout.BeginVertical();

        EditorGUILayout.Separator();

        if (!IsMultiSelected())
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Go to");

            if (GUILayout.Button("▲▲ Spline Group ▲▲"))
            {
                OnGotoParentSplineGroup();
                CloseSplineMenu();
            }

            if (GUILayout.Button("▲ Spline ▲"))
            {
                OnGotoParentSpline();
                CloseSplineMenu();
            }


            GUILayout.BeginHorizontal();

            if (GUILayout.Button("◀ Previous Point", GUILayout.Width(110)))
            {
                OnGotoPreviousPoint();
                CloseSplineMenu();
            }
            if (GUILayout.Button("Next Point ▶", GUILayout.Width(110)))
            {
                OnGotoNextPoint();
                CloseSplineMenu();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        if (GUILayout.Button("Unlink"))
        {
            Unlinks();
        }

        if (IsMultiSelected())
        {
            if (GUILayout.Button("Snap Points"))
            {
                CloseSplineMenu();
                SnapPoints();
            }
        }

        EditorGUILayout.Separator();

        if (GUILayout.Button("Close"))
        {
            CloseSplineMenu();
        }

        GUILayout.EndVertical();
    }

    void FuncSnpaPointMenu(int windowID)
    {
        selectedGoToBeSnapped.Clear();

        foreach (GameObject go in Selection.gameObjects)
            selectedGoToBeSnapped.Add(go);

        if (selectedGoToBeSnapped.Count > 0)
        {

            EditorGUILayout.Separator();

            

            EditorGUI.BeginChangeCheck();
            string[] currentSelections = new string[selectedGoToBeSnapped.Count];

            


            for (int i=0;i< selectedGoToBeSnapped.Count;i++)
            {
                currentSelections[i] = (selectedGoToBeSnapped[i].name) + " : " + selectedGoToBeSnapped[i].GetInstanceID();
            }
            if (EditorGUI.EndChangeCheck())
            {
                OnDrawArrowsForTargeting();
                
            }
            OnDrawArrowsForTargeting();


            GUILayout.Label("Snap To");
            targetPointIndex = EditorGUILayout.Popup(targetPointIndex, currentSelections);

            if (GUILayout.Button("Ok"))
            {
                SnapSelectedPoints(targetPointIndex);
            }

            if (GUILayout.Button("Cancel"))
            {
                CloseSnapPointMenu();
            }
        }
    }

    void SnapSelectedPoints(int targetIndex)
    {
        Vector3 targetPos = selectedGoToBeSnapped[targetIndex].transform.position;

        foreach (GameObject go in selectedGoToBeSnapped)
        {
            if (go != selectedGoToBeSnapped[targetIndex])
            {
                //TemplateSplinePoint curPoint = selectedGoToBeSnapped[targetIndex].GetComponent<TemplateSplinePoint>();
                TemplateSplinePoint curPoint = go.GetComponent<TemplateSplinePoint>();

                if (curPoint == null)
                    go.transform.position = targetPos;
                else
                {
                    if (curPoint.linkedTransform == null)
                        go.transform.position = targetPos;
                    else
                    {
                        Debug.Log("Linked point are moved!!");
                        curPoint.linkedTransform.gameObject.transform.position = targetPos; 
                    }
                }
            }
        }

        SceneView.lastActiveSceneView.ShowNotification(new GUIContent(string.Format("{0} Points are snapped.", selectedGoToBeSnapped.Count-1)));

        CloseSnapPointMenu();
    }


    void OnDrawArrowsForTargeting()
    {
        Vector3 targetPos = Selection.gameObjects[targetPointIndex].transform.position;

        foreach (GameObject go in Selection.gameObjects)
        {
            if (targetPos == go.transform.position)
                continue;

            DrawArrow.ForDebug(go.transform.position, (targetPos - go.transform.position) * 0.85f, 0.01f, Color.cyan, ArrowType.Solid);
            

        }
    }


    bool IsMultiSelected()
    {
        if (Selection.objects.Length >= 2)
        {
            
            /*
            for (int i=0;i< Selection.objects.Length;i++)
            {
                if (((GameObject)Selection.objects[i]).GetComponent<TemplateSplinePoint>() == null)
                {
                    return false;
                }
            }
            */
        }
        else
        {
            return false;
        }

        return true;
    }

    private void OnGenerateMesh()
    {
        TemplateSplinePoint pointComp = (TemplateSplinePoint)target;

        if (targets.Length == 1)
            pointComp.gameObject.GetComponentInParent<TemplateSplineComponent>().GenerateWireMeshThroughSpline();
        else if (targets.Length > 1)
        {
            foreach (TemplateSplinePoint pComp in targets)
            {
                pComp.gameObject.GetComponentInParent<TemplateSplineComponent>().GenerateWireMeshThroughSpline();
            }
        }
    }


    public override void OnInspectorGUI()
    {
        TemplateSplinePoint pointComp = (TemplateSplinePoint)target;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();


        if (GUILayout.Button("Generate Wire Mesh"))
        {
            OnGenerateMesh();
        }


        /*
        if (IsMultiSelected())
        {
            if (GUILayout.Button("Snap points"))
            {
                
            }
        }
        */

        if (EditorGUI.EndChangeCheck())
        {
            pointComp.UpdateControlPointPosition();
        }
    }


    private void OnDestroy()
    {
        if (Application.isEditor)
        {
            if (target == null)
            {
                //Debug.Log("REMOVED!!!");
            }
        }
    }
}
