using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.VFX;
using UnityEditor.Experimental;
using Random = UnityEngine.Random;

public class BossController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public GameObject player;
    [SerializeField] public GameObject playerWeapon;
    [SerializeField] public float speed;
    [SerializeField] public float runSpeed;
    [SerializeField] public float attackRange;
    [SerializeField] public float runningDistance;
    [SerializeField] public float health;
    [SerializeField] public AnimationClip walk;
    [SerializeField] public AnimationClip idle;
    [SerializeField] public AnimationClip run;
    [SerializeField] public AnimationClip spawn;
    [SerializeField] public AnimationClip hit;
    [SerializeField] public AnimationClip death;
    [SerializeField] private int attackCoolDownInSec;
    private bool isCoolDown = false;
    private AnimatorController animatorController;
    private Animator anim;
    [SerializeField] public bool navMovement;
    [SerializeField] private bool includeRun;
    [SerializeField] public bool hitFlag;
    [SerializeField] public int phaseCounter;
    [SerializeField] public List<AnimatorStateMachine> attackStateMachines;
    [SerializeField] private List<AnimatorStateMachine> currentStateMachines = new List<AnimatorStateMachine>();
    [SerializeField] private float activateDistance;
    private int randomAttack;
    [Header("Phase Selection")]
    [SerializeField] public List<bool> phases;
    [Header("Moves Selection")]
    [SerializeField] public List<Moves> moves = new List<Moves>();
    [Header("Phase Health Selection")]
    [SerializeField] public List<float> phaseHealth = new List<float>();

    [SerializeField] private int deathTimer;

    private List<Tuple<int, float, bool>> phasesHealthTup = new List<Tuple<int, float, bool>>();

    [System.Serializable]
    public struct Moves
    {
        [SerializeField] public List<bool> moveSet;
        
        public Moves(List<bool> moveSet)
        {
            this.moveSet = moveSet;
        }
        public List<bool> getmoveSet()
        {
            return moveSet;
        }
        
        public void setmoveSet(List<bool> moveSet)
        {
            this.moveSet = moveSet;
        }
    }

    public void Constructor(GameObject player, GameObject playerWeapon, float speed,  float attackRange, float runSpeed, 
        float runningDistance, bool includeRun, float health, bool navFlag, AnimationClip idle, AnimationClip walk, 
        AnimationClip run, AnimationClip spawn, AnimationClip hit, AnimationClip death, List<AnimatorStateMachine> attackStateMachines, 
        List<bool> phases, List<List<bool>> moves, List<float> phasesHealth, float activateDistance)
    {
        //Constructor to delegate Information from the BattleBossFramework to the BossController
        this.player = player;
        this.playerWeapon = playerWeapon;
        this.speed = speed;
        this.runSpeed = runSpeed;
        this.attackRange = attackRange;
        this.runningDistance = runningDistance; 
        this.idle = idle;
        this.walk = walk;
        this.run = run;
        this.includeRun = includeRun;
        this.health = health;
        if (navFlag)
        {
            this.AddComponent<NavMeshAgent>();
            GetComponent<NavMeshAgent>().stoppingDistance = attackRange - 1;
            navMovement = true;
        }
        this.spawn = spawn;
        this.hit = hit;
        this.death = death;
        this.attackStateMachines = attackStateMachines;
        this.phases = phases;
        for (int i = 0; i < moves.Count; i++)
        {
            this.moves.Add(new Moves(moves[i]));
        }
        this.phaseHealth = phasesHealth;
        this.activateDistance = activateDistance;
    }
    
    private void Awake()
    {
        anim = GetComponent<Animator>();
        //Converts the phaseHealth List to a Tuple List to be able to save the corresponding phase to the health value
        parsePhaseHealth();
        //Determines the first phase of the boss which is enabled in the Inspector
        determineFirstPhase();
        //Extracting attack state machines from the Animator Controller
        getAttackStateMachines();
        anim.enabled = false;
        if (navMovement)
        {
            GetComponent<NavMeshAgent>().enabled = false;
        }
        StartCoroutine(waitUntilDistance());
    }

    // Update is called once per frame
    void Update()
    {
        //Always Look At the Player
        transform.LookAt(GameObject.Find(player.name).transform.position);
        //Check if the Boss is dead
        checkDeath();
        //Determine the current phase of the boss
        determineCurrentPhase();
        //Determine the current attack pool of the boss, depending on the current phase and the moves enabled in the Inspector
        determineCurrentAttackPool();
        //Basic Boss AI
        if (Vector3.Distance(transform.position, GameObject.Find(player.name).transform.position) > attackRange && !anim.GetCurrentAnimatorStateInfo(0).IsName("Spawn"))
        {
            if (Vector3.Distance(GameObject.Find(player.name).transform.position, transform.position) >= runningDistance && includeRun && !anim.GetBool("Attacking"))
            {
                //Run
                anim.SetBool("Walking", false);
                anim.SetBool("Running", true);
                if (navMovement)
                {
                    GetComponent<NavMeshAgent>().destination = GameObject.Find(player.name).transform.position;
                    GetComponent<NavMeshAgent>().speed = runSpeed;
                }
                else
                {
                    transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
                }
            }
            else if (Vector3.Distance(GameObject.Find(player.name).transform.position, transform.position) <= runningDistance && includeRun && !anim.GetBool("Attacking"))
            {
                //Walk
                anim.SetBool("Running", false);
                anim.SetBool("Walking", true);
                if (navMovement)
                {
                    GetComponent<NavMeshAgent>().destination = GameObject.Find(player.name).transform.position;
                    GetComponent<NavMeshAgent>().speed = speed;
                }
                else
                {
                    transform.Translate(Vector3.forward * speed * Time.deltaTime);
                }
            }
            else if (!anim.GetBool("Attacking"))
            {
                //Walk, run is disabled
                anim.SetBool("Walking", true);
                if (navMovement)
                {
                    GetComponent<NavMeshAgent>().destination = GameObject.Find(player.name).transform.position;
                    GetComponent<NavMeshAgent>().speed = speed;
                }
                else
                {
                    transform.Translate(Vector3.forward * speed * Time.deltaTime);
                }
            }
            
        }
        else
        {
            //Attack
            if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f && !anim.IsInTransition(0))
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Attacking", true);
                GetComponent<NavMeshAgent>().destination = transform.position;
                if (!isCoolDown)
                {
                    if (currentStateMachines.Count > 0)
                    {
                        randomAttack = Random.Range(0, currentStateMachines.Count);
                        string name = "Attack 1";
                        StartCoroutine(decideNextAttack(name));    
                    }

                    StartCoroutine(coolDown());
                }
                
            }
        }
    }

    IEnumerator decideNextAttack(string name)
    {
        //Completely Random Attack
        anim.SetBool(currentStateMachines[randomAttack].name, true);
        //Wait until the attack state machine is entered
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName(name));
        foreach (var parameter in anim.parameters)
        {
            if (parameter.name.Contains("Moveset"))
            {
                anim.SetBool(parameter.name, false);
            }
        }
        anim.SetBool("Attacking", false);
    }

    private void OnCollisionEnter(Collision other)
    {
        //Hit the Boss
        if (other.gameObject.CompareTag("Player Weapon") && GameObject.Find(player.name).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            health -= 50;
            Debug.Log(health);
            if (hitFlag && (hit != null))
            {
                anim.Play("Hit");
            }
        }
    }
    
    private void determineFirstPhase()
    {
        //Determines the first phase of the boss which is enabled in the Inspector
        for (int i = 0; i < phasesHealthTup.Count; i++)
        {
            if (phasesHealthTup[i].Item3)
            {
                phaseCounter = phasesHealthTup[i].Item1;
                break;
            }
        }
    }
    
    private void determineCurrentPhase()
    {
        //Determines the current phase of the boss
        if (phasesHealthTup.Count > phaseCounter)
        {
            if (health <= phasesHealthTup[phaseCounter].Item2)
            {
                phaseCounter = phasesHealthTup[phaseCounter].Item1;
            }
        }
    }

    private void determineCurrentAttackPool()
    {
        //Determines the current attack pool of the boss, depending on the current phase and the moves enabled in the Inspector
        string currentPhase = "Phase " + phaseCounter;
        currentStateMachines.Clear();
        for (int i = 0; i < attackStateMachines.Count; i++)
        {
            if (attackStateMachines[i].name.Contains(currentPhase))
            {
                currentStateMachines.Add(attackStateMachines[i]);
            }
        }
        int count = currentStateMachines.Count - 1;
        for (int i = count; i >= 0; i--)
        {
            if (currentStateMachines.Count > 0)
            {
                if (moves[phaseCounter - 1].moveSet[i] == false)
                {
                    currentStateMachines.RemoveAt(i);
                }
            }
        }
    }

    private void checkDeath()
    {
        //Check if the Boss is dead
        if (health <= 0)
        {
            //Disable all Animations
            for (int i = 0; i < anim.parameters.Length; i++)
            {
                anim.SetBool(anim.parameters[i].name, false);
            }
            //Play Death Animation if it exists else play Idle Animation
            if (death != null)
            {
                anim.Play("Death");
            }
            else
            {
                anim.Play("Idle");
            }
            //Disable everything that is not needed anymore
            GetComponent<NavMeshAgent>().speed = 0;
            GetComponent<NavMeshAgent>().isStopped = true;
            Destroy(gameObject, deathTimer);
            GetComponent<BossController>().enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            GetComponent<Rigidbody>().freezeRotation = true;
        }
    }

    private void getAttackStateMachines()
    {
        //Extract all Attack State Machines from the Animator Controller
        animatorController = AssetDatabase
            .LoadAssetAtPath<AnimatorController>(
                AssetDatabase.GetAssetPath(GetComponent<Animator>().runtimeAnimatorController));
        foreach (var sm in animatorController.layers[0].stateMachine.stateMachines)
        {
            if (sm.stateMachine.name.Contains("Attack"))
            {
                attackStateMachines.Add(sm.stateMachine);
            }
        }
    }

    IEnumerator waitUntilDistance()
    {
        //Wait until the Boss is close enough to the Player to activate the Boss
        yield return new WaitUntil(() => Vector3.Distance(transform.position, GameObject.Find(player.name).transform.position) < activateDistance);
        anim.enabled = true;
        if (navMovement)
        {
            GetComponent<NavMeshAgent>().enabled = true;
        }
    }
    
    private void parsePhaseHealth()
    {
        //Parse the Phase Health from the Inspector
        for (int i = 0; i < phaseHealth.Count; i++)
        {
            phasesHealthTup.Add(new Tuple<int, float, bool>(i + 1, phaseHealth[i], phases[i]));
        }
    }

    IEnumerator coolDown()
    {
        isCoolDown = true;
        yield return new WaitForSeconds(attackCoolDownInSec);
        isCoolDown = false;
    }
    
}
