public interface IPoolable // 풀링 가능 대상 지정
{
    void OnSpawn();
    void OnDespawn();
}