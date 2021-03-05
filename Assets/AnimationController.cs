using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public enum AnimType
    {
        Idle, Run, Air
    }
    public class Clip
    {
        
        public int id;
        public string name;
        public AnimType type;
        public float length;

        public Clip(string animationName)
        {
            name = animationName;
            id = Animator.StringToHash(name);
        }
        public Clip(string animationName, AnimType clipType)
        {
            name = animationName;
            id = Animator.StringToHash(name);
            type = clipType;
        }
    }

    public float animSpeedMultipler = 1.5f;

    private Clip IDLE = new Clip("Idle", AnimType.Idle);
    private Clip IDLE2 = new Clip("Idle2", AnimType.Idle);
    private Clip IDLE_TURN = new Clip("IdleTurn", AnimType.Run);
    private Clip RUN_START = new Clip("RunStart", AnimType.Run);
    private Clip RUNNING = new Clip("Running", AnimType.Run);
    private Clip RUN_TURN = new Clip("RunTurn", AnimType.Run);
    private Clip RUN_STOP = new Clip("RunStop", AnimType.Run);
    private Clip JUMP = new Clip("Jump", AnimType.Air);
    private Clip MOVE_AIR = new Clip("MoveAir", AnimType.Air);
    private Clip MOVE_AIR2 = new Clip("MoveAir2", AnimType.Air);
    
    private Clip FALLING = new Clip("Falling", AnimType.Air);
    private Clip LANDING = new Clip("Landing", AnimType.Idle);
    private Clip currentState;
    public string stateName;
    private Clip nextState;
    private float timeInState = 0f;
    private Animator animator; 

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        ClipSetup();
        currentState = IDLE;
        Idle();
    }

    // Update is called once per frame
    void Update()
    {
        PlayQueuedState();
        if(currentState == IDLE_TURN)
        {
            Debug.Log(currentState.name);
        }
    }

    void PlayQueuedState()
    {
        timeInState += Time.deltaTime;
        if(timeInState >= currentState.length && nextState != null)
        {
            ChangeAnimationState(nextState);
        }
    }

    void ClipSetup()
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach(AnimationClip clip in clips)
        {
            switch(clip.name)
            {
                case "Idle":
                    IDLE.length = clip.length * 5;//repeat the clip 5 times before idle2
                    break;
                case "Idle2":
                    IDLE2.length = clip.length;
                    break;
                case "IdleTurn":
                    IDLE_TURN.length = clip.length / animSpeedMultipler;
                    break;
                case "RunStart":
                    RUN_START.length = clip.length / animSpeedMultipler;
                    break;
                case "Running":
                    RUNNING.length = clip.length / animSpeedMultipler;
                    break;
                case "RunStop":
                    RUN_STOP.length = clip.length / animSpeedMultipler;
                    break;
                case "RunTurn":
                    RUN_TURN.length = clip.length / animSpeedMultipler;
                    break;
                case "Jump":
                    JUMP.length = clip.length / animSpeedMultipler;
                    break;
                case "MoveAir":
                    MOVE_AIR.length = clip.length / animSpeedMultipler;
                    break;
                case "MoveAir2":
                    MOVE_AIR2.length = clip.length / animSpeedMultipler;
                    break;
                case "Falling":
                    FALLING.length = clip.length / animSpeedMultipler;
                    break;
                case "Landing":
                    LANDING.length = clip.length / animSpeedMultipler;
                    break;
            }
        }
    }
    void ChangeAnimationState(Clip newState)
    {
        //guard the current animation from interrupting itself
        if(currentState == newState || newState == null) return;

        //play the animation
        animator.Play(newState.id);

        //reassign the current state
        currentState = newState;

        //save anim string for debug purposes
        stateName = currentState.name;
        
        //reset the time spent in current state
        timeInState = 0f;

        //unqueue any state set to run
        nextState = null;
    }
    

    public void Run()
    {
        if(currentState != RUNNING && 
           currentState != RUN_TURN && 
           currentState != IDLE_TURN)
        {
            ChangeAnimationState(RUN_START);
            nextState = RUNNING;
        }
    }

    public void Idle()
    {
        if(currentState.type == AnimType.Run)
        {
            ChangeAnimationState(RUN_STOP);
            nextState = IDLE;
        }
        else if(currentState == IDLE)
        {
            nextState = IDLE2;
        }
    }

    public void Turn()
    {
        if(currentState.type == AnimType.Idle)
            ChangeAnimationState(RUN_TURN);
        else if(currentState.type == AnimType.Run)
            ChangeAnimationState(RUN_TURN);
        
        nextState = RUNNING;
    }

    public void Jump()
    {
        ChangeAnimationState(JUMP);
    }

    public void MoveAir()
    {
        if(currentState!= MOVE_AIR2)
        {
            ChangeAnimationState(MOVE_AIR);
            nextState = MOVE_AIR2;
        }
        else
        {
            ChangeAnimationState(MOVE_AIR2);
        }
    }

    public void Falling()
    {
        ChangeAnimationState(FALLING);
    }

    public void Landing()
    {
        ChangeAnimationState(LANDING);
        nextState = IDLE;
    }

    void RunningAnimation()
    {
        if(currentState == RUN_START || 
           currentState == RUN_TURN || 
           currentState == IDLE_TURN)
        {
            ChangeAnimationState(RUNNING);
        }
    }
}
