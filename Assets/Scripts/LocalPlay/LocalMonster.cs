using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalMonster : MonoBehaviour
{
    public MonsterConfig config;
    public int monsterId;
    public LocalGameData gameData;

    private Vector3 movePoint;
    private Vector3 prevMovePointPos;

    public bool turnFinished = false;
    public bool moving = false;
    private int moveCount = 0;

    private float stepLength;

    public LocalTile currentTile;

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

    public void SetUpConfig(MonsterConfig config, int MonsterId, LocalGameData data)
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
        LocalCharacter.Direction direction = LocalCharacter.Direction.Wait;
        List<LocalCharacter.Direction> directions = new List<LocalCharacter.Direction>() { 
            LocalCharacter.Direction.Up,
            LocalCharacter.Direction.Down,
            LocalCharacter.Direction.Left,
            LocalCharacter.Direction.Right};
        
        // shuffle directions
        for (int i = 0; i < directions.Count; i++) {
            int j = Random.Range(i, directions.Count);
            LocalCharacter.Direction temp = directions[i];
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
            LocalCharacter.Direction.Up => Vector3.forward,
            LocalCharacter.Direction.Down => Vector3.back,
            LocalCharacter.Direction.Left => Vector3.left,
            LocalCharacter.Direction.Right => Vector3.right,
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
                //LocalGameManager.Instance.monsterMoveFinished();
            }
            Debug.Log(string.Format("monsterID: {0}, direction: {1}, movement: {2}, count: {3}", monsterId, direction, config.movement, moveCount));
        }
        State = CharacterState.Idle;
        moving = false;
        yield break;
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


    //public void moveOneStep()
    //{
    //    if (!turnFinished)
    //    {
    //        LocalCharacter.Direction direction = 0;
    //        List<int> directions = new List<int>() { 1, 2, 3, 4 };

    //        while (directions.Count != 0)
    //        {
    //            int i = Random.Range(1, 5);

    //            if (CheckMove((LocalCharacter.Direction)i))
    //            {
    //                direction = (LocalCharacter.Direction)i;
    //                break;
    //            }
    //            else
    //            {
    //                directions.Remove(i);
    //            }
    //        }

    //        Vector3 moveVec = Vector3.zero;
    //        //Quaternion rot = Quaternion.identity;

    //        switch (direction)
    //        {
    //            case LocalCharacter.Direction.Up:
    //                moveVec = Vector3.forward;
    //                //rot = Quaternion.Euler(0, 0, 0);
    //                break;

    //            case LocalCharacter.Direction.Down:
    //                moveVec = Vector3.back;
    //                //rot = Quaternion.Euler(0, 180, 0);
    //                break;

    //            case LocalCharacter.Direction.Left:
    //                moveVec = Vector3.left;
    //                //rot = Quaternion.Euler(0, 270, 0);
    //                break;

    //            case LocalCharacter.Direction.Right:
    //                moveVec = Vector3.right;
    //                //rot = Quaternion.Euler(0, 90, 0);
    //                break;

    //            default:
    //                break;
    //        }

    //        prevMovePointPos = movePoint;
    //        movePoint += moveVec * stepLength;

    //        this.transform.position = movePoint;

    //        moveCount += 1;

    //        if (moveCount >= config.movement)
    //        {
    //            Debug.Log(string.Format("monsterID: {0}, turnFinished", monsterId));
    //            turnFinished = true;
    //            LocalGameManager.Instance.monsterMoveFinished();
    //        }

    //        Debug.Log(string.Format("monsterID: {0}, direction: {1}, movement: {2}, count: {3}", monsterId, direction, config.movement, moveCount));
    //    }
        
    //}

    public bool CheckMove(LocalCharacter.Direction direction)
    {
        Vector3 moveVec = direction switch
        {
            LocalCharacter.Direction.Up => Vector3.forward,
            LocalCharacter.Direction.Down => Vector3.back,
            LocalCharacter.Direction.Left => Vector3.left,
            LocalCharacter.Direction.Right => Vector3.right,
            _ => Vector3.forward
        };

        return !Physics.Raycast(this.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible"));
    }

    public JObject HMTStateRep() {
        return new JObject {
            {"name", "monster" + monsterId },
            {"type", config.type.ToString() },
            {"actionPoints", config.movement },
            {"combatDice", config.combatDice.ToString() }

        };
    }
}
