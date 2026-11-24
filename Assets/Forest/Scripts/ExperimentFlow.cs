using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

// 실험 한 회차의 흐름(R8):
// 시작(참여자 번호 → 조건 순서) → [스트레스 과제 2분 → 숲 산책 3분 → 설문 안내] × 2조건 → 종료
// 조건 순서 역균형화(1단계): 홀수 = A→B, 짝수 = B→A
public class ExperimentFlow : MonoBehaviour
{
    public enum Phase { Start, Stress, Walk, Survey, End }

    [Header("연결 (비우면 씬에서 자동으로 찾음)")]
    public ConditionManager conditions;
    public StressTask stressTask;
    public FirstPersonController player;
    public StarterAssetsInputs playerInput;

    [Header("시간 (초)")]
    public float walkDuration = 180f;

    [Header("테스트 배속 (에디터·Development 빌드에서만 F9로 1→5→20배)")]
    public float[] debugSpeeds = { 1f, 5f, 20f };

    public Phase Current { get; private set; } = Phase.Start;
    public int Participant { get; private set; }
    public char[] Order { get; private set; }
    public int Step { get; private set; }               // 0 = 첫 번째 조건, 1 = 두 번째 조건
    public float WalkTimeLeft { get; private set; }
    public float WalkDistance { get; private set; }
    public char CurrentCondition => Order[Step];

    CharacterController cc;
    Vector3 spawnPos;
    Quaternion spawnRot;
    Vector3 lastPos;
    int speedIndex;
    string numberText = "";
    GUIStyle titleStyle, textStyle, fieldStyle, smallStyle;

    void Awake()
    {
        if (conditions == null) conditions = FindFirstObjectByType<ConditionManager>();
        if (stressTask == null) stressTask = GetComponent<StressTask>();
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (playerInput == null) playerInput = player.GetComponent<StarterAssetsInputs>();
        cc = player.GetComponent<CharacterController>();
        spawnPos = player.transform.position;
        spawnRot = player.transform.rotation;
        Time.timeScale = 1f;   // 실제 빌드 기본값은 정상 속도
    }

    void Start()
    {
        conditions.SetCondition('A');   // 시작 화면·과제 중에는 무음·정지(두 조건 공통)
        LockPlayer(true);
    }

    // 홀수 = A→B, 짝수 = B→A
    public static char[] OrderFor(int participant) =>
        participant % 2 == 1 ? new[] { 'A', 'B' } : new[] { 'B', 'A' };

    public void StartExperiment(int participant)
    {
        if (Current != Phase.Start || participant <= 0) return;
        Participant = participant;
        Order = OrderFor(participant);
        Step = 0;
        BeginStress();
    }

    void BeginStress()
    {
        Current = Phase.Stress;
        conditions.SetCondition('A');   // 과제는 조건과 무관하게 같은 화면·같은 소리 환경
        LockPlayer(true);
        stressTask.Begin(BeginWalk);
    }

    void BeginWalk()
    {
        Current = Phase.Walk;
        // 두 조건 모두 같은 지점·방향에서 출발
        cc.enabled = false;
        player.transform.SetPositionAndRotation(spawnPos, spawnRot);
        cc.enabled = true;
        lastPos = player.transform.position;
        WalkDistance = 0f;
        WalkTimeLeft = walkDuration;
        conditions.SetCondition(CurrentCondition);
        LockPlayer(false);
    }

    void EndWalk()
    {
        LockPlayer(true);
        conditions.SetCondition('A');
        ExperimentLog.Append(Participant, CurrentCondition, Step + 1,
                             stressTask.Correct, stressTask.Wrong, WalkDistance);
        Current = Phase.Survey;
    }

    // 설문 화면에서 Space/Enter(또는 AI 테스트)가 부른다.
    public void NextFromSurvey()
    {
        if (Current != Phase.Survey) return;
        if (Step == 0) { Step = 1; BeginStress(); }
        else Current = Phase.End;
    }

    // 과제·설문 중에는 이동·시선을 막고 커서를 보여 준다.
    void LockPlayer(bool locked)
    {
        player.enabled = !locked;
        playerInput.move = Vector2.zero;
        playerInput.look = Vector2.zero;
        playerInput.cursorLocked = !locked;     // StarterAssets가 포커스 때 다시 잠그지 않게
        playerInput.cursorInputForLook = !locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }

    public void SetDebugSpeed(float speed) => Time.timeScale = speed;

    void Update()
    {
        if ((Application.isEditor || Debug.isDebugBuild) && Keyboard.current != null &&
            Keyboard.current.f9Key.wasPressedThisFrame)
        {
            speedIndex = (speedIndex + 1) % debugSpeeds.Length;
            SetDebugSpeed(debugSpeeds[speedIndex]);
        }

        if (Current == Phase.Walk)
        {
            // 수평 이동 거리만 누적(점프·경사의 높이 변화는 뺌)
            Vector3 p = player.transform.position;
            Vector3 d = p - lastPos; d.y = 0f;
            WalkDistance += d.magnitude;
            lastPos = p;

            WalkTimeLeft -= Time.deltaTime;
            if (WalkTimeLeft <= 0f) EndWalk();
        }
        else if (Current == Phase.Survey && Keyboard.current != null &&
                 (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            NextFromSurvey();
        }
    }

    void OnDestroy() => Time.timeScale = 1f;

    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return total / 60 + ":" + (total % 60).ToString("00");
    }

    void OnGUI()
    {
        float s = Screen.height / 1080f;
        titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        textStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        fieldStyle ??= new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter };
        smallStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight };
        titleStyle.fontSize = Mathf.RoundToInt(64 * s);
        textStyle.fontSize = Mathf.RoundToInt(38 * s);
        fieldStyle.fontSize = Mathf.RoundToInt(56 * s);
        smallStyle.fontSize = Mathf.RoundToInt(22 * s);
        float w = Screen.width, h = Screen.height;

        if (Time.timeScale != 1f)   // 테스트 배속 중임을 항상 표시(실험 중 실수 방지)
            GUI.Label(new Rect(0, 8 * s, w - 16 * s, 30 * s), "배속 x" + Time.timeScale + " (F9)", smallStyle);

        switch (Current)
        {
            case Phase.Start: DrawStart(w, h, s); break;
            case Phase.Walk:
                GUI.Label(new Rect(0, 30 * s, w, 60 * s), "숲을 자유롭게 산책하세요   남은 시간 " + FormatTime(WalkTimeLeft), textStyle);
                break;
            case Phase.Survey:
                Backdrop(w, h);
                GUI.Label(new Rect(0, h * 0.25f, w, 100 * s), "설문을 작성해 주세요", titleStyle);
                GUI.Label(new Rect(w * 0.1f, h * 0.40f, w * 0.8f, 160 * s),
                          "참여자 번호: " + Participant + "     조건 코드: " + CurrentCondition +
                          "\n(" + (Step + 1) + "/2번째 조건)", textStyle);
                GUI.Label(new Rect(0, h * 0.68f, w, 60 * s),
                          Step == 0 ? "설문을 마치면 Space 키 → 다음 조건(스트레스 과제 → 산책)" : "설문을 마치면 Space 키 → 종료", textStyle);
                break;
            case Phase.End:
                Backdrop(w, h);
                GUI.Label(new Rect(0, h * 0.25f, w, 100 * s), "실험이 끝났습니다. 감사합니다!", titleStyle);
                GUI.Label(new Rect(w * 0.05f, h * 0.45f, w * 0.9f, 200 * s),
                          "참여자 " + Participant + " · 순서 " + Order[0] + "→" + Order[1] +
                          "\n기록 파일: " + ExperimentLog.FilePath, textStyle);
                break;
        }
    }

    void Backdrop(float w, float h)
    {
        GUI.color = new Color(0.08f, 0.1f, 0.08f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawStart(float w, float h, float s)
    {
        Backdrop(w, h);
        GUI.Label(new Rect(0, h * 0.18f, w, 100 * s), "가상 숲 실험", titleStyle);
        GUI.Label(new Rect(0, h * 0.32f, w, 60 * s), "참여자 번호를 입력하세요", textStyle);

        bool valid = int.TryParse(numberText, out int n) && n > 0;
        Event e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            if (valid) StartExperiment(n);
            e.Use();
        }
        GUI.SetNextControlName("pid");
        numberText = GUI.TextField(new Rect(w / 2 - 160 * s, h * 0.40f, 320 * s, 90 * s), numberText, 3, fieldStyle);
        numberText = System.Text.RegularExpressions.Regex.Replace(numberText, "[^0-9]", "");
        GUI.FocusControl("pid");

        if (valid)
        {
            char[] o = OrderFor(n);
            GUI.Label(new Rect(0, h * 0.55f, w, 60 * s),
                      "조건 순서: " + o[0] + " → " + o[1] + (n % 2 == 1 ? "  (홀수)" : "  (짝수)"), textStyle);
            GUI.Label(new Rect(0, h * 0.66f, w, 60 * s), "Enter를 누르면 스트레스 과제(2분)가 시작됩니다", textStyle);
        }
    }
}
