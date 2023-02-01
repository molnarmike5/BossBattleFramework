using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Plastic.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEditor;

public class BattleBossFramework : EditorWindow
{
    private int counter = 0;
    private bool spawnGate;
    private GameObject bossPrefab;
    private GameObject playerPrefab;
    private float walkSpeed;
    private float runSpeed;
    private bool alwaysRun;
    
    
    //[SerializeField]
    //public List<MoveSet> moveset = new List<MoveSet>();

    [SerializeField] public List<List<MoveSet>> phasesList = new ListStack<List<MoveSet>>();
    private bool prefabTrigger;
    private bool distanceTrigger;



    [MenuItem("Tools/Boss Battle Creator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(BattleBossFramework));
    }

    private void OnGUI()
    {
        GUILayout.Label("Create New Boss Battle", EditorStyles.boldLabel);
        
        bossPrefab = EditorGUILayout.ObjectField("Boss Prefab", bossPrefab, typeof(GameObject), false) as GameObject;
        playerPrefab = EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), false) as GameObject;
        alwaysRun = EditorGUILayout.Toggle("Always run towards player:", alwaysRun);
        spawnGate = EditorGUILayout.Toggle("Generate Boss Battle Gate: ", spawnGate);

        EditorGUILayout.LabelField("Moves: ", GUILayout.MaxWidth(50));
        for (int p = 0; p < phasesList.Count; p++)
        {
            EditorGUILayout.LabelField("Phase " + (p + 1));
            for (int i = 0; i < phasesList[p].Count; i++)
            {
                EditorGUILayout.LabelField("Moveset " + (i + 1));
                if (GUILayout.Button("Edit Moveset " + (i + 1)))
                {
                    var newMoves = ScriptableObject.CreateInstance("MoveSet") as MoveSet;
                    newMoves.init(i,p, this, phasesList[p][i].moves);
                    newMoves.Show();
                }
                
            }
            if (GUILayout.Button("Add New Moveset"))
            {
                var moves = ScriptableObject.CreateInstance("MoveSet") as MoveSet;
                moves.init(counter, p, this, new List<Animation>());
                phasesList[p].Add(moves);
                counter++;
            }
            
        }
        if (GUILayout.Button("Add New Phase"))
        {
            phasesList.Add(new List<MoveSet>());
        }

        if (GUILayout.Button("Create Boss Battle"))
        {
            Close();
        }
        
    }
    
    
}
