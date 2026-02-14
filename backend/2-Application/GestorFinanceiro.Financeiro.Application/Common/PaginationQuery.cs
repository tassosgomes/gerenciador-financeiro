namespace GestorFinanceiro.Financeiro.Application.Common;

public record PaginationQuery
{
    public const int MaxSize = 100;

    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;

    public int GetClampedSize()
    {
        return Math.Clamp(Size, 1, MaxSize);
    }
}
