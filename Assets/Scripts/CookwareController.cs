using UnityEngine;
public class CookwareController
{
    public StoveKnob heatKnob;
    void Update()
    {
        float heat = heatKnob.CurrentValue;
        // ApplyHeat(heat);
    }
}
