using UnityEngine;

// MCP 테스트용: 나무 윗부분을 바람에 흔들리듯 좌우로 기울인다.
// (조건 B '흔들리는 풀·나무'의 가장 단순한 형태 — 3단계에서 다시 정리)
public class Sway : MonoBehaviour
{
    public float angle = 4f;   // 최대 기울기(도)
    public float speed = 1.2f; // 흔들림 속도
    float offset;
    Quaternion baseRot;

    void Start()
    {
        baseRot = transform.localRotation;
        offset = Random.value * 10f; // 나무마다 위상을 달리해 동시에 흔들리지 않게
    }

    void Update()
    {
        float z = Mathf.Sin((Time.time + offset) * speed) * angle;
        transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, z);
    }
}
