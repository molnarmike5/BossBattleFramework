using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.AI;
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
    private float attackRange = 15f;
    private float speed;
    private List<AnimatorStateMachine> attackStateMachines = new List<AnimatorStateMachine>();

    [SerializeField] public List<List<MoveSet>> phasesList = new ListStack<List<MoveSet>>();
    private bool prefabTrigger;
    private bool distanceTrigger;
    private AnimatorController animatorController;



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
        spawnFlag = EditorGUILayout.Toggle("Include Spawn Animation?", spawnFlag);
        if (spawnFlag)
        {
            spawn = EditorGUILayout.ObjectField("Spawn Animation: ", spawn, typeof(AnimationClip), false) as AnimationClip;
        }
        runFlag = EditorGUILayout.Toggle("Include Running?", runFlag);
        if (runFlag)
        {
            run = EditorGUILayout.ObjectField("Run Animation: ", run, typeof(AnimationClip), false) as AnimationClip;
            runSpeed = EditorGUILayout.FloatField("Running Speed: ", runSpeed);
            runningDistance = EditorGUILayout.FloatField("Distance for running", runningDistance);
        }
        hitFlag = EditorGUILayout.Toggle("Include Hit Animation?", hitFlag);
        if (hitFlag)
        {
            hit = EditorGUILayout.ObjectField("Hit Animation: ", hit, typeof(AnimationClip), false) as AnimationClip;
        }
        deathFlag = EditorGUILayout.Toggle("Include Death Animation?", deathFlag);
        if (deathFlag)
        {
            death = EditorGUILayout.ObjectField("Death Animation: ", death, typeof(AnimationClip), false) as AnimationClip;
        }
        navFlag = EditorGUILayout.Toggle("Include NavMesh Movement?", navFlag);
        attackRange = EditorGUILayout.FloatField("Attack Range: ", attackRange);
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
                    newMoves.init(i, p, this, phasesList[p][i].moves);
                    newMoves.Show();
                }

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

        if (GUILayout.Button("Remove Existing Components"))
        {
            checkComponents();
        }

        if (GUILayout.Button("Create Boss Battle"))
        {
            var controller = GameObject.Find(bossPrefab.name).AddComponent<BossController>();
            controller.Constructor(playerPrefab, speed, attackRange, runSpeed, runningDistance, runFlag, health, navFlag, idle,
                walk, run, spawn, hit, death);
            initializeAnimator();
            attackMachines();
            //Close();
        }

    }

    void initializeAnimator()
    {

        animatorController =
            AnimatorController.CreateAnimatorControllerAtPath("Assets/Editor/BossController.controller");

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

        if (runFlag)
        {
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
        }

        if (hitFlag)
        {
            var stateHit = rootStateMachine.AddState("Hit");
            animatorController.AddParameter("Hit", AnimatorControllerParameterType.Bool);

            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name != stateHit.name)
                {
                    var transDest = stateHit.AddTransition(state.state);
                    transDest.AddCondition(AnimatorConditionMode.IfNot, 0, "Hit");
                    var transFrom = state.state.AddTransition(stateHit);
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
        
        GameObject.Find(bossPrefab.name).AddComponent<Animator>().runtimeAnimatorController = animatorController;

    }

    void attackMachines()
    {
        var rootstateMachine = animatorController.layers[0].stateMachine;
        AnimatorStateMachine stateMachine = new AnimatorStateMachine();

        for (int y = 0; y < phasesList.Count; y++)
        {
            for (int i = 0; i < phasesList[y].Count; i++)
            {
                stateMachine = rootstateMachine.AddStateMachine("Moveset " + (i + 1));
                attackStateMachines.Add(stateMachine);
                rootstateMachine.AddStateMachineTransition(rootstateMachine, stateMachine);
                for (int j = 0; j < phasesList[y][i].moves.Count; j++)
                {
                    var state = stateMachine.AddState("Attack " + (j + 1));
                    state.motion = phasesList[y][i].moves[j];
                    if (j == 0)
                    {
                        stateMachine.AddEntryTransition(state);
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

            }

            foreach (var state in rootstateMachine.states)
            {
                foreach (var sm in attackStateMachines)
                {
                    if (state.state.name is not ("Spawn" and "Death"))
                    {
                        var trans = state.state.AddTransition(sm.states[0].state);
                        trans.AddCondition(AnimatorConditionMode.If, 0, "Attacking");
                    }
                    
                }
            }
        }

    }

    void checkComponents()
    {
        for (int i = 0; i < GameObject.Find(bossPrefab.name).GetComponents<Component>().Length; i++)
        {
            if (GameObject.Find(bossPrefab.name).GetComponents<Component>()[i] is BossController or Animator or NavMeshAgent)
            {
                DestroyImmediate(GameObject.Find(bossPrefab.name).GetComponents<Component>()[i]);
            }
        }
    }
}
