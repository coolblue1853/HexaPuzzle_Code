using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    public void InitBackgroundScaler() // 카메라 기준 배경 크기 설정(세로를 기준)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        float camHeight = cam.orthographicSize * 2f;
        transform.localScale = new Vector3(camHeight, camHeight, 1f);
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 100f);
    }
}