using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Splash : MonoBehaviour
{

    public Transform blackBot, blackMid, blackTop, eta;

    public float strikeSpeed = 0.1f;
    public float etaDuration = 0.7f;

    Vector3 etaFinish = new Vector3(68,-23,0), etaStart = new Vector3(-355,-23,0);

    public AudioSource zetaAS, pew, pew2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(splish());
    }

    IEnumerator splish()
    {
        yield return new WaitForSeconds(3f);
        pew.Play();
        while(blackBot.localPosition.x > -822)
        {
            blackBot.Translate(Vector3.left, Space.Self);
            yield return new WaitForSeconds(strikeSpeed);
        }
        yield return new WaitForSeconds(0.5f);
        pew2.Play();
        while(blackMid.localPosition.x < 234)
        {
            blackMid.Translate(Vector3.right, Space.Self);
            yield return new WaitForSeconds(strikeSpeed);
        }
        pew.Stop();
        pew.Play();
        while(blackTop.localPosition.x > -658)
        {
            blackTop.Translate(Vector3.left, Space.Self);
            yield return new WaitForSeconds(strikeSpeed);
        }

        yield return new WaitForSeconds(1f);

        float timeElapsed = 0f;

        zetaAS.Play();
        while (timeElapsed < etaDuration)
        {
            float t = timeElapsed / etaDuration;

            eta.localPosition = Vector3.Lerp(etaStart, etaFinish, t);

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        eta.localPosition = etaFinish;

        yield return new WaitForSeconds(1f);
        float yield = FindFirstObjectByType<Fading>().BeginFade(1);

        yield return new WaitForSeconds(yield);
        SceneManager.LoadScene("Main");
    }
}
