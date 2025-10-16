using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var mgr = FindObjectOfType<LevelManagerLogic>();
        if (mgr != null)
            mgr.FinishRun();
    }
}