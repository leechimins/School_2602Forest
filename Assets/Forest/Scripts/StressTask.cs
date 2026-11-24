using System;
using UnityEngine;

// 스트레스 과제: 제한시간(기본 2분) 동안 두 자리 ± 두 자리 암산.
// 문제당 5초, 오답·시간초과면 경고음. 정답 수·오답 수(시간초과 포함)를 센다.
public class StressTask : MonoBehaviour
{
    [Header("시간 (초)")]
    public float duration = 120f;
    public float timePerQuestion = 5f;

    [Header("경고음 (비우면 코드로 만든 비프음)")]
    public AudioSource beepSource;
    public AudioClip beepClip;

    public bool Running { get; private set; }
    public int Correct { get; private set; }
    public int Wrong { get; private set; }        // 오답 + 시간초과
    public int Timeouts { get; private set; }
    public float TimeLeft { get; private set; }
    public float QuestionTimeLeft { get; private set; }
    public string Question { get; private set; }
    public int CurrentAnswer { get; private set; }  // AI Play 확인에서 정답·오답을 넣어 볼 때 사용

    Action onFinished;
    string input = "";
    float flashTimer;        // 오답 때 화면을 잠깐 붉게
    string feedback = "";
    GUIStyle bigStyle, midStyle, boxStyle;

    void Awake()
    {
        if (beepSource == null)
        {
            beepSource = gameObject.AddComponent<AudioSource>();
            beepSource.playOnAwake = false;
            beepSource.spatialBlend = 0f;   // 2D: 어디서나 같은 크기
        }
        if (beepClip == null) beepClip = MakeBeep(880f, 0.35f);
    }

    // 880Hz 사인파를 코드로 만든다 → 외부 음원 파일·라이선스가 필요 없다.
    static AudioClip MakeBeep(float freq, float seconds)
    {
        const int rate = 44100;
        int n = Mathf.RoundToInt(rate * seconds);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Min(1f, Mathf.Min(i, n - i) / (rate * 0.01f));   // 앞뒤 10ms 페이드(딸깍 소리 방지)
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * env;
        }
        AudioClip clip = AudioClip.Create("Beep", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void Begin(Action finished)
    {
        onFinished = finished;
        Correct = Wrong = Timeouts = 0;
        TimeLeft = duration;
        feedback = "";
        Running = true;
        NextQuestion();
    }

    void NextQuestion()
    {
        int a = UnityEngine.Random.Range(10, 100);
        int b = UnityEngine.Random.Range(10, 100);
        bool plus = UnityEngine.Random.value < 0.5f;
        if (!plus && b > a) { int tmp = a; a = b; b = tmp; }   // 뺄셈은 큰 수 - 작은 수(음수 입력 없음)
        Question = plus ? a + " + " + b : a + " - " + b;
        CurrentAnswer = plus ? a + b : a - b;
        QuestionTimeLeft = timePerQuestion;
        input = "";
    }

    // 키보드 입력(Enter)이나 AI 테스트가 부른다.
    public void Submit(int answer)
    {
        if (!Running) return;
        if (answer == CurrentAnswer) { Correct++; feedback = "정답"; }
        else { Wrong++; feedback = "오답!"; Warn(); }
        NextQuestion();
    }

    void Warn()
    {
        beepSource.PlayOneShot(beepClip);
        flashTimer = 0.4f;
    }

    void Update()
    {
        if (!Running) return;
        // Time.deltaTime 사용 → Time.timeScale로 테스트 가속이 된다.
        TimeLeft -= Time.deltaTime;
        QuestionTimeLeft -= Time.deltaTime;
        flashTimer -= Time.unscaledDeltaTime;

        if (TimeLeft <= 0f)
        {
            Running = false;
            onFinished?.Invoke();
            return;
        }
        if (QuestionTimeLeft <= 0f)
        {
            Wrong++; Timeouts++;
            feedback = "시간 초과!";
            Warn();
            NextQuestion();
        }
    }

    void OnGUI()
    {
        if (!Running) return;
        float s = Screen.height / 1080f;
        bigStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        midStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        boxStyle ??= new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter };
        bigStyle.fontSize = Mathf.RoundToInt(110 * s);
        midStyle.fontSize = Mathf.RoundToInt(40 * s);
        boxStyle.fontSize = Mathf.RoundToInt(70 * s);

        // 배경을 가려 두 조건의 과제 화면을 똑같이 만든다(숲 화면이 과제에 섞이지 않게).
        GUI.color = flashTimer > 0f ? new Color(0.55f, 0.05f, 0.05f) : new Color(0.08f, 0.08f, 0.1f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = Screen.width, h = Screen.height;
        GUI.Label(new Rect(0, 40 * s, w, 60 * s), "남은 시간 " + ExperimentFlow.FormatTime(TimeLeft) +
                  "      이 문제 " + Mathf.CeilToInt(Mathf.Max(0f, QuestionTimeLeft)) + "초", midStyle);
        GUI.Label(new Rect(0, h * 0.28f, w, 160 * s), Question + " = ?", bigStyle);

        // Enter는 TextField가 먹기 전에 먼저 처리한다.
        Event e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            if (int.TryParse(input, out int v)) Submit(v);
            e.Use();
        }
        GUI.SetNextControlName("answer");
        input = GUI.TextField(new Rect(w / 2 - 200 * s, h * 0.52f, 400 * s, 110 * s), input, 4, boxStyle);
        input = System.Text.RegularExpressions.Regex.Replace(input, "[^0-9]", "");
        GUI.FocusControl("answer");

        GUI.Label(new Rect(0, h * 0.68f, w, 60 * s), feedback, midStyle);
        GUI.Label(new Rect(0, h * 0.78f, w, 60 * s), "답을 입력하고 Enter", midStyle);
    }
}
