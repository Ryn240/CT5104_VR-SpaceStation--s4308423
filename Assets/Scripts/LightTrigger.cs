using UnityEngine;

public class LightTrigger : MonoBehaviour
{
    public bool useFade = true;

    private void OnTriggerEnter(Collider other)
    {
        if (LightingManager.Instance == null) return;

        if (useFade)
        {
            LightingManager.Instance.FadeLightsOn();
        }
        else
        {
            LightingManager.Instance.TurnLightsOn();
        }
    }
}