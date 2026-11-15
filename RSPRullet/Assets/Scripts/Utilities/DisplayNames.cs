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

    public static string GetSideName(Side side)
    {
        return side == Side.Player ? "플레이어" : "딜러";
    }
}
