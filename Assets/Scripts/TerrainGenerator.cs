using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour {

    public Camera mainCamera;

    public bool TwoDimensional = true;
    public bool useCameraX = false;

    [Header("Slope Management")]
    public float slopeLength;
    public float slopeMaxHeight;
    public float slopeMinHeight;

    [Header("Mesh Modifiers")]
    public float yThickness = 10;
    public float zThickness = 5;

    public int segmentsPerSlope = 50;

    [Header("Textures")]
    public int textureWidth = 200;

    public Material[] textures;

    [Header("Obstacles")]
    [Range(0.0f, 1.0f)]
    public float obstacleChance;
    public GameObject[] obstacles;

    [Header("Decorations")]
    [Range(0.0f, 1.0f)]
    public float decorationChance;
    public GameObject[] decorations;

    private Vector3[] keys = null;
    private GameObject terrain;
    private Queue<GameObject> spawnedObstacles = new Queue<GameObject>();
    private Queue<GameObject> spawnedDecorations = new Queue<GameObject>();

    private Vector3 previousLocation;
    private float previousScreenWidth;
    private float segmentsMoved = 0;
    private float additionalSegments = 0;
    private float startX;

    private XRandomGen numGen;

    public void Start()
    {
        numGen = new XRandomGen(Random.Range(int.MinValue, int.MaxValue));
        Generate();
        previousLocation = mainCamera.transform.position;
    }

    private void Generate()
    {
        float left = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, (transform.position.z + zThickness) - mainCamera.transform.position.z)).x;
        float right = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, (transform.position.z + zThickness) - mainCamera.transform.position.z)).x;
        float screenWidth = right - left;
        previousScreenWidth = screenWidth;
        int numSegments = (int)(screenWidth / slopeLength) + 2;

        keys = new Vector3[numSegments];

        float segmentsWidth = slopeLength * (float)(numSegments - 1);
        float offscreenAmount = segmentsWidth - screenWidth;
        startX = left - (offscreenAmount / 2f);

        Debug.Log("Start X - " + startX);

        keys[0] = new Vector3(startX, transform.position.y + numGen.GetRange(startX, slopeMinHeight, slopeMaxHeight), transform.position.z); 

        for (int i = 1; i < keys.Length; i++)
        {
            keys[i] = new Vector3(keys[i - 1].x + slopeLength, keys[i - 1].y + numGen.GetRange(keys[i - 1].x + slopeLength, slopeMinHeight, slopeMaxHeight), transform.position.z);
            Debug.Log("Key " + i + " X - " + keys[i].x);
        }

        terrain = new GameObject("Terrain");
        terrain.transform.position = keys[0];
        terrain.AddComponent<MeshFilter>();
        terrain.AddComponent<MeshRenderer>();

        terrain.GetComponent<MeshFilter>().mesh = GenerateMesh();
        terrain.GetComponent<MeshRenderer>().materials = textures;

        terrain.AddComponent<PolygonCollider2D>();
        terrain.GetComponent<PolygonCollider2D>().points = Generate2DCollision(terrain.GetComponent<MeshFilter>().mesh);
    }

    private Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();

        int segments = segmentsPerSlope * (keys.Length - 1);

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> trisFront = new List<int>();
        List<int> trisTop = new List<int>();

        if (TwoDimensional)
        {
            mesh.subMeshCount = 1;
            verts = new List<Vector3>((segments + 1) * 2);
            uv = new List<Vector2>(verts.Capacity);
            trisFront = new List<int>((verts.Capacity - 2) * 3);
        }
        else
        {
            mesh.subMeshCount = 2;
            verts = new List<Vector3>((segments + 1) * 3);
            uv = new List<Vector2>(verts.Capacity);
            trisFront = new List<int>((verts.Capacity - 3) * 2);
            trisTop = new List<int>((verts.Capacity - 3) * 2);
        }

        float cosSeg = Mathf.PI / (float)(segmentsPerSlope);
        float uvSeg = (float)segments / (float)textureWidth;
        float segmentWidth = slopeLength / (float)segmentsPerSlope;

        float z = 0;

        for (int i = 1; i < keys.Length; i++)
        {
            float yMid = (keys[i].y + keys[i - 1].y) / 2f;
            float amplitude = (keys[i - 1].y - keys[i].y) / 2f;

            // Set up Vertices and UV vectors
            for (int s = 0; s < segmentsPerSlope + 1; s++)
            {
                float x = ((((i - 1) * (segmentsPerSlope * segmentWidth)) + (float)s * segmentWidth) + (keys[0].x + slopeLength));
                float y = yMid + amplitude * Mathf.Cos(cosSeg * (float)s);
                verts.Add(new Vector3(x, y, z));
                uv.Add(new Vector2((float)s * uvSeg, 0.5f));
                /*
                 * Temporary quarentine of obstacle code.
                if (Random.Range(0f,1f) < obstacleChance)
                {
                    GameObject obstacle = (GameObject)GameObject.Instantiate(obstacles[Random.Range(0, obstacles.Length)], new Vector3(keys[start].x + x, y, zThickness / 2), Quaternion.identity);
                    spawnedObstacles.Enqueue(obstacle);
                }
                if (Random.Range(0f, 1f) < decorationChance)
                {
                    GameObject obstacle = (GameObject)GameObject.Instantiate(decorations[Random.Range(0, obstacles.Length)], new Vector3(keys[start].x + x, y, zThickness * Random.Range(0, 2)), Quaternion.identity);
                    spawnedDecorations.Enqueue(obstacle);
                }
                */
                verts.Add(new Vector3(x, y - yThickness, z));
                uv.Add(new Vector2((float)s * uvSeg, 0));

                if (!TwoDimensional)
                {
                    verts.Add(new Vector3(x, y, z + zThickness));
                    uv.Add(new Vector2((float)s * uvSeg, 1));
                }
            }

            // Create Triangles
            if (TwoDimensional)
            {
                for (int v = 0; v < verts.Count - 2; v += 2)
                {
                    trisFront.Add(v + 1);
                    trisFront.Add(v);
                    trisFront.Add(v + 2);

                    trisFront.Add(v + 1);
                    trisFront.Add(v + 2);
                    trisFront.Add(v + 3);
                }
            }
            else
            {
                for (int v = 0; v < verts.Count - 3; v += 3)
                {
                    trisTop.Add(v);
                    trisTop.Add(v + 2);
                    trisTop.Add(v + 5);

                    trisTop.Add(v);
                    trisTop.Add(v + 5);
                    trisTop.Add(v + 3);

                    trisFront.Add(v + 1);
                    trisFront.Add(v);
                    trisFront.Add(v + 3);

                    trisFront.Add(v + 1);
                    trisFront.Add(v + 3);
                    trisFront.Add(v + 4);
                }
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.SetTriangles(trisFront.ToArray(), 0);
        if (!TwoDimensional)
        {
            mesh.SetTriangles(trisTop.ToArray(), 1);
        }
        mesh.uv = uv.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        return mesh;
    }

    /// <summary>
    /// Generates a physics2D collision mesh based on the provided mesh.
    /// </summary>
    /// <param name="parentMesh">The mesh to use to generate the collision mesh.</param>
    /// <returns>An array of vectors that represent the collision mesh.</returns>
    private Vector2[] Generate2DCollision(Mesh parentMesh)
    {
        if (!TwoDimensional)
        {
            List<Vector2> vectors = new List<Vector2>();
            for (int i = 0; i < parentMesh.vertexCount; i += 3)
            {
                vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
            }
            for (int i = parentMesh.vertexCount - 2; i > 0; i -= 3)
            {
                vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
            }

            return vectors.ToArray();
        }
        else
        {
            List<Vector2> vectors = new List<Vector2>();
            for (int i = 0; i < parentMesh.vertexCount; i += 2)
            {
                vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
            }
            for (int i = parentMesh.vertexCount - 1; i > 0; i -= 2)
            {
                vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
            }

            return vectors.ToArray();
        }
    }

    public void Update()
    {
        float segmentWidth = slopeLength / (float)segmentsPerSlope;
        float deltaX = mainCamera.transform.position.x - previousLocation.x;

        segmentsMoved += deltaX / segmentWidth;

        UpdateKeys();
        if (additionalSegments != 0 || segmentsMoved != 0)
        {
            terrain.GetComponent<MeshFilter>().mesh = GenerateMesh();
            terrain.GetComponent<PolygonCollider2D>().points = Generate2DCollision(terrain.GetComponent<MeshFilter>().mesh);
        }

        if (segmentsMoved > 0)
        {
            segmentsMoved = segmentsMoved - Mathf.Floor(segmentsMoved);
        }
        if (segmentsMoved < 0)
        {
            segmentsMoved = segmentsMoved - Mathf.Ceil(segmentsMoved);
        }
        if (additionalSegments > 0)
        {
            additionalSegments = additionalSegments - Mathf.Floor(additionalSegments);
        }
        if (additionalSegments < 0)
        {
            additionalSegments = additionalSegments - Mathf.Ceil(additionalSegments);
        }
        previousLocation = mainCamera.transform.position;
    }

    private void UpdateKeys()
    {
        float segmentWidth = slopeLength / (float)segmentsPerSlope;
        float left = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, (transform.position.z + zThickness) - mainCamera.transform.position.z)).x;
        float right = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, (transform.position.z + zThickness) - mainCamera.transform.position.z)).x;
        float screenWidth = right - left;

        Debug.Log("screenWidth - " + screenWidth);

        int numSegments = (int)(screenWidth / slopeLength) + 2;

        Vector3[] newKeys = new Vector3[numSegments];

        Vector3 closest = GetClosestKeyAtLocation(left);
        if (closest.x >= left)
        {
            closest = GetClosestKeyAtLocation(left - slopeLength);
        }

        newKeys[0] = closest;

        for (int i = 1; i < newKeys.Length; i++)
        {
            newKeys[i] = new Vector3(newKeys[i - 1].x + slopeLength, newKeys[i - 1].y + numGen.GetRange(newKeys[i - 1].x + slopeLength, slopeMinHeight, slopeMaxHeight), transform.position.z);
        }

        keys = newKeys;

        float deltaScreen = screenWidth - previousScreenWidth;
        additionalSegments += (deltaScreen/2f) / segmentWidth;
    }

    private Vector3 GetClosestKeyAtLocation(float x)
    {
        float intervals = Mathf.Round((x - startX) / slopeLength);

        float goalX = startX + (intervals * slopeLength);

        Vector3 closest = new Vector3(goalX, numGen.GetRange(goalX, slopeMinHeight, slopeMaxHeight), transform.position.z);

        return closest;
    }

    public void RemoveObjects(Camera camera)
    {
        if (useCameraX)
        {
            if (spawnedObstacles.Count > 0 && camera.transform.position.x > spawnedObstacles.Peek().transform.position.x + slopeLength)
            {
                DestroyImmediate(spawnedObstacles.Dequeue());
            }
            if (spawnedDecorations.Count > 0 && camera.transform.position.x > spawnedDecorations.Peek().transform.position.x + slopeLength)
            {
                DestroyImmediate(spawnedDecorations.Dequeue());
            }
        }
        else
        {
            if (spawnedObstacles.Count > 0 && camera.ScreenToWorldPoint(new Vector3(-camera.pixelWidth, 0, spawnedObstacles.Peek().transform.position.z - camera.transform.position.z)).x > spawnedObstacles.Peek().transform.position.x + (slopeLength / 2))
            {
                DestroyImmediate(spawnedObstacles.Dequeue());
            }
            if (spawnedDecorations.Count > 0 && camera.ScreenToWorldPoint(new Vector3(-camera.pixelWidth, 0, spawnedDecorations.Peek().transform.position.z - camera.transform.position.z)).x > spawnedDecorations.Peek().transform.position.x + (slopeLength / 2))
            {
                DestroyImmediate(spawnedDecorations.Dequeue());
            }
        }
    }



    /// <summary>
    /// Old Method
    /// Generates a slope from the end of the last slope to a new location following the variables.
    /// If there are no keys, it generates them.
    /// </summary>
    /// <returns>Returns a gameobject representing one slope.</returns>
    //public GameObject Generate()
    //{
    //    if (keys == null)
    //    {
    //        keys = new Vector3[2];

    //        keys[0] = new Vector3(-Random.Range(deltaXMin, deltaXMax), 0);
    //        keys[1] = new Vector3(keys[0].x + Random.Range(deltaXMin, deltaXMax), keys[0].y + Random.Range(deltaYMin, deltaYMax));
    //    }
    //    else
    //    {
    //        GenerateKeys();
    //    }

    //    GameObject terrain = new GameObject("Terrain");
    //    terrain.transform.position = new Vector3(keys[0].x, 0);
    //    terrain.AddComponent<MeshFilter>();
    //    terrain.AddComponent<MeshRenderer>();

    //    terrain.GetComponent<MeshFilter>().mesh = GenerateMesh(0);
    //    terrain.GetComponent<MeshRenderer>().materials = textures;

    //    terrain.AddComponent<PolygonCollider2D>();
    //    terrain.GetComponent<PolygonCollider2D>().points = Generate2DCollision(terrain.GetComponent<MeshFilter>().mesh);

    //    return terrain;
    //}

    /// <summary>
    /// Creates either a 2D or 3D mesh following the parameters.
    /// </summary>
    /// <param name="start">The key to start on. Used for potential larger key arrays.</param>
    /// <returns>A mesh that represents a curve between the two keys and with the designated thickness.</returns>
    //private Mesh GenerateMesh(int start)
    //{
    //    Mesh mesh = new Mesh();

    //    if (!TwoDimensional)
    //    {
    //        mesh.subMeshCount = 2;
    //        Vector3[] verts = new Vector3[(segments + 1) * 3];
    //        Vector2[] uv = new Vector2[verts.Length];
    //        int[] trisFront = new int[(verts.Length - 3) * 2];
    //        int[] trisTop = new int[(verts.Length - 3) * 2];
    //        float cosSeg = Mathf.PI / (float)(segments);
    //        float uvSeg = (float)segments / (float)textureWidth;
    //        float segmentWidth = (keys[start + 1].x - keys[start].x) / (float)segments;
    //        float yMid = (keys[start + 1].y + keys[start].y) / 2f;
    //        float amplitude = (keys[start].y - keys[start + 1].y) / 2f;
    //        int vertIndex = 0;

    //        // Set up Vertices and UV vectors
    //        for (int i = 0; i < segments + 1; i++)
    //        {
    //            float x = ((float)i * segmentWidth);
    //            float y = yMid + amplitude * Mathf.Cos(cosSeg * (float)i);
    //            verts[vertIndex] = new Vector3(x, y);
    //            uv[vertIndex] = new Vector2((float)i * uvSeg, 0.5f);
    //            vertIndex++;
    //            if (Random.Range(0f,1f) < obstacleChance)
    //            {
    //                GameObject obstacle = (GameObject)GameObject.Instantiate(obstacles[Random.Range(0, obstacles.Length)], new Vector3(keys[start].x + x, y, zThickness / 2), Quaternion.identity);
    //                spawnedObstacles.Enqueue(obstacle);
    //            }
    //            if (Random.Range(0f, 1f) < decorationChance)
    //            {
    //                GameObject obstacle = (GameObject)GameObject.Instantiate(decorations[Random.Range(0, obstacles.Length)], new Vector3(keys[start].x + x, y, zThickness * Random.Range(0, 2)), Quaternion.identity);
    //                spawnedDecorations.Enqueue(obstacle);
    //            }
    //            verts[vertIndex] = new Vector3(x, y - yThickness);
    //            uv[vertIndex] = new Vector2((float)i * uvSeg, 0);
    //            vertIndex++;
    //            verts[vertIndex] = new Vector3(x, y, zThickness);
    //            uv[vertIndex] = new Vector2((float)i * uvSeg, 1);
    //            vertIndex++;
    //        }

    //        // Create Triangles
    //        int v = 0;
    //        for (int t = 0; t < trisFront.Length; t += 6)
    //        {
    //            trisTop[t] = v;
    //            trisTop[t + 1] = v + 2;
    //            trisTop[t + 2] = v + 5;

    //            trisTop[t + 3] = v;
    //            trisTop[t + 4] = v + 5;
    //            trisTop[t + 5] = v + 3;

    //            trisFront[t] = v + 1;
    //            trisFront[t + 1] = v;
    //            trisFront[t + 2] = v + 3;

    //            trisFront[t + 3] = v + 1;
    //            trisFront[t + 4] = v + 3;
    //            trisFront[t + 5] = v + 4;

    //            v += 3;
    //        }

    //        // Finalize mesh
    //        mesh.vertices = verts;
    //        mesh.SetTriangles(trisFront, 0);
    //        mesh.SetTriangles(trisTop, 1);
    //        mesh.uv = uv;
    //        mesh.RecalculateBounds();
    //        mesh.RecalculateNormals();
    //        mesh.Optimize();

    //        return mesh;
    //    }
    //    else
    //    {
    //        mesh.subMeshCount = 1;
    //        Vector3[] verts = new Vector3[(segments + 1) * 2];
    //        Vector2[] uv = new Vector2[verts.Length];
    //        int[] trisFront = new int[(verts.Length - 2) * 3];
    //        float cosSeg = Mathf.PI / (float)(segments + 1);
    //        float uvSeg = (float)segments / (float)textureWidth;
    //        float segmentWidth = (keys[start + 1].x - keys[start].x) / (float)segments;
    //        float yMid = (keys[start + 1].y + keys[start].y) / 2f;
    //        float amplitude = (keys[start].y - keys[start + 1].y) / 2f;
    //        int vertIndex = 0;

    //        // Set up Vertices and UV vectors
    //        for (int i = 0; i < segments + 1; i++)
    //        {
    //            float x = ((float)i * segmentWidth);
    //            float y = yMid + amplitude * Mathf.Cos(cosSeg * (float)i);
    //            verts[vertIndex] = new Vector3(x, y);
    //            uv[vertIndex] = new Vector2((float)i * uvSeg, 1);
    //            if (Random.Range(0f, 1f) < obstacleChance)
    //            {
    //                GameObject obstacle = (GameObject)GameObject.Instantiate(obstacles[Random.Range(0, obstacles.Length)], new Vector3(keys[start].x + x, y, zThickness / 2), Quaternion.identity);
    //                spawnedObstacles.Enqueue(obstacle);
    //            }
    //            if (Random.Range(0f, 1f) < decorationChance)
    //            {
    //                GameObject obstacle = (GameObject)GameObject.Instantiate(decorations[Random.Range(0, obstacles.Length)], new Vector3(keys[start].x + x, y, zThickness * Random.Range(0, 2)), Quaternion.identity);
    //                spawnedDecorations.Enqueue(obstacle);
    //            }
    //            vertIndex++;
    //            verts[vertIndex] = new Vector3(x, y - yThickness);
    //            uv[vertIndex] = new Vector2((float)i * uvSeg, 0);
    //            vertIndex++;
    //        }

    //        // Create Triangles
    //        int v = 0;
    //        for (int t = 0; t < trisFront.Length; t += 6)
    //        {
    //            trisFront[t] = v + 1;
    //            trisFront[t + 1] = v;
    //            trisFront[t + 2] = v + 2;

    //            trisFront[t + 3] = v + 1;
    //            trisFront[t + 4] = v + 2;
    //            trisFront[t + 5] = v + 3;
    //            v += 2;
    //        }

    //        // Finalize mesh
    //        mesh.vertices = verts;
    //        mesh.SetTriangles(trisFront, 0);
    //        mesh.uv = uv;
    //        mesh.RecalculateBounds();
    //        mesh.RecalculateNormals();
    //        mesh.Optimize();

    //        return mesh;
    //    }
    //}

    /// <summary>
    /// Generates a physics2D collision mesh based on the provided mesh.
    /// </summary>
    /// <param name="parentMesh">The mesh to use to generate the collision mesh.</param>
    /// <returns>An array of vectors that represent the collision mesh.</returns>
    //private Vector2[] Generate2DCollision(Mesh parentMesh)
    //{
    //    if (!TwoDimensional)
    //    {
    //        List<Vector2> vectors = new List<Vector2>();
    //        for (int i = 0; i < parentMesh.vertexCount; i += 3)
    //        {
    //            vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
    //        }
    //        for (int i = parentMesh.vertexCount - 2; i > 0; i -= 3)
    //        {
    //            vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
    //        }

    //        return vectors.ToArray();
    //    }
    //    else
    //    {
    //        List<Vector2> vectors = new List<Vector2>();
    //        for (int i = 0; i < parentMesh.vertexCount; i += 2)
    //        {
    //            vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
    //        }
    //        for (int i = parentMesh.vertexCount - 1; i > 0; i -= 2)
    //        {
    //            vectors.Add(new Vector2(parentMesh.vertices[i].x, parentMesh.vertices[i].y));
    //        }

    //        return vectors.ToArray();
    //    }
    //}

    ///// <summary>
    ///// Creates new key points.
    ///// </summary>
    //private void GenerateKeys()
    //{
    //    keys[0] = keys[1];
    //    keys[1] = new Vector3(keys[0].x + Random.Range(deltaXMin, deltaXMax), keys[0].y + Random.Range(deltaYMin, deltaYMax));
    //}
}
