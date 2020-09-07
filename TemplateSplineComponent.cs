using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Assertions;

using UnityEditor;
using UnityEngine.Animations;
using UnityEditor.Animations;



[ExecuteInEditMode]
public class TemplateSplineComponent : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    public List<GameObject> controlPoints = new List<GameObject>();
    

    private List<Vector3> renderPoints = new List<Vector3>();
    private List<Vector3> dirPoints = new List<Vector3>();


    [HideInInspector]
    public List<Transform> simulatedTransform = new List<Transform>();


    private Mesh bakedMesh;

    private Mesh generatedMesh;


    public Color color = Color.red;
    public float width = 0.1f;
    public int numberOfPoints = 20;

    private LineRenderer lineRenderer = new LineRenderer();

    private MeshCollider meshCollider = new MeshCollider();

    public float diameter = 0.0625f;
    [Range(3,30)]
    public int radialSegments = 12;

    [HideInInspector]
    public Material controllerMaterial;


    public bool EnableBending = false;
    public bool EnableSplineRender = true;

    public enum CurveType
    {
        Bezier,
        CatmullRom,
        BSpline,
    }

    [HideInInspector]
    public CurveType curveType = CurveType.Bezier;

    private CurveType oldCurveType = CurveType.Bezier;

    private int vertexCountDiff = 0;

    private Func<int, bool> CheckBoundary = null;
    private Action<int> SetupPoints = null;
    private Func<float, Vector3> ParametricTransformWithGeometryMatrix = null;



    public void CheckControlPoints()
    {
        int lastIndex = controlPoints.Count;

        bool isNullFound = false;

        for (int i=0;i<lastIndex;i++)
        {
            if (controlPoints[i] == null)
            {
                isNullFound = true;
                controlPoints.RemoveAt(i);
                i = i - 1;
                lastIndex = controlPoints.Count;
            }
        }

    }


    public LineRenderer GetCurrentLineRenderer()
    {
        return lineRenderer;
    }

    private void Awake()
    {
        controllerMaterial = new Material(Shader.Find("HDRP/Lit"));

        

        //if (lineRenderer == null)
        //{
            if (gameObject.GetComponent<LineRenderer>() == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/TemplateTools/SplineTool/Shader/SplineDefaultShader.mat");
            }
            else
                lineRenderer = gameObject.GetComponent<LineRenderer>();
        //}

        meshCollider = gameObject.GetComponent<MeshCollider>();

        lineRenderer.hideFlags = HideFlags.HideInInspector;
        
    }

    public void BakeMeshForSnapshot()
    {
        bakedMesh = new Mesh();
        bakedMesh.name = "bakedLine";
        lineRenderer.BakeMesh(bakedMesh,SceneView.lastActiveSceneView.camera, false);

        meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = bakedMesh;
    }


    public TemplateSplinePoint GetFirstSplinePoint()
    {
        TemplateSplinePoint[] curControlPoints = this.GetComponentsInChildren<TemplateSplinePoint>();

        if (curControlPoints.Length != 0)
        {
            return curControlPoints[0];
        }
        else
            return null;
    }

    public TemplateSplinePoint GetLastSplinePoint()
    {
        TemplateSplinePoint[] curControlPoints = this.GetComponentsInChildren<TemplateSplinePoint>();

        if (curControlPoints.Length != 0)
        {
            return curControlPoints[curControlPoints.Length-1];
        }
        else
            return null;
    }



    // Use this for initialization
    void Start()
    {
        //lineRenderer = this.gameObject.AddComponent<LineRenderer>();   

        
        controllerMaterial.SetColor("_BaseColor", Color.green);

        
        lineRenderer.useWorldSpace = true;
        
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        this.width = 0.05f;
    }

    // Update is called once per frame
    void Update()
    {
        CheckControlPoints();

        lineRenderer.enabled = EnableSplineRender;

        if (controlPoints == null || controlPoints.Count < 3)
        {
            //Debug.LogError("Control Points are not found");
            return;
        }

        if (numberOfPoints < 2)
        {
            numberOfPoints = 2;
        }

        if (oldCurveType != curveType)
        {
            CheckBoundary = null;
            SetupPoints = null;
            ParametricTransformWithGeometryMatrix = null;

            oldCurveType = curveType;
        }

        if (CheckBoundary == null)
        {
            Matrix4x4 matrix = new Matrix4x4();

            if (curveType == CurveType.Bezier)
            {
                vertexCountDiff = 2;

                CheckBoundary = (i) => {
                    return !(controlPoints[i] == null
                        || controlPoints[i + 1] == null
                        || controlPoints[i + 2] == null);
                };

                Matrix4x4 m = new Matrix4x4();
                m.SetRow(0, new Vector3(1f, -2f, 1f));
                m.SetRow(1, new Vector3(-2f, 2f, 0f));
                m.SetRow(2, new Vector3(1f, 0f, 0f));
                m = m.transpose;

                SetupPoints = (i) => {
                    matrix.SetColumn(0, 0.5f * (controlPoints[i].transform.position + controlPoints[i + 1].transform.position));
                    matrix.SetColumn(1, controlPoints[i + 1].transform.position);
                    matrix.SetColumn(2, 0.5f * (controlPoints[i + 1].transform.position + controlPoints[i + 2].transform.position));
                    matrix *= m;
                };

                ParametricTransformWithGeometryMatrix = (t) => {
                    return matrix * new Vector3(t * t, t, 1);
                };
            }
            else if (curveType == CurveType.CatmullRom)
            {
                vertexCountDiff = 1;

                CheckBoundary = (i) => {
                    return !(controlPoints[i] == null
                        || controlPoints[i + 1] == null
                        || (i > 0 && controlPoints[i - 1] == null)
                        || (i < controlPoints.Count - 2 && controlPoints[i + 2] == null));
                };

                Matrix4x4 m = new Matrix4x4();
                m.SetRow(0, new Vector4(1f, 0f, 0f, 0f));
                m.SetRow(1, new Vector4(0f, 0f, 1f, 0f));
                m.SetRow(2, new Vector4(-3f, 3f, -2f, -1f));
                m.SetRow(3, new Vector4(2f, -2f, 1f, 1f));
                m = m.transpose;

                SetupPoints = (i) => {
                    matrix.SetColumn(0, controlPoints[i].transform.position);
                    matrix.SetColumn(1, controlPoints[i + 1].transform.position);

                    if (i > 0)
                    {
                        matrix.SetColumn(2, 0.5f * (controlPoints[i + 1].transform.position - controlPoints[i - 1].transform.position));
                    }
                    else
                    {
                        matrix.SetColumn(2, controlPoints[i + 1].transform.position - controlPoints[i].transform.position);
                    }

                    if (i < controlPoints.Count - 2)
                    {
                        matrix.SetColumn(3, 0.5f * (controlPoints[i + 2].transform.position - controlPoints[i].transform.position));
                    }
                    else
                    {
                        matrix.SetColumn(3, controlPoints[i + 1].transform.position - controlPoints[i].transform.position);
                    }

                    matrix *= m;
                };

                ParametricTransformWithGeometryMatrix = (t) => {
                    return matrix * new Vector4(1, t, t * t, t * t * t);
                };
            }
            else if (curveType == CurveType.BSpline)
            {
                vertexCountDiff = 3;

                CheckBoundary = (i) => {
                    return !(controlPoints[i] == null
                        || controlPoints[i + 1] == null
                        || controlPoints[i + 2] == null
                        || controlPoints[i + 3] == null);
                };

                Matrix4x4 m = new Matrix4x4();
                m.SetRow(0, new Vector4(-1f, 3f, -3f, 1f) * 1 / 6);
                m.SetRow(1, new Vector4(3f, -6f, 3f, 0f) * 1 / 6);
                m.SetRow(2, new Vector4(-3f, 0f, 3f, 0f) * 1 / 6);
                m.SetRow(3, new Vector4(1f, 4f, 1f, 0f) * 1 / 6);
                m = m.transpose;

                SetupPoints = (i) => {
                    matrix.SetColumn(0, controlPoints[i].transform.position);
                    matrix.SetColumn(1, controlPoints[i + 1].transform.position);
                    matrix.SetColumn(2, controlPoints[i + 2].transform.position);
                    matrix.SetColumn(3, controlPoints[i + 3].transform.position);
                    matrix *= m;
                };

                ParametricTransformWithGeometryMatrix = (t) => {
                    return matrix * new Vector4(t * t * t, t * t, t, 1);
                };
            }
        }

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = numberOfPoints * (controlPoints.Count - vertexCountDiff);

        renderPoints.Clear();
        dirPoints.Clear();

        for (int j = 0; j < controlPoints.Count - vertexCountDiff; j++)
        {
            if (!CheckBoundary(j))
            {
                return;
            }

            SetupPoints(j);

            float pointStep = j == controlPoints.Count - (vertexCountDiff + 1)
                ? 1.0f / (numberOfPoints - 1.0f) : 1.0f / numberOfPoints;

            for (int i = 0; i < numberOfPoints; i++)
            {
                Vector3 position = ParametricTransformWithGeometryMatrix(i * pointStep);
                lineRenderer.SetPosition(i + j * numberOfPoints, position);

                renderPoints.Add(position);

            }
        }

        // Caculate each directional vector
        for (int i=0;i<renderPoints.Count;i++)
        {
            if (i != renderPoints.Count - 1)
            {
                dirPoints.Add((renderPoints[i + 1] - renderPoints[i]).normalized);
            }
            else
            {
                dirPoints.Add((renderPoints[i] - renderPoints[i-1]).normalized);
            }
        }


        
    }


    void OnDrawControllerLine()
    {
        if (controlPoints.Count > 2)
        {
            if (curveType == CurveType.Bezier)
            {
                for (int i = 0; i < controlPoints.Count; i = i + 2)
                {
                    if (controlPoints[i] != null && controlPoints[i + 1] != null)
                        Debug.DrawLine(controlPoints[i].transform.position, controlPoints[i + 1].transform.position, Color.green);
                }
            }
        }
        
    }



    private void OnDrawGizmos()
    {
        OnDrawControllerLine();
    }

    /*
    private void OnDrawGizmos()
    {
        if (Event.current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(2);

        if (Event.current.type == EventType.MouseUp)
        {
            Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition));
            RaycastHit hit;
            //List<GameObject> controlPointsNew = new List<GameObject>();
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("New Hit!");

                GameObject hitObj = hit.transform.gameObject;

                Debug.Log("\t" + hitObj);

                //hit.point
                //hit.normal

                Vector3 offset = (controlPoints[controlPoints.Count - 1].transform.position - controlPoints[controlPoints.Count - 2].transform.position) / 2f;

                //GameObject newControlPoint1 = SpawnSphereAt(hit.point + offset);
                //GameObject newControlPoint2 = SpawnSphereAt(hit.point - offset);
                GameObject newControlPoint1 = SpawnSphereAt(hit.point);
                GameObject newControlPoint2 = SpawnSphereAt(hit.point);

                newControlPoint1.name = "CP" + newControlPoint1.GetInstanceID().ToString();
                newControlPoint2.name = "CP" + newControlPoint2.GetInstanceID().ToString();


                controlPoints.Add(newControlPoint1);
                controlPoints.Add(newControlPoint2);

            }
        }
    }
    */

    public GameObject SpawnSphereAt(Vector3 mousePosition)
    {
        GameObject sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Vector3 position = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));
        sp.transform.position = new Vector3(position.x, position.y, 0);
        sp.name += sp.GetInstanceID().ToString();
        return sp;
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


    public void GenerateWireMeshThroughSpline()
    {
        System.GC.Collect();

        
        //GameObject generatedMeshGo = GameObject.Find(string.Format("{0}/GeneratedMesh", this.gameObject.name));
        GameObject generatedMeshGo = GameObject.Find(string.Format("{0}/GeneratedMesh", GetFullName(this.gameObject)));

        TemplateSplineComponent splineComp = this.gameObject.GetComponent<TemplateSplineComponent>();

        if (generatedMeshGo != null)
        {
            DestroyImmediate(generatedMeshGo);
        }

        generatedMeshGo = new GameObject("GeneratedMesh");
        generatedMeshGo.transform.SetParent(this.gameObject.transform);
        
        
        //LineRenderer lRender = this.gameObject.GetComponent<LineRenderer>();



        List<RingInfo> RingInfoGroup = new List<RingInfo>();


        for (int i = 0; i < lineRenderer.positionCount; i++)
        {

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //GameObject go = new GameObject("Cube");

            

            Vector3 forward = Vector3.one;

            if (i != lineRenderer.positionCount - 1)
            {
                Vector3 pos = lineRenderer.GetPosition(i);
                Vector3 pos1 = lineRenderer.GetPosition(i + 1);

                go.transform.position = pos;
                go.transform.LookAt(pos1);

            }
            else
            {
                // Last one
                Vector3 pos = lineRenderer.GetPosition(i - 1);
                Vector3 pos1 = lineRenderer.GetPosition(i);

                go.transform.position = pos1;
                go.transform.forward = (pos1 - pos).normalized;

            }

            forward = go.transform.forward;

            go.transform.SetParent(generatedMeshGo.transform);
            go.transform.SetAsLastSibling();


            // Make Ring info

            RingInfo curRingInfo = new RingInfo();



            // Set RingInfo
            curRingInfo.center = go.transform.position;
            curRingInfo.radialSegement = radialSegments;
            curRingInfo.diameter = diameter;
            curRingInfo.forward = forward;

            Vector3 up = go.transform.position + (go.transform.up * diameter);

            for (int k = 0; k < radialSegments; k++)
            {
                GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                circle.transform.position = up;
                circle.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                circle.transform.RotateAround(go.transform.position, forward, (360f / (float)radialSegments) * k);

                circle.transform.forward = forward;


                // Save Ring infos
                curRingInfo.Vertex.Add(circle.transform.position);
                curRingInfo.Normal.Add((circle.transform.position - go.transform.position).normalized);

                GameObject.DestroyImmediate(circle);

            }

            RingInfoGroup.Add(curRingInfo);
              
            GameObject.DestroyImmediate(go);

        }

        // Generate Meshes
        int[] indices = GenerateIndices(RingInfoGroup);

        Mesh mesh = new Mesh();
        mesh.name = "GeneratedMesh";

        Vector3[] vertices = new Vector3[RingInfoGroup.Count * radialSegments];
        Color[] colors = new Color[RingInfoGroup.Count * radialSegments];

        BoneWeight[] boneWeight = new BoneWeight[RingInfoGroup.Count * radialSegments];

        int vCount = 0;

        //GameObject GeneratedRig = GameObject.Find(string.Format("{0}/GeneratedRigRoot", this.gameObject.name));
        GameObject GeneratedRig = GameObject.Find(string.Format("{0}/GeneratedRigRoot", GetFullName(this.gameObject)));

        if (GeneratedRig != null)
        {
            DestroyImmediate(GeneratedRig);
        }

        GeneratedRig = new GameObject("GeneratedRigRoot");

        //GeneratedRig.transform.SetPositionAndRotation(generatedMeshGo.transform.position, Quaternion.identity);
        GeneratedRig.transform.SetParent(generatedMeshGo.transform.parent);

        Transform previousTransform = GeneratedRig.transform;

        Matrix4x4[] bindPoses = new Matrix4x4[RingInfoGroup.Count];
        Transform[] bones = new Transform[RingInfoGroup.Count];

        int bindPoseCount = 0;

        
        foreach (RingInfo curIn in RingInfoGroup)
        {

            GameObject currentNewJoint = new GameObject("Joint" + bindPoseCount.ToString());
            //GameObject currentNewJoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //currentNewJoint.name = "Joint" + bindPoseCount.ToString();



            currentNewJoint.transform.SetPositionAndRotation(curIn.center, Quaternion.identity);
            currentNewJoint.transform.forward = curIn.forward;
            currentNewJoint.transform.rotation = currentNewJoint.transform.rotation.normalized;



            //////////////////////////////////////////////////////////////////////////////////////
            /// For bending wires, should be fixed (Some critical parts)
            currentNewJoint.transform.SetParent(GeneratedRig.transform);
            //currentNewJoint.transform.SetParent(previousTransform); 
            //////////////////////////////////////////////////////////////////////////////////////

            if (EnableBending)
            {
                PositionConstraint currentConstraint = currentNewJoint.AddComponent<PositionConstraint>();

                currentConstraint.weight = 0f;
            }

            //currentConstraint.AddSource(new ConstraintSource());


            //currentNewJoint.transform.position = curIn.center;
            //currentNewJoint.transform.forward = curIn.forward;



            //currentNewJoint.transform.forward = curIn.forward;
            //currentNewJoint.transform.localPosition = curIn.center;
            //currentNewJoint.transform.localRotation = Quaternion.identity;

            bones[bindPoseCount] = currentNewJoint.transform;
            bindPoses[bindPoseCount] = currentNewJoint.transform.worldToLocalMatrix * GeneratedRig.transform.localToWorldMatrix;

            previousTransform = currentNewJoint.transform;

            

            

            foreach (Vector3 v in curIn.Vertex)
            {
                vertices[vCount] = v;
                colors[vCount] = Color.cyan;

                
                boneWeight[vCount].boneIndex0 = bindPoseCount;
                boneWeight[vCount].weight0 = 1f;

                vCount++;
            }

            bindPoseCount++;

        }

        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.uv = GenerateUVs(RingInfoGroup);

        mesh.boneWeights = boneWeight;
        mesh.bindposes = bindPoses;
        mesh.colors = colors;
        
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        generatedMesh = new Mesh();
        generatedMesh = mesh;


        //MeshFilter mFilter = generatedMeshGo.AddComponent<MeshFilter>();
        //mFilter.mesh = mesh;
        SkinnedMeshRenderer mRender = generatedMeshGo.AddComponent<SkinnedMeshRenderer>();
        mRender.updateWhenOffscreen = true;
        mRender.sharedMesh = mesh;

        mRender.sharedMaterial = new Material(Shader.Find("HDRP/Lit"));

        

        //mRender.rootBone = GeneratedRig.transform;
        mRender.bones = bones;

        //mRender.sharedMesh = null;
        //DestroyImmediate(mesh);



        /*        
                #region Add components for physics simulation


                GameObject genRigidBodyRoot = GameObject.Find(string.Format("{0}/GeneratedRigidBody", GetFullName(this.gameObject)));

                if (genRigidBodyRoot != null)
                {
                    DestroyImmediate(genRigidBodyRoot);
                }

                genRigidBodyRoot = new GameObject("GeneratedRigidBody");

                genRigidBodyRoot.transform.SetParent(this.gameObject.transform);



                Transform[] childTransforms = GeneratedRig.GetComponentsInChildren<Transform>();

                int rigidBodyIndex = 0;

                foreach (Transform targetChild in childTransforms)
                {

                    if (targetChild.gameObject == GeneratedRig)
                        continue;

                    GameObject targetRigidBodyGo = new GameObject("RigidBody" + rigidBodyIndex.ToString());


                    targetRigidBodyGo.transform.position = targetChild.transform.position;
                    targetRigidBodyGo.transform.rotation = targetChild.transform.rotation;


                    targetRigidBodyGo.transform.SetParent(genRigidBodyRoot.transform);
                    targetRigidBodyGo.transform.SetAsLastSibling();

                    simulatedTransform.Add(targetRigidBodyGo.transform);



                    Rigidbody targetRigidbody = targetRigidBodyGo.AddComponent<Rigidbody>();

                    targetRigidbody.mass = 1f;
                    targetRigidbody.useGravity = true;
                    targetRigidbody.drag = 0.02f;
                    targetRigidbody.isKinematic = true;

                    if (rigidBodyIndex == 0 || rigidBodyIndex == childTransforms.Length - 2)
                    {
                        targetRigidbody.useGravity = false;
                        targetRigidbody.constraints = RigidbodyConstraints.FreezePosition;
                    }

                    // if "DistanceJoint" is on
                    //targetRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;


                    //targetRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    //targetRigidbody.interpolation = RigidbodyInterpolation.Interpolate;


                    SphereCollider targetSpCollider = targetRigidBodyGo.AddComponent<SphereCollider>();

                    targetSpCollider.radius = 0.2f;


                    //DistanceJoint3D distanceJoint = targetChild.gameObject.AddComponent<DistanceJoint3D>();
                    //distanceJoint.ConnectedRigidbody = targetChild.parent;
                    //distanceJoint.Damper = 7f;


                    if (rigidBodyIndex != 0 && rigidBodyIndex % 2 != 0)
                    {
                        HingeJoint hJoint0 = targetRigidBodyGo.AddComponent<HingeJoint>();
                        HingeJoint hJoint1 = targetRigidBodyGo.AddComponent<HingeJoint>();

                        hJoint0.autoConfigureConnectedAnchor = true;
                        hJoint1.autoConfigureConnectedAnchor = true;



                        hJoint0.useSpring = true;
                        hJoint1.useSpring = true;

                        hJoint0.useLimits = true;
                        hJoint1.useLimits = true;

                        JointLimits jLimit = hJoint0.limits;
                        jLimit.max = 25f;
                        jLimit.min = -25f;

                        hJoint0.limits = jLimit;
                        hJoint1.limits = jLimit;

                        JointSpring jSpring = hJoint0.spring;
                        jSpring.damper = 3f;
                        jSpring.spring = 10f;
                        jSpring.targetPosition = 20f;

                        hJoint0.spring = jSpring;
                        hJoint1.spring = jSpring;

                        hJoint0.axis = new Vector3(1f, 0f, 0f);
                        hJoint1.axis = new Vector3(1f, 0f, 0f);
                        hJoint0.anchor = new Vector3(0f, 0f, 0f);
                        hJoint1.anchor = new Vector3(0f, 0f, 0f);
                    }


                    rigidBodyIndex++;

                }
                #endregion


                #region connect Hinges

                childTransforms.Initialize();
                //childTransforms = genRigidBodyRoot.GetComponentsInChildren<Transform>();

                for (int i=0;i<genRigidBodyRoot.transform.childCount;i++)
                {

                    Transform targetChild = genRigidBodyRoot.transform.GetChild(i);

                    if (i == 0)
                        continue;


                    if (i != childTransforms.Length-1)
                    {


                        HingeJoint[] joints = targetChild.gameObject.GetComponents<HingeJoint>();

                        Debug.Log("Joijnts : " + joints.Length);

                        if (joints.Length == 2)
                        {
                            Debug.Log("Body " + i.ToString());

                            //if (GameObject.Find(string.Format("{0}/GeneratedRigidBody/RigidBody{1}", this.gameObject.name, i - 1)) != null)
                            if (GameObject.Find(string.Format("{0}/GeneratedRigidBody/RigidBody{1}", GetFullName(this.gameObject), i - 1)) != null)
                                joints[0].connectedBody = GameObject.Find(string.Format("{0}/GeneratedRigidBody/RigidBody{1}", GetFullName(this.gameObject), i - 1)).GetComponent<Rigidbody>();

                            //if (GameObject.Find(string.Format("{0}/GeneratedRigidBody/RigidBody{1}", this.gameObject.name, i + 1)) != null)
                            if (GameObject.Find(string.Format("{0}/GeneratedRigidBody/RigidBody{1}", GetFullName(this.gameObject), i + 1)) != null)
                                joints[1].connectedBody = GameObject.Find(string.Format("{0}/GeneratedRigidBody/RigidBody{1}", GetFullName(this.gameObject), i + 1)).GetComponent<Rigidbody>();


                        }
                    }



                }
                #endregion


                #region Activate Physics simulations
                for (int i = 0; i < genRigidBodyRoot.transform.childCount; i++)
                {

                    Transform targetChild = genRigidBodyRoot.transform.GetChild(i);

                    if (i == 0 || i== genRigidBodyRoot.transform.childCount - 1)
                        continue;

                    Rigidbody currentBody = targetChild.gameObject.GetComponent<Rigidbody>();

                    currentBody.isKinematic = false;

                }
                #endregion
        */

        
            #region Add Bending controllers

        //GameObject genBendingControolerRoot = GameObject.Find(string.Format("{0}/GeneratedBendingController", this.gameObject.name));

        bool isExistingBendingControl = false;
        GameObject genBendingControolerRoot = GameObject.Find(string.Format("{0}/GeneratedBendingController", GetFullName(this.gameObject)));

        if (EnableBending)
        {
            List<string> exBendInfo = new List<string>();


            if (genBendingControolerRoot != null)
            {

                isExistingBendingControl = true;

                TemplateSplineBendingController[] exControllers = genBendingControolerRoot.GetComponentsInChildren<TemplateSplineBendingController>();

                foreach (TemplateSplineBendingController exController in exControllers)
                {
                    float offset = exController.GetOffset();
                    float degreeOfBend = exController.GetDegreeOfBend();

                    exBendInfo.Add(offset.ToString() + "," + degreeOfBend.ToString());

                }


                DestroyImmediate(genBendingControolerRoot);
            }

            genBendingControolerRoot = new GameObject("GeneratedBendingController");
            genBendingControolerRoot.transform.SetParent(this.gameObject.transform);


            for (int i = 0; i < bones.Length; i++)
            {
                if (i % (this.numberOfPoints * 2) == this.numberOfPoints && i != 0 && i != bones.Length - 1)
                {
                    //GameObject bendController = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //GameObject bendController = new GameObject("BendingControl");

                    GameObject bendController = new GameObject(string.Format("BendingControl_{0}", i));

                    /*
                    if (isExistingBendingControl)
                        bendController = GameObject.Find(string.Format("{0}/BendingControl_{1}", GetFullName(genBendingControolerRoot), i));
                    else
                        bendController = new GameObject(string.Format("BendingControl_{0}",i));
                    */

                    TemplateSplineBendingController currentBendController = bendController.AddComponent<TemplateSplineBendingController>();
                    //TemplateSplineBendingController currentBendController = bendController.GetComponent<TemplateSplineBendingController>();


                    //bendController.transform.position = bones[i].position;
                    currentBendController.SetInitTransform(bones[i].position);

                    for (int j = i - (this.numberOfPoints - 1); j < i + (this.numberOfPoints); j++)
                    {
                        PositionConstraint currentConstraint = bones[j].gameObject.GetComponent<PositionConstraint>();

                        currentBendController.linkedJoints.Add(bones[j]);

                        ConstraintSource source = new ConstraintSource();
                        source.sourceTransform = bendController.transform;

                        source.weight = 1f;

                        currentConstraint.AddSource(source);

                        currentConstraint.translationAxis = Axis.Y;


                        currentConstraint.weight = 0f;

                        currentConstraint.locked = true;
                        currentConstraint.constraintActive = true;

                    }


                    bendController.transform.SetParent(genBendingControolerRoot.transform);
                    bendController.transform.SetAsLastSibling();

                    if (isExistingBendingControl)
                    {

                        int curSiblingIndex = bendController.transform.GetSiblingIndex();

                        if (curSiblingIndex < exBendInfo.Count)
                        {
                            string[] tokens = exBendInfo[curSiblingIndex].ToString().Split(',');

                            if (tokens.Length >= 2)
                            {
                                float recordedOffset = float.Parse(tokens[0]);
                                float recordedBend = float.Parse(tokens[1]);

                                currentBendController.gameObject.transform.Translate(0f, recordedOffset, 0f);
                                currentBendController.degreeOfBend = recordedBend;
                            }
                        }
                    }

                }
            }
            // add controller



            #endregion
        }
        else
        {
            if (genBendingControolerRoot != null)
                DestroyImmediate(genBendingControolerRoot);
        }

        //EditorApplication.update += UpdateGeneratedMesh;



    }


    public void DisconnectWires()
    {
        //GameObject rigidBodyRoot = GameObject.Find("SplineTool/GeneratedRigidBody");
        GameObject rigidBodyRoot = GameObject.Find(string.Format("{0}/GeneratedRigidBody", this.gameObject.name));

        if (rigidBodyRoot.transform.childCount != 0)
        {
            GameObject firstChild = rigidBodyRoot.transform.GetChild(0).gameObject;
            GameObject lastChild = rigidBodyRoot.transform.GetChild(rigidBodyRoot.transform.childCount-1).gameObject;

            HingeJoint[] hinges = lastChild.GetComponents<HingeJoint>();

            for (int i=0;i<hinges.Length;i++)
            {
                if (hinges[i].connectedBody == null)
                {
                    DestroyImmediate(hinges[i]);
                }
            }

            Rigidbody firstRigidBody = firstChild.GetComponent<Rigidbody>();
            Rigidbody lastRigidBody = lastChild.GetComponent<Rigidbody>();

            if (firstRigidBody != null)
            {
                firstRigidBody.isKinematic = false;
                firstRigidBody.constraints = RigidbodyConstraints.None;
                firstRigidBody.useGravity = true;
            }

            if (lastRigidBody != null)
            {
                lastRigidBody.isKinematic = false;
                lastRigidBody.constraints = RigidbodyConstraints.None;
                lastRigidBody.useGravity = true;
            }


        }

    }


    public void DeformGeneratedMesh()
    {
        //GameObject.Find("SplineTool/GeneratedRigRoot/Joint0")
        //GameObject.Find("SplineTool/GeneratedRigidBody")

        //GameObject jointRoot = GameObject.Find(string.Format("SplineTool/GeneratedRigRoot/Joint0"));
        //GameObject jointRoot = GameObject.Find(string.Format("{0}/GeneratedRigRoot/Joint0", this.gameObject.name));
        GameObject jointRoot = GameObject.Find(string.Format("{0}/GeneratedRigRoot", GetFullName(this.gameObject)));

        Transform[] joints = jointRoot.GetComponentsInChildren<Transform>();

        for (int i=0;i<joints.Length;i++)
        {

            //GameObject sourceTrans = GameObject.Find(string.Format("SplineTool/GeneratedRigidBody/RigidBody{0}", i.ToString()));
            GameObject sourceTrans = GameObject.Find(string.Format("{1}/GeneratedRigidBody/RigidBody{0}", i.ToString(), GetFullName(this.gameObject)));

            joints[i].position = sourceTrans.transform.position;
            joints[i].rotation = sourceTrans.transform.rotation;
            //Debug.Log(joint.name);

        }


    }




    public void UpdateGeneratedMesh()
    {
        if (generatedMesh != null)
        {
            Debug.Log("Update Mesh");
            generatedMesh.RecalculateBounds();
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateTangents();

            
            Debug.Log("center : " + generatedMesh.bounds.center);
            Debug.Log("extent : " + generatedMesh.bounds.extents);

            //GameObject.Find(string.Format("SplineTool/GeneratedMesh", GetFullName(this.gameObject))).GetComponent<SkinnedMeshRenderer>().sharedMesh.RecalculateBounds();
            GameObject.Find(string.Format("{0}/GeneratedMesh", GetFullName(this.gameObject))).GetComponent<SkinnedMeshRenderer>().sharedMesh.RecalculateBounds();
            //GameObject.Find("SplineTool/GeneratedMesh").GetComponent<SkinnedMeshRenderer>().sharedMesh = generatedMesh;
        }
        else
        {
            EditorApplication.update -= UpdateGeneratedMesh;
        }
    }



    public Vector2[] GenerateUVs(List<RingInfo> info)
    {
        
        int radialSegement = info[0].radialSegement;

        float splineDistance = GetSplineDistance();

        var uvs = new Vector2[info.Count * radialSegement];

        for (int segment = 0; segment < info.Count; segment++)
        {
            for (int side = 0; side < radialSegement; side++)
            {
                int vertIndex = (segment * radialSegement + side);
                float u = side / (radialSegement - 1f);
                float v = (segment / (info.Count - 1f)) * (1.0f * splineDistance);

                //Rotated 90 degrees
                uvs[vertIndex] = new Vector2(v, u);
            }
        }

        return uvs;
    }


    public float GetSplineDistance()
    {
        if (lineRenderer == null)
            return 0f;

        float totalDistnace = 0;

        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            float distance = Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
            totalDistnace += distance;
        }

        return totalDistnace;
    }



    public int[] GenerateIndices(List<RingInfo> info)
    {

        if (info == null)
            return null;
        if (info.Count == 0)
            return null;

        int radialSegment = info[0].radialSegement;

        var indices = new int[info.Count * radialSegment * 2 * 3];

        var currentIndicesIndex = 0;
        for (int segment = 1; segment < info.Count; segment++)
        {
            for (int side = 0; side < radialSegment; side++)
            {
                var vertIndex = (segment * radialSegment + side);
                var prevVertIndex = vertIndex - radialSegment;

                // Triangle one
                indices[currentIndicesIndex++] = prevVertIndex;
                indices[currentIndicesIndex++] = (side == radialSegment - 1) ? (vertIndex - (radialSegment - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = vertIndex;

                // Triangle two
                indices[currentIndicesIndex++] = (side == radialSegment - 1) ? (prevVertIndex - (radialSegment - 1)) : (prevVertIndex + 1);
                indices[currentIndicesIndex++] = (side == radialSegment - 1) ? (vertIndex - (radialSegment - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = prevVertIndex;
            }
        }

        return indices;
    }


}


public class RingInfo
{
    public Vector3 center;
    public int radialSegement;
    public float diameter;
    public Vector3 forward;

    public List<Vector3> Vertex;
    public List<Vector3> Normal;
    

    public RingInfo()
    {
        center = Vector3.zero;
        Vertex = new List<Vector3>();
        Normal = new List<Vector3>();
    }
}



public class DebugSpline
{


    [MenuItem("DEBUG/Get Spline Distance")]
    static void DebugGetSplineDistance()
    {
        LineRenderer lRender = GameObject.Find("SplineTool").GetComponent<LineRenderer>();

        float totalDistnace = 0;

        for (int i = 0; i < lRender.positionCount - 1; i++)
        {
            float distance = Vector3.Distance(lRender.GetPosition(i), lRender.GetPosition(i + 1));
            totalDistnace += distance;
        }

        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Total Distance : " + totalDistnace.ToString() + " m"),5f);
    }




    [MenuItem("DEBUG/Check Spline")]
    static void DebugCheckSpline()
    {
        
        /*
        Matrix4x4 matrix = new Matrix4x4();

        
        vertexCountDiff = 2;

        CheckBoundary = (i) => {
            return !(controlPoints[i] == null
                || controlPoints[i + 1] == null
                || controlPoints[i + 2] == null);
        };

        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, new Vector3(1f, -2f, 1f));
        m.SetRow(1, new Vector3(-2f, 2f, 0f));
        m.SetRow(2, new Vector3(1f, 0f, 0f));
        m = m.transpose;

        SetupPoints = (i) => {
            matrix.SetColumn(0, 0.5f * (controlPoints[i].transform.position + controlPoints[i + 1].transform.position));
            matrix.SetColumn(1, controlPoints[i + 1].transform.position);
            matrix.SetColumn(2, 0.5f * (controlPoints[i + 1].transform.position + controlPoints[i + 2].transform.position));
            matrix *= m;
        };

        ParametricTransformWithGeometryMatrix = (t) => {
            return matrix * new Vector3(t * t, t, 1);
        };
        
        */

        GameObject splineTool = GameObject.Find("SplineTool");

        GameObject generatedMeshGo = GameObject.Find("SplineTool/GeneratedMesh");

        TemplateSplineComponent splineComp = splineTool.GetComponent<TemplateSplineComponent>();

        if (generatedMeshGo == null)
        {
            generatedMeshGo = new GameObject("GeneratedMesh");
            generatedMeshGo.transform.SetParent(splineTool.transform);
        }

        LineRenderer lRender = splineTool.GetComponent<LineRenderer>();

        int segment = 60;

        float offest = 1.0f / (float)segment;

        /*
        GameObject firstGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject lastGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        */

        GameObject firstGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject lastGo = GameObject.CreatePrimitive(PrimitiveType.Cube);


        firstGo.transform.SetPositionAndRotation(lRender.GetPosition(0), Quaternion.identity);
        lastGo.transform.SetPositionAndRotation(lRender.GetPosition(lRender.positionCount-1), Quaternion.identity);

        firstGo.transform.SetParent(generatedMeshGo.transform);
        lastGo.transform.SetParent(generatedMeshGo.transform);

        List<GameObject> generatedGos = new List<GameObject>();

        for (float step=offest; step<1f;step=step+offest)
        {

            float w = Mathf.Lerp(0, lRender.positionCount, step);

            int index = Mathf.FloorToInt(w);

            if (index < lRender.positionCount - 1)
            {
                Vector3 pos = Vector3.Lerp(lRender.GetPosition(index), lRender.GetPosition(index + 1), w - (float)index);

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(pos, Quaternion.identity);
                go.transform.LookAt(lRender.GetPosition(index + 1));

                go.transform.SetParent(generatedMeshGo.transform);
                go.transform.SetAsFirstSibling();
                generatedGos.Add(go);
                
            }
        }

        firstGo.transform.LookAt(generatedGos[0].transform);
        firstGo.transform.SetAsLastSibling();
        lastGo.transform.LookAt(generatedGos[generatedGos.Count - 1].transform);
        lastGo.transform.SetAsFirstSibling();


        generatedMeshGo.transform.SetAsLastSibling();

    }

}