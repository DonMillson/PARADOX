namespace Paradox.Settings;

/// <summary>
/// Deterministic 15-card meeting pages for experimental 16–50 player lobbies.
/// This class contains no Unity state so page boundaries can be unit-tested.
/// </summary>
public static class LargeLobbyPagination
{
    public const int CardsPerPage = 15;

    public static int PageCount(int playerCount) =>
        playerCount <= 0 ? 0 : (playerCount - 1) / CardsPerPage + 1;

    public static int ClampPage(int requestedPage, int playerCount) =>
        Math.Clamp(requestedPage, 0, Math.Max(0, PageCount(playerCount) - 1));

    public static int CyclePage(int page, int delta, int playerCount)
    {
        var pages = PageCount(playerCount);
        return pages == 0 ? 0 : ((page + delta) % pages + pages) % pages;
    }

    public static bool IsOnPage(int cardIndex, int page, int playerCount) =>
        cardIndex >= 0 && cardIndex < playerCount &&
        cardIndex / CardsPerPage == ClampPage(page, playerCount);

    public static (int Row, int Column) PositionWithinPage(int cardIndex) =>
        ((cardIndex % CardsPerPage) / 3, cardIndex % 3);
}
