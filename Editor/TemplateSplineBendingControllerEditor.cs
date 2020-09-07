using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(TemplateSplineBendingController))]
public class TemplateSplineBendingControllerEditor : Editor
{
    bool isUIvisible = false;
    Vector2 lastMousePosition = Vector2.zero;

    Rect winRect = new Rect();

    private void OnEnable()
    {
        isUIvisible = false;
        lastMousePosition = Vector2.zero;
        winRect = new Rect(lastMousePosition, new Vector2(230, 140));
    }

    void UpdateCurrentMousePosition()
    {
        Vector2 pos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

        lastMousePosition.x = pos.x - SceneView.currentDrawingSceneView.position.x;
        lastMousePosition.y = pos.y - SceneView.currentDrawingSceneView.position.y;

        winRect.x = lastMousePosition.x;
        winRect.y = lastMousePosition.y;
    }

    private void OnSceneGUI()
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        switch (Event.current.GetTypeForControl(controlID))
        {

            case EventType.MouseDown:

                if (Event.current.button == 1)
                {
                    GUIUtility.hotControl = controlID;

                    UpdateCurrentMousePosition();


                    Event.current.Use();
                }
                
                break;

            case EventType.MouseUp:

                if (Event.current.button == 1)
                {
                    GUIUtility.hotControl = 0;

                    isUIvisible = !isUIvisible;

                    Event.current.Use();
                }
                break;
        }

        Handles.BeginGUI();

        if (isUIvisible)
        {
            winRect = GUILayout.Window(8182, winRect, FuncBendingPointMenu, "Bending Point Menu");
        }

        Handles.EndGUI();
    }

    void FuncBendingPointMenu(int windowID)
    {
        EditorGUILayout.Separator();
        GUILayout.BeginVertical();

        GUILayout.BeginVertical("Box");
        GUILayout.Label("Go to");

        if (GUILayout.Button("▲▲ Spline Group ▲▲"))
        {
            OnGotoParentSplineGroup();
            isUIvisible = false;
        }

        if (GUILayout.Button("▲ Spline ▲"))
        {
            OnGotoParentSpline();
            isUIvisible = false;
        }
        

        GUILayout.EndVertical();

        GUILayout.EndVertical();
    }

    void OnGotoParentSplineGroup()
    {
        TemplateSplineComponentGroup sGrp = (target as TemplateSplineBendingController).gameObject.GetComponentInParent<TemplateSplineComponentGroup>();
        
        if (sGrp != null)
        {
            Selection.activeGameObject = sGrp.gameObject;
        }
    }

    void OnGotoParentSpline()
    {
        TemplateSplineComponent sComp = (target as TemplateSplineBendingController).gameObject.GetComponentInParent<TemplateSplineComponent>();

        if (sComp != null)
        {
            Selection.activeGameObject = sComp.gameObject;
        }
    }

}
