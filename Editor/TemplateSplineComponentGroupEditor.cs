#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;


[CustomEditor(typeof(TemplateSplineComponentGroup))]
public class TemplateSplineComponentGroupEditor : Editor
{
    TemplateSplineComponentGroup currentGroup;
    GameObject currentGroupGo;

    private void OnEnable()
    {
        currentGroup = target as TemplateSplineComponentGroup;
    }


    public string GetFullName(GameObject go)
    {
        string name = go.name;
        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            name = go.name + "/" + name;
        }
        return name;
    }


    public string GenerateNewNameWithIndex(GameObject go)
    {
        string newName = string.Empty;

        if (go != null)
        {
            int index = 0;

            string goName = string.Empty;

            if (go.name.Split('_').Length >= 2)
            {
                string indexToBeDel = go.name.Split('_')[go.name.Split('_').Length - 1];

                goName = go.name.Replace("_" + indexToBeDel, string.Empty);
            }
            else
            {
                goName = go.name;
            }


            newName = goName + "_" + index.ToString();

            while(GameObject.Find(newName) != null)
            {
                index++;
                newName = goName + "_" + index.ToString();
            }
            
        }

        return newName;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Linked Clone"))
        {
            // Find Spline End control points (Original)
            TemplateSplineComponent[] originComponents = currentGroup.gameObject.GetComponentsInChildren<TemplateSplineComponent>();
            //TemplateSplinePoint[] originSplinePoints = currentGroup.gameObject.GetComponentsInChildren<TemplateSplinePoint>();

            List<TemplateSplinePoint> pointsToBeLinked = new List<TemplateSplinePoint>();
            List<Transform> targetPoints = new List<Transform>();

            foreach (TemplateSplineComponent originComponent in originComponents)
            {
                TemplateSplinePoint endPoint = originComponent.GetLastSplinePoint();

                pointsToBeLinked.Add(endPoint);
                //endPoint.linkedTransform

                Debug.Log(GetFullName(endPoint.gameObject));
                Debug.Log("\t" + endPoint.GetSiblingIndex());
            }

            

            GameObject clonedGroup = GameObject.Instantiate<GameObject>(currentGroup.gameObject);
            clonedGroup.name = GenerateNewNameWithIndex(currentGroup.gameObject);


            TemplateSplineComponent[] clonedComponents = clonedGroup.GetComponentsInChildren<TemplateSplineComponent>();

            foreach (TemplateSplineComponent clonedComponent in clonedComponents)
            {
                TemplateSplinePoint startPoint = clonedComponent.GetFirstSplinePoint();
                targetPoints.Add(startPoint.linkedTransform);
            }

            if (pointsToBeLinked.Count == targetPoints.Count)
            {
                for (int i=0;i<pointsToBeLinked.Count;i++)
                {
                    pointsToBeLinked[i].linkedTransform = targetPoints[i];
                }
            }

            // Select "cloned Group"
            Selection.activeGameObject = clonedGroup;


            



            //currentGroupGo = GameObject.Instantiate<GameObject>(currentGroup.gameObject);
            //GameObject.Instantiate<GameObject>((target as TemplateSplineComponentGroup).gameObject);


            // Find Spline Start control points (Duplicated one)
            //TemplateSplineComponent[] splineComponents = currentGroupGo.GetComponentsInChildren<TemplateSplineComponent>();

            /*
            foreach(TemplateSplineComponent splineComponent in splineComponents)
            {
                // (Originnal) End control point = Start point
                splineComponent.gameObject.GetComponentInChildren<>
            }
            */

        }

        if (GUILayout.Button("Generate Meshes"))
        {
            TemplateSplineComponent[] splineComponents = currentGroup.gameObject.GetComponentsInChildren<TemplateSplineComponent>();

            foreach(TemplateSplineComponent splineComponent in splineComponents)
            {
                splineComponent.GenerateWireMeshThroughSpline();
            }
        }
    }
}

#endif