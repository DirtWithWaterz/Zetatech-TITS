using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(InputSystemUIInputModule))]
public class LensDistortionInputCorrector : MonoBehaviour
{
    [Header("Lens Distortion Settings")]
    [Tooltip("Match these settings exactly with the Lens Distortion Volume.")]
    public float intensity = 0.34f;
    public float xMult = 1f;
    public float yMult = 1f;
    public Vector2 center = new Vector2(0.5f,0.5f);
    public float scale = 1.1f;

    void Awake()
    {
        var _module = GetComponent<InputSystemUIInputModule>();
        var _pointAction = _module.point != null ? _module.point.action : null;

        if(_pointAction == null)
        {
            Debug.LogWarning("LensDistortionInputCorrector: InputSystemUIInputModule has no 'Point' action assigned.", this);
            return;
        }

        string _processors = 
            "LensDistortionCorrection(" +
            $"intensity={F(intensity)}," +
            $"xMult={F(xMult)}," +
            $"yMult={F(yMult)}," +
            $"centerX={F(center.x)}," +
            $"centerY={F(center.y)}," +
            $"scale={F(scale)})";

        for(int i = 0; i < _pointAction.bindings.Count; i++)
            _pointAction.ApplyBindingOverride(i, new InputBinding{ overrideProcessors = _processors});
    }

    static string F(float value) => value.ToString(CultureInfo.InvariantCulture);
}
