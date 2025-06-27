using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class OBJToXML : MonoBehaviour
{
    private static string filePath;

    private static List<OBJNode> NodesAll = new List<OBJNode>();
    private static List<OBJTriangle> TrianglesAll = new List<OBJTriangle>();

    public static int nodeCounter = 0;
    public static int triangleCounter = 0;

    public string modelName;

    [Header("Node Settings")]
    public bool MacroNode;
    public float Mass = 10;
    public bool Collisible = true;
    public bool Cloth = false;

    [Header("ChildNode Settings")]
    public string childNodesModel = "0.xml";
    public List<string> childNodes = new List<string>();

    public static XMLToOBJ model0;

    public static OBJToXML Current
    {
        get
        {
            return FindObjectOfType<OBJToXML>();
        }
    }

    [MenuItem("Vectorier/Miscellaneous/Model/Convert OBJ to XML")]
    private static void Begin()
    {
        NodesAll.Clear();
        TrianglesAll.Clear();
        nodeCounter = 0;
        triangleCounter = 0;

        filePath = EditorUtility.OpenFilePanel("Select .obj file", Application.dataPath + "/XML", "obj");

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            switch (parts[0])
            {
                case "v":
                    nodeCounter++;
                    var nodey = new OBJNode(parts);
                    NodesAll.Add(nodey);
                    break;

                case "f":
                    triangleCounter++;
                    var triangy = new OBJTriangle(parts);
                    TrianglesAll.Add(triangy);
                    break;

                default:
                    continue;
            }
        }

        ComenzateThis();
    }

    private static void ComenzateThis()
    {
        string outputFilePath = Path.Combine(Path.GetDirectoryName(filePath), Current.modelName + ".xml");

        XmlDocument xdoc = new XmlDocument();
        xdoc.CreateXmlDeclaration("1.0", "UTF-8", null);
        model0 = new XMLToOBJ().RenderModel(Current.childNodesModel);

        var scene = xdoc.CreateElement("Scene");
        var nodes = xdoc.CreateElement("Nodes");
        var figures = xdoc.CreateElement("Figures");

        if (Current.MacroNode)
        {
            if (Current.childNodes.Count == 0)
            {
                Debug.LogError("Cant be a macronode without childnodes");
                return;
            }
        }

        foreach (var objnode in NodesAll)
        {
            var node = Current.MacroNode ? CreateMacroNodeNode(objnode, xdoc) : CreateNodeNode(objnode, xdoc);
            nodes.AppendChild(node);
        }
        foreach (var trinode in TrianglesAll)
        {
            var figure = CreateTriangleNode(trinode, xdoc);
            figures.AppendChild(figure);
        }

        scene.AppendChild(nodes);
        scene.AppendChild(figures);
        xdoc.AppendChild(scene);

        xdoc.Save(outputFilePath);
        Debug.Log("Xml created successfully");
        AssetDatabase.Refresh();
    }

    private static XmlNode CreateNodeNode(OBJNode objnode, XmlDocument xdoc)
    {
        var node = xdoc.CreateElement(objnode.Name);
        node.SetAttribute("Type", "Node");

        node.SetAttribute("X", (-objnode.Position.x).ToString());
        node.SetAttribute("Y", objnode.Position.y.ToString());
        node.SetAttribute("Z", objnode.Position.z.ToString());

        node.SetAttribute("Mass", Current.Mass.ToString());
        node.SetAttribute("Fixed", "0");
        node.SetAttribute("PinFixed", "0");
        node.SetAttribute("Visible", "1");
        node.SetAttribute("Collisible", Convert.ToInt32(Current.Collisible).ToString());
        node.SetAttribute("Passive", "0");
        node.SetAttribute("Cloth", Convert.ToInt32(Current.Cloth).ToString());
        node.SetAttribute("Rank", "0");

        return node;
    }

    private static XmlNode CreateMacroNodeNode(OBJNode objnode, XmlDocument xdoc)
    {

        var node = xdoc.CreateElement(objnode.Name);
        node.SetAttribute("Type", "MacroNode");

        node.SetAttribute("X", (-objnode.Position.x).ToString());
        node.SetAttribute("Y", objnode.Position.y.ToString());
        node.SetAttribute("Z", objnode.Position.z.ToString());

        node.SetAttribute("Mass", "10");
        node.SetAttribute("Fixed", "0");
        node.SetAttribute("Visible", "1");
        node.SetAttribute("Selectable", "1");
        node.SetAttribute("Rank", "0");
        node.SetAttribute("NodesCount", Current.childNodes.Count.ToString());

        for (int i = 0; i < Current.childNodes.Count; i++)
        {
            node.SetAttribute("ChildNode" + (i + 1), Current.childNodes[i]);
        }

        Vector3 fixedpos = objnode.Position;
        fixedpos.x = -objnode.Position.x;

        try
        {
            if (Current.childNodes.Count > 3)
            {
                LCCCalculator.CalculateLCC4(model0.GetNode1(Current.childNodes[0]).Position, model0.GetNode1(Current.childNodes[1]).Position, model0.GetNode1(Current.childNodes[2]).Position, model0.GetNode1(Current.childNodes[3]).Position, fixedpos, out float lcc1, out float lcc2, out float lcc3, out float lcc4);
                node.SetAttribute("LCC1", lcc1.ToString());
                node.SetAttribute("LCC2", lcc2.ToString());
                node.SetAttribute("LCC3", lcc3.ToString());
                node.SetAttribute("LCC4", lcc4.ToString());
            }
            else if (Current.childNodes.Count == 3)
            {
                LCCCalculator.CalculateLCC3(model0.GetNode1(Current.childNodes[0]).Position, model0.GetNode1(Current.childNodes[1]).Position, model0.GetNode1(Current.childNodes[2]).Position, fixedpos, out float lcc1, out float lcc2, out float lcc3);
                node.SetAttribute("LCC1", lcc1.ToString());
                node.SetAttribute("LCC2", lcc2.ToString());
                node.SetAttribute("LCC3", lcc3.ToString());
            }
            else if (Current.childNodes.Count == 2)
            {
                LCCCalculator.CalculateLCC2(model0.GetNode1(Current.childNodes[0]).Position, model0.GetNode1(Current.childNodes[1]).Position, fixedpos, out float lcc1, out float lcc2);
                node.SetAttribute("LCC1", lcc1.ToString());
                node.SetAttribute("LCC2", lcc2.ToString());
            }
            else if (Current.childNodes.Count == 1)
            {
                node.SetAttribute("LCC1", "1");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }

        return node;
    }

    private static XmlNode CreateTriangleNode(OBJTriangle trinode, XmlDocument xdoc)
    {
        var node = xdoc.CreateElement(trinode.Name);
        node.SetAttribute("Type", "Triangle");
        node.SetAttribute("Shading", "0");

        node.SetAttribute("Node1", trinode.node1.Name);
        node.SetAttribute("Node2", trinode.node2.Name);
        node.SetAttribute("Node3", trinode.node3.Name);

        return node;
    }

    public static OBJNode GetNodeAgaiin(int index)
    {
        for (int i = 0; i < NodesAll.Count; i++)
        {
            if (NodesAll[i].MyCounter == index)
            {
                return NodesAll[i];
            }
        }
        return null;
    }

}

public class OBJNode
{
    public Vector3 Position;
    public string Name;
    public int MyCounter;

    public OBJNode(string[] node)
    {
        MyCounter = OBJToXML.nodeCounter;
        Position = new Vector3(float.Parse(node[1]), float.Parse(node[2]), float.Parse(node[3]));
        if (MyCounter > 1)
        {
            Name = OBJToXML.Current.modelName + "-Node" + MyCounter;
        }
        else
        {
            Name = OBJToXML.Current.MacroNode ? OBJToXML.Current.modelName + "-Node" + MyCounter : "NPivot";
        }
    }
}

public class OBJTriangle
{
    public OBJNode node1, node2, node3;
    public string Name;

    public OBJTriangle(string[] node)
    {
        node1 = OBJToXML.GetNodeAgaiin(int.Parse(node[1]));
        node2 = OBJToXML.GetNodeAgaiin(int.Parse(node[2]));
        node3 = OBJToXML.GetNodeAgaiin(int.Parse(node[3]));

        Name = OBJToXML.Current.modelName + "-Triangle-" + OBJToXML.triangleCounter;
    }
}

[CustomEditor(typeof (OBJToXML))]
public class maybenotthateditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        OBJToXML component = (OBJToXML)target;

        if (component.childNodes.Count > 4)
        {
            component.childNodes.RemoveAt(4);
        }
    }
}
