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
    
    public void MonsterTurnStart()
    {
        turnFinished = false;
        moveCount = 0;
    }
    
    public IEnumerator TakeNextMove(float stepTime) {
        if(moveCount >= config.movement) {
            yield break;
        }
        moving = true;
        NetworkCharacter.Direction direction = NetworkCharacter.Direction.Wait;
        List<NetworkCharacter.Direction> directions = new List<NetworkCharacter.Direction>() { 
            NetworkCharacter.Direction.Up,
            NetworkCharacter.Direction.Down,
            NetworkCharacter.Direction.Left,
            NetworkCharacter.Direction.Right};
        
        // shuffle directions
        for (int i = 0; i < directions.Count; i++) {
            int j = Random.Range(i, directions.Count);
            NetworkCharacter.Direction temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }

        for (int i =0; i < directions.Count; i++) {
            if (CheckMove(directions[i])) {
                direction = directions[i];
                break;
            }
        }

        Vector3 moveVec = direction switch {
            NetworkCharacter.Direction.Up => Vector3.forward,
            NetworkCharacter.Direction.Down => Vector3.back,
            NetworkCharacter.Direction.Left => Vector3.left,
            NetworkCharacter.Direction.Right => Vector3.right,
            _ => Vector3.zero
        };

        float timeStart = Time.time;
        if (moveVec != Vector3.zero) {
            State = CharacterState.Walking;
            Vector3 origin = transform.position;
            Vector3 target = transform.position + moveVec * stepLength;
            Quaternion targetRotation = Quaternion.LookRotation(moveVec, Vector3.up);
            while (Time.time - timeStart < stepTime) {
                float t = (Time.time - timeStart) / stepTime;
                transform.position = Vector3.Lerp(origin, target, t);
                model.rotation = Quaternion.Slerp(model.rotation, targetRotation, t);
                yield return null;
            }
            transform.position = target;
            model.rotation = targetRotation;
            moveCount += 1;
            if (moveCount >= config.movement) {
                Debug.Log(string.Format("monsterID: {0}, turnFinished", monsterId));
                turnFinished = true;
            }
            Debug.Log(string.Format("monsterID: {0}, direction: {1}, movement: {2}, count: {3}", monsterId, direction, config.movement, moveCount));
        }
        State = CharacterState.Idle;
        moving = false;
        yield break;
    }
    
    public IEnumerator moveToTargetLocation(Vector3 target, float stepTime)
    {
        float timeStart = Time.time;

        State = CharacterState.Walking;
        Vector3 origin = transform.position;
        //Vector3 target = transform.position + moveVec * stepLength;
        Vector3 direction = target - origin;
        // Normalize the direction
        direction.Normalize();
        if (direction != Vector3.zero)
        {
            // Create a rotation that looks in the direction of movement
            moving = true;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            model.rotation = targetRotation;
            while (Time.time - timeStart < stepTime)
            {
                float t = (Time.time - timeStart) / stepTime;
                transform.position = Vector3.Lerp(origin, target, t);
                yield return null;
            }
            transform.position = target;
            model.rotation = targetRotation;
        }
        State = CharacterState.Idle;
        moving = false;
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
    
    public bool CheckMove(NetworkCharacter.Direction direction)
    {
        Vector3 moveVec = direction switch
        {
            NetworkCharacter.Direction.Up => Vector3.forward,
            NetworkCharacter.Direction.Down => Vector3.back,
            NetworkCharacter.Direction.Left => Vector3.left,
            NetworkCharacter.Direction.Right => Vector3.right,
            _ => Vector3.forward
        };
        bool passible = true;
        if (Physics.Raycast(this.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible")) ) 
        {
            passible = false;
        }
        //return !Physics.Raycast(this.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible"));
        return passible;
    }
}
