using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class AnimationManager : MonoBehaviour
{
    public GameObject[] scenes;
    public Image[] charactors;
    public Image[] swords;
    public Text[] txts;
    public Image[] monsters;
    public Image evil;
    public GameObject[] dropMonsters;
    public GameObject[] charactors_scene4;
    public Image hand;
    public Image[] weapons;
    public Image[] alters;
    public Image[] charactors_scene5;

    private float fadeTime = 1f;

    public GameObject tutorial;
    private Coroutine animationCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        animationCoroutine = StartCoroutine(StartAnimation());
    }

    private IEnumerator StartAnimation()
    {
        // Scene 1
        ShowTxt(txts[0], 5f);
        StartCoroutine(CharactorDrop());
        yield return new WaitForSeconds(6f);
        ShowTxt(txts[1], 4f);
        ShowSwords();
        yield return new WaitForSeconds(1.5f);
        SwordFight();
        yield return new WaitForSeconds(4f);
        scenes[0].SetActive(false);

        // Scene 2
        scenes[1].SetActive(true);
        ShowTxt(txts[2], 5f);
        StartCoroutine(Drop(evil, 62));
        yield return new WaitForSeconds(2f);
        ShowMonsters();
        yield return new WaitForSeconds(4f);
        scenes[1].SetActive(false);

        // Scene 3
        scenes[2].SetActive(true);
        ShowTxt(txts[3], 4f);
        StartCoroutine(DropMonsters());
        yield return new WaitForSeconds(5f);
        ShowTxt(txts[4], 6f);
        StartCoroutine(DropMoreMonsters());
        yield return new WaitForSeconds(7f);
        scenes[2].SetActive(false);

        // Scene 4
        scenes[3].SetActive(true);
        ShowTxt(txts[5], 8f);
        StartCoroutine(FadeIn(hand));
        StartCoroutine(CharactorsMoveClose());
        yield return new WaitForSeconds(5f);
        scenes[3].SetActive(false);

        // Scene 5
        scenes[4].SetActive(true);
        ShowTxt(txts[6], 5f);
        StartCoroutine(FadeInOut(weapons[0], 2f));
        yield return new WaitForSeconds(1.5f);
        ShowWeapon();
        yield return new WaitForSeconds(2f);
        ShowAlter();
        yield return new WaitForSeconds(4f);
        ShowTxt(txts[7], 5f);
        ShowCharactors();
        yield return new WaitForSeconds(6f);
        scenes[4].SetActive(false);

        // Scene 6
        scenes[5].SetActive(true);
        ShowTxt(txts[8], 5f);

        yield return new WaitForSeconds(4f);
        tutorial.SetActive(false);
        GameManager.Instance.CallEndTutorial();
    }

    private IEnumerator FadeOut(Image img)
    {
        Color color = img.color;
        for (float a = fadeTime; a >= 0; a -= Time.deltaTime)
        {
            img.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }
    private IEnumerator FadeOut(Text txt)
    {
        Color color = txt.color;
        for (float a = fadeTime; a >= 0; a -= Time.deltaTime)
        {
            txt.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }
    private IEnumerator FadeIn(Image img)
    {
        Color color = img.color;
        for (float a = 0; a <= fadeTime; a += Time.deltaTime)
        {
            img.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }
    private IEnumerator FadeIn(Text txt)
    {
        Color color = txt.color;
        for (float a = 0; a <= fadeTime; a += Time.deltaTime)
        {
            txt.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }

    private IEnumerator FadeInOut(Image img, float waitTime)
    {
        StartCoroutine(FadeIn(img));
        yield return new WaitForSeconds(waitTime);
        StartCoroutine(FadeOut(img));
    }

    private IEnumerator FadeInOut(Text txt, float waitTime)
    {
        StartCoroutine(FadeIn(txt));
        yield return new WaitForSeconds(waitTime);
        StartCoroutine(FadeOut(txt));
    }

    private void ShowTxt(Text txt, float waitTime)
    {
        StartCoroutine(FadeInOut(txt, waitTime));
    }


    private void ShowSwords()
    {
        foreach (Image s in swords){
            StartCoroutine(FadeIn(s));
        }
    }

    private void SwordFight()
    {
        for (int i = 0; i < 6; i++)
        {
            RotateSword(swords[i]); 
        }
    }

    private void RotateSword(Image img)
    {
        img.gameObject.GetComponent<Animator>().SetTrigger("Rotate");
    }

    private IEnumerator CharactorDrop()
    {
        StartCoroutine(Drop(charactors[0], 88.0f));
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(Drop(charactors[1], 155.0f));
        yield return new WaitForSeconds(1f);
        StartCoroutine(Drop(charactors[2], 76.0f));
    }

    private IEnumerator Drop(Image img, float EndPosY)
    {
        while (img.rectTransform.localPosition.y > EndPosY)
        {
            img.rectTransform.localPosition = new Vector3(img.rectTransform.localPosition.x, img.rectTransform.localPosition.y - Time.deltaTime*300, 0);
            yield return null;
        }
    }
    private IEnumerator Drop(GameObject obj)
    {
        //Debug.Log(obj.GetComponent<RectTransform>().localPosition.x);
        while (obj.GetComponent<RectTransform>().localPosition.y > 0)
        {
            obj.GetComponent<RectTransform>().localPosition = new Vector3(obj.GetComponent<RectTransform>().localPosition.x, obj.GetComponent<RectTransform>().localPosition.y - Time.deltaTime * 300, 0);
            yield return null;
        }
    }

    private void ShowMonsters()
    {
        foreach (Image s in monsters)
        {
            StartCoroutine(FadeIn(s));
        }
    }

    private IEnumerator DropMonsters()
    {
        StartCoroutine(Drop(dropMonsters[0]));
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(Drop(dropMonsters[1]));
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator DropMoreMonsters()
    {
        for (int m = 2; m < 7; m++)
        {
            StartCoroutine(Drop(dropMonsters[m]));
            yield return new WaitForSeconds(1f);
        }
    }

/*    private void DropCharactors()
    {
        foreach (GameObject c in charactors_scene4)
        {
            StartCoroutine(Drop(c));
        }
    }*/

    private IEnumerator CharactorsMoveClose()
    {
        while (Vector3.Distance(charactors_scene4[0].GetComponent<RectTransform>().localPosition, charactors_scene4[2].GetComponent<RectTransform>().localPosition) < 137f)
        {
            charactors_scene4[0].GetComponent<RectTransform>().localPosition = new Vector3(charactors_scene4[0].GetComponent<RectTransform>().localPosition.x - Time.deltaTime * 200, charactors_scene4[0].GetComponent<RectTransform>().localPosition.y, 0);
            charactors_scene4[2].GetComponent<RectTransform>().localPosition = new Vector3(charactors_scene4[2].GetComponent<RectTransform>().localPosition.x + Time.deltaTime * 200, charactors_scene4[2].GetComponent<RectTransform>().localPosition.y, 0);
            yield return null;
        }
    }

    private void ShowWeapon()
    {
        for (int i = 1; i < 4; i++)
        {
            StartCoroutine(FadeIn(weapons[i]));
        }
    }

    private void ShowAlter()
    {
        foreach (Image a in alters)
        {
            StartCoroutine(FadeInOut(a, 2.5f)); 
        }
    }

    private void ShowCharactors()
    {
        foreach (Image c in charactors_scene5)
        {
            StartCoroutine(FadeIn(c));
        }
    }

    public void LoadGameScene()
    {
        //PhotonNetwork.LoadLevel("Level_1");
        StopCoroutine(animationCoroutine);
        tutorial.SetActive(false);
        GameManager.Instance.CallEndTutorial();
    }
}
