using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCharacter : MonoBehaviour
{
    public enum Direction
    {
        Left = 1, Right = 2, Up = 3, Down = 4
    }

    public float speed;
    private Vector3 movePoint;
    private Vector3 prevMovePointPos;

    private float stepLength;

    public int playerId;

    public int moveCount;

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

        moveCount = 0;

        health = 3;
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void startPlanning() {
        indicator.SetActive(true);
    }
}
