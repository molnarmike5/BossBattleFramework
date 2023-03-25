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
    private AnimatorController animatorController;
    private Animator anim;
    [SerializeField] public bool navMovement;
    [SerializeField] private bool includeRun;
    [SerializeField] private bool hitFlag;
    [SerializeField] public int phaseCounter;
    [SerializeField] public List<AnimatorStateMachine> attackStateMachines;
    [SerializeField] private List<AnimatorStateMachine> currentStateMachines = new List<AnimatorStateMachine>();
    private int randomAttack;
    [Header("Phase Selection")]
    [SerializeField] public List<bool> phases;
    [Header("Moves Selection")]
    [SerializeField] public List<Moves> moves = new List<Moves>();
    [Header("Phase Health Selection")]
    [SerializeField] public List<float> phaseHealth = new List<float>();

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
        List<bool> phases, List<List<bool>> moves, List<float> phasesHealth)
    {
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
    }
    
    private void Start()
    {
        anim = GetComponent<Animator>();
        determineFirstPhase();
        getAttackStateMachines();
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(GameObject.Find(player.name).transform.position);
        checkDeath();
        determineCurrentPhase();
        determineCurrentAttackPool();
        if (Vector3.Distance(transform.position, GameObject.Find(player.name).transform.position) > attackRange && !anim.GetCurrentAnimatorStateInfo(0).IsName("Spawn"))
        {
            
            
            if (Vector3.Distance(GameObject.Find(player.name).transform.position, this.transform.position) >= runningDistance && includeRun)
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Running", true);
                //anim.SetBool(currentStateMachines[randomAttack].name, false);
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
            else if (Vector3.Distance(GameObject.Find(player.name).transform.position, this.transform.position) <= runningDistance && includeRun)
            {
                anim.SetBool("Running", false);
                anim.SetBool("Walking", true);
                //anim.SetBool(currentStateMachines[randomAttack].name, false);
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
            else
            {
                anim.SetBool("Walking", true);
                //anim.SetBool(currentStateMachines[randomAttack].name, false);
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
            if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f && !anim.IsInTransition(0))
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Attacking", true);
                if (currentStateMachines.Count > 0)
                {
                    StartCoroutine(decideNextAttack());    
                }
                
            }
        }
    }

    IEnumerator decideNextAttack()
    {
        randomAttack = Random.Range(0, currentStateMachines.Count);
        anim.SetBool(currentStateMachines[randomAttack].name, true);
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName(currentStateMachines[randomAttack].states[0].state.name));
        anim.SetBool(currentStateMachines[randomAttack].name, false);
    }

    private void OnCollisionEnter(Collision other)
    {
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
        for (int i = 0; i < phases.Count; i++)
        {
            if (phases[i])
            {
                phaseCounter = i + 1;
                break;
            }
        }
    }
    
    private void determineCurrentPhase()
    {
        if (phaseCounter < phaseHealth.Count)
        {
            if (health <= phaseHealth[phaseCounter])
            {
                phaseCounter++;
            }
        }
    }

    private void determineCurrentAttackPool()
    {
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
        if (health <= 0)
        {
            for (int i = 0; i < anim.parameters.Length; i++)
            {
                anim.SetBool(anim.parameters[i].name, false);
            }

            if (death != null)
            {
                anim.Play("Death");
            }
            else
            {
                anim.Play("Idle");
            }
            GetComponent<NavMeshAgent>().speed = 0;
            GetComponent<NavMeshAgent>().isStopped = true;
            Destroy(this.gameObject, 10);
            GetComponent<BossController>().enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            GetComponent<Rigidbody>().freezeRotation = true;
        }
    }

    private void getAttackStateMachines()
    {
        animatorController = anim.runtimeAnimatorController as AnimatorController;
        foreach (var sm in animatorController.layers[0].stateMachine.stateMachines)
        {
            if (sm.stateMachine.name.Contains("Attack"))
            {
                attackStateMachines.Add(sm.stateMachine);
            }
        }
    }
    
}
