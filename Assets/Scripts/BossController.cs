using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.VFX;

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
    private AnimatorController animatorController;
    private Animator anim;
    [SerializeField] private bool includeRun;
    
    

    public void Constructor(GameObject player, float speed,  float attackRange, float runSpeed, float runningDistance, bool includeRun, float health, AnimationClip idle, AnimationClip walk, AnimationClip run)
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
        
        if (Vector3.Distance(transform.position, GameObject.Find(player.name).transform.position) > attackRange)
        {
            
            
            if (Vector3.Distance(GameObject.Find(player.name).transform.position, this.transform.position) >= runningDistance && includeRun)
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Running", true);
                transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
            }
            else if (Vector3.Distance(GameObject.Find(player.name).transform.position, this.transform.position) <= runningDistance && includeRun)
            {
                anim.SetBool("Running", false);
                anim.SetBool("Walking", true);
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
            }
            else
            {
                anim.SetBool("Walking", true);
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
            }
            
        }
        else
        {
            if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f && !anim.IsInTransition(0))
            {
                anim.SetBool("Walking", false);
            }
        }
    }
}
