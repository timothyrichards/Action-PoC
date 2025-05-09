using UnityEngine;
using UnityEditor;
using System.IO;
using SpacetimeDB.Types;

public class BuildingPieceCreatorTool : EditorWindow
{
    private string pieceName = "";
    private DbBuildingPieceType selectedPieceType;
    private GameObject templatePrefab;
    private BuildingPieceDatabase buildingPieceDatabase;
    private SerializedObject serializedDatabase;

    [MenuItem("Tools/Building/Building Piece Creator")]
    public static void ShowWindow()
    {
        GetWindow<BuildingPieceCreatorTool>("Building Piece Creator");
    }

    private void OnEnable()
    {
        // Try to find the building piece database
        string[] guids = AssetDatabase.FindAssets("t:BuildingPieceDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            buildingPieceDatabase = AssetDatabase.LoadAssetAtPath<BuildingPieceDatabase>(path);
            if (buildingPieceDatabase != null)
            {
                serializedDatabase = new SerializedObject(buildingPieceDatabase);
            }
        }
    }

    private void OnGUI()
    {
        if (buildingPieceDatabase == null)
        {
            EditorGUILayout.HelpBox("Building Piece Database not found!", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Building Piece Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Template Prefab (first field)
        EditorGUI.BeginChangeCheck();
        templatePrefab = (GameObject)EditorGUILayout.ObjectField("Template Prefab", templatePrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            // Auto-fill name and type when template is selected
            if (templatePrefab != null)
            {
                var buildingPiece = templatePrefab.GetComponent<BuildingPiece>();
                if (buildingPiece != null)
                {
                    selectedPieceType = buildingPiece.pieceType;
                    pieceName = templatePrefab.name;
                }
            }
        }

        // Piece Name (with template name as default)
        pieceName = EditorGUILayout.TextField("Piece Name", pieceName);

        // Piece Type (with template type as default)
        selectedPieceType = (DbBuildingPieceType)EditorGUILayout.EnumPopup("Piece Type", selectedPieceType);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Create Building Piece"))
        {
            CreateBuildingPiece();
        }
    }

    private void CreateBuildingPiece()
    {
        if (string.IsNullOrEmpty(pieceName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a piece name.", "OK");
            return;
        }

        if (templatePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a template prefab.", "OK");
            return;
        }

        // Check if a prefab with this name already exists in any building piece folder
        foreach (DbBuildingPieceType pieceType in System.Enum.GetValues(typeof(DbBuildingPieceType)))
        {
            string checkPath = Path.Combine("Assets/Prefabs/Building", pieceType.ToString(), $"{pieceName}.prefab");
            if (File.Exists(checkPath))
            {
                EditorUtility.DisplayDialog("Error",
                    $"A prefab named '{pieceName}' already exists in the {pieceType} folder. Please choose a different name.",
                    "OK");
                return;
            }
        }

        // Create the directory structure if it doesn't exist
        string baseDir = "Assets/Prefabs/Building";
        string typeDir = Path.Combine(baseDir, selectedPieceType.ToString());

        if (!Directory.Exists(typeDir))
        {
            Directory.CreateDirectory(typeDir);
        }

        // Calculate the next variant ID by finding the highest ID across all piece types
        uint nextVariantId = 1;
        foreach (var piece in buildingPieceDatabase.GetPrefabs())
        {
            nextVariantId = System.Math.Max(nextVariantId, piece.variantId + 1);
        }

        // Create a deep copy of the template prefab
        GameObject newPiece = Instantiate(templatePrefab);
        newPiece.name = pieceName; // Set the name before creating the prefab asset

        // Configure the BuildingPiece component
        BuildingPiece buildingPiece = newPiece.GetComponent<BuildingPiece>();
        if (buildingPiece == null)
        {
            buildingPiece = newPiece.AddComponent<BuildingPiece>();
        }

        buildingPiece.pieceType = selectedPieceType;
        buildingPiece.variantId = nextVariantId;

        // Create the prefab asset as a completely new prefab
        string prefabPath = Path.Combine(typeDir, $"{pieceName}.prefab");
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(newPiece, prefabPath);
        DestroyImmediate(newPiece);

        // Add to database
        buildingPieceDatabase.AddPrefabVariant(newPrefab.GetComponent<BuildingPiece>(), nextVariantId);

        EditorUtility.SetDirty(buildingPieceDatabase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Store success message variables before clearing form
        string finalPieceName = pieceName;
        var finalPieceType = selectedPieceType;

        // Clear the form
        pieceName = "";
        templatePrefab = null;

        // Show success message with stored variables
        EditorUtility.DisplayDialog("Success",
            $"Created new {finalPieceType} piece '{finalPieceName}' with variant ID {nextVariantId}\n\n" +
            $"Copy this message and send it to Tim if you want the piece to be added to the Server.", "OK");
    }
}
