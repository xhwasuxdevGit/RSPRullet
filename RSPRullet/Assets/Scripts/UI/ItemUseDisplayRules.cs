// 이번 판에 쓴 아이템의 사용 표시를 지금 진행 단계에서 보여 줄지 정한다
// (UI·UX 화면 흐름 세부 기획서 5-3장 아이템 사용 표시, 5-2장 시계 사용 표시).
// 사용 기록(IItemView.ItemUses)은 다음 판을 시작할 때 비워지지만, 그 직전에 단계 변경 알림이 먼저 오므로
// 화면이 사용 기록만 보면 지난 판 표시가 남는다. 그래서 효과가 사라지는 시점을 진행 단계로 정한다.
// 화면과 분리한 순수 규칙이라 EditMode 테스트로 확인한다.
public static class ItemUseDisplayRules
{
    public static bool IsShown(ItemType item, GamePhase phase)
    {
        switch (phase)
        {
            // 아이템 단계부터 판정까지는 이번 판에 쓴 아이템을 모두 보여 준다
            case GamePhase.ItemTurnPlayer:
            case GamePhase.ItemOpponent:
            case GamePhase.Reveal:
            case GamePhase.Judge:
                return true;

            // 판 결과를 보여 주는 동안: 돋보기는 본 카드가 판정에 쓰였으므로 지우고,
            // 커피는 판 결과 표시가 끝날 때, 시계는 다음 판이 시작될 때 지운다
            case GamePhase.ShowResult:
                return item != ItemType.Magnifier;

            // 두 번째 판에 쓴 시계는 다음 카드 셔플의 첫 번째 판이 시작될 때까지 남긴다
            case GamePhase.Settlement:
            case GamePhase.SettlementWait:
                return item == ItemType.Clock;

            // 뽑기·분배는 새 판이라 이번 판에 쓴 아이템이 아직 없다. 지난 판 기록이 남아 있어도 보여 주지 않는다
            default:
                return false;
        }
    }
}
