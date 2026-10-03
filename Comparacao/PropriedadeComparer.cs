namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Abordagem 1D: compara duas imagens por uma única propriedade escalar, em ordem crescente.
/// </summary>
/// <remarks>
/// Imagens acromáticas têm <see cref="ItemImagem.Tonalidade"/> igual a −1 e, por isso,
/// ficam naturalmente antes de todas as cromáticas ao ordenar por Tonalidade.
/// </remarks>
public sealed class PropriedadeComparer : ItemImagemComparer
{
  private readonly Func<ItemImagem, double> _seletor;

  public PropriedadeComparer(Propriedade propriedade)
  {
    Propriedade = propriedade;

    // O seletor é escolhido uma única vez, para que cada comparação custe apenas
    // a leitura direta do campo, sem o switch de ItemImagem.ObterValor.
    _seletor = propriedade switch
    {
      Propriedade.Luminosidade => item => item.Luminosidade,
      Propriedade.Tonalidade => item => item.Tonalidade,
      Propriedade.Saturacao => item => item.Saturacao,
      Propriedade.Complexidade => item => item.Complexidade,
      Propriedade.Entropia => item => item.Entropia,
      _ => throw new ArgumentOutOfRangeException(nameof(propriedade), propriedade, "Propriedade desconhecida.")
    };
  }

  public Propriedade Propriedade { get; }

  protected override int CompararItens(ItemImagem x, ItemImagem y) =>
      _seletor(x).CompareTo(_seletor(y));

  public override string ToString() => Propriedade.ToString();
}
