namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Ponto único para obter o comparador de cada critério de ordenação,
/// já ajustado ao sentido (crescente ou decrescente).
/// </summary>
public static class ComparerFactory
{
  /// <summary>Abordagem 1D: uma única propriedade.</summary>
  public static IComparer<ItemImagem> PorPropriedade(Propriedade propriedade, Ordem ordem = Ordem.Crescente) =>
      AplicarOrdem(new PropriedadeComparer(propriedade), ordem);

  /// <summary>Abordagem lexicográfica: faixa de tonalidade e, dentro dela, a propriedade de desempate.</summary>
  public static IComparer<ItemImagem> PorFaixaDeTonalidade(
      double larguraFaixaGraus,
      Propriedade desempate = Propriedade.Luminosidade,
      Ordem ordem = Ordem.Crescente) =>
      AplicarOrdem(new FaixaTonalidadeComparer(larguraFaixaGraus, desempate), ordem);

  /// <summary>Abordagem CBIR: distância à imagem de referência (crescente = mais parecida primeiro).</summary>
  public static IComparer<ItemImagem> PorDistancia(
      ItemImagem referencia,
      PesosDistancia? pesos = null,
      Ordem ordem = Ordem.Crescente) =>
      AplicarOrdem(new DistanciaReferenciaComparer(referencia, pesos), ordem);

  private static IComparer<ItemImagem> AplicarOrdem(IComparer<ItemImagem> comparador, Ordem ordem) => ordem switch
  {
    Ordem.Crescente => comparador,
    Ordem.Decrescente => new InvertidoComparer<ItemImagem>(comparador),
    _ => throw new ArgumentOutOfRangeException(nameof(ordem), ordem, "Ordem desconhecida.")
  };
}
