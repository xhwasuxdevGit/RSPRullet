using System;
using UnityEngine;

// 진행 단계 관리 (코어 규칙 상세 기획서 2장, 7-2장 F3).
// 모든 단계 전환을 이 클래스 한 곳에서 처리한다.
// 각 Complete 메서드는 "지금 단계가 끝났다"는 신호이며, 맞지 않는 단계에서 들어오면 무시하고 false를 돌려준다.
// 판정 결과나 체력 같은 규칙 계산은 하지 않고, 호출하는 쪽이 넘겨준 결과로 다음 단계만 정한다.
public class PhaseMachine
{
    public GamePhase CurrentPhase { get; private set; } = GamePhase.RoundStart;

    // 이번 카드 셔플에서 몇 번째 판인지 (0: 첫 번째 판, 1: 두 번째 판)
    public int MatchIndex { get; private set; }

    // 단계가 바뀔 때 (이전 단계, 새 단계)를 알린다
    public event Action<GamePhase, GamePhase> PhaseChanged;

    public bool CompleteRoundStart()
    {
        return TryMove(GamePhase.RoundStart, GamePhase.CardShuffle);
    }

    public bool CompleteCardShuffle()
    {
        return TryMove(GamePhase.CardShuffle, GamePhase.StartCardDeal);
    }

    // 시계 효과의 이월이 있으면 선 정하기를 건너뛰고 바로 첫 번째 판으로 간다 (예외 E2, 시계는 S3에서 연결)
    public bool CompleteStartCardDeal(bool hasClockCarryOver)
    {
        if (CurrentPhase != GamePhase.StartCardDeal)
        {
            return Ignore(nameof(CompleteStartCardDeal));
        }

        if (hasClockCarryOver)
        {
            StartFirstMatch();
            return true;
        }

        Move(GamePhase.DecideFirst);
        return true;
    }

    // 선 정하기에서 비기면 카드 셔플로 돌아간다 (예외 E1)
    public bool CompleteDecideFirst(bool isDraw)
    {
        if (CurrentPhase != GamePhase.DecideFirst)
        {
            return Ignore(nameof(CompleteDecideFirst));
        }

        if (isDraw)
        {
            Move(GamePhase.CardShuffle);
            return true;
        }

        StartFirstMatch();
        return true;
    }

    public bool CompleteDraw()
    {
        return TryMove(GamePhase.Draw, GamePhase.Distribute);
    }

    public bool CompleteDistribute()
    {
        return TryMove(GamePhase.Distribute, GamePhase.ItemTurnPlayer);
    }

    // "아이템 사용 완료" 입력. 턴 플레이어 → 상대 순서로 한 번씩 받는다
    public bool CompleteItemUse()
    {
        if (CurrentPhase == GamePhase.ItemTurnPlayer)
        {
            Move(GamePhase.ItemOpponent);
            return true;
        }

        return TryMove(GamePhase.ItemOpponent, GamePhase.Reveal);
    }

    public bool CompleteReveal()
    {
        return TryMove(GamePhase.Reveal, GamePhase.Judge);
    }

    public bool CompleteJudge()
    {
        return TryMove(GamePhase.Judge, GamePhase.ShowResult);
    }

    // 판 결과 표시가 끝난 뒤: 체력 0이 있으면 남은 판과 정산 없이 라운드 종료 (예외 E5),
    // 없으면 첫 번째 판 뒤에는 두 번째 판의 뽑기로, 두 번째 판 뒤에는 정산으로 간다
    public bool CompleteShowResult(bool someoneAtZeroHealth)
    {
        if (CurrentPhase != GamePhase.ShowResult)
        {
            return Ignore(nameof(CompleteShowResult));
        }

        if (someoneAtZeroHealth)
        {
            Move(GamePhase.RoundEnd);
            return true;
        }

        if (MatchIndex < GameRules.MatchesPerShuffle - 1)
        {
            MatchIndex++;
            Move(GamePhase.Draw);
            return true;
        }

        Move(GamePhase.Settlement);
        return true;
    }

    // 정산 결과 한쪽만 체력 0이면 라운드 종료, 그 외(둘 다 0 → 1로 되돌린 경우 포함)는 다음 카드 셔플
    public bool CompleteSettlement(bool isRoundOver)
    {
        if (CurrentPhase != GamePhase.Settlement)
        {
            return Ignore(nameof(CompleteSettlement));
        }

        Move(isRoundOver ? GamePhase.RoundEnd : GamePhase.CardShuffle);
        return true;
    }

    private void StartFirstMatch()
    {
        MatchIndex = 0;
        Move(GamePhase.Draw);
    }

    private bool TryMove(GamePhase expected, GamePhase next)
    {
        if (CurrentPhase != expected)
        {
            return Ignore($"{expected} 완료");
        }

        Move(next);
        return true;
    }

    private void Move(GamePhase next)
    {
        GamePhase previous = CurrentPhase;
        CurrentPhase = next;
        Debug.Log($"[PhaseMachine] {previous} → {next} (판 {MatchIndex + 1})");
        PhaseChanged?.Invoke(previous, next);
    }

    private bool Ignore(string input)
    {
        Debug.Log($"[PhaseMachine] 현재 단계 {CurrentPhase}에 맞지 않는 입력 무시: {input}");
        return false;
    }
}
