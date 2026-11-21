// 라운드를 넘어 이어지는 진행 상태 (라운드와 게임 진행 세부 기획서 4-2장 원칙).
// 나머지 값은 라운드마다 GameFlow를 새로 만들어 라운드 시작 상태로 돌아가고, 이 클래스는 몇 번째 라운드인지와 응급약 복용 횟수만 이어 간다.
public class RoundProgress
{
    // 라운드 수는 라운드 설정 에셋 3개와 게임 클리어 조건이 전제로 하는 고정 규칙이다 (라운드와 게임 진행 세부 기획서 8-1-2장)
    public const int RoundCount = 3;

    // 새 게임은 1라운드부터 시작한다 (라운드와 게임 진행 세부 기획서 8-2장 R-F1 새 게임)
    public int RoundNumber { get; private set; } = 1;

    // 한 게임 동안 응급약을 복용한 총 횟수. 새 게임을 시작할 때만 0으로 돌린다 (라운드와 게임 진행 세부 기획서 5장 응급약 복용)
    public int MedicineCount { get; private set; }

    // 마지막 라운드는 이겨도 다음 라운드가 없다 (라운드와 게임 진행 세부 기획서 3장 게임 진행 흐름)
    public bool IsFinalRound => RoundNumber == RoundCount;

    // 라운드 번호 +1 (라운드와 게임 진행 세부 기획서 8-2장 R-F5 다음 라운드). 마지막 라운드에서는 넘어가지 않는다
    public bool AdvanceRound()
    {
        if (IsFinalRound)
        {
            return false;
        }

        RoundNumber++;
        return true;
    }

    // 응급약 복용 횟수 +1, 라운드 번호는 그대로 (라운드와 게임 진행 세부 기획서 8-2장 R-F4 응급약 복용).
    // 마지막 라운드에는 응급약 복용이 없다 (라운드와 게임 진행 세부 기획서 8-1-2장)
    public bool TakeMedicine()
    {
        if (IsFinalRound)
        {
            return false;
        }

        MedicineCount++;
        return true;
    }

    // 1라운드, 응급약 복용 0회로 돌린다 (라운드와 게임 진행 세부 기획서 8-2장 R-F1 새 게임).
    // 언제 새 게임을 시작할 수 있는지는 부르는 쪽(GameFlowRunner)이 확인한다
    public void StartNewGame()
    {
        RoundNumber = 1;
        MedicineCount = 0;
    }
}
