using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class BossController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject playerWeapon;
    [SerializeField] private float speed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float turnSpeed = 1;
    [SerializeField] private float attackRange;
    [SerializeField] private float runningDistance;
    [SerializeField] private float health;
    [SerializeField] private AnimationClip walk;
    [SerializeField] private AnimationClip idle;
    [SerializeField] private AnimationClip run;
    [SerializeField] private AnimationClip spawn;
    [SerializeField] private AnimationClip hit;
    [SerializeField] private AnimationClip death;
    [SerializeField] private int attackCoolDownInSec;
    private bool isCoolDown = false;
    private AnimatorController animatorController;
    private Animator anim;
    [SerializeField] private bool navMovement;
    [SerializeField] private bool includeRun;
    [SerializeField] private bool hitFlag;
    [SerializeField] private int phaseCounter;
    [Header("Hits needed for Hit Animation to Play and Hit Timer")]
    [SerializeField] private int hitCount;
    private int hitCounter = 0;
    [SerializeField] private List<AnimatorStateMachine> attackStateMachines;
    [SerializeField] private List<AnimatorStateMachine> currentStateMachines = new List<AnimatorStateMachine>();
    [SerializeField] private float activateDistance;
    private int randomAttack;
    [Header("Phase Selection")]
    [SerializeField] private List<bool> phases;
    [Header("Moves Selection")]
    [SerializeField] private List<Moves> moves = new List<Moves>();
    [Header("Phase Health Selection")]
    [SerializeField] private List<float> phaseHealth = new List<float>();

    [SerializeField] private int deathTimer;
    private bool collision = false;

    private List<Tuple<int, float, bool>> phasesHealthTup = new List<Tuple<int, float, bool>>();
    
    private NavMeshAgent agent;
    private bool waiting = true;
    private State currentState = State.SPAWN;
    private bool setAttackingRunning = false;
    
    private enum State
    {
        SPAWN, WALKING, RUNNING, ATTACKING, DEAD, HIT, IDLE 
    }

    [Serializable]
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
        float runningDistance, bool includeRun, float health, bool navMovement, AnimationClip idle, AnimationClip walk, 
        AnimationClip run, AnimationClip spawn, AnimationClip hit, int hitCount, AnimationClip death, int deathTimer, int attackCoolDownInSec, List<AnimatorStateMachine> attackStateMachines, 
        List<bool> phases, List<Moves> moves, List<float> phaseHealth, float activateDistance)
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
        this.navMovement = navMovement;
        this.spawn = spawn;
        this.hit = hit;
        this.death = death;
        this.attackStateMachines = attackStateMachines;
        this.phases = phases;
        this.moves = moves;
        this.phaseHealth = phaseHealth;
        this.activateDistance = activateDistance;
        this.deathTimer = deathTimer;
        this.attackCoolDownInSec = attackCoolDownInSec;
        this.hitCount = hitCount;
    }
    
    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (navMovement)
        {
            if (GetComponent<NavMeshAgent>() == null)
            {
                this.AddComponent<NavMeshAgent>();
            }
            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = attackRange - 2f;
        }
        //Converts the phaseHealth List to a Tuple List to be able to save the corresponding phase to the health value
        parsePhaseHealth();
        //Determines the first phase of the boss which is enabled in the Inspector
        determineFirstPhase();
        //Extracting attack state machines from the Animator Controller
        getAttackStateMachines();
        anim.enabled = false;
        StartCoroutine(waitUntilDistance());
    }

    // Update is called once per frame
    void Update()
    {
        if (!waiting)
        {
            //Always Look At the Player
            var targetRotation =
                Quaternion.LookRotation(GameObject.Find(player.name).transform.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            //Determine the current phase of the boss
            determineCurrentPhase();
            //Determine the current attack pool of the boss, depending on the current phase and the moves enabled in the Inspector
            determineCurrentAttackPool();
            //Calculate the distance between the player and the boss
            var distance = Vector3.Distance(GameObject.Find(player.name).transform.position, transform.position);
            //Decide the current state of the boss
            currentState = decideCurrentState(distance);
            //Run the current state logic
            runLogic(currentState);
        }
    }

    private State decideCurrentState(float distance)
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Spawn"))
        {
            return State.SPAWN;
        }
        else if (health <= 0)
        {
            return State.DEAD;
        } 
        else if (hitCounter >= hitCount && hitFlag && hitCount > 0)
        {
            return State.HIT;
        }
        else if ((distance <= attackRange || anim.GetBool("Attacking")))
        {
            return State.ATTACKING;
        }
        else if (distance >= runningDistance && distance > attackRange && includeRun && !anim.GetBool("Attacking"))
        {
            return State.RUNNING;
        }
        else if (distance > attackRange && distance < runningDistance && !anim.GetBool("Attacking"))
        {
            return State.WALKING;
        } 
        else
        {
            return State.IDLE;
        }
        
    }

    private void runLogic(State state)
    {
        switch (state)
        {
            case State.SPAWN:
                break;
            case State.DEAD:
                anim.SetBool("Attacking", false);
                setAllMoveSetsFalse();
                executeDeath();
                break;
            case State.HIT:
                anim.SetBool("Attacking", false);
                setAllMoveSetsFalse();
                anim.Play("Hit");
                hitCounter = 0;
                isCoolDown = false;
                break;
            case State.RUNNING:
                anim.SetBool("Idle", false);
                setAllMoveSetsFalse();
                anim.SetBool("Walking", false);
                anim.SetBool("Running", true);
                if (navMovement)
                {
                    agent.destination = GameObject.Find(player.name).transform.position;
                    agent.speed = runSpeed;
                }
                else
                {
                    transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
                }
                break;
            case State.WALKING:
                anim.SetBool("Idle", false);
                setAllMoveSetsFalse();
                anim.SetBool("Running", false);
                anim.SetBool("Walking", true);
                if (navMovement)
                {
                    agent.destination = GameObject.Find(player.name).transform.position;
                    agent.speed = speed;
                }
                else
                {
                    transform.Translate(Vector3.forward * speed * Time.deltaTime);
                }
                break;
            case State.ATTACKING:
                anim.SetBool("Idle", false);
                anim.SetBool("Running", false);
                anim.SetBool("Walking", false);
                if (!isCoolDown)
                {
                    anim.SetBool("Attacking", true);
                }
                if (navMovement)
                {
                    agent.destination = transform.position;
                }

                if (currentStateMachines.Count > 0 && !isCoolDown && !setAttackingRunning)
                {
                    randomAttack = Random.Range(0, currentStateMachines.Count);
                    anim.SetBool(currentStateMachines[randomAttack].name, true);
                    //Wait until the attack state machine is entered
                    isCoolDown = true;
                    StartCoroutine(setAttacking());
                }
                break;
            case State.IDLE:
                anim.SetBool("Idle", true);
                anim.SetBool("Walking", false);
                anim.SetBool("Running", false);
                setAllMoveSetsFalse();
                anim.Play("Idle");
                break;
        }
    }

    IEnumerator setAttacking()
    {
        setAttackingRunning = true;
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsTag("Last Attack"));
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        anim.SetBool("Attacking", false);
        anim.SetBool(currentStateMachines[randomAttack].name, false);
        StartCoroutine(coolDown());
        setAttackingRunning = false;
    }

    private void setAllMoveSetsFalse()
    {

        foreach (var parameter in anim.parameters)
        {
            if (parameter.name.Contains("Moveset"))
            {
                anim.SetBool(parameter.name, false);
            }
        }
        
    }

    private void OnCollisionEnter(Collision other)
    {
        
        if (other.gameObject.CompareTag("Player Weapon") && GameObject.Find(player.name).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack") && !collision)
        {
            collision = true;
            health -= 50;
            Debug.Log(health);
            if (hitFlag && hit != null)
            {
                hitCounter++;
            }
            StartCoroutine(resetCollision());
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
        for (int i = 0; i < phasesHealthTup.Count; i++)
        {
            if (health <= phasesHealthTup[i].Item2)
            {
                phaseCounter = phasesHealthTup[i].Item1;
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

    private void executeDeath()
    {
        //Check if the Boss is dead
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
            if (navMovement)
            {
                GetComponent<NavMeshAgent>().speed = 0;
                GetComponent<NavMeshAgent>().isStopped = true;
            }
            Destroy(gameObject, deathTimer);
            GetComponent<BossController>().enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            GetComponent<Rigidbody>().isKinematic = true;
        
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
        waiting = false;
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
        yield return new WaitForSeconds(attackCoolDownInSec);
        isCoolDown = false;
    }

    IEnumerator resetCollision()
    {
        yield return new WaitUntil(() =>
            !GameObject.Find(player.name).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack"));
        collision = false;
    }

}
