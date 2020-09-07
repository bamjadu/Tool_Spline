
#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.EditorTools;


public class TemplateSplineCreator
{
    [MenuItem("Template/Spline Tools/Run \"Spline Tool\"", priority = 140)]
    static void OnCreateSpline()
    {
        EditorTools.SetActiveTool<TemplateSplineComponentTool>();
    }
}

#endif