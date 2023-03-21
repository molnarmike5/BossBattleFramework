using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector2 turn;
    public float sensitivity = .5f;
    [SerializeField] public float speed;
    private Animator anim;
    private CharacterController controller;
    [SerializeField] private GameObject weapon;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        turn.x += Input.GetAxis("Mouse X") * sensitivity;
        //turn.y += Input.GetAxis("Mouse Y") * sensitivity;
        transform.rotation = Quaternion.Euler(0, turn.x, 0);

        if (Input.GetKey("w"))
        {
            controller.Move(transform.forward * Time.deltaTime * speed);
            anim.SetBool("Moving", true);
        }
        else if (Input.GetKey("s"))
        {
            controller.Move(-transform.forward * Time.deltaTime * speed);
            anim.SetBool("Moving", true);
        }
        else if (Input.GetKey("a"))
        {
            controller.Move(-transform.right * Time.deltaTime * speed);
            anim.SetBool("Moving", true);
        }
        else if (Input.GetKey("d"))
        {
            controller.Move(transform.right * Time.deltaTime * speed);
            anim.SetBool("Moving", true);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(Attack());
        }
        else
        {
            anim.SetBool("Moving", false);
        }
        
        if(anim.GetBool("Attack"))
        {
            weapon.GetComponent<CapsuleCollider>().enabled = true;
        }
        else
        {
            weapon.GetComponent<CapsuleCollider>().enabled = false;
        }
        
    }
    
    IEnumerator Attack()
    {
        anim.SetBool("Attack", true);
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f && !anim.IsInTransition(0) && anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
        anim.SetBool("Attack", false);
    }
}
