using UnityEngine;
using UnityEngine.InputSystem;


#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
public class LensDistortionCorrectionProcessor : InputProcessor<Vector2>
{
    [Tooltip("Match this exactly with the Lens Distortion Volume's Intensity.")]
    public float intensity = 0.34f;

    [Tooltip("Match this exactly with the Lens Distortion Volume's X Multiplier.")]
    public float xMult = 1f;

    [Tooltip("Match this exactly with the Lens Distortion Volume's Y Multiplier.")]
    public float yMult = 1f;

    [Tooltip("Match this exactly with the Lens Distortion Volume's Center X (default 0.5).")]
    public float centerX = 0.5f;

    [Tooltip("Match this exactly with the Lens Distortion Volume's Center Y (default 0.5).")]
    public float centerY = 0.5f;

    [Tooltip("Match this exactly with the Lens Distortion Volume's Scale (default 1).")]
    public float scale = 1.1f;

    const float HALF_MIN = 6.103515625e-5f;

#if UNITY_EDITOR
    static LensDistortionCorrectionProcessor()
    {
        Initilize();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initilize()
    {
        InputSystem.RegisterProcessor<LensDistortionCorrectionProcessor>("LensDistortionCorrection");
    }

    public override Vector2 Process(Vector2 screenPos, InputControl control)
    {
        if(Screen.width<=0||Screen.height<=0)
            return screenPos;
        
        Vector2 uv = new Vector2(screenPos.x/Screen.width,screenPos.y/Screen.height);

        float amount = 1.6f*Mathf.Max(Mathf.Abs(intensity*100f),1f);
        float theta = Mathf.Deg2Rad*Mathf.Min(160f,amount);
        float sigma = 2f*Mathf.Tan(theta*0.5f);

        Vector2 center = new Vector2(centerX,centerY)*2f-Vector2.one;
        Vector2 axis = new Vector2(Mathf.Max(xMult,1e-4f),Mathf.Max(yMult,1e-4f));

        float distTheta = intensity>=0f?theta:1f/theta;
        float distSigma = sigma;
        float distScale = 1f/Mathf.Max(scale,1e-4f);
        float distIntensity = intensity*100f;

        uv = (uv-(Vector2.one*0.5f))*distScale+(Vector2.one*0.5f);
        Vector2 ruv = Vector2.Scale(axis,uv-(Vector2.one*0.5f)-center);
        float ru = ruv.magnitude;

        float ruFactor = distIntensity>0f
            ?Mathf.Tan(ru*distTheta)/(ru*distSigma+HALF_MIN)
            :Mathf.Atan(ru*distSigma)*distTheta/(ru+HALF_MIN);

        uv += ruv*(ruFactor-1f);

        return new Vector2(uv.x*Screen.width,uv.y*Screen.height);
    }
}
