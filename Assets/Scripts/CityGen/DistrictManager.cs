using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Point
{
    public Vector3 Position;
    public List<District> Districts = new List<District>();

    public Point(Vector3 position)
    {
        Position = position;
    }
}

[System.Serializable]
public class District
{
    public string Name;
    public string Type;
    public float Population;
    public List<Point> Points = new List<Point>();

    public District(string name, string type)
    {
        Name = name;
        Type = type;
        Population = 0f;
    }
}

public class DistrictManager : MonoBehaviour
{
    [Header("Visualization Settings")]
    public float visualizationHeight = 5f;
    public float cubeSize = 1f;
    
    [Header("Debug Data")]
    [SerializeField]
    private List<District> allDistricts = new List<District>();
    
    private GameObject visualizationParent;

    private TerrainGenerator terrainGenerator;

    // Setters
    public void setTerrainGenerator(TerrainGenerator _terrainGenerator)
    {
        terrainGenerator = _terrainGenerator;
    }

    [ContextMenu("Draw Districts")]
    public void VisualizeDistricts()
    {
        ClearVisualization();
        
        if(allDistricts.Count == 0)
        {
            Debug.LogWarning("No districts to visualize!");
            return;
        }

        visualizationParent = new GameObject("DistrictVisuals");

        foreach(var district in allDistricts)
        {
            Color districtColor = GenerateColorFromName(district.Name);
            VisualizeDistrict(district, districtColor);
        }
    }

    public void AddDistrict(District district)
    {
        allDistricts.Add(district);
    }

    void VisualizeDistrict(District district, Color color)
    {
        foreach(var point in district.Points)
        {
            CreateCube(
                new Vector3(point.Position.x, point.Position.y == 0 ? visualizationHeight : point.Position.y, point.Position.z),
                color,
                cubeSize,
                district.Name
            );
        }
    }

    // Helpers

    void CreateCube(Vector3 position, Color color, float size, string label)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = Vector3.one * size;
        cube.transform.SetParent(visualizationParent.transform);
        
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;

        // Add label text
        GameObject textObj = new GameObject("Label");
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = label;
        textMesh.fontSize = 20;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textObj.transform.SetParent(cube.transform);
        textObj.transform.localPosition = Vector3.zero;
    }

    Color GenerateColorFromName(string name)
    {
        // Generate consistent color from name hash
        int hash = name.GetHashCode();
        Color color = Color.HSVToRGB(
            Mathf.Abs(hash % 1000) / 1000f, 
            0.7f, 
            0.8f
        );
        return color;
    }

    // Context

    [ContextMenu("Clear Visualization")]
    public void ClearVisualization()
    {
        if(visualizationParent != null)
        {
            DestroyImmediate(visualizationParent);
        }
    }

    // Example district creation
    [ContextMenu("Create Test District")]
    void CreateTestDistrict()
    {
        District testDistrict = new District("Downtown", "Commercial");
        
        // Add sample points
        testDistrict.Points.Add(new Point(new Vector3(10, 0, 10)));
        testDistrict.Points.Add(new Point(new Vector3(20, 0, 10)));
        testDistrict.Points.Add(new Point(new Vector3(20, 0, 20)));
        testDistrict.Points.Add(new Point(new Vector3(10, 0, 20)));
        
        allDistricts.Add(testDistrict);
    }
}