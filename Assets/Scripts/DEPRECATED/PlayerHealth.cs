using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private int health;
    public GameObject[] heart;
    public GameObject[] brokeHeart;
    void Start()
    {
        health = 3;
        heart = new GameObject[3];
        brokeHeart = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            heart[i] = GameObject.Find("UI").transform.GetChild(2).transform.GetChild(i).gameObject;
            brokeHeart[i] = GameObject.Find("UI").transform.GetChild(2).transform.GetChild(i + 3).gameObject;
        }
    }
    private void Update()
    {
        if (health == 0)
        {
            GameManager.Instance.CallEndGame();
        }
    }
    public int Gethealth()
    {
        return health;
    }

    public void Damage()
    {
        health--;
        brokeHeart[2 - health].SetActive(true);
        heart[2 - health].SetActive(false);
    }
}
