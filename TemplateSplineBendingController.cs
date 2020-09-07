
#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

using UnityEngine.Animations;
using UnityEditor.Animations;

[ExecuteInEditMode]
public class TemplateSplineBendingController : MonoBehaviour
{

    [HideInInspector]
    public List<Transform> linkedJoints = new List<Transform>();

    public float degreeOfBend = 1f;

    //public int controllerIndex;
    private float offset = 0f;
    private float initYPos = 0f;

    private void Awake()
    {
        linkedJoints = new List<Transform>();
    }

    public void SetInitTransform(Vector3 pos)
    {
        this.gameObject.transform.position = pos;
        initYPos = pos.y;
    }

   


    public float GetOffset()
    {
        return offset;
    }

    public float GetDegreeOfBend()
    {
        return degreeOfBend;
    }


    private void Start()
    {
        
    }

    void Update()
    {
        // Center transform
        // linkedJoints[linkedJoints.Count/2]
        //if (GUI.changed)
        //offset = this.gameObject.transform.position.y - initYPos;
        offset = this.gameObject.transform.localPosition.y - initYPos;

        {
            int centerIndex = linkedJoints.Count / 2;

            for (int j = 1; j < linkedJoints.Count-1; j++)
            {
                if (linkedJoints[j] == null)
                    continue;

                if (linkedJoints[j].gameObject == null)
                    continue;

                PositionConstraint currentConstraint = linkedJoints[j].gameObject.GetComponent<PositionConstraint>();

                if (currentConstraint != null)
                {
                    //currentConstraint.weight = 1f - (Mathf.Pow(Mathf.Abs(centerIndex - j), 2) * (1f / linkedJoints.Count * bendingRange) / (linkedJoints.Count));
                    currentConstraint.weight = 1f - (Mathf.Pow(Mathf.Abs(j-centerIndex), 2) * (1f / centerIndex * degreeOfBend) / (centerIndex));
                }
            }

            // for (int j= i-(this.numberOfPoints-1); j< i + (this.numberOfPoints); j++)
            // i == 3,  3-(3-1)
            // currentConstraint.weight = 1f - (Mathf.Pow(Mathf.Abs(i - j), 2) * (1f/this.numberOfPoints*2f) / (this.numberOfPoints));
        }

    }

    private void OnDrawGizmos()
    {
        bool isSelected = false;

        foreach(GameObject go in Selection.gameObjects)
        {
            if (go == this.gameObject)
            {
                isSelected = true;
                break;
            }
        }

        if (isSelected == false)
            Gizmos.DrawIcon(this.transform.position, "IconBend", true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawIcon(this.transform.position, "IconBend", true, Color.yellow);
    }

    

}


#endif