using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Fading : MonoBehaviour
{
    public Texture2D fadeOutTexture;
    public float fadeSpeed = 2f;
    private int drawDepth = -1000;
    [SerializeField] float alpha = 1.0f;
    private int fadeDir = -1;
    [SerializeField] Material retroMat;
    float strength = 1;

    private void Awake(){
        SceneManager.sceneLoaded
        +=SceneLoaded;
    } void OnGUI(){
        alpha += fadeDir * fadeSpeed * Time.deltaTime;

        strength += fadeDir * fadeSpeed * Time.deltaTime * 16f;
        strength = Mathf.Clamp(strength, 0.05f, 30f);
        retroMat.SetFloat("_DistortionStrength", strength);
        
        alpha = Mathf.Clamp01(alpha);
        GUI.color = new Color(
            GUI.color.r,
            GUI.color.g,
            GUI.color.b,
            alpha);
        GUI.depth = drawDepth;
        GUI.DrawTexture(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height),
                fadeOutTexture);
        // Debug.Log(alpha);
    } public float BeginFade(int direction){
        fadeDir = direction;
        alpha = Mathf.Clamp01(-direction);
        return(fadeSpeed);
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode){
        BeginFade(-1);
    }
}
