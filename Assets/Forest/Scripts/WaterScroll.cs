using UnityEngine;

// 물결: 연못 재질의 텍스처 오프셋을 시간에 따라 옮겨 노멀맵 무늬가 흐르게 한다.
// ConditionManager가 조건 A에서 이 컴포넌트를 끄면(enabled = false) 물결이 그 자리에서 멈춘다.
public class WaterScroll : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.02f, 0.012f);   // 초당 UV 이동량

    Material material;
    Vector2 offset;

    void Awake()
    {
        // .material = 이 연못 전용 복사본 → Water.mat 에셋 파일은 바뀌지 않는다
        material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        offset += speed * Time.deltaTime;
        offset.x %= 1f;
        offset.y %= 1f;
        material.SetTextureOffset("_BaseMap", offset);   // URP Lit은 노멀맵도 _BaseMap 타일링·오프셋을 같이 씀
    }
}
