using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMonster : MonoBehaviour
{
    public MonsterConfig config;
    public int monsterId;
    public NetworkGameData gameData;

    private Vector3 movePoint;
    private Vector3 prevMovePointPos;

    public bool turnFinished = false;
    public bool moving = false;
    private int moveCount = 0;

    private float stepLength;

    public NetworkTile currentTile;

    private Transform model;
    private Animator animator;
    private CharacterState characterState;

    public enum CharacterState
    {
        Idle, Walking, Attacking, Die
    }
    
    public CharacterState State
    {
        get { return characterState; }
        set
        {
            if (value != characterState)
            {
                characterState = value;
                switch (value)
                {
                    case CharacterState.Idle:
                        animator.SetBool("Idle", true);
                        animator.SetBool("Attacking", false);
                        animator.SetBool("Walk", false);
                        break;
                    case CharacterState.Walking:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attacking", false);
                        animator.SetBool("Walk", true);
                        break;
                    case CharacterState.Attacking:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attacking", true);
                        animator.SetBool("Walk", false);
                        break;
                    case CharacterState.Die:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attacking", false);
                        animator.SetBool("Walk", false);
                        animator.SetBool("Die", true);
                        break;
                }
            }
        }
    }
    
    void Start()
    {
        movePoint = transform.position;
        prevMovePointPos = movePoint;
        animator = GetComponentInChildren<Animator>();
        animator.SetBool("Idle", true);
        State = CharacterState.Idle;

        model = transform.Find("Model");
        if (model == null) {
            Debug.LogErrorFormat("Character {0} has no Model child object", gameObject.name);
        }

        //State = CharacterState.Die;
        animator.speed = Random.Range(0.8f, 1.2f); // Randomize speed within a range
    }
    
    public void SetUpConfig(MonsterConfig config, int MonsterId, NetworkGameData data)
    {
        this.config = config;
        monsterId = MonsterId;

        gameData = data;

        stepLength = data.tileSize + data.tileGapLength;
    }
    
    public void Kill(float stepTime) {
        State = CharacterState.Die;
        StartCoroutine(KillCoroutine(stepTime));
    }

    IEnumerator KillCoroutine(float stepTime) {
        //This code existed in the old MonsterDie function and might be relevant to bring back.
        //AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        //float animationLength = stateInfo.length;
        //yield return new WaitForSeconds(animationLength - 2f);
        yield return new WaitForSeconds(stepTime);
        Destroy(this.gameObject);
        yield break;
    }
    
    public void Retreat()
    {
        transform.position = prevMovePointPos;
        movePoint = prevMovePointPos;
    }
}
