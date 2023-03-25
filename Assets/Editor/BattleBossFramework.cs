using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Antlr3.Runtime.Misc;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.AI;
using UnityEngine.UI;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

public class BattleBossFramework : EditorWindow
{
    private int counter = 0;
    private bool spawnGate;
    public GameObject bossPrefab;
    private GameObject playerPrefab;
    private GameObject playerWeapon;
    private AnimationClip walk;
    private AnimationClip run;
    private AnimationClip idle;
    private AnimationClip spawn;
    private AnimationClip hit;
    private AnimationClip death;
    private float walkSpeed;
    private float runSpeed;
    private float runningDistance;
    private float health;
    private bool runFlag;
    private bool navFlag;
    private bool hitFlag;
    private bool deathFlag;
    private bool spawnFlag;
    private bool rbFlag;
    private int rbMass;
    private bool rbUseGravity;
    private bool prefabFlag;
    private string path = "";
    private string bossName;
    private CollisionDetectionMode rbCollisionDetection;
    private float attackRange = 15f;
    private float speed;
    private List<AnimatorStateMachine> attackStateMachines = new List<AnimatorStateMachine>();

    [SerializeField] public List<List<MoveSet>> phasesList = new List<List<MoveSet>>();
    private bool prefabTrigger;
    private bool distanceTrigger;
    private AnimatorController animatorController;
    Vector2 scrollPosition = Vector2.zero;



    [MenuItem("Tools/Boss Battle Creator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(BattleBossFramework));
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, true, false, GUILayout.Width(position.width), GUILayout.Height(position.height));
        var style = new GUIStyle();
        style.normal.background = EditorGUIUtility.whiteTexture;
        GUILayout.Label("Create New Boss Battle", EditorStyles.boldLabel);
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
        bossName = EditorGUILayout.TextField("Boss Name: ", bossName);
        bossPrefab = EditorGUILayout.ObjectField("Boss Prefab", bossPrefab, typeof(GameObject), true) as GameObject;
        playerPrefab = EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), true) as GameObject;
        playerWeapon = EditorGUILayout.ObjectField("Player Weapon", playerWeapon, typeof(GameObject), true) as GameObject;
        health = EditorGUILayout.FloatField("Health: ", health);
        idle = EditorGUILayout.ObjectField("Idle Animation: ", idle, typeof(AnimationClip), false) as AnimationClip;
        walk = EditorGUILayout.ObjectField("Walk Animation: ", walk, typeof(AnimationClip), false) as AnimationClip;
        speed = EditorGUILayout.FloatField("Speed: ", speed);
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
        spawnFlag = EditorGUILayout.Toggle("Include Spawn Animation?", spawnFlag);
        if (spawnFlag)
        {
            EditorGUI.indentLevel++;
            spawn = EditorGUILayout.ObjectField("Spawn Animation: ", spawn, typeof(AnimationClip), false) as AnimationClip;
            EditorGUI.indentLevel--;
        }
        runFlag = EditorGUILayout.Toggle("Include Running?", runFlag);
        if (runFlag)
        {
            EditorGUI.indentLevel++;
            run = EditorGUILayout.ObjectField("Run Animation: ", run, typeof(AnimationClip), false) as AnimationClip;
            runSpeed = EditorGUILayout.FloatField("Running Speed: ", runSpeed);
            runningDistance = EditorGUILayout.FloatField("Distance for running", runningDistance);
            EditorGUI.indentLevel--;
        }
        hitFlag = EditorGUILayout.Toggle("Include Hit Animation?", hitFlag);
        if (hitFlag)
        {
            EditorGUI.indentLevel++;
            hit = EditorGUILayout.ObjectField("Hit Animation: ", hit, typeof(AnimationClip), false) as AnimationClip;
            EditorGUI.indentLevel--;
        }
        deathFlag = EditorGUILayout.Toggle("Include Death Animation?", deathFlag);
        if (deathFlag)
        {
            EditorGUI.indentLevel++;
            death = EditorGUILayout.ObjectField("Death Animation: ", death, typeof(AnimationClip), false) as AnimationClip;
            EditorGUI.indentLevel--;
        }
        navFlag = EditorGUILayout.Toggle("Include NavMesh Movement?", navFlag);
        rbFlag = EditorGUILayout.Toggle("Include Rigidbody?", rbFlag);
        EditorGUILayout.LabelField("Rigidbody is required for Damage detection to work!");
        if (rbFlag)
        {
            EditorGUI.indentLevel++;
            rbMass = EditorGUILayout.IntField("Rigidbody Mass: ", rbMass);
            rbUseGravity = EditorGUILayout.Toggle("Use Gravity?", rbUseGravity);
            rbCollisionDetection = (CollisionDetectionMode) EditorGUILayout.EnumPopup("Collision Detection Mode: ", rbCollisionDetection);
            EditorGUI.indentLevel--;
        }
        attackRange = EditorGUILayout.FloatField("Attack Range: ", attackRange);
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
        EditorGUILayout.LabelField("Moves: ", GUILayout.MaxWidth(50));
        for (int p = 0; p < phasesList.Count; p++)
        {
            EditorGUILayout.LabelField("Phase " + (p + 1) + ":");
            GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
            for (int i = 0; i < phasesList[p].Count; i++)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Moveset " + (i + 1) + ":");
                if (GUILayout.Button("Edit Moveset " + (i + 1)))
                {
                    var newMoves = ScriptableObject.CreateInstance("MoveSet") as MoveSet;
                    newMoves.init(i, p, this, phasesList[p][i].moves);
                    newMoves.Show();
                }
                if (GUILayout.Button("Remove Moveset") && phasesList[p].Count > 0)
                {
                    phasesList[p].RemoveAt(i);
                    counter--;
                }
                GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Add New Moveset"))
            {
                var moves = CreateInstance("MoveSet") as MoveSet;
                moves.init(counter, p, this, new List<AnimationClip>());
                phasesList[p].Add(moves);
                counter++;
            }

        }

        if (GUILayout.Button("Add New Phase"))
        {
            phasesList.Add(new List<MoveSet>());
        }
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));

        //prefabFlag = EditorGUILayout.Toggle("Create Prefab?", prefabFlag);
        /*if (prefabFlag)
        {
            path = "Assets/Prefabs/BossPrefabs/";
            if (GUILayout.Button("Select Folder to Save Prefab in"))
            {
                path = EditorUtility.OpenFolderPanel("Select Folder to Save Prefab in", "", "");
            }
            EditorGUILayout.LabelField("Path: " + path);
        }*/
        if (GUILayout.Button("Instantiate Boss Prefab"))
        {
            Instantiate(bossPrefab).name = bossName;
        }
        
        if (GUILayout.Button("Create Boss Battle"))
        {
            if (GameObject.Find(bossName) != null)
            {
                var bossobj = GameObject.Find(bossName);
                bossobj.AddComponent<CapsuleCollider>();
                var controller = bossobj.AddComponent<BossController>();
                if (rbFlag)
                {
                    var rb = bossobj.AddComponent<Rigidbody>();
                    rb.mass = rbMass;
                    rb.useGravity = rbUseGravity;
                    rb.collisionDetectionMode = rbCollisionDetection;
                }
                initializeAnimator();
                attackMachines();
                List<bool> phases = new List<bool>();
                List<float> phasesHealth = new List<float>();
                phasesHealth.Add(health + 1);
                List<List<bool>> moves = new List<List<bool>>();
                for (int i = 0; i < phasesList.Count; i++)
                {
                    phases.Add(true);
                    moves.Add(new List<bool>());
                    if (i > 0)
                    {
                        phasesHealth.Add((float) (health / Math.Pow(2,i)));
                    }
                    for (int j = 0; j < phasesList[i].Count; j++)
                    {
                    
                        moves[i].Add(true);
                    }
                }
                controller.Constructor(playerPrefab, playerWeapon, speed, attackRange, runSpeed, runningDistance, runFlag, health, navFlag, idle,
                    walk, run, spawn, hit, death, attackStateMachines, phases, moves, phasesHealth);
                /*if (prefabFlag)
                {
                    PrefabUtility.SaveAsPrefabAsset(GameObject.Find(bossPrefab.name),  path + bossPrefab.name + ".prefab");
                }*/
                Close();
            }
            else
            {
                //Error message
                EditorUtility.DisplayDialog("Error", "Boss Prefab not found or Name already in use", "OK");
            }
            
        }
        EditorGUILayout.EndScrollView();
    }

    void initializeAnimator()
    {

        animatorController =
            AnimatorController.CreateAnimatorControllerAtPath("Assets/Editor/AnimatorController/BossController" + GetInstanceID() + ".controller");

        animatorController.AddParameter("Idle", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("Attacking", AnimatorControllerParameterType.Bool);

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
        var stateRun = rootStateMachine.AddState("Run");
        animatorController.AddParameter("Running", AnimatorControllerParameterType.Bool);
        
        foreach (var state in rootStateMachine.states)
        {
          if (state.state.name != stateRun.name)
          {
             var transDest = stateRun.AddTransition(state.state);
             transDest.AddCondition(AnimatorConditionMode.IfNot, 0, "Running");
             var transFrom = state.state.AddTransition(stateRun);
             transFrom.AddCondition(AnimatorConditionMode.If, 0, "Running");
          }
                
        }
        stateRun.motion = run;
        

        if (hitFlag)
        {
            var stateHit = rootStateMachine.AddState("Hit");
            animatorController.AddParameter("Hit", AnimatorControllerParameterType.Bool);

            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name != stateHit.name)
                {
                    var transDest = stateHit.AddTransition(state.state);
                    //transDest.AddCondition(AnimatorConditionMode.IfNot, 0, "Hit");
                    transDest.hasExitTime = true;
                    var transFrom = state.state.AddTransition(stateHit);
                    //transFrom.hasExitTime = true;
                    transFrom.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                }
                
            }
            stateHit.motion = hit;
        }

        if (deathFlag)
        {
            var stateDeath = rootStateMachine.AddState("Death");
            animatorController.AddParameter("Death", AnimatorControllerParameterType.Bool);

            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name != stateDeath.name)
                {
                    var transFrom = state.state.AddTransition(stateDeath);
                    transFrom.AddCondition(AnimatorConditionMode.If, 0, "Death");
                }
                
            }
            stateDeath.motion = death;
        }

        if (spawnFlag)
        {
            var stateSpawn = rootStateMachine.AddState("Spawn");
            rootStateMachine.AddEntryTransition(stateSpawn);
            rootStateMachine.defaultState = stateSpawn;
            var trans = stateSpawn.AddTransition(stateIdle);
            trans.hasExitTime = true;
            stateSpawn.motion = spawn;
        }
        
        GameObject.Find(bossName).AddComponent<Animator>().runtimeAnimatorController = animatorController;

    }

    void attackMachines()
    {
        var rootstateMachine = animatorController.layers[0].stateMachine;
        AnimatorStateMachine stateMachine = new AnimatorStateMachine();
        var hitState = new AnimatorState();
        for (int y = 0; y < phasesList.Count; y++)
        {
            for (int i = 0; i < phasesList[y].Count; i++)
            {
                stateMachine = rootstateMachine.AddStateMachine("Phase " + (y + 1) + "Moveset " + (i + 1));
                attackStateMachines.Add(stateMachine);
                animatorController.AddParameter(stateMachine.name, AnimatorControllerParameterType.Bool);
                
                //rootstateMachine.AddStateMachineTransition(rootstateMachine, stateMachine);
                for (int j = 0; j < phasesList[y][i].moves.Count; j++)
                {
                    var state = stateMachine.AddState("Attack " + (j + 1));
                    state.motion = phasesList[y][i].moves[j];
                    if (j == 0)
                    {
                        stateMachine.AddEntryTransition(state);
                        stateMachine.defaultState = state;
                    }
                    else
                    {
                        var previousState = stateMachine.states[j - 1].state;
                        var transition = previousState.AddTransition(state);
                        transition.hasExitTime = true;
                    }

                    if (j == phasesList[y][i].moves.Count - 1)
                    {
                        if (rootstateMachine.states[0].state.name == "Spawn")
                        {
                            var trans = state.AddTransition(rootstateMachine.states[1].state);
                            trans.hasExitTime = true;
                        }
                        else
                        {
                            var trans = state.AddTransition(rootstateMachine.states[0].state);
                            trans.hasExitTime = true;
                        }
                    }
                }

                if (hitFlag)
                {
                    hitState = stateMachine.AddState("Hit");
                    hitState.motion = hit;
                    foreach (var state in stateMachine.states)
                    {
                        var hitTrans = state.state.AddTransition(hitState);
                        hitTrans.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                        hitTrans.hasExitTime = false;
                        var hitToIdle = hitState.AddTransition(rootstateMachine.states[0].state);
                        hitToIdle.hasExitTime = true;
                    }
                }
            }

            foreach (var state in rootstateMachine.states)
            {
                foreach (var sm in attackStateMachines)
                {
                    if (state.state.name is not ("Spawn" or "Death" or "Hit"))
                    {
                        var trans = state.state.AddTransition(sm.states[0].state);
                        trans.AddCondition(AnimatorConditionMode.If, 0, sm.name);
                    }
                    
                }
            }
        }

    }
}
