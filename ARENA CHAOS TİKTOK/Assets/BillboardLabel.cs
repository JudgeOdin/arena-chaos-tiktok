using UnityEngine;

public class BillboardLabel : MonoBehaviour
{
    private Camera mainCamera;

    private void LateUpdate()
    {
        mainCamera ??= Camera.main;
        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }
    }
}
