using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System;

public class MapLoader
{
    private const string MAP_FOLDER = "Maps";

    public IEnumerator LoadMap(string level, Action<MapData> onLoaded)
    {
        string fileName = $"Map_Level{level}.json";
        string fullPath = Path.Combine(Application.streamingAssetsPath, MAP_FOLDER, fileName); // 폴더 위치 주소 병합

        UnityWebRequest request = UnityWebRequest.Get(new Uri(fullPath).AbsoluteUri); // 파일 URL 객체 받아오기

        yield return request.SendWebRequest(); // 파일 로딩 대기 

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonTxt = request.downloadHandler.text;          // 받아온 Json 텍스트  
            MapData data = JsonUtility.FromJson<MapData>(jsonTxt);  // MapData로 역직렬화 실시 
            onLoaded?.Invoke(data);                                 // LevelInitializer 콜백
        }
        else
        {
            onLoaded?.Invoke(null);
        }
    }
}
