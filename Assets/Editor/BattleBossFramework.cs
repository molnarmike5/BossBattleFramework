using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

public class BattleBossFramework : EditorWindow
{
    private int counter = 0;
    private bool spawnGate;
    private GameObject bossPrefab;
    private GameObject playerPrefab;
    private AnimationClip walk;
    private AnimationClip run;
    private AnimationClip idle;
    private float walkSpeed;
    private float runSpeed;
    private float runningDistance;
    private float health;
    private bool runFlag;
    private float attackRange = 15f;
    private float speed;


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
        health = EditorGUILayout.FloatField("Health: ", health);
        idle = EditorGUILayout.ObjectField("Idle Animation: ", idle, typeof(AnimationClip), false) as AnimationClip;
        walk = EditorGUILayout.ObjectField("Walk Animation: ", walk, typeof(AnimationClip), false) as AnimationClip;
        speed = EditorGUILayout.FloatField("Speed: ", speed);
        runFlag = EditorGUILayout.Toggle("Include Running?", runFlag);
        if (runFlag)
        {
            run = EditorGUILayout.ObjectField("Run Animation: ", run, typeof(AnimationClip), false) as AnimationClip;
            runSpeed = EditorGUILayout.FloatField("Running Speed: ", runSpeed);
            runningDistance = EditorGUILayout.FloatField("Distance for running", runningDistance);
        }
        attackRange = EditorGUILayout.FloatField("Attack Range: ", attackRange);
        
        
        
        //spawnGate = EditorGUILayout.Toggle("Generate Boss Battle Gate: ", spawnGate);

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
            var controller = GameObject.Find(bossPrefab.name).AddComponent<BossController>();
            controller.Constructor(playerPrefab, speed, attackRange, runSpeed, runningDistance, runFlag, health, idle, walk, run);
            initializeAnimator();
            Close();
        }
        
    }

    void initializeAnimator()
    {

            AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath("Assets/Editor/BossController.controller");
            
            animatorController.AddParameter("Idle", AnimatorControllerParameterType.Bool);
            animatorController.AddParameter("Walking", AnimatorControllerParameterType.Bool);

            var rootStateMachine = animatorController.layers[0].stateMachine;

            var stateIdle = rootStateMachine.AddState("Idle");
            var stateWalk = rootStateMachine.AddState("Walk");

            rootStateMachine.AddEntryTransition(stateIdle);

            var idletoWalk = stateIdle.AddTransition(stateWalk);
            idletoWalk.AddCondition(AnimatorConditionMode.If, 0, "Walking");
            
            var walktoIdle = stateWalk.AddTransition(stateIdle);
            walktoIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");
            
            stateIdle.motion = idle;
            stateWalk.motion = walk;

            rootStateMachine.defaultState = stateIdle;

            if (runFlag)
            {
                var stateRun = rootStateMachine.AddState("Run");
                animatorController.AddParameter("Running", AnimatorControllerParameterType.Bool);
                
                var idletoRun = stateIdle.AddTransition(stateRun);
                idletoRun.AddCondition(AnimatorConditionMode.If, 0, "Running");
                
                walktoIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Running");
                
                var walktoRun = stateWalk.AddTransition(stateRun);
                walktoRun.AddCondition(AnimatorConditionMode.If, 0, "Running");

                var runtoWalk = stateRun.AddTransition(stateWalk);
                runtoWalk.AddCondition(AnimatorConditionMode.If, 0, "Walking");
                var runtoIdle = stateRun.AddTransition(stateIdle);
                runtoIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Running");
                runtoIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");
                
                stateRun.motion = run;
            }
            
            GameObject.Find(bossPrefab.name).AddComponent<Animator>().runtimeAnimatorController = animatorController;

    }


}
