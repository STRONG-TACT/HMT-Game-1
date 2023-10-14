using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCharacter : MonoBehaviour
{
    public enum Direction
    {
        Up = 1, Down = 2, Left = 3, Right = 4, Wait = 0
    }

    public float speed;
    private Vector3 movePoint;
    private Vector3 prevMovePointPos;

    private float stepLength;

    public int playerId;

    public CharacterConfig config;
    public int CharacterId;

    public GameObject indicator;

    //Health
    public int Health { get { return health; } }
    private int health;

    void Start()
    {
        movePoint = transform.position;
        prevMovePointPos = movePoint;

        health = 3;
    }

    public void SetUpConfig(CharacterConfig config, int characterId, LocalGameData gameData)
    {
        this.config = config;
        CharacterId = characterId;

        stepLength = gameData.tileSize + gameData.tileGapLength;
    }

    public bool CheckMove(Direction direction)
    {
        Vector3 moveVec = direction switch
        {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.forward
        };

        return !Physics.Raycast(indicator.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible"));
    }

    public void startPlanning()
    {
        indicator.SetActive(true);
    }

    public void pausePlanning()
    {
        indicator.SetActive(false);
    }

    public void endPlanning()
    {
        indicator.SetActive(false);
        indicator.transform.position = this.transform.position;
    }

    public void planNewStep(Direction direction)
    {
        Vector3 moveVec = Vector3.zero;
        //Quaternion rot = Quaternion.identity;

        switch (direction)
        {
            case Direction.Up:
                moveVec = Vector3.forward;
                //rot = Quaternion.Euler(0, 0, 0);
                break;

            case Direction.Down:
                moveVec = Vector3.back;
                //rot = Quaternion.Euler(0, 180, 0);
                break;

            case Direction.Left:
                moveVec = Vector3.left;
                //rot = Quaternion.Euler(0, 270, 0);
                break;

            case Direction.Right:
                moveVec = Vector3.right;
                //rot = Quaternion.Euler(0, 90, 0);
                break;

            default:
                break;
        }

        indicator.transform.position += moveVec * stepLength;
    }

    public void backOnePlannedStep(Direction direction)
    {
        Vector3 moveVec = Vector3.zero;
        //Quaternion rot = Quaternion.identity;

        switch (direction)
        {
            case Direction.Up:
                moveVec = Vector3.forward;
                //rot = Quaternion.Euler(0, 0, 0);
                break;

            case Direction.Down:
                moveVec = Vector3.back;
                //rot = Quaternion.Euler(0, 180, 0);
                break;

            case Direction.Left:
                moveVec = Vector3.left;
                //rot = Quaternion.Euler(0, 270, 0);
                break;

            case Direction.Right:
                moveVec = Vector3.right;
                //rot = Quaternion.Euler(0, 90, 0);
                break;

            default:
                break;
        }

        indicator.transform.position += -moveVec * stepLength;
    }

    public void moveOneStep(Direction direction)
    {
        Vector3 moveVec = Vector3.zero;
        //Quaternion rot = Quaternion.identity;

        switch (direction)
        {
            case Direction.Up:
                moveVec = Vector3.forward;
                //rot = Quaternion.Euler(0, 0, 0);
                break;

            case Direction.Down:
                moveVec = Vector3.back;
                //rot = Quaternion.Euler(0, 180, 0);
                break;

            case Direction.Left:
                moveVec = Vector3.left;
                //rot = Quaternion.Euler(0, 270, 0);
                break;

            case Direction.Right:
                moveVec = Vector3.right;
                //rot = Quaternion.Euler(0, 90, 0);
                break;

            default:
                break;
        }

        prevMovePointPos = movePoint; 
        movePoint += moveVec * stepLength;

        this.transform.position = movePoint;
    }

    private void OnTriggerEnter(Collider col)
    {
        //Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
        switch (col.gameObject.tag)
        {
            case "Goal":
                //Debug.Log("Triggered Goal");
                if (CheckRightGoal(col.gameObject))
                {
                    LocalGameManager.Instance.GoalReached(CharacterId);
                    Destroy(col.gameObject);
                }
                break;

            case "Door":
                //Debug.Log("Triggered Door");
                //After collect all the goal, the door can be stepped and end game
                if (LocalGameManager.Instance.goalCount == 3)
                { //take th econditional logic out of the character and move it to the Manager
                    LocalGameManager.Instance.NextLevel();
                }
                break;

            default:
                //Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
                break;
        }
    }

    private bool CheckRightGoal(GameObject goal)
    {
        if (config.type == CharacterConfig.CharacterType.Dwarf && goal.name.Contains("DwarfGoal"))
        {
            return true;
        }
        else if (config.type == CharacterConfig.CharacterType.Giant && goal.name.Contains("GiantGoal"))
        {
            return true;
        }
        else if (config.type == CharacterConfig.CharacterType.Human && goal.name.Contains("HumanGoal"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void HealthReduced()
    {
        health -= 1;
        Debug.Log(string.Format("Character {0} health: {1}", config.characterName, health));

        if (health == 0)
        {
            Debug.Log(string.Format("Character {0} Died!", config.characterName));
        }
    }
}
