using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;


public class XMLToOBJ
{
    public static List<ModelsNode> NodesList = new List<ModelsNode>();
    public static List<ModelsTriangle> TrianglesList = new List<ModelsTriangle>();
    private static string filePath;

    [MenuItem("Vectorier/Miscellaneous/Model/Convert Model to OBJ")]
    public static void Comenzate()
    {
        Begin(false);
    }

    [MenuItem("Vectorier/Miscellaneous/Model/Convert Model to OBJ (Batch)")]
    public static void ComenzateBatch()
    {
        filePath = EditorUtility.OpenFolderPanel("Select folder with xmls", Application.dataPath + "/XML", string.Empty);
        Begin(true);
    }

    public static void Begin(bool batch = false)
    {
        NodesList.Clear();
        TrianglesList.Clear();
        if (!batch)
        {
            filePath = EditorUtility.OpenFilePanel("Select .xml file", Application.dataPath + "/XML", "xml");
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            var nodes = doc["Scene"]["Nodes"];
            var figures = doc["Scene"]["Figures"];

            foreach (XmlNode node in nodes)
            {
                var modelNode = new ModelsNode(node);
                NodesList.Add(modelNode);
            }

            foreach (XmlNode triangle in figures)
            {
                if (triangle.Attributes["Type"].Value == "Triangle")
                {
                    var modelTriangle = new ModelsTriangle(triangle);
                    TrianglesList.Add(modelTriangle);
                }
            }

            BeginConversion();
        }
        else
        {
            foreach (string file in Directory.GetFiles(filePath, "*.xml"))
            {
                NodesList.Clear();
                TrianglesList.Clear();

                filePath = file;
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                var nodes = doc["Scene"]["Nodes"];
                var figures = doc["Scene"]["Figures"];

                foreach (XmlNode node in nodes)
                {
                    var modelNode = new ModelsNode(node);
                    NodesList.Add(modelNode);
                }

                foreach (XmlNode triangle in figures)
                {
                    if (triangle.Attributes["Type"].Value == "Triangle")
                    {
                        var modelTriangle = new ModelsTriangle(triangle);
                        TrianglesList.Add(modelTriangle);
                    }
                }

                BeginConversion();
            }
        }
        Debug.Log("Obj created successfully");
        AssetDatabase.Refresh();
    }
    private static void BeginConversion()
    {
        StringBuilder sb = new StringBuilder();
        string outputFilePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".obj");

        foreach (ModelsNode modelnode in NodesList)
        {
            sb.AppendFormat("v {0} {1} {2}\n", modelnode.Position.x.ToString("R"), modelnode.Position.y.ToString("R"), modelnode.Position.z.ToString("F4"));
        }
        foreach (ModelsTriangle modeltriangle in TrianglesList)
        {
            sb.AppendFormat("f {0} {1} {2}\n", NodesList.IndexOf(modeltriangle.node1) + 1, NodesList.IndexOf(modeltriangle.node2) + 1, NodesList.IndexOf(modeltriangle.node3) + 1);
        }

        File.WriteAllText(outputFilePath, sb.ToString());
    }

    public static ModelsNode GetNode(string name)
    {
        for (int i = 0; i < NodesList.Count; i++)
        {
            if (NodesList[i].Name == name)
            {
                return NodesList[i];
            }
        }
        return null;
    }

    public XMLToOBJ RenderModel(string modelname)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(Application.dataPath + "/Resources/gamedata/run_data/models/" + modelname);

        var nodes = doc["Scene"]["Nodes"];
        var figures = doc["Scene"]["Figures"];

        foreach (XmlNode node in nodes)
        {
            var modelNode = new ModelsNode(node);
            NodesList.Add(modelNode);
        }

        return this;
    }

    public ModelsNode GetNode1(string name)
    {
        for (int i = 0; i < NodesList.Count; i++)
        {
            if (NodesList[i].Name == name)
            {
                return NodesList[i];
            }
        }
        return null;
    }
}

public class ModelsTriangle
{
    public ModelsNode node1, node2, node3;

    public ModelsTriangle(XmlNode node)
    {
        node1 = XMLToOBJ.GetNode(node.Attributes["Node1"].Value);
        node2 = XMLToOBJ.GetNode(node.Attributes["Node2"].Value);
        node3 = XMLToOBJ.GetNode(node.Attributes["Node3"].Value);
    }
}

public class ModelsNode
{
    public string Name;
    public Vector3 Position;

    public ModelsNode(XmlNode node)
    {
        Name = node.Name;
        Position = new Vector3(float.Parse(node.Attributes["X"].Value), float.Parse(node.Attributes["Y"].Value), float.Parse(node.Attributes["Z"].Value));
    }

    public ModelsNode(OBJNode objNode)
    {
        Position = objNode.Position;
        Name = objNode.Name;
    }
}
