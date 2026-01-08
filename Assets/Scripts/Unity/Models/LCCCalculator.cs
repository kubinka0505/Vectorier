using MathNet.Numerics.LinearAlgebra;
using UnityEditor;
using UnityEngine;

public class LCCCalculator : EditorWindow
{
    string chilNodesModel;

    string node1name, node2name, node3name, node4name;
    Vector3 node1, node2, node3, node4;
    Vector3 macroNode;
    Vector3 realposition;

    float lcc1, lcc2, lcc3, lcc4;

    [MenuItem("Tools/LCC Calculator")]
    public static void ShowWindow()
    {
        GetWindow<LCCCalculator>("LCC Calculator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Node Coordinates", EditorStyles.boldLabel);

        chilNodesModel = EditorGUILayout.TextField("Model", chilNodesModel);

        node1name = EditorGUILayout.TextField("Node 1", node1name);
        node2name = EditorGUILayout.TextField("Node 2", node2name);
        node3name = EditorGUILayout.TextField("Node 3", node3name);
        node4name = EditorGUILayout.TextField("Node 4", node4name);

        GUILayout.Space(10);
        GUILayout.Label("MacroNode Coordinates", EditorStyles.boldLabel);
        macroNode = EditorGUILayout.Vector3Field("MacroNode", macroNode);
        realposition = EditorGUILayout.Vector3Field("RealPosition", realposition);

        if (GUILayout.Button("Calculate LCC"))
        {
            CalculateLCC();
        }

        GUILayout.Space(10);
        GUILayout.Label("LCC Results", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("LCC1", lcc1.ToString("F6"));
        EditorGUILayout.LabelField("LCC2", lcc2.ToString("F6"));
        EditorGUILayout.LabelField("LCC3", lcc3.ToString("F6"));
        EditorGUILayout.LabelField("LCC4", lcc4.ToString("F6"));

        EditorGUILayout.LabelField("Sum", (lcc1 + lcc2 + lcc3 + lcc4).ToString("R"));
    }

    private void CalculateLCC()
    {

        var model = new XMLToOBJ().RenderModel(chilNodesModel);

        node1 = model.GetNode1(node1name).Position;
        node2 = model.GetNode1(node2name).Position;
        node3 = model.GetNode1(node3name).Position;
        node4 = model.GetNode1(node4name).Position;

        // Calculate the total signed volume
        double V = CalculateTetrahedronVolume(node1, node2, node3, node4);

        // Calculate signed volumes for each LCC
        double V1 = CalculateTetrahedronVolume(macroNode, node2, node3, node4);
        double V2 = CalculateTetrahedronVolume(node1, macroNode, node3, node4);
        double V3 = CalculateTetrahedronVolume(node1, node2, macroNode, node4);
        double V4 = CalculateTetrahedronVolume(node1, node2, node3, macroNode);

        // Calculate LCCs
        lcc1 = (float)(V1 / V);
        lcc2 = (float)(V2 / V);
        lcc3 = (float)(V3 / V);
        lcc4 = (float)(V4 / V);

        const double tolerance = 1e-4;
        lcc1 = Mathf.Abs(lcc1) < tolerance ? 0f : lcc1;
        lcc2 = Mathf.Abs(lcc2) < tolerance ? 0f : lcc2;
        lcc3 = Mathf.Abs(lcc3) < tolerance ? 0f : lcc3;
        lcc4 = Mathf.Abs(lcc4) < tolerance ? 0f : lcc4;

        Debug.Log(lcc1 + lcc2 + lcc3 + lcc4);

        Debug.Log(lcc1);
        Debug.Log(lcc2);
        Debug.Log(lcc3);
        Debug.Log(lcc4);

        Debug.Log(1 - (lcc1 + lcc2 + lcc3 + lcc4));
           
    }

    public static void CalculateLCC4(Vector3 node1, Vector3 node2, Vector3 node3, Vector3 node4, Vector3 macroNode, out float lcc1, out float lcc2, out float lcc3, out float lcc4)
    {

        // Calculate the total signed volume
        double V = CalculateTetrahedronVolume(node1, node2, node3, node4);

        // Calculate signed volumes for each LCC
        double V1 = CalculateTetrahedronVolume(macroNode, node2, node3, node4);
        double V2 = CalculateTetrahedronVolume(node1, macroNode, node3, node4);
        double V3 = CalculateTetrahedronVolume(node1, node2, macroNode, node4);
        double V4 = CalculateTetrahedronVolume(node1, node2, node3, macroNode);

        // Calculate LCCs
        lcc1 = (float)(V1 / V);
        lcc2 = (float)(V2 / V);
        lcc3 = (float)(V3 / V);
        lcc4 = (float)(V4 / V);

        const double tolerance = 1e-4;
        lcc1 = Mathf.Abs(lcc1) < tolerance ? 0f : lcc1;
        lcc2 = Mathf.Abs(lcc2) < tolerance ? 0f : lcc2;
        lcc3 = Mathf.Abs(lcc3) < tolerance ? 0f : lcc3;
        lcc4 = Mathf.Abs(lcc4) < tolerance ? 0f : lcc4;

    }

    public static void CalculateLCC3(Vector3 node1, Vector3 node2, Vector3 node3, Vector3 macroNode, out float lcc1, out float lcc2, out float lcc3)
    {

        // construct the A matrix using Node coordinates
        var A = Matrix<float>.Build.DenseOfArray(new float[,]
        {
            { node1.x, node2.x, node3.x },
            { node1.y, node2.y, node3.y },
            { node1.z, node2.z, node3.z }
        });

        // construct the B vector using Macronode coordinates
        var B = MathNet.Numerics.LinearAlgebra.Vector<float>.Build.Dense(new float[]
        {
            macroNode.x,
            macroNode.y,
            macroNode.z
        });

        // Apply the least squares method: LCC = (A^T * A)^-1 * A^T * B
        var AT = A.Transpose();
        var ATA = AT * A;
        var ATAInv = ATA.Inverse();
        var ATAInvAT = ATAInv * AT;
        var LCC = ATAInvAT * B;

        // Store results
        lcc1 = LCC[0];
        lcc2 = LCC[1];
        lcc3 = LCC[2];
    }

    public static void CalculateLCC2(Vector3 node1, Vector3 node2, Vector3 macroNode, out float lcc1, out float lcc2)
    {

        // construct the A matrix using Node coordinates
        var A = Matrix<float>.Build.DenseOfArray(new float[,]
        {
            { node1.x, node2.x},
            { node1.y, node2.y},
            { node1.z, node2.z}
        });

        // construct the B vector using Macronode coordinates
        var B = MathNet.Numerics.LinearAlgebra.Vector<float>.Build.Dense(new float[]
        {
            macroNode.x,
            macroNode.y,
            macroNode.z
        });

        // Apply the least squares method: LCC = (A^T * A)^-1 * A^T * B
        var AT = A.Transpose();
        var ATA = AT * A;
        var ATAInv = ATA.Inverse();
        var ATAInvAT = ATAInv * AT;
        var LCC = ATAInvAT * B;

        // Store results
        lcc1 = LCC[0];
        lcc2 = LCC[1];
    }

    // helper method for tetrahedron volume calculation
    private static double CalculateTetrahedronVolume(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        Vector3 u = p2 - p1;
        Vector3 v = p3 - p1;
        Vector3 w = p4 - p1;

        // The sign of the volume indicate if the point is "inside" or "outside" of the orientation
        return Vector3.Dot(u, Vector3.Cross(v, w)) / 6.0;
    }
}