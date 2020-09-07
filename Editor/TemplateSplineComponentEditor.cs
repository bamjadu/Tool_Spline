using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;



[CanEditMultipleObjects]
[CustomEditor(typeof(TemplateSplineComponent))]
public class TemplateSplineComponentEditor : Editor
{
    private Vector3[] points;

    

    private void OnSceneGUI()
    {
        TemplateSplineComponent comp = (TemplateSplineComponent)target;

        /*
        points = Handles.MakeBezierPoints(
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(-1.0f, 0.75f, 0.75f),
            new Vector3(1.0f, -0.75f, -0.75f),
            20);
            
        Handles.DrawAAPolyLine(points);

        Debug.Log(points.Length);
        */
    }


    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
        {
            if (targets.Length != 0)
            {
                foreach(Object obj in targets)
                {
                    TemplateSplineComponent splineComp = (TemplateSplineComponent)obj;
                    //splineComp.GetCurrentLineRenderer().material.SetColor("_UnlitColor", splineComp.color);

                    MaterialPropertyBlock mBlock = new MaterialPropertyBlock();
                    mBlock.SetColor("_UnlitColor", splineComp.color);
                    splineComp.GetCurrentLineRenderer().SetPropertyBlock(mBlock);
                }
            }
        }

        EditorGUILayout.Separator();

        if (GUILayout.Button("Generate Wire Mesh"))
        {
            /*
            TemplateSplineComponent comp = (TemplateSplineComponent)target;
            comp.GenerateWireMeshThroughSpline();
            */

            for (int i=0;i<targets.Length;i++)
            {
                TemplateSplineComponent comp = (TemplateSplineComponent)targets[i];
                comp.GenerateWireMeshThroughSpline();
            }

        }

        /*
        if (GUILayout.Button("Bake Mesh"))
        {
            TemplateSplineComponent comp = (TemplateSplineComponent)target;
            comp.BakeMeshForSnapshot();
        }

        if (GUILayout.Button("Recalculate Mesh"))
        {
            TemplateSplineComponent comp = (TemplateSplineComponent)target;
            comp.UpdateGeneratedMesh();
        }
        */

        /*
        if (GUILayout.Button("Disconnect"))
        {
            TemplateSplineComponent comp = (TemplateSplineComponent)target;
            comp.DisconnectWires();

        }
        */

        /*
        if (GUILayout.Button("Deform Mesh"))
        {
            TemplateSplineComponent comp = (TemplateSplineComponent)target;
            comp.DeformGeneratedMesh();
        }
        */


    }



}