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
    private float health = 1;
    private bool runFlag;
    private bool hitFlag;
    private bool deathFlag;
    private int deathtimer;
    private bool spawnFlag;
    private bool rbFlag;
    private bool navMovement;
    private int rbMass;
    private bool rbUseGravity;
    private bool prefabFlag;
    private string bossName;
    private float activateDistance;
    private CollisionDetectionMode rbCollisionDetection;
    private float attackRange = 15f;
    private float speed;
    private bool deathMessage = true;
    [SerializeField] private List<AnimatorStateMachine> attackStateMachines = new List<AnimatorStateMachine>();
    [SerializeField] public List<List<MoveSet>> phasesList = new List<List<MoveSet>>();
    private bool prefabTrigger;
    private bool distanceTrigger;
    private AnimatorController animatorController;
    Vector2 scrollPosition = Vector2.zero;
    private List<bool> phases = new List<bool>();
    private List<float> phasesHealth = new List<float>();
    private List<List<bool>> moves = new List<List<bool>>();
    private Avatar avatar;



    [MenuItem("Tools/Boss Battle Creator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(BattleBossFramework));
    }
    
    public List<List<MoveSet>> getPhasesList()
    {
        return phasesList;
    }
    private void OnGUI()
    {
        //Scroll View
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, true, false, GUILayout.Width(position.width), GUILayout.Height(position.height));
        var style = new GUIStyle();
        //White separating line for better UI
        style.normal.background = EditorGUIUtility.whiteTexture;
        GUILayout.Label("Create New Boss Battle", EditorStyles.boldLabel);
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
        //Framework Options
        bossName = EditorGUILayout.TextField("Boss Name: ", bossName);
        bossPrefab = EditorGUILayout.ObjectField("Boss Prefab", bossPrefab, typeof(GameObject), true) as GameObject;
        GUILayout.Label("Only if Animator Avatar exists, otherwise leave blank!", EditorStyles.boldLabel);
        avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(Avatar), true) as Avatar;
        playerPrefab = EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), true) as GameObject;
        playerWeapon = EditorGUILayout.ObjectField("Player Weapon", playerWeapon, typeof(GameObject), true) as GameObject;
        health = EditorGUILayout.FloatField("Health: ", health);
        idle = EditorGUILayout.ObjectField("Idle Animation: ", idle, typeof(AnimationClip), false) as AnimationClip;
        walk = EditorGUILayout.ObjectField("Walk Animation: ", walk, typeof(AnimationClip), false) as AnimationClip;
        speed = EditorGUILayout.FloatField("Speed: ", speed);
        activateDistance = EditorGUILayout.FloatField("Activate Distance: ", activateDistance);
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
        GUILayout.Label("Movement Options:", EditorStyles.boldLabel);
        GUILayout.Label("By default movement is simply handled by transform translate, movement options can be later enabled!", EditorStyles.boldLabel);
        navMovement = EditorGUILayout.Toggle("Include NavMesh Movement?", navMovement);
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
            deathtimer = EditorGUILayout.IntField("Death Timer in seconds(Until the disappearance of the game object): ", deathtimer);
            EditorGUI.indentLevel--;
        }
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
                    //Delegation and Reclaiming MoveSet Information
                    var newMoves = CreateInstance("MoveSet") as MoveSet;
                    newMoves.init(i, p, this, phasesList[p][i].moves);
                    DestroyImmediate(phasesList[p][i]);
                    newMoves.ShowModal();
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

            if (GUILayout.Button("Remove Phase") && phasesList.Count > 0)
            {
                phasesList.RemoveAt(p);
            }
            
        }

        if (GUILayout.Button("Add New Phase"))
        {
            phasesList.Add(new List<MoveSet>());
        }
        GUILayout.Box(GUIContent.none, style, GUILayout.Height(1));
        
        if (GUILayout.Button("Instantiate Boss Prefab"))
        {
            Instantiate(bossPrefab).name = bossName;
        }
        
        if (GUILayout.Button("Create Boss Battle"))
        {
            //Error Handling and checking for optimal use
            if (GameObject.Find(bossName) == null)
            {
                if (bossPrefab == null || bossName == "")
                {
                    EditorUtility.DisplayDialog("Error",
                        "Boss Prefab not found or name is empty!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Boss Prefab not found in scene! Make sure it is instantiated!", "OK");
                }

            } else if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player Prefab not found!", "OK");
            } 
            else if (playerWeapon == null)
            {
                EditorUtility.DisplayDialog("Error", "Player Weapon not found!", "OK");
            }
            else if (idle == null)
            {
                EditorUtility.DisplayDialog("Error", "Idle Animation not found!", "OK");
            }
            else if (walk == null)
            {
                EditorUtility.DisplayDialog("Error", "Walk Animation not found!", "OK");
            }
            else if (death == null && deathMessage)
            {
                EditorUtility.DisplayDialog("Error", "Death Animation not found! Are you sure? I'm only asking once :D", "OK");
                deathMessage = false;
            }
            else if (attackRange < 1)
            {
                EditorUtility.DisplayDialog("Error", "Attack Range must be greater than 0!", "OK");
            }
            else if (activateDistance < 1)
            {
                EditorUtility.DisplayDialog("Error", "Activate Distance must be greater than 0!", "OK");
            }
            else
            {
                //Creating Boss Battle
                var bossobj = GameObject.Find(bossName);
                bossobj.AddComponent<CapsuleCollider>();
                if (rbFlag)
                {
                    var rb = bossobj.AddComponent<Rigidbody>();
                    rb.mass = rbMass;
                    rb.useGravity = rbUseGravity;
                    rb.collisionDetectionMode = rbCollisionDetection;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }
                if (navMovement)
                {
                    bossobj.AddComponent<NavMeshAgent>().stoppingDistance = attackRange - 1;
                }
                var controller = bossobj.AddComponent<BossController>();
                initializeAnimator();
                initializeAttackMachines();
                controller.Constructor(playerPrefab, playerWeapon, speed, attackRange, runSpeed, runningDistance, runFlag, health, navMovement, idle,
                    walk, run, spawn, hit, death, deathtimer, attackStateMachines, phases, generateInspectorOptions(), phasesHealth, activateDistance);
                Close();
            }
            
        }
        EditorGUILayout.EndScrollView();
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnEnable()
    {
        EditorUtility.DisplayDialog("Information", "Some useful information for this framework to work!\n" +
                                                   "1. Make sure the suitable collider is attached to the player weapon for health system and collision detection to work!\n" +
                                                    "2. Currently getting hit by the boss is not included, since this is very special and dependant on a lot of factors!\n", "OK");
    }

    void initializeAnimator()
    {
        //Creating Animator Controller, only this way because of the way Unity handles Animator Controllers, otherwise there is no Base Layer and Root State Machine
        animatorController =
            AnimatorController.CreateAnimatorControllerAtPath("Assets/Editor/AnimatorController/BossController" + GetInstanceID() + ".controller");

        //Adding basic Parameters to the Animator Controller
        animatorController.AddParameter("Idle", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("Attacking", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("Running", AnimatorControllerParameterType.Bool);

        var rootStateMachine = animatorController.layers[0].stateMachine;
        Debug.Log(rootStateMachine.name);
        
        //Basic States and Transitions
        var stateIdle = rootStateMachine.AddState("Idle");
        var stateWalk = rootStateMachine.AddState("Walk");
        var stateRun = rootStateMachine.AddState("Run");
        
        rootStateMachine.AddEntryTransition(stateIdle);

        var idletoWalk = stateIdle.AddTransition(stateWalk);
        idletoWalk.AddCondition(AnimatorConditionMode.If, 0, "Walking");

        var walktoIdle = stateWalk.AddTransition(stateIdle);
        walktoIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");
        
        //Assigning Animations to the States
        idle.wrapMode = WrapMode.Loop;
        stateIdle.motion = idle;
        walk.wrapMode = WrapMode.Loop;
        stateWalk.motion = walk;
        //Declaring the Default State
        rootStateMachine.defaultState = stateIdle;

        //Generating resources for running, so it may be used later if wished for
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

        if (runFlag)
        {
            run.wrapMode = WrapMode.Loop;
        }
        
        stateRun.motion = run;
        
        //Generating resources for hit, so it may be used later if wished for has to be done later for attack state machines too
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
            hit.wrapMode = WrapMode.Once;
            stateHit.motion = hit;
        }
        
        //Generating resources for death, so it may be used later if wished for
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
            death.wrapMode = WrapMode.Once;
            stateDeath.motion = death;
        }
        
        //Generating resources for spawn, so it may be used later if wished for
        if (spawnFlag)
        {
            var stateSpawn = rootStateMachine.AddState("Spawn");
            rootStateMachine.AddEntryTransition(stateSpawn);
            rootStateMachine.defaultState = stateSpawn;
            var trans = stateSpawn.AddTransition(stateIdle);
            trans.hasExitTime = true;
            spawn.wrapMode = WrapMode.Once;
            stateSpawn.motion = spawn;
        }
        
        //Adding the Animator Controller to the Boss and setting the runtimeAnimatorController to the currently generated one
        GameObject.Find(bossName).AddComponent<Animator>().avatar = avatar;
        GameObject.Find(bossName).GetComponent<Animator>().runtimeAnimatorController = animatorController;

    }

    void initializeAttackMachines()
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
                //Parsing phasesList and converting every MoveSet to an Attack State Machine
                for (int j = 0; j < phasesList[y][i].moves.Count; j++)
                {
                    var state = stateMachine.AddState("Attack " + (j + 1));
                    var anim = phasesList[y][i].moves[j];
                    anim.wrapMode = WrapMode.Once;
                    state.motion = anim;
                    //First Attack is always the default state and Entry Transition
                    if (j == 0)
                    {
                        stateMachine.AddEntryTransition(state);
                        stateMachine.defaultState = state;
                    }
                    else if (j > 0)
                    {
                        //States after the first one are connected to the previous one and are automatically played after the previous one
                        var previousState = stateMachine.states[j - 1].state;
                        var transition = previousState.AddTransition(state);
                        transition.hasExitTime = true;
                    } 
                    if (j == phasesList[y][i].moves.Count - 1)
                    {
                        //Reconnecting to the Idle state, if Spawn exists, it has to be considered
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
                
                //Adding Hit State to every Attack State Machine so that Attacks can be interrupted
                if (hitFlag)
                {
                    hitState = stateMachine.AddState("Hit");
                    hit.wrapMode = WrapMode.Once;
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
            
            //Connecting every Attack State Machine to every State other than Spawn, Death and Hit(Base Layer)
            foreach (var state in rootstateMachine.states)
            {
                foreach (var sm in attackStateMachines)
                {
                    if (state.state.name is not ("Spawn" or "Death" or "Hit") && sm.states.Length > 0)
                    {
                        var trans = state.state.AddTransition(sm.states[0].state);
                        trans.AddCondition(AnimatorConditionMode.If, 0, sm.name);
                    }
                    
                }
            }
        }

    }

    private List<BossController.Moves> generateInspectorOptions()
    {
        //Generating the resources for Inspector Options
        List<BossController.Moves> inspMoves = new List<BossController.Moves>();
        phasesHealth.Add(health + 1);
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
        for (int i = 0; i < moves.Count; i++)
        {
            inspMoves.Add(new BossController.Moves(moves[i]));
        }
        return inspMoves;
    }
}
