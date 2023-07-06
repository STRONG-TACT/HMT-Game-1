using System.Collections;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class CombatSystem : MonoBehaviour {
    public static CombatSystem Instance;

    public bool Initialized { get; private set; }

    public enum FightType {
        Rock=0,
        Trap=1,
        Monster=2
    }

    public enum FightState {
        Idle,
        Waiting,
        Rolling,
        Result
    }


    public FightState State { get; private set;} = FightState.Idle;

    private GameAssets gameAssets;

    [HideInInspector] public GameObject target;
    [HideInInspector] public Character characterInFight;

    //[HideInInspector] public bool isInFight;
    //[HideInInspector] public bool fightEnd;
    [HideInInspector] public FightType fightType; // 0: Fight Rock, 1: fight Trap, 2: Fight Monster
    [HideInInspector] public int attemptCount;
    public bool autoReroll;

    //[HideInInspector] public int playerDiceNum;
    [HideInInspector] public int[] monsterNum;

    PhotonView photonView;
    UIManager uiManager;
    GameData gameData;
    DiceRoll die;
    GameObject roller;

    IEnumerator Start() {
        uiManager = FindObjectOfType<UIManager>();
        gameData = FindObjectOfType<GameData>();
        Instance = this;
        photonView = GetComponent<PhotonView>();
        attemptCount = 0;
        if (PhotonNetwork.IsMasterClient) {
            roller = PhotonNetwork.Instantiate("Roller", transform.position + Vector3.up * 100, Quaternion.identity);
            die = roller.GetComponentInChildren<DiceRoll>();
        }
        else {
            while (die == null) {
                die = FindObjectOfType<DiceRoll>();
                if (die != null) {
                    roller = die.transform.parent.gameObject;
                }
                yield return null;
            }
        }
        
        Initialized = true;
        yield break;
    }

    public void StartFight(Character character, GameObject target) {
        Debug.LogFormat("Start Fight: {0} vs {1}", character.name, target.name);
        //GameObject enemy, FightType fightType, Character playerObj) {
        //Debug.Log("StartFight");
        characterInFight = character;
        this.target = target;
        fightType = this.target.tag switch {
            "Rock" => FightType.Rock,
            "Trap" => FightType.Trap,
            "Monster" => FightType.Monster,
            _ => FightType.Monster,
        };

        characterInFight.State = Character.CharacterState.Attacking;

        //setup the roller
        die.ConfigureDie(characterInFight.config.dieFaces);
        roller.transform.position = character.transform.position;
        
        if (this.fightType == FightType.Monster) {
            monsterNum = this.target.GetComponent<Monster>().targetValues;
            this.target.GetComponent<Animator>().SetBool("Attack", true);
            this.target.GetComponent<Animator>().SetBool("Idle", false);
        }
        else {
            monsterNum = new int[0];
        }
        uiManager.ShowCombatUI(fightType, monsterNum);
        State = FightState.Waiting;
    }
    
    public void RollAttack() {
        Debug.Log("RollAttack"); 
        if (State == FightState.Waiting) {
           StartCoroutine(RollAttackCoroutine());
        }
    }

    private IEnumerator RollAttackCoroutine() {
        State = FightState.Rolling;
        die.CallReroll();
        while(die.dieState != DiceRoll.DieState.Stopped) {
            yield return null;
        }
        switch (fightType) {
            case FightType.Trap: 
                yield return TrapFightCheck();
                break;
            case FightType.Rock: 
                yield return RockFightCheck();
                break;
            case FightType.Monster:
                yield return MonsterFightCheck();
                break;

        }
        yield break;
    }

    private IEnumerator RockFightCheck() {
        if(die.GetFaceValue() >= 5) {
            State = FightState.Result;
            CallCombatResult("You broke the rock!");
            yield return new WaitForSeconds(2f);
            CallFightWin();
        }
        else {
            attemptCount += 1;
            if (attemptCount < gameData.maxCombatAttempts) {
                //reset
                CallCombatResult(string.Format("The rock didn't budge. Try again? {0} chances left.", gameData.maxCombatAttempts - attemptCount));
                yield return new WaitForSeconds(2f);
                if(autoReroll) {
                    RollAttack();
                    yield break;
                }
                else {
                    State = FightState.Waiting;
                }
            }
            else {
                CallCombatResult("You couldn't move the rock...");
                yield return new WaitForSeconds(2f);
                CallFightLose();
            }
        }
    }
    
    
    private IEnumerator TrapFightCheck() {
        if (die.GetFaceValue() <= 3) {
            State = FightState.Result;
            CallCombatResult("You disabled the trap!");
            yield return new WaitForSeconds(2f);
            CallFightWin();
        }
        else {
            attemptCount += 1;
            if (attemptCount < gameData.maxCombatAttempts) {
                //reset
                CallCombatResult(string.Format("You failed to disable the trap. Try again? {0} chances left.", gameData.maxCombatAttempts - attemptCount));
                yield return new WaitForSeconds(2f);
                if (autoReroll) {
                    RollAttack();
                    yield break;
                }
                else {
                    State = FightState.Waiting;
                }
            }
            else {
                CallCombatResult("The trap went off!");
                yield return new WaitForSeconds(2f);
                CallFightLose();
            }
        }
    }
    private IEnumerator MonsterFightCheck() {
        if (monsterNum.Contains(die.GetFaceValue())) {
            CallCombatResult("You deafeted the monster!");
            yield return new WaitForSeconds(2f);
            CallFightWin();
        }
        else {
            attemptCount += 1;
            if (attemptCount < gameData.maxCombatAttempts) {
                //reset
                CallCombatResult(string.Format("You failed to defeat the monster. Try again? {0} chances left.", gameData.maxCombatAttempts - attemptCount));
                yield return new WaitForSeconds(2f);
                if (autoReroll) {
                    RollAttack();
                    yield break;
                }
                else {
                    State = FightState.Waiting;
                }
            }
            else {
                CallCombatResult("The monster won! Health -1");
                characterInFight.Damage(1);
                yield return new WaitForSeconds(2f);
                CallFightLose();
            }

        }
    }

    private void EndFight() {
        uiManager.DismissCombatUI();
        characterInFight = null;
        //characterInFight.transform.GetChild(3).gameObject.SetActive(false); // Dice
        //characterInFight.transform.GetChild(4).gameObject.SetActive(false); // Dice Ground
        roller.transform.position = Vector3.up * 100;
        die.ResetDie();
        State = FightState.Idle;
    }

    public void CallCombatResult(string message) {
        photonView.RPC("DisplayCombatResult", RpcTarget.All, message);
    }

    [PunRPC]
    public void DisplayCombatResult(string message) {
        uiManager.DisplayCombatResult(message);
    }

    public void CallFightWin() {
        photonView.RPC("FightWin", RpcTarget.All);
    }
    public void CallFightLose() {
        photonView.RPC("FightLose", RpcTarget.All);
    }
    [PunRPC]
    public void FightWin() {
        //win
        characterInFight.CompleteMove();
        if (PhotonNetwork.IsMasterClient) {
            if (target != null) {
                PhotonNetwork.Destroy(target);
            }
        }

        EndFight();
        //Debug.Log("FightEnd : win");
    }
    [PunRPC]
    public void FightLose() {
        target.GetComponent<Animator>().SetBool("Attack", false);
        target.GetComponent<Animator>().SetBool("Idle", true);
        characterInFight.ResetPosition();
        EndFight();
        //Debug.Log("FightEnd : Lose");
    }

    public void CallDamageCharacter(int characterID, int damage) {
        photonView.RPC("DamageCharacter", RpcTarget.All, characterID, damage);
    }

    [PunRPC]
    public void DamageCharacter(int characterID, int damage) {
        Character character = PhotonView.Find(characterID).GetComponent<Character>();
        character.Damage(damage);
    }
        
}
