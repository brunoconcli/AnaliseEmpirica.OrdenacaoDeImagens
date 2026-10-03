namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Base dos comparadores de <see cref="ItemImagem"/>: trata referências nulas
/// de forma uniforme e deixa para as subclasses apenas a regra de negócio.
/// </summary>
/// <remarks>
/// Toda subclasse deve definir uma ordem total (transitiva e antissimétrica)
/// e nunca depender de valores <c>NaN</c>. Ver docs/estudo-propps.md.
/// </remarks>
public abstract class ItemImagemComparer : IComparer<ItemImagem>
{
  public int Compare(ItemImagem? x, ItemImagem? y)
  {
    if (ReferenceEquals(x, y))
    {
      return 0;
    }

    if (x is null)
    {
      return -1;
    }

    if (y is null)
    {
      return 1;
    }

    return CompararItens(x, y);
  }

  protected abstract int CompararItens(ItemImagem x, ItemImagem y);
}
