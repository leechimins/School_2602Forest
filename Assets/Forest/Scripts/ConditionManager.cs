using UnityEngine;

// 실험 조건 전환: A = 정적 숲(무음, 움직임 없음) / B = 동적 숲(소리 + 움직임).
// 나무·지형·조명 같은 시각 배치는 건드리지 않는다 → 두 조건의 시각 환경이 같다(R7).
public class ConditionManager : MonoBehaviour
{
    [Header("시작 조건 ('A' 또는 'B')")]
    public char startCondition = 'A';

    [Header("소리 (B에서만 재생)")]
    public AudioSource[] sounds;          // 새소리·물소리·바람

    [Header("파티클 (B에서만 방출)")]
    public ParticleSystem[] particles;    // 새·나뭇잎

    [Header("물결 (B에서만 스크롤)")]
    public WaterScroll water;

    [Header("풀 바람 (B = 풀 재질 원래 값, A = 0)")]
    public Terrain terrain;
    public float terrainWindSpeed = 0.5f;     // Terrain > Settings > Wind Settings for Grass
    public float terrainWindBending = 0.5f;

    public char Current { get; private set; }

    // 풀 재질·TerrainData는 에셋이라 Play 중 바꾼 값이 에디터에 남는다 → 시작 값을 기억했다가 끝날 때 되돌린다.
    Material[] grassMaterials;
    float[] savedIntensity, savedBlast, savedRipples;
    float savedSpeed, savedBending, savedStrength;

    void Awake()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        TerrainData data = terrain.terrainData;
        // 풀(디테일) 인스턴스 캐시를 다시 만든다. 11/24 프리팹이 Missing인 상태로 로드된 뒤 풀이 하나도 안 그려진 적이 있음.
        data.RefreshPrototypes();

        DetailPrototype[] protos = data.detailPrototypes;
        grassMaterials = new Material[protos.Length];
        savedIntensity = new float[protos.Length];
        savedBlast = new float[protos.Length];
        savedRipples = new float[protos.Length];
        for (int i = 0; i < protos.Length; i++)
        {
            Renderer r = protos[i].prototype != null ? protos[i].prototype.GetComponentInChildren<Renderer>() : null;
            if (r == null || !r.sharedMaterial.HasProperty("Wind_Intensity")) continue;
            grassMaterials[i] = r.sharedMaterial;
            savedIntensity[i] = grassMaterials[i].GetFloat("Wind_Intensity");
            savedBlast[i] = grassMaterials[i].GetFloat("Wind_Blast");
            savedRipples[i] = grassMaterials[i].GetFloat("Wind_Ripples");
        }
        savedSpeed = data.wavingGrassSpeed;
        savedBending = data.wavingGrassAmount;
        savedStrength = data.wavingGrassStrength;
    }

    void Start()
    {
        SetCondition(startCondition);
    }

    // 4단계(실험 흐름)에서 호출: SetCondition('A') / SetCondition('B')
    public void SetCondition(char condition)
    {
        condition = char.ToUpper(condition);
        if (condition != 'A' && condition != 'B')
        {
            Debug.LogWarning("ConditionManager: 조건은 A 또는 B만 가능 — " + condition);
            return;
        }
        Current = condition;
        bool dynamic = condition == 'B';

        foreach (AudioSource sound in sounds)
        {
            if (dynamic) { if (!sound.isPlaying) sound.Play(); }
            else sound.Stop();
        }

        // Project3C 레이저와 같은 방식: EmissionModule을 켜고 끈다
        foreach (ParticleSystem particle in particles)
        {
            var emission = particle.emission;
            emission.enabled = dynamic;
            if (dynamic) { if (!particle.isPlaying) particle.Play(); }
            else particle.Clear();   // 이미 떠 있는 입자도 즉시 없앤다(A는 움직임 0)
        }

        if (water != null) water.enabled = dynamic;

        for (int i = 0; i < grassMaterials.Length; i++)
        {
            if (grassMaterials[i] == null) continue;
            grassMaterials[i].SetFloat("Wind_Intensity", dynamic ? savedIntensity[i] : 0f);
            grassMaterials[i].SetFloat("Wind_Blast", dynamic ? savedBlast[i] : 0f);
            grassMaterials[i].SetFloat("Wind_Ripples", dynamic ? savedRipples[i] : 0f);
        }
        TerrainData data = terrain.terrainData;
        data.wavingGrassSpeed = dynamic ? terrainWindSpeed : 0f;
        data.wavingGrassAmount = dynamic ? terrainWindBending : 0f;
        data.wavingGrassStrength = dynamic ? savedStrength : 0f;

        Debug.Log("ConditionManager: 조건 " + condition + (dynamic ? " (동적 숲)" : " (정적 숲)"));
    }

    void OnDestroy()
    {
        if (grassMaterials == null) return;
        for (int i = 0; i < grassMaterials.Length; i++)
        {
            if (grassMaterials[i] == null) continue;
            grassMaterials[i].SetFloat("Wind_Intensity", savedIntensity[i]);
            grassMaterials[i].SetFloat("Wind_Blast", savedBlast[i]);
            grassMaterials[i].SetFloat("Wind_Ripples", savedRipples[i]);
        }
        if (terrain != null)
        {
            terrain.terrainData.wavingGrassSpeed = savedSpeed;
            terrain.terrainData.wavingGrassAmount = savedBending;
            terrain.terrainData.wavingGrassStrength = savedStrength;
        }
    }
}
