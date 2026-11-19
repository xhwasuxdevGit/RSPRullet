// 로그와 화면에 쓰는 한글 이름. 같은 이름 변환을 여러 클래스에 두지 않도록 한 곳에 모은다
public static class DisplayNames
{
    public static string GetShapeName(HandShape shape)
    {
        switch (shape)
        {
            case HandShape.Scissors:
                return "가위";
            case HandShape.Rock:
                return "바위";
            default:
                return "보";
        }
    }

    public static string GetItemName(ItemType item)
    {
        switch (item)
        {
            case ItemType.Coffee:
                return "커피";
            case ItemType.Magnifier:
                return "돋보기";
            default:
                return "시계";
        }
    }

    public static string GetSideName(Side side)
    {
        return side == Side.Player ? "플레이어" : "딜러";
    }

    // 한 카드 셔플은 두 판이다 (matchIndex 0, 1). 판정 기록과 상단 정보 UI가 함께 쓴다
    public static string GetMatchName(int matchIndex)
    {
        return matchIndex == 0 ? "첫 번째 판" : "두 번째 판";
    }
}
