using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnEnableScript : MonoBehaviour
{
    public float DelayTime;

    public UnityEvent InvokeFunc;
    public UnityEvent InvokeDisFunc;
    public UnityEvent OnAnimCompleteEvent;

    private void OnEnable()
    {
        Invoke("InvokeStart", DelayTime);
    }

    public void InvokeStart()
    {
        InvokeFunc.Invoke();
    }
            

    private void OnDisable()
    {
        InvokeDisFunc.Invoke();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCompleteScene()
    {
        OnAnimCompleteEvent.Invoke();
    }

    public void RigidBodyOn()
    {
        Invoke("WaitFor",0.5f);
    }

    private void WaitFor()
    {
        this.GetComponent<Rigidbody>().isKinematic = false;
        this.GetComponent<Animator>().enabled = true;
        Invoke("GameOver",5f);
    }

    public void GameOver()
    {

    }

    public void DestroyObject()
    {
        this.GetComponent<OnEnableScript>().enabled = false;
    }
}
