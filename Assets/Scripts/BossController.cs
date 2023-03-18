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

public class BossController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public GameObject player;
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
    private int phaseCounter = 1;
    [SerializeField] private List<AnimatorStateMachine> attackStateMachines;
    [SerializeField] private List<AnimatorStateMachine> currentPhaseStateMachines;
    private int randomAttack;



    public void Constructor(GameObject player, float speed,  float attackRange, float runSpeed, float runningDistance, bool includeRun, float health, bool navFlag, AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip spawn, AnimationClip hit, AnimationClip death, List<AnimatorStateMachine> attackStateMachines)
    {
        this.player = player;
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

    private enum States
    {
        Walking,
        Idle,
        Attacking,
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
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
                Debug.Log("Walking");
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
                //Cooldown for next attack
                StartCoroutine(decideNextAttack());
            }
        }
    }

    IEnumerator decideNextAttack()
    {
        randomAttack = Random.Range(0, currentPhaseStateMachines.Count);
        anim.SetBool(currentPhaseStateMachines[randomAttack].name, true);
        yield return new WaitForSeconds(0.5f);
        anim.SetBool(currentPhaseStateMachines[randomAttack].name, false);
    }
}
