namespace AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Uma imagem já reduzida às suas propriedades visuais escalares.
/// </summary>
/// <remarks>
/// Imutável: as propriedades são calculadas uma única vez, na fase de extração.
/// O critério de ordenação não fica guardado aqui; ele é definido pelo comparador
/// entregue ao algoritmo (ver pasta Comparacao).
/// </remarks>
public sealed record ItemImagem(
    string CaminhoArquivo,
    double Luminosidade,
    double Tonalidade,
    double Saturacao,
    double Complexidade,
    double Entropia)
{
  /// <summary>Valor de <see cref="Tonalidade"/> atribuído a imagens sem cor predominante.</summary>
  public const double TonalidadeAcromatica = -1;

  public bool IsAcromatica => Tonalidade == TonalidadeAcromatica;

  public double ObterValor(Propriedade propriedade) => propriedade switch
  {
    Propriedade.Luminosidade => Luminosidade,
    Propriedade.Tonalidade => Tonalidade,
    Propriedade.Saturacao => Saturacao,
    Propriedade.Complexidade => Complexidade,
    Propriedade.Entropia => Entropia,
    _ => throw new ArgumentOutOfRangeException(nameof(propriedade), propriedade, "Propriedade desconhecida.")
  };
}
