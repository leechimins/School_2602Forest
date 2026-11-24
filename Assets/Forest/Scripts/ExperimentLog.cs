using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// 실험 결과를 CSV 한 줄씩 덧붙여(append) 저장한다.
// 위치: Application.persistentDataPath (Windows: C:\Users\<사용자>\AppData\LocalLow\<회사>\<제품>\)
// → 참여자 데이터는 공개 저장소(repo) 밖에 쌓인다.
public static class ExperimentLog
{
    public const string FileName = "forest_experiment.csv";
    const string Header = "참여자,조건,순서,암산정답수,오답수,산책이동거리";

    // GetFullPath: Windows에서 '/'와 '\'가 섞여 보이지 않게 정리
    public static string FilePath => Path.GetFullPath(Path.Combine(Application.persistentDataPath, FileName));

    // order: 그 참여자가 몇 번째로 한 조건인지(1 또는 2)
    public static void Append(int participant, char condition, int order, int correct, int wrong, float walkDistance)
    {
        string path = FilePath;
        var sb = new StringBuilder();
        if (!File.Exists(path)) sb.AppendLine(Header);   // 헤더는 파일을 처음 만들 때 한 번만
        sb.AppendLine(string.Join(",",
            participant.ToString(CultureInfo.InvariantCulture),
            condition.ToString(),
            order.ToString(CultureInfo.InvariantCulture),
            correct.ToString(CultureInfo.InvariantCulture),
            wrong.ToString(CultureInfo.InvariantCulture),
            walkDistance.ToString("F1", CultureInfo.InvariantCulture)));   // 소수점은 항상 '.'

        // UTF-8 BOM: 엑셀에서 한글 헤더가 깨지지 않게. 이어 쓸 때는 BOM이 다시 붙지 않는다.
        File.AppendAllText(path, sb.ToString(), new UTF8Encoding(true));
        Debug.Log("ExperimentLog: " + path + " ← " + sb.ToString().TrimEnd());
    }
}
