﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;


[ExecuteInEditMode]
public class TemplateSplinePoint : MonoBehaviour
{

    List<Transform> controlPoints = new List<Transform>();

    public float scale = 1.0f;

    public Transform linkedTransform;

    float timeToRemesh = 1f;

    bool isIdeal = true;



    private void Awake()
    {
        for (int i=0;i<this.transform.childCount;i++)
        {
            Transform child = this.transform.GetChild(i);
            controlPoints.Add(child);

            child.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

    }

    public void UpdateControlPointPosition()
    {
        
        controlPoints[0].position = this.transform.position + (scale * controlPoints[0].right);
        controlPoints[1].position = this.transform.position - (scale * controlPoints[1].right);
        
        //    t.position += scale
        
    }

    public int GetSiblingIndex()
    {
        return this.transform.GetSiblingIndex();
    }



    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawIcon(this.transform.position,"Packages/com.unity.production.spline/Editor/Gizmos/IconControlPoint.png", true, Color.red);

        //Gizmos.DrawIcon(this.transform.position, "IconControlPoint", true, Color.red);
    }

    private void OnDrawGizmos()
    {
        bool isSelected = false;

        foreach(GameObject selGo in Selection.gameObjects)
        {
            if (selGo == this.gameObject)
            {
                isSelected = true;
                break;
            }
        }

        if (isSelected == false)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var packagePath = PackagePath.FindForAssembly(assembly).assetPath;
            
            Gizmos.DrawIcon(this.transform.position, "Packages/com.unity.production.spline/Editor/Gizmos/IconControlPoint.png", true, Color.green);
            //Gizmos.DrawIcon(this.transform.position, "IconControlPoint", true, Color.green);
        }
    }


    private void Update()
    {
        if (linkedTransform != null)
        {
            this.transform.position = linkedTransform.position;
            UpdateControlPointPosition();
        }


        if (this.transform.hasChanged)
        {
            timeToRemesh = 1f;
            isIdeal = false;
            this.transform.hasChanged = false;
        }
        else
        {
            if (isIdeal == false)
                timeToRemesh = timeToRemesh - Time.deltaTime;

            if (timeToRemesh < 0)
            {
                //Debug.Log("Remeshed....");

                this.gameObject.GetComponentInParent<TemplateSplineComponent>().GenerateWireMeshThroughSpline();

                timeToRemesh = 1;
                isIdeal = true;
            }
        }

        

    }

   

    

}
