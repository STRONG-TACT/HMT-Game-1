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
    private int moveCount = 0;

    private float stepLength;

    void Start()
    {
        movePoint = transform.position;
        prevMovePointPos = movePoint;
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

    public void moveOneStep()
    {
        if (!turnFinished)
        {
            LocalCharacter.Direction direction = 0;
            List<int> directions = new List<int>() { 1, 2, 3, 4 };

            while (directions.Count != 0)
            {
                int i = Random.Range(1, 5);

                if (CheckMove((LocalCharacter.Direction)i))
                {
                    direction = (LocalCharacter.Direction)i;
                    break;
                }
                else
                {
                    directions.Remove(i);
                }
            }

            Vector3 moveVec = Vector3.zero;
            //Quaternion rot = Quaternion.identity;

            switch (direction)
            {
                case LocalCharacter.Direction.Up:
                    moveVec = Vector3.forward;
                    //rot = Quaternion.Euler(0, 0, 0);
                    break;

                case LocalCharacter.Direction.Down:
                    moveVec = Vector3.back;
                    //rot = Quaternion.Euler(0, 180, 0);
                    break;

                case LocalCharacter.Direction.Left:
                    moveVec = Vector3.left;
                    //rot = Quaternion.Euler(0, 270, 0);
                    break;

                case LocalCharacter.Direction.Right:
                    moveVec = Vector3.right;
                    //rot = Quaternion.Euler(0, 90, 0);
                    break;

                default:
                    break;
            }

            prevMovePointPos = movePoint;
            movePoint += moveVec * stepLength;

            this.transform.position = movePoint;

            moveCount += 1;

            if (moveCount >= config.movement)
            {
                Debug.Log(string.Format("monsterID: {0}, turnFinished", monsterId));
                turnFinished = true;
                LocalGameManager.Instance.monsterMoveFinished();
            }

            Debug.Log(string.Format("monsterID: {0}, direction: {1}, movement: {2}, count: {3}", monsterId, direction, config.movement, moveCount));
        }
        
    }

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
}
