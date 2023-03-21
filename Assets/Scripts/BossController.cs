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
    [SerializeField] public int phaseCounter = 1;
    [SerializeField] public List<AnimatorStateMachine> attackStateMachines;
    [SerializeField] public List<AnimatorStateMachine> currentPhaseStateMachines;
    private int randomAttack;



    public void Constructor(GameObject player, GameObject playerWeapon, float speed,  float attackRange, float runSpeed, float runningDistance, bool includeRun, float health, bool navFlag, AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip spawn, AnimationClip hit, AnimationClip death, List<AnimatorStateMachine> attackStateMachines)
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
            GetComponent<NavMeshAgent>().stoppingDistance = attackRange + 5;
            navMovement = true;
        }
        this.spawn = spawn;
        this.hit = hit;
        this.death = death;
        this.attackStateMachines = attackStateMachines;
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (includeRun)
        {
            if(run == null)
            {
                Debug.LogError("Run animation not set");
                EditorApplication.isPlaying = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            for (int i = 0; i < anim.parameters.Length; i++)
            {
                anim.SetBool(anim.parameters[i].name, false);
            }   
            anim.Play("Death");
            GetComponent<NavMeshAgent>().speed = 0;
            GetComponent<NavMeshAgent>().isStopped = true;
            Destroy(this.gameObject, 10);
            GetComponent<BossController>().enabled = false;
            GetComponent<Rigidbody>().freezeRotation = true;
        }

        transform.LookAt(GameObject.Find(player.name).transform.position);
        string currentPhase = "Phase " + phaseCounter;
        currentPhaseStateMachines.Clear();
        foreach (var sm in attackStateMachines)
        {
            if (sm.name.Contains(currentPhase))
            {
                currentPhaseStateMachines.Add(sm);
            }
        }
        
        if (Vector3.Distance(transform.position, GameObject.Find(player.name).transform.position) > attackRange && !anim.GetCurrentAnimatorStateInfo(0).IsName("Spawn"))
        {
            
            
            if (Vector3.Distance(GameObject.Find(player.name).transform.position, this.transform.position) >= runningDistance && includeRun)
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Running", true);
                anim.SetBool(currentPhaseStateMachines[randomAttack].name, false);
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
                anim.SetBool(currentPhaseStateMachines[randomAttack].name, false);
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
                anim.SetBool(currentPhaseStateMachines[randomAttack].name, false);
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
                StartCoroutine(decideNextAttack());
            }
        }
    }

    IEnumerator decideNextAttack()
    {
        randomAttack = Random.Range(0, currentPhaseStateMachines.Count);
        anim.SetBool(currentPhaseStateMachines[randomAttack].name, true);
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName(currentPhaseStateMachines[randomAttack].states[currentPhaseStateMachines[randomAttack].states.Length - 1].state.name));
        anim.SetBool(currentPhaseStateMachines[randomAttack].name, false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player Weapon") && GameObject.Find(player.name).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            health -= 50;
            Debug.Log(health);
            if (hitFlag)
            {
                anim.Play("Hit");
            }
        }
    }
    
}
