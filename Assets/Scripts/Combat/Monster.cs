using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour {
    public enum MonsterState {
        Idle,
        Attacking
    }

    [HideInInspector] public MonsterState state;
    public int monsterType; // L_Monster is 1, M_Monster is 2, S_Monster is 3
    public int[] targetValues;
    public bool generateTargetValues = false;
    private Animator animator;

    // Start is called before the first frame update
    void Start() {
        animator = GetComponent<Animator>();
        SetMonsterState(MonsterState.Idle);
        //animator.SetBool("Idle", true);
        //state = MonsterState.Idle;
        if (generateTargetValues) {
            targetValues = new int[monsterType];
            List<int> numRandom = new List<int> { 1, 2, 3, 4, 5, 6 };

            for (int i = 0; i < monsterType; i++) {
                int index = Random.Range(0, numRandom.Count - 1);
                targetValues[i] = numRandom[index];
                numRandom.RemoveAt(index);
            }
        }
        else {
            if(monsterType != targetValues.Length) {
                Debug.LogWarningFormat("MonsterType: {0} does not match targetValue Size: {1}", monsterType, targetValues.Length);
            }
        }
    }

    public void SetMonsterState(MonsterState newState) {
        if (newState != state) {
            if (animator != null) {
                switch (newState) {
                    case MonsterState.Idle:
                        animator.SetBool("Attack", false);
                        animator.SetBool("Idle", true);
                        break;
                    case MonsterState.Attacking:
                        animator.SetBool("Attack", true);
                        animator.SetBool("Idle", false);
                        break;
                }
            }
            state = newState;
        }
    }
}
