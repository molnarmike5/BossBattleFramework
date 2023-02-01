using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MoveSet : EditorWindow
{

    public void init(int index,int phase, BattleBossFramework parent, List<Animation> moves)
    {
        this.index = index;
        this.parent = parent;
        this.moves = moves;
        this.phase = phase;
    }

    public BattleBossFramework parent;
    public int index;
    public List<Animation> moves = new List<Animation>();
    public int phase;
    
    public static void ShowWindow()
    {
        GetWindow(typeof(MoveSet));
    }

    private void OnGUI()
    {
        GUILayout.Label("Manage Moveset " + (index + 1));

        for (int i = 0; i < moves.Count; i++)
        {
            moves[i] = EditorGUILayout.ObjectField("Move " + (i + 1 ), moves[i], typeof(GameObject)) as Animation;
        }

        if (GUILayout.Button("Add New Move"))
        {
            moves.Add(null);
        }

        if (GUILayout.Button("Save Moves"))
        {
            parent.phasesList[phase][index] = this;
            Close();
        }
    }
}
