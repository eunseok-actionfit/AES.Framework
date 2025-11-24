namespace AES.Tools
{
    public enum ContextLookupMode
    {
        Nearest,        // 기존처럼 가장 가까운 상위 DataContext
        ByNameInParents,// 부모 계층(DataContextBase들)에서 이름으로 검색
        ByNameInScene,   // 씬 전체에서 이름으로 검색
    }
}