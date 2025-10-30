using UnityEngine;

public class CameraManager
{
    private readonly Camera _mainCamera;
    private readonly Camera _uiCamera;
    private readonly float _cameraPadding;

    public CameraManager(Camera mainCamera, Camera uiCamera, float cameraPadding)
    {
        _mainCamera = mainCamera;
        _uiCamera = uiCamera;
        _cameraPadding = cameraPadding;
    }

    public void AdjustCameraView(Bounds boardBounds)    // 보드가 카메라에 항상 꽉 차게 보이도록 설정
    {
        if (boardBounds.size == Vector3.zero)   // 보드 크기가 0이라면 -> 기본 크기로 설정 (오류)
        {
            if (_mainCamera != null)
            {
                _mainCamera.orthographicSize = 5f;
                if (_uiCamera != null)
                    _uiCamera.orthographicSize = 5f;
                _mainCamera.transform.position = new Vector3(0, 0, -10f);
            }
            return;
        }

        float boardWorldWidth = boardBounds.size.x + (_cameraPadding * 2);  // 실제 여백 계산
        float boardWorldHeight = boardBounds.size.y + (_cameraPadding * 2);
        float screenAspect = (Screen.width > 0 && Screen.height > 0) ? (float)Screen.width / Screen.height : 1f;    // 가로세로 비율 계산
        float targetOrthoSize = (boardWorldWidth / 2f) / screenAspect;  // 가로로 꽉 채우기 위한 사이즈 계산
        float minOrthoSizeForHeight = boardWorldHeight / 2f;    // 세로를 꽉 채우기 위한 비율 계산
        float finalOrthoSize = Mathf.Max(targetOrthoSize, minOrthoSizeForHeight, 2.0f); // 가로 세로중 더 큰값 선택

        if (_mainCamera != null)    // 카메라에 해당값을 적용
        {
            _mainCamera.orthographicSize = finalOrthoSize;
            if (_uiCamera != null)
            {
                _uiCamera.orthographicSize = finalOrthoSize;
            }
            _mainCamera.transform.position = new Vector3(0, 0, -30f);
        }
    }
}