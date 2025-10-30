using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }   // 싱글턴 선언

    private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>(); // 원본 프리팹 , 비활성 오브젝트 큐
    private Dictionary<GameObject, GameObject> _prefabLookup = new Dictionary<GameObject, GameObject>();    // 활성화된 오브젝트. 원본 프리팹

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Instance = this;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null)
            return null;

        if (!_pools.ContainsKey(prefab))        // 해당 프리팹용 큐가 없다면 생성
            _pools[prefab] = new Queue<GameObject>();

        GameObject obj;
        if (_pools[prefab].Count > 0)       // 비활성이 남아있다면 꺼내기 
            obj = _pools[prefab].Dequeue();
        else                                // 없다면 생성
            obj = Instantiate(prefab);

        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        _prefabLookup[obj] = prefab;
        obj.GetComponent<IPoolable>()?.OnSpawn();

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null)
            return;

        if (!_prefabLookup.TryGetValue(obj, out GameObject prefab)) // 조회에서 원본 프리팹 찾기
        {
            Destroy(obj);
            return;
        }

        obj.GetComponent<IPoolable>()?.OnDespawn();

        obj.SetActive(false);
        obj.transform.SetParent(this.transform);
        _pools[prefab].Enqueue(obj);
        _prefabLookup.Remove(obj);
    }

    public void CleanupAll()    // 모든 풀링 오브젝트 정리
    {
        List<GameObject> activeObjects = new List<GameObject>(_prefabLookup.Keys);
        foreach (var obj in activeObjects)
        {
            if (obj != null)
                Despawn(obj);
        }

        foreach (var queue in _pools.Values)
        {
            while (queue.Count > 0)
                Destroy(queue.Dequeue());
        }
        _pools.Clear();
    }
}