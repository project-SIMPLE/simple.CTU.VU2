using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchGate : MonoBehaviour
{
    public Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("1"))
        {
             anim.Play("Switch_OFF", -1,0f);
             anim.Play("PFB_Gate2_ON", -1,0f);
        }
        if(Input.GetKeyDown("2"))
        {
            anim.Play("Switch_ON", -1,0f);
            anim.Play("PFB_Gate2_OFF", -1,0f);
        
        }
    }
}
