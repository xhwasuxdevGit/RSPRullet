using TMPro;
using UnityEngine;

// 테이블 가운데 왼쪽 카드풀 정보: "n장 → n장"과 그 아래 가위·바위·보 개수
// (UI·UX 화면 흐름 세부 기획서 2-2장 카드풀 영역, 5-4장 카드풀).
// 개수는 이번 카드 셔플의 카드풀 구성이라 카드를 뽑아도 줄지 않는다.
// 손 모양 칸은 Scene에 카드 앞면과 같은 손 모양 이미지(가위·바위·보 순서)로 고정해 두고, 오른쪽 정렬한 개수 칸만 바꾼다.
// 진행 흐름(GameFlow)에 변경 이벤트가 없어 매 프레임 상태를 읽고, 값이 바뀌었을 때만 문구를 다시 만든다
public class CardPoolInfoUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private TMP_Text _deckCountText;
    [SerializeField] private TMP_Text _shapeCountsText;

    private GameFlow _flow;

    // 마지막으로 화면에 적용한 값. 처음에는 -1이라 첫 Update에서 반드시 적용된다
    private int _shownStartCount = -1;
    private int _shownScissors = -1;
    private int _shownRock = -1;
    private int _shownPaper = -1;

    private void Awake()
    {
        _flow = _runner.Flow;
    }

    private void Update()
    {
        int startCount = _flow.MatchStartDeckCount;
        int scissors = _flow.GetPoolCardCount(HandShape.Scissors);
        int rock = _flow.GetPoolCardCount(HandShape.Rock);
        int paper = _flow.GetPoolCardCount(HandShape.Paper);

        if (startCount == _shownStartCount && scissors == _shownScissors && rock == _shownRock && paper == _shownPaper)
        {
            return;
        }

        _shownStartCount = startCount;
        _shownScissors = scissors;
        _shownRock = rock;
        _shownPaper = paper;

        _deckCountText.text = $"{startCount}장 → {_flow.MatchEndDeckCount}장";
        _shapeCountsText.text = $"{scissors}개\n{rock}개\n{paper}개";
    }
}
